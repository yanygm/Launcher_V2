using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace KartRider;

public class ModLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ModLoadContext(string modPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(modPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            byte[] assemblyBytes = File.ReadAllBytes(path);
            return LoadFromStream(new MemoryStream(assemblyBytes));
        }
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}