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
    /// <summary>处理 <see cref="PacketName.PqChannelMovein"/> 数据包。</summary>
    internal sealed class PqChannelMovein : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqChannelMovein;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            uint UserNO = iPacket.ReadUInt();
            // 第二个 int 是频道号（0-based）：[hash][UserNO][channel]...
            int channelId = (iPacket.Length - iPacket.Position >= 4) ? iPacket.ReadInt() : -1;

            // 换频道 = 新连接 + 用 UserNO 重新握手。这里若查不到昵称，绝不能"静默 return"：
            // 不回包会让客户端一直等，表现为整个游戏卡死。优先用账号级映射，
            // 其次退回本连接已有的昵称；都拿不到才断开（让客户端显式重连，而不是空等）。
            string nickname = ClientManager.GetNickname(UserNO);
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = Parent.Client.Nickname;
            }
            if (string.IsNullOrEmpty(nickname))
            {
                Metrics.Inc(ref Metrics.ProtocolErrors);
                Log.Warn($"[ChannelMoveIn] 未知 UserNO={UserNO}（{Parent.Client.GetRemoteAddress()}），断开连接以避免客户端空等");
                Parent.Client.Disconnect();
                return true;
            }
            Log.Info($"PqChannelMovein nickname = {nickname} (UserNO={UserNO}, 频道={channelId})");
            IPEndPoint clientEndPoint = Parent.Client.Socket.RemoteEndPoint as IPEndPoint;
            if (clientEndPoint == null) return true;
            string clientId = ClientManager.GetClientId(clientEndPoint);
            if (!string.IsNullOrEmpty(nickname))
            {
                if (string.IsNullOrEmpty(Parent.Client.Nickname))
                {
                    Parent.Client.Nickname = nickname;
                }
                var nicknameConfig = ProfileService.GetProfileConfig(nickname);
                if (nicknameConfig?.Rider == null)
                {
                    Log.Info($"[PrChannelMoveIn] Warning: ProfileConfig or Rider is null for {nickname}");
                    return true;
                }
                nicknameConfig.Rider.ClientId = clientId;
                nicknameConfig.Rider.Channel = channelId;
                ProfileService.Save(nickname, nicknameConfig);
                using (OutPacket oPacket = new OutPacket("PrChannelMoveIn"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteEndPoint(IPAddress.Any, ProfileService.SettingConfig.ServerPort);
                    oPacket.WriteEndPoint(IPAddress.Any, (ushort)(ProfileService.SettingConfig.ServerPort + 1));
                    Parent.Client.Send(oPacket);
                }
            }
            return true;
        }
    }
}
