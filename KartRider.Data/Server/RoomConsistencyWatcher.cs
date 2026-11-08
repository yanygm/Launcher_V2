using System;
using System.Threading;
using KartRider.Logging;

namespace KartRider
{
    /// <summary>
    /// 房间一致性巡检（每 30 秒）。
    ///
    /// 目的：不管幽灵是怎么产生的（心跳没跑到、重连抢了昵称、清理提前 return），
    /// 只要房间槽位里的 Player 已经和"活着的会话"对不上，就把它清掉；房间空了就销毁。
    ///
    /// 判定"死槽位"的三种情况：
    ///   1. Player.Session 为空；
    ///   2. Session 已断开 / socket 已关；
    ///   3. 昵称索引已经不指向这个 Session（说明已被新会话接管）。
    /// </summary>
    public static class RoomConsistencyWatcher
    {
        private const int IntervalMs = 30000;
        private static Timer _timer;
        private static volatile bool _running;
        private static readonly object _lock = new object();

        public static void Start()
        {
            lock (_lock)
            {
                if (_running) return;
                _running = true;
                _timer = new Timer(Tick, null, IntervalMs, IntervalMs);
                Log.Debug("[Sweep] 房间一致性巡检已启动（每 30 秒）");
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;
                _running = false;
                _timer?.Dispose();
                _timer = null;
                Log.Debug("[Sweep] 房间一致性巡检已停止");
            }
        }

        private static void Tick(object state)
        {
            if (!_running) return;
            try
            {
                int removed = 0;
                foreach (var pair in RoomManager._rooms.ToArray())
                {
                    GameRoom room = pair.Value;
                    if (room == null) continue;

                    for (byte s = 0; s < room._slots.Length; s++)
                    {
                        if (room._slots[s] is not Player p) continue;
                        if (string.IsNullOrEmpty(p.Nickname)) continue;

                        string reason = null;
                        SessionGroup session = p.Session;
                        if (session?.Client == null)
                        {
                            reason = "会话为空";
                        }
                        else if (session.Client.mDisconnected != 0)
                        {
                            reason = "会话已断开";
                        }
                        else
                        {
                            SessionGroup indexed = ClientManager.GetParent(p.Nickname);
                            if (indexed == null || !ReferenceEquals(indexed, session))
                            {
                                reason = "昵称索引已被新会话接管";
                            }
                        }

                        if (reason == null) continue;

                        Log.Debug($"[Sweep] 房间 {room.RoomId} 槽位 {s} 清理幽灵玩家 {p.Nickname}（{reason}）");
                        try
                        {
                            RoomManager.RemovePlayer(room.RoomId, s, p.Nickname);
                            removed++;
                        }
                        catch (Exception ex)
                        {
                            Log.Warn($"[Sweep] 清理 {p.Nickname} 失败：{ex.Message}");
                        }
                    }

                    // 房间没人了（且没有观战）→ 销毁
                    try
                    {
                        if (room.GetPlayerCount() == 0 && room.GetOBCount() == 0)
                        {
                            Log.Debug($"[Sweep] 房间 {room.RoomId} 已空，销毁");
                            RoomManager.RemoveRoom(room);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"[Sweep] 销毁空房 {room.RoomId} 失败：{ex.Message}");
                    }
                }

                if (removed > 0)
                {
                    Log.Debug($"[Sweep] 本轮共清理 {removed} 个幽灵槽位");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[Sweep] 巡检异常：{ex.Message}");
            }
        }
    }
}
