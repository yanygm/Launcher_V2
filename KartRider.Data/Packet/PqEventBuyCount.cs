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
    /// <summary>处理 <see cref="PacketName.PqEventBuyCount"/> 数据包。</summary>
    internal sealed class PqEventBuyCount : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqEventBuyCount;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int buyCount = inPacket.ReadInt();
            if (buyCount < 0 || buyCount > 64 || buyCount * 4 > inPacket.Available)
            {
                // 原实现直接用包内长度开数组：负数溢出、极大值直接 OOM
                Log.Warn($"[EventBuyCount] 非法数量 {buyCount}，已忽略");
                return true;
            }
            EventBuyCount.BuyCount = buyCount;
            EventBuyCount.ShopItem = new int[buyCount];
            for (int i = 0; i < buyCount; i++)
            {
                EventBuyCount.ShopItem[i] = inPacket.ReadInt();
            }
            EventBuyCount.PrEventBuyCount(session);
            return true;
        }
    }
}
