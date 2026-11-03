using KartRider.Logging;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Profile;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqInitClubInfoPacket"/> 数据包。</summary>
    /// <remarks>
    /// 应答结构按客户端 IDA 反编译逐字段对齐：
    ///   ClubInfo 序列化 sub_14044CAD0 + PrInitClubInfoPacket 序列化 sub_14044A3B0
    ///   顺序：u32、string、u16、u16、u8、u8、u32、string、u32、u32、u32、u16、u16、
    ///         string、u32、u32、u32、u32、u32、u32、u32，最后追加 4 字节 + 8 字节。
    /// 之前多写了一个 u8（应为 u16）并且缺了结尾 12 字节，客户端解析错位后俱乐部界面打不开，
    /// 会退回单人游戏界面。
    /// </remarks>
    internal sealed class PqInitClubInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqInitClubInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var config = ProfileService.GetProfileConfig(session.Client.Nickname);
            if (config?.Rider == null)
            {
                return true;
            }
            var rider = config.Rider;
            uint userNo = ClientManager.GetUserNO(session.Client.Nickname);

            using (OutPacket outPacket = new OutPacket("PrInitClubInfoPacket"))
            {
                outPacket.WriteInt(rider.ClubCode);          // +16 俱乐部编号
                outPacket.WriteString(rider.ClubName);       // +24 俱乐部名
                outPacket.WriteDateTime(System.DateTime.Now);// +32 时间（u16 天 + u16 刻）
                outPacket.WriteByte(5);                      // +36 俱乐部等级
                outPacket.WriteByte(1);                      // +37 标志
                outPacket.WriteUInt(userNo);                 // +40 玩家编号
                outPacket.WriteString(session.Client.Nickname); // +48 昵称
                outPacket.WriteInt(500);                     // +56 最大成员数
                outPacket.WriteInt(0);                       // +60
                outPacket.WriteInt(1000);                    // +64
                outPacket.WriteShort(4);                     // +70 低 16 位
                outPacket.WriteShort(-1);                // +70 高 16 位
                outPacket.WriteString(rider.ClubIntro);      // +80 俱乐部介绍
                outPacket.WriteInt(rider.ClubMark_LOGO);     // +88 队标
                outPacket.WriteInt(rider.ClubMark_LINE);     // +92 队标底纹
                outPacket.WriteInt(3000000);                 // +96 俱乐部活跃度
                outPacket.WriteInt(1);                       // +100 附加数据标志（非 0 才会写后面 3 项）
                outPacket.WriteInt(3000000);                 // +104 周活跃度
                outPacket.WriteInt(3000000);                 // +108
                outPacket.WriteInt(0);                       // +112
                outPacket.WriteInt(0);                       // +144 追加 4 字节
                outPacket.WriteLong(0);                      // +152 追加 8 字节
                session.Client.Send(outPacket);
            }
            Log.Info($"[Club] {session.Client.Nickname} 下发俱乐部信息: code={rider.ClubCode} name={rider.ClubName} "
                     + $"logo={rider.ClubMark_LOGO} line={rider.ClubMark_LINE}");
            return true;
        }
    }
}
