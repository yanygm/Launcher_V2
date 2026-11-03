using System;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using RiderData;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.LoRqUpdateRiderSchoolDataPacket"/> 数据包。</summary>
    /// <remarks>
    /// 客户端每通过一关驾照考试就上报一次，且不等待回包。
    /// 这里把进度写进玩家档案（只允许逐关推进），之后 PrRiderSchoolDataPacket 会下发真实进度。
    /// </remarks>
    internal sealed class LoRqUpdateRiderSchoolDataPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqUpdateRiderSchoolDataPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 包体布局随版本略有差异，解析与合法性校验统一放在 RiderSchool 里做
            RiderSchool.RecordStageClear(session, inPacket.ToArray(), inPacket.Position);
            return true;
        }
    }
}
