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
    /// <summary>处理 <see cref="PacketName.PqStartChallenger"/> 数据包。</summary>
    internal sealed class PqStartChallenger : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqStartChallenger;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int stage_id = inPacket.ReadInt();
            int GameType = inPacket.ReadInt();
            short Kart = inPacket.ReadShort();
            byte Unk1 = inPacket.ReadByte();
            byte Unk2 = inPacket.ReadByte();
            byte Unk3 = inPacket.ReadByte();
            using (OutPacket outPacket = new OutPacket("PrStartChallenger"))
            {
                outPacket.WriteInt(stage_id);
                outPacket.WriteInt(GameType);
                outPacket.WriteByte(0);
                outPacket.WriteByte(1);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
