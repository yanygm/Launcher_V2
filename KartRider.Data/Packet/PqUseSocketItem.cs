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
    /// <summary>处理 <see cref="PacketName.PqUseSocketItem"/> 数据包。</summary>
    internal sealed class PqUseSocketItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUseSocketItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Item = inPacket.ReadShort();
            short Item_Id = inPacket.ReadShort();
            short Kart = inPacket.ReadShort();
            inPacket.ReadShort();
            short KartSN = inPacket.ReadShort();
            using (OutPacket outPacket = new OutPacket("PrUseSocketItem"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteShort(Item);
                outPacket.WriteShort(Item_Id);
                outPacket.WriteShort(Kart);
                outPacket.WriteShort(KartSN);
                outPacket.WriteShort(KartSN);
                outPacket.WriteShort(2);
                outPacket.WriteHexString("00 00 00 00 FF FF 00 00 00 00 00 00 00 00");
                session.Client.Send(outPacket);
            }
            KartExcData.AddTuneList(session.Client.Nickname, Kart, KartSN, 0, 0, 0, -1, 0, -1, 0);
            return true;
        }
    }
}
