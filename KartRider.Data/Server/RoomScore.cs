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
    ///       0 个数字 → 默认表：个人 {10,7,5,4,3,1,0,-1}、未完成 -5
    ///       1~8 个   → 前 8 名分，未完成用默认
    ///       9 个     → 8 个名次分 + 第 9 个是未完成分
    ///   #结束计分             停止并清空
    ///   #重赛                 仅发起者可发；撤销最近一局，恢复到上一局结束时的累计并播报
    ///
    /// 播报时机：本局所有参赛玩家回房（GrFirstRequestPacket）后 1.5 秒；
    ///           部分回房 10 秒补播；全员不回 60 秒兜底。
    ///
    /// 计分规则：
    ///   个人：按名次分累计（含 AI，名字 AI{格子+1}）
    ///   组队：红/蓝胜场
    ///   双人：每队成员名次分之和
    ///
    /// 掉线：开局瞬间记录参赛名单，结算时名单里有人不在场/没成绩 → 按未完成分计入，
    ///       并且分数保留在列表里（直到 #结束计分 或房间销毁）。
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

            /// <summary>本局参赛者：昵称 → 队伍（开局瞬间记录，用于掉线判未完成）。</summary>
            public Dictionary<string, int> RaceParticipants = new(StringComparer.OrdinalIgnoreCase);
            public bool RaceArmed;

            /// <summary>最近一局的快照（只保留一层，供 #重赛 使用）。</summary>
            public Snapshot LastRound;

            // 播报等待
            public bool Pending;
            public HashSet<string> Participants = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Returned = new(StringComparer.OrdinalIgnoreCase);
            public DateTime PendingSince;
            public bool PartialTimerStarted;
        }

        /// <summary>一局结束时的累计快照，用于 #重赛 回退。</summary>
        private sealed class Snapshot
        {
            public int Round;
            public Dictionary<string, int> Points;
            public Dictionary<int, int> TeamPoints;
            public int[] TeamWins;
            public string LastTrack;
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
            Log.Debug($"[Score] 房间 {room.RoomId} 开始计分（发起人 {starter.Nickname}，{(individual ? "个人" : room.DuoTeam ? "双人" : "组队")}）");
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

        /// <summary>开局瞬间记录本局参赛者（昵称 + 队伍），用于把中途掉线的人判为未完成。</summary>
        public static void OnRaceStart(GameRoom room)
        {
            if (room == null) return;
            lock (Gate)
            {
                if (!States.TryGetValue(room.RoomId, out State st) || st == null) return;
                st.RaceParticipants.Clear();
                foreach (RoomMember m in room._IDs)
                {
                    if (m is Player p && !string.IsNullOrEmpty(p.Nickname))
                    {
                        st.RaceParticipants[p.Nickname] = p.Team;
                    }
                }
                st.RaceArmed = st.RaceParticipants.Count > 0;
                st.LastRound = null;   // 新的一局开始，上一局的快照作废（只在房间等待态可重赛）
            }
        }

        /// <summary>每局结算：累加，并进入"等待全员回房"状态。</summary>
        public static void OnRoundEnd(GameRoom room, RoomMember[] members, Dictionary<int, int> ranking, Dictionary<int, uint> timeData, byte winnerTeam)
        {
            if (room == null || members == null || ranking == null) return;
            lock (Gate)
            {
                if (!States.TryGetValue(room.RoomId, out State st) || st == null) return;

                // 结算前先把"本局之前"的累计存一份快照，供 #重赛 回退
                st.LastRound = new Snapshot
                {
                    Round = st.Round,
                    Points = new Dictionary<string, int>(st.Points, StringComparer.Ordinal),
                    TeamPoints = new Dictionary<int, int>(st.TeamPoints),
                    TeamWins = (int[])st.TeamWins.Clone(),
                    LastTrack = st.LastTrack
                };

                st.Round++;
                st.LastTrack = RandomTrack.GetTrackName(room.trackTemp);

                // 在场成员按 ID 建索引，便于判断参赛者是否掉线
                var byNick = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
                foreach (RoomMember m in members)
                {
                    if (m is Player p && !string.IsNullOrEmpty(p.Nickname)) byNick[p.Nickname] = p;
                }

                if (st.TeamMode && st.DuoMode)
                {
                    // 双人：每队成员名次分之和；掉线者按未完成分计入其原队伍
                    if (st.RaceArmed)
                    {
                        foreach (var kv in st.RaceParticipants)
                        {
                            int team = kv.Value;
                            if (team < 1 || team > 4) continue;
                            int pts;
                            if (byNick.TryGetValue(kv.Key, out Player mem) && timeData.TryGetValue(mem.ID, out uint tp))
                            {
                                pts = tp == uint.MaxValue ? st.Dnf : Score(st, ranking.TryGetValue(mem.ID, out int r) ? r : -1);
                            }
                            else
                            {
                                pts = st.Dnf;   // 掉线/离场 → 未完成
                            }
                            st.TeamPoints.TryGetValue(team, out int oldT);
                            st.TeamPoints[team] = oldT + pts;
                        }
                    }
                    else
                    {
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
                }
                else if (st.TeamMode)
                {
                    if (winnerTeam >= 1 && winnerTeam <= 4) st.TeamWins[winnerTeam]++;
                }
                else
                {
                    if (st.RaceArmed)
                    {
                        // 个人：开局名单里的每个人都算，掉线者按未完成分
                        foreach (var kv in st.RaceParticipants)
                        {
                            int pts;
                            if (byNick.TryGetValue(kv.Key, out Player mem) && timeData.TryGetValue(mem.ID, out uint tp))
                            {
                                pts = tp == uint.MaxValue ? st.Dnf : Score(st, ranking.TryGetValue(mem.ID, out int r) ? r : -1);
                            }
                            else
                            {
                                pts = st.Dnf;
                            }
                            st.Points.TryGetValue(kv.Key, out int oldP);
                            st.Points[kv.Key] = oldP + pts;
                        }
                        // AI 仍然按在场成员计
                        foreach (RoomMember m in members)
                        {
                            if (m is not Ai a) continue;
                            string name = $"AI{a.SlotId + 1}";
                            if (!timeData.TryGetValue(a.ID, out uint tp)) continue;
                            int pts = tp == uint.MaxValue ? st.Dnf : Score(st, ranking.TryGetValue(a.ID, out int r) ? r : -1);
                            st.Points.TryGetValue(name, out int oldA);
                            st.Points[name] = oldA + pts;
                        }
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

        /// <summary>#重赛：仅发起者可用，撤销最近一局并播报恢复后的累计。</summary>
        public static bool Replay(GameRoom room, Player asker, out string message)
        {
            if (room == null || asker == null) { message = "房间状态异常"; return false; }
            State st;
            List<string> lines = new();
            lock (Gate)
            {
                if (!States.TryGetValue(room.RoomId, out st) || st == null) { message = "当前没有计分"; return false; }
                if (!string.Equals(st.StarterNick, asker.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    message = "只有计分发起者可以重赛";
                    return false;
                }
                if (room.Started) { message = "比赛进行中，无法重赛"; return false; }
                if (st.LastRound == null) { message = "没有可重赛的局"; return false; }

                // 恢复上一局结束时的累计
                st.Round = st.LastRound.Round;
                st.Points = st.LastRound.Points;
                st.TeamPoints = st.LastRound.TeamPoints;
                st.TeamWins = st.LastRound.TeamWins;
                st.LastTrack = st.LastRound.LastTrack;
                st.LastRound = null;      // 只允许撤回最近一局
                st.Pending = false;       // 取消本局待播报

                lines.Add($"（重赛）第{st.Round}局，{st.LastTrack}：");
                if (st.TeamMode && st.DuoMode)
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
                else if (st.TeamMode)
                {
                    lines.Add($"红队={st.TeamWins[1]}，蓝队={st.TeamWins[2]}");
                }
                else
                {
                    foreach (var kv in st.Points.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
                    {
                        lines.Add($"[{kv.Key}={kv.Value}]");
                    }
                }

                foreach (string line in lines)
                {
                    using var pkt = new OutPacket("GrRiderEchoPacket");
                    pkt.WriteInt(asker.ID);
                    pkt.WriteString(line);
                    MultyPlayer.BroadCast(room.RoomId, pkt);
                }
            }
            Log.Debug($"[Score] 房间 {room.RoomId} 重赛：{asker.Nickname}");
            message = "已重赛，回到上一局结束时的分数";
            return true;
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
