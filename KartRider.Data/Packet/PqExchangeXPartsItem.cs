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
    /// <summary>处理 <see cref="PacketName.PqExchangeXPartsItem"/> 数据包。</summary>
    internal sealed class PqExchangeXPartsItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqExchangeXPartsItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short KartType = inPacket.ReadShort();
            inPacket.ReadShort();
            byte Grade = inPacket.ReadByte();
            Random random = new Random();
            short[] numbers = { 63, 64, 65, 66 };
            int randomIndex = random.Next(0, numbers.Length);
            short randomNumber = numbers[randomIndex];
            using (OutPacket outPacket = new OutPacket("PrExchangeXPartsItem"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.SlotChanger);
                outPacket.WriteShort(randomNumber);
                outPacket.WriteShort(KartType);
                outPacket.WriteShort(1);
                outPacket.WriteShort(0);
                outPacket.WriteByte(Grade);
                outPacket.WriteByte(1);
                outPacket.WriteShort(1200);
                outPacket.WriteShort(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
