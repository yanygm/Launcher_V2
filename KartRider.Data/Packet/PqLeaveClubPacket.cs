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
    /// <summary>处理 <see cref="PacketName.PqLeaveClubPacket"/> 数据包。</summary>
    internal sealed class PqLeaveClubPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqLeaveClubPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int ClubID = inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("PrLeaveClubPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(ClubID);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
