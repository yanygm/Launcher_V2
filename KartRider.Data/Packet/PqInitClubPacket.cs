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
    /// <summary>处理 <see cref="PacketName.PqInitClubPacket"/> 数据包。</summary>
    internal sealed class PqInitClubPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqInitClubPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var clubSwitchConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrInitClubPacket"))
            {
                if (clubSwitchConfig.Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteInt(0);
                }
                else
                {
                    outPacket.WriteInt(1);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
