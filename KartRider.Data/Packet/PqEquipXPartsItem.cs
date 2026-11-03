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
    /// <summary>处理 <see cref="PacketName.PqEquipXPartsItem"/> 数据包。</summary>
    internal sealed class PqEquipXPartsItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqEquipXPartsItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Kart = inPacket.ReadShort();
            short KartSN = inPacket.ReadShort();
            short Item_Cat_Id = inPacket.ReadShort();
            short Item_Id = inPacket.ReadShort();
            short Quantity = inPacket.ReadShort();
            short Unk1 = inPacket.ReadShort();
            byte Grade = inPacket.ReadByte();
            byte Unk2 = inPacket.ReadByte();
            short PartsValue = inPacket.ReadShort();
            short Unk3 = inPacket.ReadShort();
            Log.Info($"[Equip] PqEquipXPartsItem kart={Kart} sn={KartSN} cat={Item_Cat_Id} item={Item_Id} qty={Quantity} unk1={Unk1} grade={Grade} unk2={Unk2} value={PartsValue} unk3={Unk3}");
            using (OutPacket outPacket = new OutPacket("PrEquipXPartsItem"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteShort(Kart);
                outPacket.WriteShort(KartSN);
                outPacket.WriteShort(Item_Cat_Id);
                outPacket.WriteShort(Item_Id);
                outPacket.WriteShort(Quantity);
                outPacket.WriteShort(Unk1);
                outPacket.WriteByte(Grade);
                outPacket.WriteByte(Unk2);
                outPacket.WriteShort(PartsValue);
                outPacket.WriteShort(Unk3);
                session.Client.Send(outPacket);
            }
            KartExcData.AddPartsList(session.Client.Nickname, Kart, KartSN, Item_Cat_Id, Item_Id, Grade, PartsValue);
            Log.Info($"ClientSession : Kart: {Kart}, KartSN: {KartSN}, Item: {Item_Cat_Id}:{Item_Id}, Quantity: {Quantity}, Grade: {Grade}, PartsValue: {PartsValue}");
            return true;
        }
    }
}
