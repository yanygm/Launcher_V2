using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using RiderData;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqRewardProEmblemPacket"/> 数据包。</summary>
    /// <remarks>
    /// PRO 驾照全部关卡通过后领取徽章（请求体是 u16 徽章编号）。
    /// 应答按客户端读取顺序写两个字节：下一对 PRO 的计时赛关卡、对决关卡。
    /// </remarks>
    internal sealed class PqRewardProEmblemPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqRewardProEmblemPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            ushort reportedEmblem = inPacket.Available >= 2 ? inPacket.ReadUShort() : (ushort)0;
            RiderSchool.RecordProEmblem(session, reportedEmblem);

            byte speedCleared = RiderSchool.GetNextProPairStep(session, true);
            byte versusCleared = RiderSchool.GetNextProPairStep(session, false);
            using (OutPacket outPacket = new OutPacket("PrRewardProEmblemPacket"))
            {
                outPacket.WriteByte(speedCleared);
                outPacket.WriteByte(versusCleared);
                session.Client.Send(outPacket);
            }
            Log.Info($"[RiderSchool] {session.Client.Nickname} 领取 PRO 徽章 请求={reportedEmblem}，本轮 计时赛{speedCleared} 对决{versusCleared}");
            return true;
        }
    }
}
