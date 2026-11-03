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
    /// <summary>处理 <see cref="PacketName.PqKart12UnlockTuningSkill"/> 数据包。</summary>
    internal sealed class PqKart12UnlockTuningSkill : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKart12UnlockTuningSkill;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart = inPacket.ReadShort();
            short sn = inPacket.ReadShort();
            short field = inPacket.ReadShort();
            short SI = inPacket.ReadShort();
            using (OutPacket outPacket = new OutPacket("PrKart12UnlockTuningSkill"))
            {
                outPacket.WriteShort(kart);
                outPacket.WriteShort(sn);
                outPacket.WriteShort(field);
                outPacket.WriteShort(SI);
                if (field == 2)
                {
                    outPacket.WriteShort(2);
                    outPacket.WriteShort(3);
                }
                else
                {
                    outPacket.WriteShort(4);
                    outPacket.WriteShort(4);
                }
                outPacket.WriteShort(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
