using System;
using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;

namespace KartRider;

public static class ModPatchService
{
    private static readonly Harmony _harmony = new Harmony("com.launcher.modpatch");
    private static readonly ConcurrentDictionary<IntPtr, Func<bool>> _prefixHandlers = new();

    public static void AddPrefix(MethodBase original, Func<bool> handler)
    {
        _prefixHandlers[original.MethodHandle.Value] = handler;
        var prefix = new HarmonyMethod(typeof(ModPatchService), nameof(PrefixDispatcher));
        _harmony.Patch(original, prefix);
    }

    public static void RemovePrefix(MethodBase original)
    {
        _prefixHandlers.TryRemove(original.MethodHandle.Value, out _);
        _harmony.Unpatch(original, HarmonyPatchType.Prefix, "com.launcher.modpatch");
    }

    private static bool PrefixDispatcher(MethodBase __originalMethod)
    {
        if (_prefixHandlers.TryGetValue(__originalMethod.MethodHandle.Value, out var handler))
            return handler();
        return true;
    }
}