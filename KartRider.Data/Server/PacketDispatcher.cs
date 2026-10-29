using KartRider.IO.Packet;
using KartRider_PacketName;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static bool Dispatch(Type serverType, PacketName packetName, InPacket packet, byte[] rawData, object clientInfo, object server)
        {
            var context = new PacketContext
            {
                ServerType = serverType,
                PacketName = packetName,
                Packet = new InPacket(packet.ToArray()) { Position = packet.Position },
                RawData = rawData,
                ClientInfo = clientInfo,
                Server = server
            };
            return Dispatch(context);
        }

        public static bool Dispatch(PacketContext context)
        {
            List<IPacketHandler> handlers = null;

            _lock.EnterReadLock();
            try
            {
                if (_handlerMap.TryGetValue(context.PacketName, out var list))
                    handlers = list.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (handlers == null || handlers.Count == 0)
                return false;

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
                    Console.WriteLine($"[PacketDispatcher] Handler '{handler.Name}' 异常: {ex.Message}");
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
