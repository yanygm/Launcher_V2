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
    /// <summary>处理 <see cref="PacketName.PqCreateClubPacket"/> 数据包。</summary>
    internal sealed class PqCreateClubPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqCreateClubPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var createClubConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            createClubConfig.Rider.ClubName = inPacket.ReadString();
            createClubConfig.Rider.ClubIntro = inPacket.ReadString();
            createClubConfig.Rider.ClubMark_LOGO = inPacket.ReadInt();
            // IDA: PqCreateClubPacket = string + string + int + int
            if (inPacket.Available >= 4) inPacket.ReadInt();
            ProfileService.Save(session.Client.Nickname, createClubConfig);
            using (OutPacket outPacket = new OutPacket("PrNewCareerNoticePacket"))
            {
                session.Client.Send(outPacket);
            }
            using (OutPacket outPacket = new OutPacket("PrCreateClubPacket"))
            {
                outPacket.WriteInt(createClubConfig.Rider.ClubCode);
                outPacket.WriteInt(createClubConfig.Rider.ClubMark_LINE);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
