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
    /// <summary>处理 <see cref="PacketName.ChRqEnterMyRoomPacket"/> 数据包。</summary>
    internal sealed class ChRqEnterMyRoomPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChRqEnterMyRoomPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: ChRqEnterMyRoomPacket = string + string
            string nickname = inPacket.ReadString(false);
            if (inPacket.Available >= 4) inPacket.ReadString(false);
            MyRoomData.ChRpEnterMyRoomPacket(session, session.Client.Nickname, nickname);
            return true;
        }
    }
}
