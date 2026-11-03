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
    /// <summary>处理 <see cref="PacketName.PqVipInfo"/> 数据包。</summary>
    internal sealed class PqVipInfo : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqVipInfo;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrVipInfo"))
            {
                outPacket.WriteInt(1);
                for (int i = 0; i < 10; i++)
                {
                    outPacket.WriteInt(0);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
