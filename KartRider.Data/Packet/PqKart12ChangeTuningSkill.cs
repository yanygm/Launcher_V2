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
    /// <summary>处理 <see cref="PacketName.PqKart12ChangeTuningSkill"/> 数据包。</summary>
    internal sealed class PqKart12ChangeTuningSkill : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKart12ChangeTuningSkill;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart = inPacket.ReadShort();
            short sn = inPacket.ReadShort();
            short Skill = inPacket.ReadShort();
            short field = inPacket.ReadShort();
            using (OutPacket outPacket = new OutPacket("PrKart12ChangeTuningSkill"))
            {
                outPacket.WriteShort(kart);
                outPacket.WriteShort(sn);
                outPacket.WriteShort(field);
                outPacket.WriteShort(Skill);
                outPacket.WriteShort(0);
                session.Client.Send(outPacket);
            }
            KartExcData.AddLevel12List(session.Client.Nickname, kart, sn, -1, field, Skill, -1, -1);
            return true;
        }
    }
}
