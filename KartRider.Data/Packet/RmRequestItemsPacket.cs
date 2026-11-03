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
    /// <summary>处理 <see cref="PacketName.RmRequestItemsPacket"/> 数据包。</summary>
    internal sealed class RmRequestItemsPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.RmRequestItemsPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            MyRoomData.RmRequestItemsPacket(session, session.Client.Nickname);
            return true;
        }
    }
}
