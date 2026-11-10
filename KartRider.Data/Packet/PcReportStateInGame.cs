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
    /// <summary>处理 <see cref="PacketName.PcReportStateInGame"/> 数据包。</summary>
    internal sealed class PcReportStateInGame : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PcReportStateInGame;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            // IDA: PcReportStateInGame = int + int
            if (iPacket.Available >= 4) iPacket.ReadInt();
            if (iPacket.Available >= 4) iPacket.ReadInt();

            // 官方抓包时序：客户端加载完赛道后才发这个包，随后服务端才下发
            // GameControlPacket(state=1) 开始 3-2-1 倒计时。这里把它作为「加载完成」信号，
            // 让倒计时起点与官方一致（而不是在发出开赛包时就开始读秒）。
            RoomManager.MarkPlayerLoaded(Parent.Client.Nickname);
            return true;
        }
    }
}
