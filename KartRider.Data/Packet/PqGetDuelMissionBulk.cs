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
    /// <summary>处理 <see cref="PacketName.PqGetDuelMissionBulk"/> 数据包。</summary>
    internal sealed class PqGetDuelMissionBulk : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetDuelMissionBulk;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            List<string> MissionList = TimeAttack.GetMissionList();
            using (OutPacket outPacket = new OutPacket("PrGetDuelMissionBulk"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(MissionList.Count);
                foreach (string Track in MissionList)
                {
                    byte Level = TimeAttack.GetTrackLevel(session.Client.Nickname, Adler32Helper.GenerateAdler32_UNICODE(Track, 0));
                    outPacket.WriteByte(Level);
                    outPacket.WriteInt(0);
                }
                //outPacket.WriteHexString("0F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
