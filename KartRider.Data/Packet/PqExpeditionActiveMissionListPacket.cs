using KartRider.IO.Packet;
using KartRider_PacketName;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqExpeditionActiveMissionListPacket"/> 数据包。</summary>
    /// <remarks>
    /// 必须回完整的活动/探险队数据：把计数写成 0（空列表）虽然不会再卡主页，
    /// 但客户端会把「没有任务」当成「本周探险已全部完成」，并因为周期时间为 0 显示出
    /// 「65532 天 5 小时 剩余」的乱码时间。当前主页卡顿的真正原因是缺少活动类处理器
    /// （PqCountdownBoxPeriodPacket / PqAddTimeEventInitPacket），已单独补齐。
    /// </remarks>
    internal sealed class PqExpeditionActiveMissionListPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqExpeditionActiveMissionListPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrExpeditionActiveMissionListPacket"))
            {
                outPacket.WriteHexString("00 00 00 00 03 00 00 00 00 00 01 F2 B4 C8 37 47 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 01 F2 B4 C8 37 24 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 09 00 01 F2 B4 C8 37 1D 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 04 00 00 00 F2 B4 9C 27 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 F2 B4 9C 27 02 B6 39 00 13 07 00 00 00 73 00 61 00 64 00 73 00 64 00 77 00 72 00 03 00 00 00 01 1E 00 F2 B4 9C 27 02 EC FC 49 7B 04 00 00 00 91 4E 77 6D 46 80 1D 52 02 00 00 00 02 0F 00 F2 B4 9C 27 02 65 1A 52 7D 05 00 00 00 89 91 84 76 05 6E CE 98 4A 55 02 00 00 00 01 14 00 03 00 00 00 00 00 00 03 00 00 00 00 07 00 00 F9 B4 18 15 00");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
