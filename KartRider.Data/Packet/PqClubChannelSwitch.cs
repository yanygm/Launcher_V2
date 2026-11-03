using System.Net;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqClubChannelSwitch"/> 数据包。</summary>
    /// <remarks>
    /// 客户端 IDA：PqClubChannelSwitch 继承普通换频道的信封
    ///   [i32 opaqueLen][opaque][u8 模式组][u16 首选频道] + u32 俱乐部相关编号
    /// 应答沿用 PrChannelSwitch（sub_14213CE60 解析）：
    ///   [u32 模式][u16 频道][u16 迁移令牌][u32 IP][u16 端口]
    /// 俱乐部界面用「模式=1 + 端点为 0.0.0.0:0」进入，且不重建连接。
    /// 之前频道号写死 13 时，客户端在本服频道表里找不到对应俱乐部频道，
    /// 会回退到单人游戏界面，所以这里改为从 channel.xml 里找俱乐部类频道，找不到才用默认值。
    /// </remarks>
    internal sealed class PqClubChannelSwitch : BuiltInPacketHandler
    {
        /// <summary>频道表里没有俱乐部频道时使用的编号（与原版抓包一致）。</summary>
        private const ushort DefaultClubChannelId = 13;

        public override PacketName TargetPacket => PacketName.PqClubChannelSwitch;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 信封：opaque 长度 + 内容 + 模式组 + 首选频道，末尾还有一个 u32
            int length = inPacket.Available >= 4 ? inPacket.ReadInt() : 0;
            if (length > 0 && length <= inPacket.Available)
            {
                inPacket.ReadBytes(length);
            }
            byte gameType = inPacket.Available >= 1 ? inPacket.ReadByte() : (byte)0;
            ushort preferred = inPacket.Available >= 2 ? inPacket.ReadUShort() : (ushort)0;
            uint clubArg = inPacket.Available >= 4 ? inPacket.ReadUInt() : 0u;

            ushort channelId = FindClubChannelId();
            using (OutPacket outPacket = new OutPacket("PrChannelSwitch"))
            {
                outPacket.WriteInt(1);                              // 模式 1 = 进入俱乐部界面
                outPacket.WriteUShort(channelId);
                outPacket.WriteUShort(0);                            // 迁移令牌
                outPacket.WriteEndPoint(IPAddress.Any, 0);           // 端点为 0：不重建连接
                session.Client.Send(outPacket);
            }
            Log.Info($"[Club] {session.Client.Nickname} 进入俱乐部: 模式组={gameType} 首选={preferred} 参数={clubArg} "
                     + $"→ 下发频道={channelId}");
            return true;
        }

        /// <summary>在 channel.xml 的频道表里找俱乐部频道，找不到就用默认编号。</summary>
        private static ushort FindClubChannelId()
        {
            foreach (var channel in GameSupport.Channels)
            {
                if (channel.Value.Name != null
                    && channel.Value.Name.IndexOf("club", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return channel.Key;
                }
            }
            return DefaultClubChannelId;
        }
    }
}
