using ExcData;
using KartRider;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms.Design;
using System.Xml;
using System.Xml.Linq;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PqStartTrainingCenter"/> 数据包。</summary>
    internal sealed class PqStartTrainingCenter : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqStartTrainingCenter;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 请求体：u8 培训中心编号 + 任务/关卡编号，进入中心时先记下来
            byte centerId = inPacket.Available >= 1 ? inPacket.ReadByte() : (byte)1;
            int missionId = inPacket.Available >= 4 ? inPacket.ReadInt() : 0;
            RiderSchool.BeginTrainingCenter(session, centerId);
            Log.Info($"[RiderSchool] {session.Client.Nickname} 开始训练中心 mission={missionId}");

            using (OutPacket outPacket = new OutPacket("PrStartTrainingCenter"))
            {
                outPacket.WriteByte(1);
                StartGameData.GetSchoolSpac(outPacket, session.Client.Nickname);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
