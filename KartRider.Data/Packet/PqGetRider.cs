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
    /// <summary>处理 <see cref="PacketName.PqGetRider"/> 数据包。</summary>
    internal sealed class PqGetRider : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetRider;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: PqGetRider = ushort
            if (inPacket.Available >= 2) inPacket.ReadUShort();
            NewRider.LoadItemData(session, session.Client.Nickname);
            return true;
        }
    }
}
