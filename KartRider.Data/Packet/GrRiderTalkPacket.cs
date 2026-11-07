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

            // 自动计分：#开始计分 [8 个名次分 + 未完成分] / #结束计分
            // #重置计分 / #重置：仅计分发起者，清零但保持计分（可带参数换表）
            if (value.Trim().StartsWith("#重置", StringComparison.OrdinalIgnoreCase) || value.Trim().StartsWith("#重置計分", StringComparison.OrdinalIgnoreCase))
            {
                string rbody = value.Trim().Substring(1);              // 去掉 '#'
                int rsp = rbody.IndexOfAny(new[] { ' ', '\u3000', '\t' });
                var rnums = new List<int>();
                bool rbad = false;
                if (rsp >= 0)
                {
                    foreach (string token in rbody.Substring(rsp + 1).Split(new[] { ' ', '\u3000', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!int.TryParse(token, out int rv) || Math.Abs((long)rv) > 999) { rbad = true; break; }
                        rnums.Add(rv);
                    }
                }
                string rmsg;
                if (rbad) rmsg = "参数不对：最多 9 个整数（8 个名次分 + 未完成分）";
                else RoomScore.Reset(room, player, rnums, out rmsg);

                using (var pkt = new OutPacket("GrRiderEchoPacket"))
                {
                    pkt.WriteInt(player.ID);
                    pkt.WriteString(rmsg);
                    BroadCast(roomId, pkt);
                }
                return true;
            }

            // #重赛：仅计分发起者，撤销最近一局
            if (value.Trim().StartsWith("#重赛", StringComparison.OrdinalIgnoreCase) || value.Trim().StartsWith("#重賽", StringComparison.OrdinalIgnoreCase))
            {
                RoomScore.Replay(room, player, out string replayMsg);
                using (var pkt = new OutPacket("GrRiderEchoPacket"))
                {
                    pkt.WriteInt(player.ID);
                    pkt.WriteString(replayMsg);
                    BroadCast(roomId, pkt);
                }
                return true;
            }

            if (value.StartsWith("#开始计分", StringComparison.OrdinalIgnoreCase) || value.StartsWith("#開始計分", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("#结束计分", StringComparison.OrdinalIgnoreCase) || value.StartsWith("#結束計分", StringComparison.OrdinalIgnoreCase))
            {
                string body = value.Substring(1);            // 去掉 '#'
                int sp = body.IndexOfAny(new[] { ' ', '\u3000', '\t' });
                string command = sp >= 0 ? body.Substring(0, sp) : body;
                string reply;
                if (command.StartsWith("开始") || command.StartsWith("開始"))
                {
                    var nums = new List<int>();
                    string rest = sp >= 0 ? body.Substring(sp + 1) : "";
                    bool bad = false;
                    foreach (string token in rest.Split(new[] { ' ', '\u3000', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!int.TryParse(token, out int v) || Math.Abs(v) > 999) { bad = true; break; }
                        nums.Add(v);
                    }
                    if (bad)
                    {
                        reply = "参数不对：最多 9 个整数（8 个名次分 + 未完成分）";
                    }
                    else
                    {
                        var result = RoomScore.TryStart(room, player, nums, out string msg);
                        reply = msg;
                    }
                }
                else
                {
                    RoomScore.Stop(roomId, out string msg);
                    reply = msg;
                }

                using (var pkt = new OutPacket("GrRiderEchoPacket"))
                {
                    pkt.WriteInt(player.ID);
                    pkt.WriteString(reply);
                    BroadCast(roomId, pkt);
                }
                return true;
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
