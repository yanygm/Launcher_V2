using System;
using KartRider.IO.Packet;
using KartRider_PacketName;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqAddTimeEventInitPacket"/> 数据包。</summary>
    /// <remarks>
    /// 限时活动列表：之前没有处理器，客户端拿不到应答。
    /// 这里返回空列表（数量 0），即「没有进行中的限时活动」。
    /// </remarks>
    internal sealed class PqAddTimeEventInitPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqAddTimeEventInitPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrAddTimeEventInitPacket"))
            {
                // P5136 的固定结构（时间用当前时间，等于没有进行中的限时活动）
                outPacket.WriteHexString("6D F8 03 00 E8 B3 18 15 EF B3 17 15");
                outPacket.WriteInt(6);
                outPacket.WriteHexString("2F 7D A1 8B 28 57 BF 7E 3B 6D A8 52");
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteHexString("6D F8 03 00");
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
