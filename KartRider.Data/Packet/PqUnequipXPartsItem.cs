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
    /// <summary>处理 <see cref="PacketName.PqUnequipXPartsItem"/> 数据包。</summary>
    internal sealed class PqUnequipXPartsItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUnequipXPartsItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Kart = inPacket.ReadShort();
            short SN = inPacket.ReadShort();
            short item = inPacket.ReadShort();
            Log.Info($"[Equip] PqUnequipXPartsItem kart={Kart} sn={SN} item={item}");
            using (OutPacket outPacket = new OutPacket("PrUnequipXPartsItem"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteShort(Kart);
                outPacket.WriteShort(SN);
                outPacket.WriteShort(item);
                session.Client.Send(outPacket);
            }
            KartExcData.AddPartsList(session.Client.Nickname, Kart, SN, item, 0, 0, 0);
            return true;
        }
    }
}
