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
    /// <summary>处理 <see cref="PacketName.PqLotteryMileagePrizePacket"/> 数据包。</summary>
    internal sealed class PqLotteryMileagePrizePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqLotteryMileagePrizePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: PqLotteryMileagePrizePacket = int + byte
            if (inPacket.Available >= 4) inPacket.ReadInt();
            if (inPacket.Available >= 1) inPacket.ReadByte();
            var bingo = Bingo.Get(session.Client.Nickname);
            Log.Info($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{session.Client.Nickname}] 领取奖励 PqLotteryMileagePrizePacket，重置面板（原已连线={bingo.CompletedLines.Count}）");
            using (OutPacket outPacket = new OutPacket("PrLotteryMileagePrizePacket"))
            {
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            uint stockId = 0;
            if (Bingo.bingoGachaSet != null && Bingo.bingoGachaSet.TryGetValue(bingo.BingoItem, out uint selectedReward))
            {
                stockId = selectedReward;
            }
            Stock.GetStockItem(session, stockId);
            bingo.BingoCount = 0;
            bingo.Save();
            return true;
        }
    }
}
