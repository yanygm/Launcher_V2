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
    /// <summary>处理 <see cref="PacketName.PqGetMyMsgrInfoPacket"/> 数据包。</summary>
    internal sealed class PqGetMyMsgrInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetMyMsgrInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            uint unk1 = inPacket.ReadUInt();
            using (OutPacket outPacket = new OutPacket("PrGetMyMsgrInfoPacket"))
            {
                outPacket.WriteUInt(unk1);
                outPacket.WriteByte(1);
                outPacket.WriteBytes(new byte[15]);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
