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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static KartRider.MultyPlayer;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PcCancelMatching"/> 数据包。</summary>
    internal sealed class PcCancelMatching : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PcCancelMatching;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
            => PcStartMatching.HandleCore(Parent, iPacket);
    }
}
