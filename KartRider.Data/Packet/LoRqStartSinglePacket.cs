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
    /// <summary>处理 <see cref="PacketName.LoRqStartSinglePacket"/> 数据包。</summary>
    internal sealed class LoRqStartSinglePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqStartSinglePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int TimeAttackStartTicks = inPacket.ReadInt();
            session.TimeAttackStartTicks = TimeAttackStartTicks;
            session.PlaneCheck1 = (byte)session.TimeAttackStartTicks;
            uint key = CryptoConstants.GetKey(CryptoConstants.GetKey((uint)session.TimeAttackStartTicks)) % 5 + 6;

            session.SendPlaneCount = (int)key;
            session.TotalSendPlaneCount = 0;
            Log.Info($"PlaneCheckMax: {session.SendPlaneCount}");
            return true;
        }
    }
}
