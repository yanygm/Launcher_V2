using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace KartRider;

public class ModLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ModLoadContext(string modPath, bool isCollectible = true) : base(isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(modPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var existing = Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        if (existing != null)
            return existing;

        string path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            return Assembly.LoadFrom(path);
        }

        try
        {
            string mainDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            if (!string.IsNullOrEmpty(mainDir))
            {
                string assemblyPath = Path.Combine(mainDir, assemblyName.Name + ".dll");
                if (File.Exists(assemblyPath))
                    return Assembly.LoadFrom(assemblyPath);
            }
        }
        catch { }

        try
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }
        catch { }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
