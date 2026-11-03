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
    /// <summary>处理 <see cref="PacketName.PqBreakUpClubPacket"/> 数据包。</summary>
    internal sealed class PqBreakUpClubPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqBreakUpClubPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int ClubID = inPacket.ReadInt();
            // IDA: PqBreakUpClubPacket = int + int
            if (inPacket.Available >= 4) inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("PrBreakUpClubPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(ClubID);
                session.Client.Send(outPacket);
            }
            var profileConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            profileConfig.Rider.ClubMark_LOGO = 0;
            ProfileService.Save(session.Client.Nickname, profileConfig);
            return true;
        }
    }
}
