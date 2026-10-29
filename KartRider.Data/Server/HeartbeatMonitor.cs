using System;
using System.Threading;
using KartRider.Common.Network;

namespace KartRider
{
    /// <summary>
    /// 心跳/掉线检测监控器：
    /// 定期扫描所有客户端会话，对长时间无网络活动的会话强制断开。
    /// 用于解决"玩家掉线（断网/崩溃，无 FIN/RST）不触发 Disconnect，导致幽灵玩家残留在房间内、
    /// 继而引发房间内其他玩家全部掉线"的问题。
    /// </summary>
    public static class HeartbeatMonitor
    {
        private static Timer _timer;
        private static volatile bool _running;
        private static readonly object _lockObj = new object();

        // 扫描间隔（毫秒）
        private const int CheckIntervalMs = 5000;
        // 未登录连接：120 秒无数据强制断开（清理僵尸连接，同时给足登录界面停留时间）
        private const int LoginTimeoutMs = 120000;
        // 房间内玩家：TCP 与 UDP 均无活动 60 秒强制断开（房间/游戏内有周期数据，超时即为掉线）
        private const int InRoomTimeoutMs = 60000;
        // 已登录但不在房间（大厅/聊天室）：300 秒无数据强制断开（长阈值，避免误踢挂机玩家）
        private const int IdleTimeoutMs = 300000;

        // 房间内"快速假死判定"阈值（毫秒）：TCP 与 UDP 双通道均超过该时长无活动即视为假死。
        // 用于游戏开始等待等场景，避免假死玩家阻塞/影响其他玩家；
        // 存活的房间内玩家会持续上报 UDP 时间同步，15 秒足够宽松，不会误判。
        public const long QuickGhostIdleMs = 15000;

        /// <summary>
        /// 启动心跳监控（幂等，可重复调用）
        /// </summary>
        public static void Start()
        {
            lock (_lockObj)
            {
                if (_running)
                {
                    return;
                }
                _running = true;
                _timer = new Timer(MonitorTick, null, CheckIntervalMs, CheckIntervalMs);
                Console.WriteLine($"[Heartbeat] 掉线检测监控已启动，每 {CheckIntervalMs / 1000} 秒扫描一次");
            }
        }

        /// <summary>
        /// 停止心跳监控
        /// </summary>
        public static void Stop()
        {
            lock (_lockObj)
            {
                if (!_running)
                {
                    return;
                }
                _running = false;
                _timer?.Dispose();
                _timer = null;
                Console.WriteLine("[Heartbeat] 掉线检测监控已停止");
            }
        }

        /// <summary>
        /// 快速判断玩家是否仍存活：
        /// 会话不存在、已标记断开、或 TCP/UDP 双通道近期均无活动时视为假死。
        /// 任一通道近期有活动即认为存活。
        /// </summary>
        public static bool IsPlayerAlive(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return false;
            }

            SessionGroup group = ClientManager.GetParent(nickname);
            Session session = group?.Client;
            if (session == null)
            {
                return false;
            }
            if (session.mDisconnected != 0)
            {
                return false;
            }

            long now = Environment.TickCount64;
            long tcpIdle = now - session.LastReceiveTime;
            long udpIdle = UdpServer.GetUdpIdle(nickname, now);
            // 任一通道近期有活动即视为存活（存活的房间内玩家会持续上报 UDP 时间同步）
            return tcpIdle < QuickGhostIdleMs || udpIdle < QuickGhostIdleMs;
        }

        private static void MonitorTick(object state)
        {
            if (!_running)
            {
                return;
            }
            try
            {
                long now = Environment.TickCount64;
                foreach (SessionGroup group in ClientManager.GetClients())
                {
                    if (group?.Client == null)
                    {
                        continue;
                    }
                    Session session = group.Client;
                    // 已在断开流程中的会话跳过
                    if (session.mDisconnected != 0)
                    {
                        continue;
                    }

                    string nickname = session.Nickname;
                    long idle = now - session.LastReceiveTime;
                    int timeoutMs;

                    if (string.IsNullOrEmpty(nickname))
                    {
                        // 未登录连接：给足登录时间，超时强制断开
                        timeoutMs = LoginTimeoutMs;
                    }
                    else
                    {
                        int roomId = RoomManager.TryGetRoomId(nickname);
                        if (roomId != -1)
                        {
                            // 房间内（含游戏中）：TCP 或 UDP 任一通道仍在活动即视为存活，
                            // 双通道都空闲才判定掉线，避免误踢仅依赖单一通道的玩家
                            long udpIdle = UdpServer.GetUdpIdle(nickname, now);
                            if (idle < InRoomTimeoutMs || udpIdle < InRoomTimeoutMs)
                            {
                                continue;
                            }
                            timeoutMs = InRoomTimeoutMs;
                        }
                        else
                        {
                            // 大厅/聊天室：保守长超时
                            timeoutMs = IdleTimeoutMs;
                        }
                    }

                    if (idle >= timeoutMs)
                    {
                        Console.WriteLine($"[Heartbeat] 玩家 {nickname} 超时无网络活动（{idle / 1000}s），强制断开连接");
                        try
                        {
                            // Disconnect 内部会触发 OnDisconnect -> ClientManager.RemoveClient，
                            // 自动将该玩家从房间、玩家映射中移除，避免幽灵玩家残留
                            session.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Heartbeat] 强制断开 {nickname} 失败：{ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Heartbeat] 扫描异常：{ex.Message}");
            }
        }
    }
}
