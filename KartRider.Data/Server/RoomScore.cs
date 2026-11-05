using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KartRider.IO.Packet;
using KartRider.Logging;

namespace KartRider
{
    /// <summary>
    /// 房间自动计分。
    ///
    ///   #开始计分 [数字...]   开启（已开启则忽略，必须先 #结束计分）
    ///       0 个数字 → 用默认表：个人 {10,7,5,4,3,1,0,-1}、未完成 -5
    ///       1~8 个   → 前 8 名分，未完成用默认
    ///       9 个     → 8 个名次分 + 第 9 个是未完成分
    ///   #结束计分             停止并清空
    ///
    /// 播报时机：本局所有参赛玩家都回到房间（GrFirstRequestPacket）后，再延迟 1.5 秒播报；
    /// 若有人卡住，8 秒后强制播报一次。
    ///
    ///   个人赛：按名次分累计（含 AI，名字 AI{格子+1}）
    ///   组队 / 双人：累计胜场
    /// </summary>
    public static class RoomScore
    {
        private sealed class State
        {
            public string StarterNick;
            public int Round;
            public bool TeamMode;
            public bool DuoMode;
            public Dictionary<string, int> Points = new(StringComparer.Ordinal);
            public Dictionary<int, int> TeamPoints = new();
            public int[] TeamWins = new int[5];
            public string LastTrack = "";
            public int[] Rank = { 10, 7, 5, 4, 3, 1, 0, -1 };
            public int Dnf = -5;
            // 播报等待
            public bool Pending;
            public HashSet<string> Participants = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Returned = new(StringComparer.OrdinalIgnoreCase);
            public DateTime PendingSince;
            public bool PartialTimerStarted;
        }

        private static readonly object Gate = new();
        private static readonly Dictionary<int, State> States = new();

        public enum StartResult { Started, AlreadyRunning, BadArgs, NotRacing, NoRoom }

        public static bool IsActive(int roomId)
        {
            lock (Gate) { return States.ContainsKey(roomId); }
        }

        /// <summary>#开始计分：已在计分中则忽略。</summary>
        public static StartResult TryStart(GameRoom room, Player starter, IReadOnlyList<int> args, out string message)
        {
            message = null;
            if (room == null || starter == null) { message = "房间状态异常"; return StartResult.NoRoom; }
            if (room.GameType != 1 && room.GameType != 3)
            {
                message = "仅竞速房支持计分";
                return StartResult.NotRacing;
            }

            int[] rank;
            int dnf;
            bool individual = room.GameType == 1;
            if (individual)
            {
                rank = new[] { 10, 7, 5, 4, 3, 1, 0, -1 };
                dnf = -5;
            }
            else
            {
                // 组队/双人走胜场，这两张表只在将来需要时用
                rank = room.DuoTeam ? new[] { 10, 8, 7, 5, 4, 3, 2, 1 } : new[] { 10, 8, 6, 5, 4, 3, 2, 1 };
                dnf = 0;
            }

            if (args != null && args.Count > 0)
            {
                if (args.Count > 9) { message = "参数最多 9 个：8 个名次分 + 1 个未完成分"; return StartResult.BadArgs; }
                for (int i = 0; i < args.Count && i < 8; i++) rank[i] = args[i];
                if (args.Count == 9) dnf = args[8];
            }

            lock (Gate)
            {
                if (States.ContainsKey(room.RoomId)) { message = "计分已在进行中（如需重来请先 #结束计分）"; return StartResult.AlreadyRunning; }
                States[room.RoomId] = new State
                {
                    StarterNick = starter.Nickname,
                    TeamMode = room.GameType == 3,
                    DuoMode = room.DuoTeam,
                    Rank = rank,
                    Dnf = dnf
                };
            }
            Log.Debug($"[Score] 房间 {room.RoomId} 开始计分（发起人 {starter.Nickname}，{(individual ? "个人" : room.DuoTeam ? "双人" : "组队")}，表 {string.Join(",", rank)} / 未完成 {dnf}）");
            message = "现在开始计分";
            return StartResult.Started;
        }

        public static bool Stop(int roomId, out string message)
        {
            lock (Gate)
            {
                bool removed = States.Remove(roomId);
                message = removed ? "已结束计分" : "当前没有计分";
                return removed;
            }
        }

        public static void Clear(int roomId)
        {
            lock (Gate) { States.Remove(roomId); }
        }

