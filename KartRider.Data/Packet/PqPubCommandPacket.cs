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
    /// <summary>处理 <see cref="PacketName.PqPubCommandPacket"/> 数据包。</summary>
    internal sealed class PqPubCommandPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqPubCommandPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 旧版实现：PqPubCommandPacket 不读取请求体，直接回 [i32 0][i32 0][时间]
            using (OutPacket outPacket = new OutPacket("PrPubCommandPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteTime(DateTime.Now);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
