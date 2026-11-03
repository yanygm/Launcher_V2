using KartRider.IO.Packet;
using KartRider_PacketName;
using System.Collections.Generic;

namespace KartRider.Packet
{
    /// <summary>
    /// 内置数据包处理器基类：与 Mod 共用 IPacketHandler / PacketDispatcher。
    /// Priority 固定为最低，保证 Mod 注册的处理器先执行，不改变 Mod 的拦截行为。
    /// </summary>
    internal abstract class BuiltInPacketHandler : IPacketHandler
    {
        private HashSet<PacketName> _targetPackets;

        public string Name => GetType().Name;

        public int Priority => int.MinValue;

        public abstract PacketName TargetPacket { get; }

        public HashSet<PacketName> TargetPackets => _targetPackets ??= new HashSet<PacketName> { TargetPacket };

        public bool HandlePacket(PacketContext context)
        {
            if (context.ServerType != typeof(ClientSession))
            {
                return false;
            }
            if (context.ClientInfo is not SessionGroup session)
            {
                return false;
            }
            return Handle(session, context.Packet);
        }

        protected abstract bool Handle(SessionGroup session, InPacket packet);
    }
}
