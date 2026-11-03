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
    /// <summary>处理 <see cref="PacketName.PqMixItem"/> 数据包。</summary>
    internal sealed class PqMixItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqMixItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            uint MixID = inPacket.ReadUInt();
            int Type = inPacket.ReadInt();
            Mix.GrtMixItem(session, MixID, Type);
            return true;
        }
    }
}
