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
    /// <summary>处理 <see cref="PacketName.PqCheckClubDirtyTimePacket"/> 数据包。</summary>
    internal sealed class PqCheckClubDirtyTimePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqCheckClubDirtyTimePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrCheckClubDirtyTimePacket"))
            {
                outPacket.WriteHexString("D43C1B01ED7E0B00");
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(0);
                outPacket.WriteHexString("22394703A87F0B00880AFB03A67F0B0000000000000000000000000000000000");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
