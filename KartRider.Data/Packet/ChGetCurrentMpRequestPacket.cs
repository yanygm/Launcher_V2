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
    /// <summary>处理 <see cref="PacketName.ChGetCurrentMpRequestPacket"/> 数据包。</summary>
    internal sealed class ChGetCurrentMpRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChGetCurrentMpRequestPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            using (OutPacket outPacket = new OutPacket("ChGetCurrentMpReplyPacket"))
            {
                for (int i = 0; i < 6; i++)
                {
                    outPacket.WriteInt(0);
                }
                for (int i = 0; i < 6; i++)
                {
                    outPacket.WriteByte(0);
                }
                Parent.Client.Send(outPacket);
            }
            return true;
        }
    }
}
