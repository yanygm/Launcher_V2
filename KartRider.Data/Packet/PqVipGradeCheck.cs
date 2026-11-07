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
    /// <summary>处理 <see cref="PacketName.PqVipGradeCheck"/> 数据包。</summary>
    internal sealed class PqVipGradeCheck : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqVipGradeCheck;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 本客户端 PrVipGradeCheck 读取顺序：u16、i32、u16、u16、u16、i32（共 16 字节）。
            // 以前只回 8 字节，客户端读越界 → 弹窗数据错乱/多弹一次。
            var vipConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            int premium = vipConfig?.Rider?.Premium ?? 0;
            int points = premium switch
            {
                1 => 10000,
                2 => 30000,
                3 => 60000,
                4 => 120000,
                5 => 200000,
                _ => 0,
            };
            Log.Info($"[VIP] {session.Client.Nickname} 等级检查: {ToHex(inPacket.ToArray())} premium={premium}");
            using (OutPacket outPacket = new OutPacket("PrVipGradeCheck"))
            {
                outPacket.WriteShort((short)premium);
                outPacket.WriteInt(points);
                outPacket.WriteShort((short)premium);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }

        private static string ToHex(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "";
            }
            return BitConverter.ToString(data, 0, Math.Min(data.Length, 64)).Replace('-', ' ');
        }
    }
}
