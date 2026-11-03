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
    /// <summary>处理 <see cref="PacketName.PqWebEventCompleteCheckPacket"/> 数据包。</summary>
    /// <remarks>原 else-if 链中该分支出现两次，只会命中第一次；此处保留第一次的行为。</remarks>
    internal sealed class PqWebEventCompleteCheckPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqWebEventCompleteCheckPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrWebEventCompleteCheckPacket"))
            {
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
