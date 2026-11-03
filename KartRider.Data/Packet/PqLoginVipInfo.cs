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
    /// <summary>处理 <see cref="PacketName.PqLoginVipInfo"/> 数据包。</summary>
    /// <remarks>
    /// 客户端登录时会连发两次 PqLoginVipInfo；两次都回包会让「VIP等级奖励说明」弹两次，
    /// 所以同一昵称短时间内的重复请求只回第一次。
    /// </remarks>
    internal sealed class PqLoginVipInfo : BuiltInPacketHandler
    {
        private const int DuplicateWindowMs = 3000;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> LastReplyTicks =
            new System.Collections.Concurrent.ConcurrentDictionary<string, long>(StringComparer.Ordinal);

        public override PacketName TargetPacket => PacketName.PqLoginVipInfo;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            string nickname = session.Client.Nickname ?? "";
            long now = Environment.TickCount64;
            if (!string.IsNullOrEmpty(nickname))
            {
                if (LastReplyTicks.TryGetValue(nickname, out long last) && now - last < DuplicateWindowMs)
                {
                    return true;
                }
                LastReplyTicks[nickname] = now;
            }

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
            using (OutPacket outPacket = new OutPacket("PrLoginVipInfo"))
            {
                // 本客户端（KartRider.exe）的 PrLoginVipInfo 读取顺序：
                // u16 等级、i32 积分、u16 等级、u8 标志、u16 时间(天)、u16 时间(刻)、i32 保留 —— 共 17 字节。
                outPacket.WriteShort((short)premium);
                outPacket.WriteInt(points);
                outPacket.WriteShort((short)premium);
                outPacket.WriteByte(1);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            Log.Info($"[VIP] {nickname} 登录 VIP 信息: premium={premium} points={points}");
            return true;
        }
    }
}
