using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace KartRider;

public static class ModManager
{
    private class ModInfo
    {
        public IMod Instance { get; set; }
        public ModLoadContext LoadContext { get; set; }
        public string FilePath { get; set; }
        public bool IsPacketHandler { get; set; }
    }

    private static readonly List<ModInfo> ModList = new();
    private static string ModPath { get; set; } = string.Empty;

    public static event Action OnAllModLoaded;
    public static event Action OnAllModReloaded;
    public static event Action OnAllModUnloaded;
    public static event Action<string> OnModLoaded;
    public static event Action<string> OnModUnloaded;
    public static event Action<string> OnModReloading;
    public static event Action<string> OnModReloaded;

    public static void Initialize(string RootDirectory)
    {
        ModPath = Path.Combine(RootDirectory, "Mods");
        Console.WriteLine($"Mod加载路径: {ModPath}");

        if (!Directory.Exists(ModPath))
        {
            Directory.CreateDirectory(ModPath);
            Console.WriteLine($"Mod文件夹已创建: {ModPath}");
        }
        LoadMods(ModPath);
        Console.WriteLine($"Mod加载完成，共加载 {ModList.Count} 个 Mod。");

        try
        {
            OnAllModLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] OnAllModLoaded 事件处理程序抛出异常: {ex.Message}");
        }
    }

    public static void LoadMods(string modPath)
    {
        string[] modDllFiles = Directory.GetFiles(modPath, "*.dll");
        foreach (string file in modDllFiles)
        {
            try
            {
                LoadMod(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] 加载 {Path.GetFileName(file)} 时发生未预期错误: {ex.Message}，跳过");
            }
        }
    }

    public static bool LoadMod(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[错误] 文件不存在: {Path.GetFileName(filePath)}");
            return false;
        }

        if (ModList.Any(m => m.FilePath == filePath))
        {
            Console.WriteLine($"[警告] Mod 已加载: {Path.GetFileName(filePath)}");
            return false;
        }

        try
        {
            byte[] dllBytes = File.ReadAllBytes(filePath);
            
            ModLoadContext? tempAlc = null;
            bool isDependencyMod = false;
            Type? modType = null;
            IMod? tempMod = null;
            Assembly? tempAssembly = null;

            tempAlc = new ModLoadContext(filePath, isCollectible: true);
            tempAssembly = tempAlc.LoadFromStream(new MemoryStream(dllBytes));

            var modTypes = tempAssembly
                .GetTypes()
                .Where(t => typeof(IMod).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            if (!modTypes.Any())
            {
                tempAlc.Unload();
                return false;
            }

            modType = modTypes.First();
            tempMod = (IMod)Activator.CreateInstance(modType);
            isDependencyMod = tempMod.IsDependencyMod;

            ModLoadContext alc;
            Assembly assembly;
            IMod mod;

            if (isDependencyMod)
            {
                Console.WriteLine($"[ModManager] 检测到依赖 Mod: {tempMod.Name}");
                tempAlc.Unload();
                
                assembly = Assembly.LoadFrom(filePath);
                Type finalType = assembly.GetType(modType.FullName!);
                mod = (IMod)Activator.CreateInstance(finalType);
                
                alc = new ModLoadContext(filePath, isCollectible: false);
            }
            else
            {
                tempAlc.Unload();
                alc = new ModLoadContext(filePath, isCollectible: true);
                assembly = alc.LoadFromStream(new MemoryStream(dllBytes));
                Type finalType = assembly.GetType(modType.FullName!);
                mod = (IMod)Activator.CreateInstance(finalType);
                Console.WriteLine($"[ModManager] 检测到普通 Mod: {mod.Name}");
            }

            mod.OnInitialize();

            bool isPacketHandler = mod is IPacketHandler;
            if (isPacketHandler)
            {
                PacketDispatcher.RegisterHandler((IPacketHandler)mod);
                Console.WriteLine(
                    $">>> Mod [{mod.Name}] 已注册数据包拦截，监听 {(mod as IPacketHandler).TargetPackets.Count} 种数据包"
                );
            }

            ModList.Add(new ModInfo
            {
                Instance = mod,
                LoadContext = alc,
                FilePath = filePath,
                IsPacketHandler = isPacketHandler
            });

            // 所有 Mod 按名称自动注册到 ServiceRegistry
            // 依赖 Mod 也会注册，消费者统一通过 ServiceRegistry.Invoke 调用
            ServiceRegistry.Register(mod.Name, mod);

            Console.WriteLine(
                $">>> 成功加载 Mod: [{mod.Name}] 来自 {Path.GetFileName(filePath)}"
            );
            Console.WriteLine("Mod描述:" + mod.Description);
            Console.WriteLine("Mod版本:" + mod.Version);

            try
            {
                OnModLoaded?.Invoke(mod.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] OnModLoaded 事件处理程序抛出异常: {ex.Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 无法加载文件 {Path.GetFileName(filePath)}: {ex.Message}");
            return false;
        }
    }

    public static bool UnloadMod(string modName)
    {
        var modInfo = ModList.FirstOrDefault(m => m.Instance.Name == modName);
        if (modInfo == null)
        {
            Console.WriteLine($"[警告] 未找到 Mod: [{modName}]");
            return false;
        }

        if (modInfo.Instance.IsDependencyMod)
        {
            Console.WriteLine($"[警告] 依赖 Mod [{modName}] 不可卸载，跳过");
            return false;
        }

        try
        {
            modInfo.Instance.OnUninitialize();

            if (modInfo.IsPacketHandler && modInfo.Instance is IPacketHandler handler)
            {
                PacketDispatcher.UnregisterHandler(handler);
            }

            ModList.Remove(modInfo);

            // 从 ServiceRegistry 移除注册
            ServiceRegistry.Unregister(modInfo.Instance.Name);

            modInfo.LoadContext.Unload();

            modInfo.Instance = null;
            modInfo.LoadContext = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.WriteLine($">>> 成功卸载 Mod: [{modName}]");

            try
            {
                OnModUnloaded?.Invoke(modName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] OnModUnloaded 事件处理程序抛出异常: {ex.Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 卸载 Mod [{modName}] 失败: {ex.Message}");
            return false;
        }
    }

    public static bool ReloadMod(string modName)
    {
        var modInfo = ModList.FirstOrDefault(m => m.Instance.Name == modName);
        if (modInfo == null)
        {
            Console.WriteLine($"[警告] 未找到 Mod: [{modName}]");
            return false;
        }

        string filePath = modInfo.FilePath;

        try
        {
            OnModReloading?.Invoke(modName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] OnModReloading 事件处理程序抛出异常: {ex.Message}");
        }

        if (!UnloadMod(modName))
        {
            return false;
        }

        if (!LoadMod(filePath))
        {
            return false;
        }

        try
        {
            OnModReloaded?.Invoke(modName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] OnModReloaded 事件处理程序抛出异常: {ex.Message}");
        }

        return true;
    }

    public static void UnloadAllMods()
    {
        foreach (var modInfo in ModList.ToArray())
        {
            UnloadMod(modInfo.Instance.Name);
        }
        Console.WriteLine(">>> 所有 Mod 已卸载");

        // 清空 ServiceRegistry 中的 MethodInfo 缓存，
        // 避免下次 Invoke 时命中旧 Assembly 的缓存（抛 TargetException）
        ServiceRegistry.ClearMethodCache();

        try
        {
            OnAllModUnloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] OnAllModUnloaded 事件处理程序抛出异常: {ex.Message}");
        }
    }

    public static void ReloadAllMods()
    {
        var filePaths = ModList.Select(m => m.FilePath).ToList();
        UnloadAllMods();
        
        foreach (string filePath in filePaths)
        {
            LoadMod(filePath);
        }
        Console.WriteLine(">>> 所有 Mod 已重新加载");

        try
        {
            OnAllModReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] OnAllModReloaded 事件处理程序抛出异常: {ex.Message}");
        }
    }

    public static Func<object[], object> GetModAction(string modName, string methodName)
    {
        var modInfo = ModList.FirstOrDefault(m => m.Instance.Name == modName);
        if (modInfo == null)
            return null;

        var method = modInfo.Instance.GetType().GetMethod(methodName);
        if (method == null)
            return null;

        return parameters => method.Invoke(modInfo.Instance, parameters);
    }

    public static IMod GetMod(string modName) => ModList.FirstOrDefault(mod => mod.Instance.Name == modName)?.Instance;

    public static IEnumerable<IMod> GetAllMods() => ModList.Select(m => m.Instance);
}
