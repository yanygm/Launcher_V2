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
    /// <summary>处理 <see cref="PacketName.PqKartLevelPointClear"/> 数据包。</summary>
    internal sealed class PqKartLevelPointClear : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKartLevelPointClear;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Kart = inPacket.ReadShort();
            short SN = inPacket.ReadShort();
            var koinConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrKartLevelPointClear"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteShort(Kart);
                outPacket.WriteShort(SN);
                outPacket.WriteShort(5);
                outPacket.WriteShort(35);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteUInt(koinConfig.Rider.Koin);
                session.Client.Send(outPacket);
            }
            KartExcData.AddLevelList(session.Client.Nickname, Kart, SN, 5, 35, 0, 0, 0, 0, 0);
            return true;
        }
    }
}
