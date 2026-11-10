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

        /// <summary>
        /// 解析计分参数：支持混排的关键字「总分/總分」「胜场/勝場」和数字。
        /// 数字含义同以前：1~8 个名次分，第 9 个是未完成分。
        /// </summary>
        private static RoomScore.ScoreKind ParseScoreArgs(IEnumerable<string> tokens, List<int> nums, out bool bad)
        {
            var kind = RoomScore.ScoreKind.Default;
            bad = false;
            foreach (string raw in tokens)
            {
                string t = raw.Trim();
                if (t.Length == 0)
                {
                    continue;
                }
                if (t == "总分" || t == "總分")
                {
                    kind = RoomScore.ScoreKind.TotalPoints;
                    continue;
                }
                if (t == "胜场" || t == "勝場")
                {
                    kind = RoomScore.ScoreKind.WinCount;
                    continue;
                }
                if (!int.TryParse(t, out int v) || Math.Abs((long)v) > 999)
                {
                    bad = true;
                    return kind;
                }
                nums.Add(v);
            }
            return kind;
        }

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

            // 自动计分命令。严格按「命令名 + 空格 + 参数」解析，
            // 命令名必须完整匹配，避免 #重置5 3 2 把 5 吞进命令名。
            string trimmedValue = value.Trim();
            string cmdBody = trimmedValue.Length > 1 && trimmedValue[0] == '#' ? trimmedValue.Substring(1) : "";
            int cmdSp = cmdBody.IndexOfAny(new[] { ' ', '\u3000', '\t' });
            string cmd = cmdSp >= 0 ? cmdBody.Substring(0, cmdSp) : cmdBody;
            string cmdRest = cmdSp >= 0 ? cmdBody.Substring(cmdSp + 1) : "";

            // #重置计分 / #重置：仅计分发起者，清零但保持计分（可带参数换表或换计分方式）
            if (cmd == "重置" || cmd == "重置计分" || cmd == "重置計分")
            {
                var rnums = new List<int>();
                var rkind = ParseScoreArgs(
                    cmdRest.Split(new[] { ' ', '\u3000', '\t' }, StringSplitOptions.RemoveEmptyEntries),
                    rnums, out bool rbad);
                string rmsg;
                if (rbad) rmsg = "参数不对：可写「总分/胜场」，再加最多 9 个整数（8 个名次分 + 未完成分）";
                else RoomScore.Reset(room, player, rnums, rkind, out rmsg);

                using (var pkt = new OutPacket("GrRiderEchoPacket"))
                {
                    pkt.WriteInt(player.ID);
                    pkt.WriteString(rmsg);
                    BroadCast(roomId, pkt);
                }
                return true;
            }

            // #重赛：仅计分发起者，撤销最近一局（不解析任何后缀）
            if (cmd == "重赛" || cmd == "重賽")
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

            if (cmd == "开始计分" || cmd == "開始計分" || cmd == "结束计分" || cmd == "結束計分")
            {
                string reply;
                if (cmd == "开始计分" || cmd == "開始計分")
                {
                    var nums = new List<int>();
                    var kind = ParseScoreArgs(
                        cmdRest.Split(new[] { ' ', '\u3000', '\t' }, StringSplitOptions.RemoveEmptyEntries),
                        nums, out bool bad);
                    if (bad)
                    {
                        reply = "参数不对：可写「总分/胜场」，再加最多 9 个整数（8 个名次分 + 未完成分）";
                    }
                    else
                    {
                        RoomScore.TryStart(room, player, nums, kind, out string msg);
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

            // 命令名后面没加空格（如 #重置5 3 2 / #开始计分10 8）：提示格式错误，
            // 不再静默忽略，避免玩家以为自己发成功了
            foreach (string known in new[] { "重置计分", "重置計分", "开始计分", "開始計分", "结束计分", "結束計分", "重置", "重赛", "重賽" })
            {
                if (cmd.Length > known.Length && cmd.StartsWith(known, StringComparison.Ordinal))
                {
                    string example =
                        known.StartsWith("重置", StringComparison.Ordinal) ? "#重置 5 3 2" :
                        known.StartsWith("开始", StringComparison.Ordinal) ? "#开始计分 总分" :
                        known.StartsWith("结束", StringComparison.Ordinal) ? "#结束计分" :
                        "#重赛";
                    using var badPkt = new OutPacket("GrRiderEchoPacket");
                    badPkt.WriteInt(player.ID);
                    badPkt.WriteString($"格式错误：命令和参数之间要加空格，例如 {example}");
                    BroadCast(roomId, badPkt);
                    return true;
                }
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
