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
    /// <summary>处理 <see cref="PacketName.SpReqNormalShopBuyItemPacket"/> 数据包。</summary>
    internal sealed class SpReqNormalShopBuyItemPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpReqNormalShopBuyItemPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
            => HandleCore(session, inPacket);

        internal static bool HandleCore(SessionGroup session, InPacket inPacket)
        {
            uint stockId = inPacket.ReadUInt();
            int unk = inPacket.ReadInt();
            byte priceType = inPacket.ReadByte();//货币类型0:电池 1:金币 3:KOIN
            Stock.ShopBuy(session, stockId, priceType);
            return true;
        }
    }
}
