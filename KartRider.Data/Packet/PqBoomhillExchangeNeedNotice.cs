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
    /// <summary>处理 <see cref="PacketName.PqBoomhillExchangeNeedNotice"/> 数据包。</summary>
    internal sealed class PqBoomhillExchangeNeedNotice : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqBoomhillExchangeNeedNotice;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: PqBoomhillExchangeNeedNotice = ushort + int
            if (inPacket.Available >= 2) inPacket.ReadUShort();
            if (inPacket.Available >= 4) inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("PrBoomhillExchangeNeedNotice"))
            {
                outPacket.WriteHexString("00 00 00 00 01");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
