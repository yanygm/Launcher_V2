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
    /// <summary>处理 <see cref="PacketName.LoRqGetRiderItemPacket"/> 数据包。</summary>
    internal sealed class LoRqGetRiderItemPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqGetRiderItemPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 只回“道具列表”（Items）。v103 回全量、v108 回部件表+PrGetRider 都会明显变慢，
            // 且对 V1 车膜“已装备”判定无效，已回滚。
            // NewRider.Items(session, session.Client.Nickname);
            return true;
        }
    }
}
