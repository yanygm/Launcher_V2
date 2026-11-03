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
    /// <summary>处理 <see cref="PacketName.SpRqTreasureHuntPacket"/> 数据包。</summary>
    internal sealed class SpRqTreasureHuntPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqTreasureHuntPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: SpRqTreasureHuntPacket = ushort + int
            if (inPacket.Available >= 2) inPacket.ReadUShort();
            if (inPacket.Available >= 4) inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("SpRpTreasureHuntPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                outPacket.WriteInt(0); //stockId
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                outPacket.WriteInt(0); //stockId
                outPacket.WriteInt(1);
                outPacket.WriteInt(0);
                outPacket.WriteInt(1);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
