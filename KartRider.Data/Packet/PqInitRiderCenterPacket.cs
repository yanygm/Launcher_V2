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
    /// <summary>处理 <see cref="PacketName.PqInitRiderCenterPacket"/> 数据包。</summary>
    internal sealed class PqInitRiderCenterPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqInitRiderCenterPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrInitRiderCenterPacket"))
            {
                outPacket.WriteHexString("0232000000E09304000000000000000000");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
