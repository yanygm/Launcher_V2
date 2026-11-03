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
    /// <summary>处理 <see cref="PacketName.PqEquipTuningExPacket"/> 数据包。</summary>
    internal sealed class PqEquipTuningExPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqEquipTuningExPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Item = inPacket.ReadShort();
            short Item_Id = inPacket.ReadShort();
            short Kart = inPacket.ReadShort();
            short Kart_Id = inPacket.ReadShort();
            short SN = inPacket.ReadShort();
            KartExcData.AddPlantList(session.Client.Nickname, Kart_Id, SN, Item, Item_Id);
            using (OutPacket outPacket = new OutPacket("PrEquipTuningPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteShort(SN);
                outPacket.WriteShort(SN);
                outPacket.WriteShort(Kart_Id);
                outPacket.WriteShort(Item);
                outPacket.WriteShort(Item_Id);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
