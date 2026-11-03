using KartRider_PacketName;
using System.Collections.Generic;

namespace KartRider
{
    /// <summary>
    /// 数据包处理器。Mod 与内置处理器共用这套接口，通过 PacketDispatcher 注册。
    /// 内置处理器优先级为 int.MinValue，Mod 处理器总是先于内置处理器执行。
    /// </summary>
    public interface IPacketHandler
    {
        string Name { get; }

        int Priority { get; }

        HashSet<PacketName> TargetPackets { get;}

        bool HandlePacket(PacketContext context);
    }
}
