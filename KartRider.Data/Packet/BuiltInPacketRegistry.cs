using KartRider.Logging;
using System;
using System.Linq;

namespace KartRider.Packet
{
    /// <summary>
    /// 发现并注册所有内置数据包处理器。
    /// 注册方式与 Mod 相同（IPacketHandler + PacketDispatcher），
    /// 但内置处理器优先级低于 Mod，Mod 仍可优先拦截。
    /// </summary>
    public static class BuiltInPacketRegistry
    {
        private static bool _registered;

        public static void RegisterAll()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;

            var handlerTypes = typeof(BuiltInPacketRegistry).Assembly
                .GetTypes()
                .Where(t => typeof(BuiltInPacketHandler).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .OrderBy(t => t.Name, StringComparer.Ordinal);

            int count = 0;
            foreach (var type in handlerTypes)
            {
                PacketDispatcher.RegisterHandler((IPacketHandler)Activator.CreateInstance(type));
                count++;
            }

            Log.Info($"内置数据包处理器已注册: {count} 个");
        }
    }
}
