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
    /// <summary>处理 <see cref="PacketName.PqBlockCatchEnterPacket"/> 数据包。</summary>
    internal sealed class PqBlockCatchEnterPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqBlockCatchEnterPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrBlockCatchEnterPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(3);
                outPacket.WriteInt(3);
                outPacket.WriteInt(0);
                outPacket.WriteInt(5);
                outPacket.WriteInt(1);
                outPacket.WriteInt(7);
                outPacket.WriteInt(2);
                outPacket.WriteInt(600);
                outPacket.WriteInt(300);
                outPacket.WriteInt(200);
                outPacket.WriteInt(100);
                outPacket.WriteInt(-100);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
