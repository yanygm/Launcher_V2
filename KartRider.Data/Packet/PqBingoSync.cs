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
    /// <summary>处理 <see cref="PacketName.PqBingoSync"/> 数据包。</summary>
    internal sealed class PqBingoSync : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqBingoSync;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: PqBingoSync = long
            if (inPacket.Available >= 8) inPacket.ReadLong();
            using (OutPacket outPacket = new OutPacket("PrBingoSync"))
            {
                outPacket.WriteByte(0);
                outPacket.WriteUShort(0);
                outPacket.WriteUShort(0);
                for (int i = 0; i < 15; i++)
                {
                    outPacket.WriteByte(0);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
