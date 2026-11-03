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
    /// <summary>处理 <see cref="PacketName.PqChangeClubIntroPacket"/> 数据包。</summary>
    internal sealed class PqChangeClubIntroPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqChangeClubIntroPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var changeIntroConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            changeIntroConfig.Rider.ClubIntro = inPacket.ReadString();
            // IDA: PqChangeClubIntroPacket = string + int + int
            if (inPacket.Available >= 4) inPacket.ReadInt();
            if (inPacket.Available >= 4) inPacket.ReadInt();
            ProfileService.Save(session.Client.Nickname, changeIntroConfig);
            using (OutPacket outPacket = new OutPacket("PrChangeClubIntroPacket"))
            {
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
