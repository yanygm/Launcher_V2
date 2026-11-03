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
    /// <summary>处理 <see cref="PacketName.PqChangeClubMarkPacket"/> 数据包。</summary>
    internal sealed class PqChangeClubMarkPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqChangeClubMarkPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int oldClubMark = inPacket.ReadInt();
            int newClubMark = inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("PrChangeClubMarkPacket"))
            {
                outPacket.WriteInt(newClubMark);
                outPacket.WriteInt(1000);
                session.Client.Send(outPacket);
                var changeMarkConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
                changeMarkConfig.Rider.ClubMark_LOGO = newClubMark;
                ProfileService.Save(session.Client.Nickname, changeMarkConfig);
            }
            return true;
        }
    }
}
