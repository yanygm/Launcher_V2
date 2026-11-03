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
    /// <summary>处理 <see cref="PacketName.RmRqUpdateMainEmblemPacket"/> 数据包。</summary>
    internal sealed class RmRqUpdateMainEmblemPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.RmRqUpdateMainEmblemPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var emblemConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            emblemConfig.Rider.Emblem1 = inPacket.ReadShort();
            emblemConfig.Rider.Emblem2 = inPacket.ReadShort();
            using (OutPacket outPacket = new OutPacket("RmRpUpdateMainEmblemPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteByte(0);
                session.Client.Send(outPacket);
            }
            ProfileService.Save(session.Client.Nickname, emblemConfig);
            return true;
        }
    }
}
