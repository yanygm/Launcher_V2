using System;
using System.Collections.Generic;
using System.Reflection;

namespace KartRider;

/// <summary>
/// 服务注册表，实现 Mod 间通过字符串名称解耦调用。
/// 
/// 设计原则：
/// - 按字符串名称注册，内部使用 WeakReference 避免阻挠 Assembly 卸载
/// - 消费方不引用提供方的 DLL，完全解耦
/// - 热加载时旧实例自动释放，新实例注册后立即生效
/// 
/// 使用方式：
///   // 提供方在 OnInitialize 中注册
///   ServiceRegistry.Register("DatabaseProvider", this);
///   
///   // 消费方获取实例（自行反射调用）
///   var db = ServiceRegistry.GetService("DatabaseProvider");
///   var method = db.GetType().GetMethod("Query");
///   var table = method.Invoke(db, new object[] { sql, parameters });
///   
///   // 或一步到位
///   var table = ServiceRegistry.Invoke("DatabaseProvider", "Query", sql, parameters);
/// </summary>
public static class ServiceRegistry
{
    // 按名称存储的弱引用字典
    private static readonly Dictionary<string, WeakReference> _services = new(StringComparer.OrdinalIgnoreCase);

    // 静态服务注册（存储类型全名 → Assembly 弱引用，不阻止 Mod 卸载）
    private static readonly Dictionary<string, WeakReference> _staticServices = new(StringComparer.OrdinalIgnoreCase);

    // 用于方法查找的缓存（MethodInfo 不持有实例引用，可以安全缓存）
    private static readonly Dictionary<string, MethodInfo> _methodCache = new();

    /// <summary>
    /// 注册服务实例（按名称，内部用弱引用）
    /// </summary>
    public static void Register(string name, object instance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("服务名称不能为空", nameof(name));
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        _services[name] = new WeakReference(instance);
    }

    /// <summary>
    /// 按名称获取服务实例（返回 object?，需要调用方自行转型）
    /// </summary>
    public static object? GetService(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_services.TryGetValue(name, out var wr))
        {
            var instance = wr.Target;
            if (instance != null)
                return instance;

            // 弱引用已失效，清理
            _services.Remove(name);
        }

        return null;
    }

    /// <summary>
    /// 一步调用：按名称获取服务实例并调用其方法。
    /// 内部缓存 MethodInfo 以减少反射开销。
    /// </summary>
    /// <param name="name">服务注册名</param>
    /// <param name="methodName">方法名</param>
    /// <param name="args">调用参数</param>
    /// <returns>方法返回值，若服务不存在返回 null</returns>
    public static object? Invoke(string name, string methodName, params object?[]? args)
    {
        var instance = GetService(name);
        if (instance == null)
            return null;

        var type = instance.GetType();
        // 使用 Module.ModuleVersionId（每次加载生成新 GUID）而非 type.GUID（固定），
        // 确保 reloadallmod 后不会命中旧 Assembly 的 MethodInfo 缓存（否则会抛 TargetException）
        var cacheKey = $"{type.Module.ModuleVersionId}:{methodName}";

        if (!_methodCache.TryGetValue(cacheKey, out var method))
        {
            method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                throw new MissingMethodException($"类型 {type.FullName} 中找不到方法 {methodName}");
            _methodCache[cacheKey] = method;
        }

        return method.Invoke(instance, args);
    }

    /// <summary>
    /// 移除注册（ModManager 卸载 Mod 时调用）
    /// </summary>
    public static void Unregister(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _services.Remove(name);
    }

    /// <summary>
    /// 清空方法缓存。ModManager 卸载全部 Mod 时调用，
    /// 避免下次 Invoke 时命中旧 Assembly 的 MethodInfo。
    /// </summary>
    public static void ClearMethodCache()
    {
        _methodCache.Clear();
    }

    /// <summary>
    /// 检查服务是否已注册且可用
    /// </summary>
    public static bool IsRegistered(string name)
    {
        return GetService(name) != null;
    }

    /// <summary>
    /// 注册静态服务（存储类型全名 → Assembly 弱引用，不阻止 Mod 卸载）
    /// </summary>
    /// <param name="type">要注册的类型</param>
    public static void RegisterStatic(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        _staticServices[type.FullName!] = new WeakReference(type.Assembly);
    }

    /// <summary>
    /// 移除静态服务注册
    /// </summary>
    public static void UnregisterStatic(string typeFullName)
    {
        if (!string.IsNullOrWhiteSpace(typeFullName))
            _staticServices.Remove(typeFullName);
    }

    /// <summary>
    /// 调用静态方法（按类型全名查找，优先使用注册时存储的 Assembly 弱引用，
    /// 不阻止 Mod 卸载，卸载后安全返回 null）
    /// </summary>
    /// <param name="typeFullName">类型全名（含命名空间）</param>
    /// <param name="methodName">方法名</param>
    /// <param name="args">调用参数</param>
    /// <returns>方法返回值，若类型或方法不存在返回 null</returns>
    public static object? InvokeStatic(string typeFullName, string methodName, params object?[]? args)
    {
        // 优先从注册的弱引用中查找 Assembly（支持自定义 ALC 中的类型）
        Assembly? assembly = null;
        if (_staticServices.TryGetValue(typeFullName, out var wr))
        {
            assembly = wr.Target as Assembly;
            if (assembly == null)
            {
                // 弱引用已失效（Assembly 已卸载），清理
                _staticServices.Remove(typeFullName);
            }
        }

        // 弱引用没找到，回退到全局扫描（兼容未注册的调用）
        Type? targetType = assembly?.GetType(typeFullName);
        if (targetType == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                targetType = asm.GetType(typeFullName);
                if (targetType != null)
                    break;
            }
        }

        if (targetType == null)
            return null;

        // ModuleVersionId 在 Assembly 重载后生成新 GUID，确保缓存自动失效
        var cacheKey = $"{targetType.Module.ModuleVersionId}:{methodName}";

        if (!_methodCache.TryGetValue(cacheKey, out var method))
        {
            method = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Console.WriteLine($"[警告] 类型 {typeFullName} 中找不到静态方法 {methodName}");
                return null;
            }
            else
                _methodCache[cacheKey] = method;
        }

        try
        {
            return method.Invoke(null, args);
        }
        catch (InvalidOperationException)
        {
            // Assembly 已卸载，清理缓存并返回 null
            _methodCache.Remove(cacheKey);
            return null;
        }
    }
}
