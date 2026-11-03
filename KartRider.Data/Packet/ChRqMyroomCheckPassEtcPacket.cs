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
    /// <summary>处理 <see cref="PacketName.ChRqMyroomCheckPassEtcPacket"/> 数据包。</summary>
    internal sealed class ChRqMyroomCheckPassEtcPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChRqMyroomCheckPassEtcPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int id = inPacket.ReadInt();
            // IDA: ChRqMyroomCheckPassEtcPacket = int + string
            if (inPacket.Available >= 4) inPacket.ReadString();
            using (OutPacket outPacket = new OutPacket("ChRpMyroomCheckPassEtcPacket"))
            {
                outPacket.WriteInt(id);
                if (id == 0)
                {
                    outPacket.WriteInt(1);
                }
                else
                {
                    outPacket.WriteInt(0); // 1 允许查看
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