        /// <summary>每局结算：累加，并进入"等待全员回房"状态。</summary>
        public static void OnRoundEnd(GameRoom room, RoomMember[] members, Dictionary<int, int> ranking, Dictionary<int, uint> timeData, byte winnerTeam)
        {
            if (room == null || members == null || ranking == null) return;
            lock (Gate)
            {
                if (!States.TryGetValue(room.RoomId, out State st) || st == null) return;
                st.Round++;
                st.LastTrack = RandomTrack.GetTrackName(room.trackTemp);

                if (st.TeamMode && st.DuoMode)
                {
                    // 双人房：每队成员的名次分之和，按队累计
                    foreach (RoomMember m in members)
                    {
                        int team = 0, id = -1;
                        if (m is Player p) { team = p.Team; id = p.ID; }
                        else if (m is Ai a) { team = a.Team; id = a.ID; }
                        else continue;
                        if (team < 1 || team > 4) continue;
                        if (!timeData.TryGetValue(id, out uint tp)) continue;
                        int pts = tp == uint.MaxValue ? st.Dnf : Score(st, ranking[id]);
                        st.TeamPoints.TryGetValue(team, out int oldT);
                        st.TeamPoints[team] = oldT + pts;
                    }
                }
                else if (st.TeamMode)
                {
                    if (winnerTeam >= 1 && winnerTeam <= 4) st.TeamWins[winnerTeam]++;
                }
                else
                {
                    foreach (RoomMember m in members)
                    {
                        if (m is Player p)
                        {
                            if (!timeData.TryGetValue(p.ID, out uint tp)) continue;
                            int pts = tp == uint.MaxValue ? st.Dnf : Score(st, ranking[p.ID]);
                            st.Points.TryGetValue(p.Nickname, out int oldP);
                            st.Points[p.Nickname] = oldP + pts;
                        }
                        else if (m is Ai a)
                        {
                            string name = $"AI{a.SlotId + 1}";
                            if (!timeData.TryGetValue(a.ID, out uint ta)) continue;
                            int pts = ta == uint.MaxValue ? st.Dnf : Score(st, ranking[a.ID]);
                            st.Points.TryGetValue(name, out int oldA);
                            st.Points[name] = oldA + pts;
                        }
                    }
                }

                st.Participants.Clear();
                st.Returned.Clear();
                foreach (RoomMember m in members)
                {
                    if (m is Player pl && !string.IsNullOrEmpty(pl.Nickname)) st.Participants.Add(pl.Nickname);
                }
                st.Pending = st.Participants.Count > 0;
                st.PendingSince = DateTime.Now;
                st.PartialTimerStarted = false;
                if (!st.Pending) return;
            }

            // 60 秒兜底：所有人都不回房（掉线/直接退出）时才强制播一次
            int roomId = room.RoomId;
            Task.Run(async () =>
            {
                await Task.Delay(60000).ConfigureAwait(false);
                ForceIfStillPending(roomId, "60 秒内无人回房");
            });
        }

        private static int Score(State st, int rank)
        {
            if (st.Rank == null || st.Rank.Length == 0) return 0;
            return rank >= 0 && rank < st.Rank.Length ? st.Rank[rank] : st.Dnf;
        }

        /// <summary>玩家回到房间：全部参赛者都回来了 → 延迟 1.5 秒播报。</summary>
        public static void OnRoomEntered(int roomId, string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return;
            bool fireAll = false;
            bool startPartial = false;
            lock (Gate)
            {
                if (!States.TryGetValue(roomId, out State st) || st == null || !st.Pending) return;
                st.Returned.Add(nickname);
                if (st.Participants.All(n => st.Returned.Contains(n)))
                {
                    st.Pending = false;
                    fireAll = true;
                }
                else if (!st.PartialTimerStarted)
                {
                    st.PartialTimerStarted = true;
                    startPartial = true;
                }
            }

            if (fireAll)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1500).ConfigureAwait(false);
                    try { Post(roomId); } catch (Exception ex) { Log.Warn($"[Score] 房间 {roomId} 播报失败: {ex.Message}"); }
                });
            }
            else if (startPartial)
            {
                // 有人先回来了、但还有人没回：再等 10 秒，仍然没齐就补播一次
                Task.Run(async () =>
                {
                    await Task.Delay(10000).ConfigureAwait(false);
                    ForceIfStillPending(roomId, "部分玩家未回房");
                });
            }
        }

        private static void ForceIfStillPending(int roomId, string reason)
        {
            bool fire = false;
            lock (Gate)
            {
                if (States.TryGetValue(roomId, out State st) && st != null && st.Pending)
                {
                    st.Pending = false;
                    fire = true;
                    Log.Debug($"[Score] 房间 {roomId} 强制播报（{reason}，回房 {st.Returned.Count}/{st.Participants.Count}）");
                }
            }
            if (!fire) return;
            try { Post(roomId); } catch (Exception ex) { Log.Warn($"[Score] 房间 {roomId} 超时播报失败: {ex.Message}"); }
        }

        private static void Post(int roomId)
        {
            var room = RoomManager.GetRoom(roomId);
            if (room == null) return;

            List<string> lines = new();
            Player speaker;
            lock (Gate)
            {
                if (!States.TryGetValue(roomId, out State st) || st == null) return;
                lines.Add($"第{st.Round}局，{st.LastTrack}：");
                if (st.TeamMode)
                {
                    if (st.DuoMode)
                    {
                        var parts = new List<string>();
                        int teams = Math.Max(1, Math.Min(4, room.DuoTeamCount));
                        for (int i = 1; i <= teams; i++)
                        {
                            st.TeamPoints.TryGetValue(i, out int tp);
                            parts.Add($"队{i}={tp}");
                        }
                        lines.Add(string.Join("，", parts));
                    }
                    else
                    {
                        lines.Add($"红队={st.TeamWins[1]}，蓝队={st.TeamWins[2]}");
                    }
                }
                else
                {
                    foreach (var kv in st.Points.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
                    {
                        lines.Add($"[{kv.Key}={kv.Value}]");
                    }
                }

                speaker = RoomManager.GetPlayer(roomId, st.StarterNick);
                if (speaker == null && room.RoomMaster >= 0 && room.RoomMaster < 8)
                {
                    speaker = RoomManager.TryGetIdDetail(roomId, room.RoomMaster) as Player;
                }
                if (speaker == null) return;

                foreach (string line in lines)
                {
                    using var pkt = new OutPacket("GrRiderEchoPacket");
                    pkt.WriteInt(speaker.ID);
                    pkt.WriteString(line);
                    MultyPlayer.BroadCast(roomId, pkt);
                }
            }
            Log.Debug($"[Score] 房间 {roomId} 播报完成（{lines.Count} 行）");
        }
    }
}
