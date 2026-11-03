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
    /// <summary>处理 <see cref="PacketName.LoRqDeleteItemPacket"/> 数据包。</summary>
    internal sealed class LoRqDeleteItemPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqDeleteItemPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            inPacket.ReadInt();
            inPacket.ReadInt();
            inPacket.ReadInt();
            inPacket.ReadShort();
            ushort ItemType = inPacket.ReadUShort();
            ushort ItemID = inPacket.ReadUShort();
            ushort SN = inPacket.ReadUShort();
            using (OutPacket outPacket = new OutPacket("LoRpDeleteItemPacket"))
            {
                session.Client.Send(outPacket);
            }
            if (ItemType == 3)
            {
                Stock.DelNewKart(session.Client.Nickname, ItemID, SN);
            }
            else
            {
                Stock.DelNewItem(session.Client.Nickname, ItemType, ItemID);
            }
            return true;
        }
    }
}
