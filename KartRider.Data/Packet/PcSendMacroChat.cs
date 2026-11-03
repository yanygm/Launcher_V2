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
    /// <summary>处理 <see cref="PacketName.PcSendMacroChat"/> 数据包。</summary>
    internal sealed class PcSendMacroChat : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PcSendMacroChat;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            // IDA: PcSendMacroChat = int + int + byte + string
            if (iPacket.Available >= 4) iPacket.ReadInt();
            int type = iPacket.Available >= 4 ? iPacket.ReadInt() : 0;
            byte id = iPacket.Available >= 1 ? iPacket.ReadByte() : (byte)0;
            if (iPacket.Available >= 4) iPacket.ReadString();
            return PqSendMacroChat.HandleCore(Parent, type, id);
        }
    }
}
