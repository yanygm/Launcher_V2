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
    /// <summary>处理 <see cref="PacketName.PqDisassembleXPartsItem"/> 数据包。</summary>
    internal sealed class PqDisassembleXPartsItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqDisassembleXPartsItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            inPacket.ReadByte();
            inPacket.ReadShort();
            ushort Kart = inPacket.ReadUShort();
            inPacket.ReadShort();
            short SN = inPacket.ReadShort();
            Log.Info($"DisassembleXPartsItem: " + Kart + " " + SN);
            var currencyConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrDisassembleXPartsItem"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteShort(3);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteInt(0);
                outPacket.WriteShort(0);
                outPacket.WriteUShort(Kart);
                outPacket.WriteShort(1);
                outPacket.WriteShort(0);
                outPacket.WriteByte(1);//Grade
                outPacket.WriteByte(0);//X-1 V1-2
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteInt(0);
                outPacket.WriteUInt(currencyConfig.Rider.Lucci);
                outPacket.WriteUInt(currencyConfig.Rider.Koin);
                outPacket.WriteBytes(new byte[20]);
                session.Client.Send(outPacket);
            }
            if (currencyConfig.RiderItem.Set_KartSN != 0)
            {
                Stock.AddUseKart(session, Kart);
            }
            return true;
        }
    }
}
