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
    /// <summary>处理 <see cref="PacketName.PqCompleteChallenger"/> 数据包。</summary>
    internal sealed class PqCompleteChallenger : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqCompleteChallenger;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int stage = 40;
            byte StageType = inPacket.ReadByte();
            inPacket.ReadInt();
            int EndType = inPacket.ReadInt();
            using (OutPacket outPacket = new OutPacket("PrCompleteChallenger"))
            {
                outPacket.WriteByte(StageType);
                outPacket.WriteInt(0);
                outPacket.WriteInt(EndType);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(stage);
                for (int i = 0; i < stage; i++)
                {
                    outPacket.WriteShort(55);
                }
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
