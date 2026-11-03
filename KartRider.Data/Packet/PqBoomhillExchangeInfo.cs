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
    /// <summary>处理 <see cref="PacketName.PqBoomhillExchangeInfo"/> 数据包。</summary>
    internal sealed class PqBoomhillExchangeInfo : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqBoomhillExchangeInfo;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Type = inPacket.ReadShort();
            inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("PrBoomhillExchangeInfo"))
            {
                outPacket.WriteInt();
                outPacket.WriteShort(Type);
                outPacket.WriteInt();
                outPacket.WriteInt();
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
