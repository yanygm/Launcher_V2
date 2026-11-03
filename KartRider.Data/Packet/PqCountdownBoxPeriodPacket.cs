using KartRider.IO.Packet;
using KartRider_PacketName;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqCountdownBoxPeriodPacket"/> 数据包。</summary>
    /// <remarks>
    /// 客户端进入主页后会反复请求活动周期，之前没有任何处理器应答，
    /// 主页会一直等着活动数据（表现为卡住，再点一次主页按钮才刷新出来）。
    /// 这里返回空周期（开始/结束时间都是 0），即「当前没有倒计时箱子活动」。
    /// </remarks>
    internal sealed class PqCountdownBoxPeriodPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqCountdownBoxPeriodPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrCountdownBoxPeriodPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
