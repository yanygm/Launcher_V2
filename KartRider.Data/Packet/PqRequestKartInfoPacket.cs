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
    /// <summary>处理 <see cref="PacketName.PqRequestKartInfoPacket"/> 数据包。</summary>
    internal sealed class PqRequestKartInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqRequestKartInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int count = 0;
            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteInt(count);
                for (int i = 0; i < count; i++)
                {
                    outPacket.WriteShort(3);
                    outPacket.WriteShort(1494);
                    outPacket.WriteShort(1);
                    outPacket.WriteShort(1);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(-1);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                }
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
