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

        OnAllModLoaded?.Invoke();
    }

    public static void LoadMods(string modPath)
    {
        string[] modDllFiles = Directory.GetFiles(modPath, "*.dll");
        foreach (string file in modDllFiles)
        {
            LoadMod(file);
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
            var alc = new ModLoadContext(filePath);
            byte[] dllBytes = File.ReadAllBytes(filePath);
            Assembly assembly = alc.LoadFromStream(new MemoryStream(dllBytes));

            var modTypes = assembly
                .GetTypes()
                .Where(t => typeof(IMod).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var type in modTypes)
            {
                IMod mod = (IMod)Activator.CreateInstance(type);
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

                Console.WriteLine(
                    $">>> 成功加载 Mod: [{mod.Name}] 来自 {Path.GetFileName(filePath)}"
                );
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

        try
        {
            modInfo.Instance.OnUninitialize();

            if (modInfo.IsPacketHandler && modInfo.Instance is IPacketHandler handler)
            {
                PacketDispatcher.UnregisterHandler(handler);
            }

            ModList.Remove(modInfo);
            modInfo.LoadContext.Unload();

            modInfo.Instance = null;
            modInfo.LoadContext = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.WriteLine($">>> 成功卸载 Mod: [{modName}]");
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

        if (!UnloadMod(modName))
        {
            return false;
        }

        return LoadMod(filePath);
    }

    public static void UnloadAllMods()
    {
        foreach (var modInfo in ModList.ToArray())
        {
            UnloadMod(modInfo.Instance.Name);
        }
        Console.WriteLine(">>> 所有 Mod 已卸载");
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