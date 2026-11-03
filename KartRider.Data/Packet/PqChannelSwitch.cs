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
    /// <summary>处理 <see cref="PacketName.PqChannelSwitch"/> 数据包。</summary>
    internal sealed class PqChannelSwitch : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqChannelSwitch;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int length = iPacket.ReadInt();
            iPacket.ReadBytes(length);
            byte rawChannel = iPacket.ReadByte(); // 客户端下发的原始频道序号（不要减 1 存档案）
            byte channel = (byte)(rawChannel - 1);
            var channelData = GameSupport.Channels.ContainsKey(channel) ? GameSupport.Channels[channel] : null;

            // 房间列表与建房都用档案里的频道做隔离，切换频道必须同步更新档案，
            // 否则切完之后仍然按旧频道过滤：自己建的房别人看不到、别人建的房自己也看不到。
            // 注意存的是 rawChannel - 1：PqChannelSwitch 的值比 PqChannelMovein 的 int 大 1
            // （实测同一次换频道：Switch=20、Movein=19），两边必须存同一个编号，否则又会对不上。
            if (!string.IsNullOrEmpty(Parent.Client.Nickname))
            {
                var switchConfig = ProfileService.GetProfileConfig(Parent.Client.Nickname);
                if (switchConfig?.Rider != null && switchConfig.Rider.Channel != channel)
                {
                    int oldChannel = switchConfig.Rider.Channel;
                    switchConfig.Rider.Channel = channel;
                    ProfileService.Save(Parent.Client.Nickname, switchConfig);
                    Log.Info($"[ChannelSwitch] {Parent.Client.Nickname} 频道 {oldChannel} -> {channel}（{channelData?.Name ?? "未知频道"}，原始序号 {rawChannel}）");
                }
            }

            if (channelData == null) return true;
            StartTimeAttack[Parent.Client.Nickname] = channelData.CreateSpeed;
            Log.Info($"Channel Switch, channel = {channelData.Name}");

            // 获取服务器IP地址
            IPEndPoint serverIPEndPoint = GetServerEndPoint(Parent);

            using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
            {
                oPacket.WriteInt(0);
                oPacket.WriteShort(channel);
                oPacket.WriteShort(iPacket.ReadShort());
                oPacket.WriteEndPoint(serverIPEndPoint);
                Parent.Client.Send(oPacket);
            }
            return true;
        }
    }
}
