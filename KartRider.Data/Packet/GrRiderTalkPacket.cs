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
    /// <summary>处理 <see cref="PacketName.GrRiderTalkPacket"/> 数据包。</summary>
    internal sealed class GrRiderTalkPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrRiderTalkPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            string value = iPacket.ReadString();
            // IDA: GrRiderTalkPacket = string + int
            if (iPacket.Available >= 4) iPacket.ReadInt();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return true;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"GetRoom Failed, roomId = {roomId}");
                return true;
            }

            var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
            if (player == null)
            {
                Log.Info($"GetPlayer Failed, roomId = {roomId}, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return true;
            }

            using (OutPacket outPacket = new OutPacket("GrRiderEchoPacket"))
            {
                outPacket.WriteInt(player.ID);
                outPacket.WriteString(value);
                BroadCast(roomId, outPacket, Parent.Client.Nickname);
            }

            if (value.StartsWith("选图", StringComparison.OrdinalIgnoreCase) || value.StartsWith("换图", StringComparison.OrdinalIgnoreCase) || value.StartsWith("選圖", StringComparison.OrdinalIgnoreCase) || value.StartsWith("換圖", StringComparison.OrdinalIgnoreCase))
            {
                uint track = RandomTrack.GetHash(value);
                if (track != 0)
                {
                    room.track = track;
                    GrSlotDataPacket(roomId);
                }
                return true;
            }
            else if (value.StartsWith("开始游戏", StringComparison.OrdinalIgnoreCase) || value.StartsWith("開始遊戲", StringComparison.OrdinalIgnoreCase))
            {
                GrSessionDataPacket(Parent);
                return true;
            }
            else if (value.StartsWith("结束游戏", StringComparison.OrdinalIgnoreCase) || value.StartsWith("結束遊戲", StringComparison.OrdinalIgnoreCase))
            {
                StopGame(roomId, Parent);
                return true;
            }
            return true;
        }
    }
}
