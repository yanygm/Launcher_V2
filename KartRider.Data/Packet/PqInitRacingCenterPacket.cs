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
    /// <summary>处理 <see cref="PacketName.PqInitRacingCenterPacket"/> 数据包。</summary>
    internal sealed class PqInitRacingCenterPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqInitRacingCenterPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            byte[] levels = new byte[] { 2, 3, 4, 5, 0 };
            using (OutPacket outPacket = new OutPacket("PrInitRacingCenterPacket"))
            {
                outPacket.WriteShort(0);
                outPacket.WriteInt(5);
                foreach (var i in levels)
                {
                    outPacket.WriteByte(i);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                }
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
