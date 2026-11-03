using KartRider.IO.Packet;
using KartRider_PacketName;
using RiderData;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqUpdateTrainingCenterDataPacket"/> 数据包。</summary>
    /// <remarks>
    /// 车手学院（培训中心）的通关上报，包体只有 1 个字节，客户端不等待回包。
    /// 与驾照进度分开记录到玩家档案（PrRiderSchoolDataPacket 的条目部分）。
    /// </remarks>
    internal sealed class PqUpdateTrainingCenterDataPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUpdateTrainingCenterDataPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            byte reportedValue = inPacket.Available >= 1 ? inPacket.ReadByte() : (byte)0;
            RiderSchool.RecordTrainingCenterClear(session, reportedValue);
            return true;
        }
    }
}
