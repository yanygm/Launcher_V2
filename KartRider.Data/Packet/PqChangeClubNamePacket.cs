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
    /// <summary>处理 <see cref="PacketName.PqChangeClubNamePacket"/> 数据包。</summary>
    internal sealed class PqChangeClubNamePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqChangeClubNamePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            string oldClubName = inPacket.ReadString();
            string newClubName = inPacket.ReadString();
            using (OutPacket outPacket = new OutPacket("PrChangeClubNamePacket"))
            {
                outPacket.WriteString(newClubName);
                outPacket.WriteInt(1000);
                session.Client.Send(outPacket);
                var changeNameConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
                changeNameConfig.Rider.ClubName = newClubName;
                ProfileService.Save(session.Client.Nickname, changeNameConfig);
            }
            return true;
        }
    }
}
