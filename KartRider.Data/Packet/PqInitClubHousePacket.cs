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
    /// <summary>处理 <see cref="PacketName.PqInitClubHousePacket"/> 数据包。</summary>
    internal sealed class PqInitClubHousePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqInitClubHousePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrInitClubHousePacket"))
            {
                outPacket.WriteString(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.ClubName);
                outPacket.WriteByte(5);
                outPacket.WriteByte(5);
                outPacket.WriteInt(500);
                outPacket.WriteInt(500);
                outPacket.WriteInt(3000000);
                outPacket.WriteInt(1000);
                outPacket.WriteInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.ClubMark_LOGO);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
