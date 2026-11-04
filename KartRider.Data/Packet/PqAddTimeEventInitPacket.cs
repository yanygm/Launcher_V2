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
                DateTime now = DateTime.Now;
                int dateCode = (now.Year % 100) * 10000 + now.Month * 100 + now.Day;

                outPacket.WriteInt(dateCode);
                outPacket.WriteDateTime(now);
                outPacket.WriteDateTime(now);
                outPacket.WriteString("累计在线活动");
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(dateCode);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteDateTime(now);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
