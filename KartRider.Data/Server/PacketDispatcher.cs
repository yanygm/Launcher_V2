using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using System;
using System.Collections.Generic;
using System.Threading;

namespace KartRider
{
    public static class PacketDispatcher
    {
        private static readonly Dictionary<PacketName, List<IPacketHandler>> _handlerMap = new();
        private static readonly ReaderWriterLockSlim _lock = new();

        public static void RegisterHandler(IPacketHandler handler)
        {
            if (handler.TargetPackets == null || handler.TargetPackets.Count == 0)
                return;

            _lock.EnterWriteLock();
            try
            {
                foreach (var packetName in handler.TargetPackets)
                {
                    if (!_handlerMap.ContainsKey(packetName))
                        _handlerMap[packetName] = new List<IPacketHandler>();

                    var handlers = _handlerMap[packetName];
                    handlers.Add(handler);
                    handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public static bool UnregisterHandler(IPacketHandler handler)
        {
            if (handler.TargetPackets == null || handler.TargetPackets.Count == 0)
                return false;

            _lock.EnterWriteLock();
            try
            {
                bool removed = false;
                foreach (var packetName in handler.TargetPackets)
                {
                    if (_handlerMap.TryGetValue(packetName, out var handlers))
                    {
                        removed |= handlers.Remove(handler);
                        if (handlers.Count == 0)
                            _handlerMap.Remove(packetName);
                    }
                }
                return removed;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public static bool Dispatch(Type serverType, PacketName packetName, InPacket packet, byte[] rawData, object clientInfo, object server, bool throwOnHandlerError = false, bool builtInOnly = false)
        {
            var handlers = GetHandlers(packetName, builtInOnly);
            if (handlers == null)
                return false;

            var context = new PacketContext
            {
                ServerType = serverType,
                PacketName = packetName,
                Packet = new InPacket(packet.ToArray()) { Position = packet.Position },
                RawData = rawData,
                ClientInfo = clientInfo,
                Server = server
            };
            return DispatchHandlers(context, handlers, throwOnHandlerError);
        }

        public static bool Dispatch(PacketContext context, bool throwOnHandlerError = false, bool builtInOnly = false)
        {
            var handlers = GetHandlers(context.PacketName, builtInOnly);
            return handlers != null && DispatchHandlers(context, handlers, throwOnHandlerError);
        }

        /// <summary>
        /// 内置处理器与 Mod 分两阶段派发，避免 Mod 被重复调用：
        /// 默认只取 Mod；builtInOnly=true 时只取内置处理器。
        /// </summary>
        private static List<IPacketHandler> GetHandlers(PacketName packetName, bool builtInOnly)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_handlerMap.TryGetValue(packetName, out var list))
                    return null;

                var handlers = new List<IPacketHandler>(list.Count);
                foreach (var handler in list)
                {
                    if ((handler is Packet.BuiltInPacketHandler) == builtInOnly)
                        handlers.Add(handler);
                }
                return handlers.Count == 0 ? null : handlers;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private static bool DispatchHandlers(PacketContext context, List<IPacketHandler> handlers, bool throwOnHandlerError)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler.HandlePacket(context))
                    {
                        context.Handled = true;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    if (throwOnHandlerError)
                    {
                        throw;
                    }
                    Log.Debug($"[PacketDispatcher] Handler '{handler.Name}' 异常: {ex.Message}");
                }
            }

            return false;
        }

        public static void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _handlerMap.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
