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
    /// <summary>处理 <see cref="PacketName.PqGetCompetitiveSlotInfo"/> 数据包。</summary>
    internal sealed class PqGetCompetitiveSlotInfo : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetCompetitiveSlotInfo;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var manager = new CompetitiveDataManager();
            var competitiveData = manager.LoadAllData(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrGetCompetitiveSlotInfo"))
            {
                outPacket.WriteInt(competitiveData.Count);
                foreach (var competitive in competitiveData)
                {
                    outPacket.WriteUInt(competitive.Track);
                    outPacket.WriteUInt(competitive.Track);
                    outPacket.WriteUShort(competitive.Kart);
                    outPacket.WriteUInt(competitive.Time);
                    outPacket.WriteHexString("FF FF FF FF");
                    outPacket.WriteShort(competitive.Booster);
                    outPacket.WriteUInt(competitive.BoosterPoint);
                    outPacket.WriteShort(competitive.Crash);
                    outPacket.WriteUInt(competitive.CrashPoint);
                    outPacket.WriteUInt(competitive.Point);
                    outPacket.WriteInt(0);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
