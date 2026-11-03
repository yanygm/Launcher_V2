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
    /// <summary>处理 <see cref="PacketName.SpRqUseCoupon"/> 数据包。</summary>
    internal sealed class SpRqUseCoupon : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqUseCoupon;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            string Coupon = inPacket.ReadString();
            CouponList.QueryCoupon(session, Coupon, true);
            return true;
        }
    }
}
