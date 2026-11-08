using ExcData;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using Profile;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using System.Security;
using System.Data.Common;
using System.Windows.Forms;

namespace KartRider;

public static class MultyPlayer
{
    public static Dictionary<short, AICharacter> aiCharacterDict = new Dictionary<short, AICharacter>();
    public static Dictionary<short, AIKart> aiKartDict = new Dictionary<short, AIKart>();
    public static Dictionary<string, byte> StartTimeAttack = new Dictionary<string, byte>();
    public static int[] teamPoints = { 10, 8, 6, 5, 4, 3, 2, 1 };

    /// <summary>双人组队专用名次分（第 3 名 6→7）。普通组队/个人仍用 teamPoints。</summary>
    public static int[] duoTeamPoints = { 10, 8, 7, 5, 4, 3, 2, 1 };

    /// <summary>按房间模式取名次分；名次超出表长返回 0（避免满员+AI 时越界）。</summary>
    static int RankPoint(GameRoom room, int rank)
    {
        int[] table = room != null && room.DuoTeam ? duoTeamPoints : teamPoints;
        return rank >= 0 && rank < table.Length ? table[rank] : 0;
    }

    // 记录正在等待全员就绪的房间，防止同一房间重复启动检测定时器（多个玩家可能同时触发 Start）
    private static readonly ConcurrentDictionary<GameRoom, byte> _readyCheckRooms = new ConcurrentDictionary<GameRoom, byte>();

    public static IPEndPoint GetServerEndPoint(SessionGroup Parent)
    {
        IPEndPoint serverEndPoint = Parent.Client.Socket.LocalEndPoint as IPEndPoint;
        IPAddress clientEndPoint = ((IPEndPoint)Parent.Client.Socket.RemoteEndPoint).Address;

        // ServerIP 由 RouterListener.Start() 后台异步填充；
        // 若尚未填充完成则走下方回退分支（配置 IP），绝不在通讯路径上同步等待网络请求
        bool isLocalIP;
        lock (RouterListener.RouterIPList)
        {
            isLocalIP = RouterListener.RouterIPList.Contains(clientEndPoint.ToString());
        }
        if (isLocalIP || LanIpGetter.IsInLocalSubnet(clientEndPoint.ToString()))
        {
            return serverEndPoint;
        }
        else if (!string.IsNullOrEmpty(RouterListener.ServerIP))
        {
            int serverPort = ProfileService.SettingConfig.ServerPort;
            return new IPEndPoint(IPAddress.Parse(RouterListener.ServerIP), serverPort);
        }
        else
        {
            string serverIP = LanIpGetter.IsIPv6(ProfileService.SettingConfig.ServerIP) ? "127.0.0.1" : ProfileService.SettingConfig.ServerIP;
            int serverPort = ProfileService.SettingConfig.ServerPort;
            return new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        }
    }

    public static uint ConvertTick()
    {
        // 1. 先处理负数（TickCount64理论上不会为负，但做防御性判断）
        if (Environment.TickCount64 < 0)
        {
            return 0; // 或根据需求返回uint.MaxValue，TickCount64实际不会为负
        }

        // 2. 判断是否超出uint范围（uint.MaxValue是4294967295）
        if (Environment.TickCount64 > uint.MaxValue)
        {
            return (uint)(Environment.TickCount64 % uint.MaxValue); // 溢出时返回余数
        }

        // 3. 未溢出则直接转换
        return (uint)Environment.TickCount64;
    }

    public static Dictionary<int, int> GetAllRanks(Dictionary<int, uint> timeData)
    {
        if (timeData.Count == 0)
            return new Dictionary<int, int>();

        // 按值降序排序（值越大排名越靠前）
        var sortedItems = timeData
            .OrderBy(item => item.Value)
            .ToList();

        var ranks = new Dictionary<int, int>();

        // 排名从0开始，逐个分配（相同值也会依次+1）
        for (int i = 0; i < sortedItems.Count; i++)
        {
            ranks[sortedItems[i].Key] = i; // 直接使用索引作为排名
        }

        return ranks;
    }

    internal static void Start(SessionGroup Parent, int roomId)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Log.Info($"房间 {roomId} 不存在");
            return;
        }

        // 双人房：按「颜色」统计人数（颜色 = 队伍），各队人数必须一致且至少 2 队。
        // 单人房（只有 1 名玩家）跳过校验。必须在 arm 之前判定，否则校验失败后无法重试。
        if (room.DuoTeam)
        {
            int duoPlayers = room.GetPlayerCount();
            var colorCounts = new List<int>();
            for (byte c = 1; c <= 4; c++)
            {
                int n = room.GetColorCount(c);
                if (n > 0)
                {
                    colorCounts.Add(n);
                }
            }
            bool unbalanced = colorCounts.Count < 2 || colorCounts.Distinct().Count() > 1;
            if (duoPlayers > 1 && unbalanced)
            {
                using (OutPacket denyPacket = new OutPacket("GrReplyStartPacket"))
                {
                    denyPacket.WriteInt(18);
                    Parent.Client.Send(denyPacket);
                }
                return;
            }
        }

        // 全员准备校验：没准备齐就绝不进等待流程（必须在 arm 之前判定，否则失败后无法重试）
        if (!EnsureAllPlayersReady(room, Parent, "GameControl/Start"))
        {
            return;
        }

        // 每个房间每局只 arm 一次：多名玩家同时点开始 / 各发一次 GameControlPacket 时，
        // 避免重复创建定时器、重复清 Ready（后者会让先到的就绪信号丢失）。
        if (Interlocked.CompareExchange(ref room.StartArmed, 1, 0) != 0)
        {
            Log.Debug($"[Start] 房间 {roomId} 已在等待开局，忽略重复触发");
            return;
        }

        room.Ready.Clear();
        foreach (var player in room._slots)
        {
            if (player is Player p && !string.IsNullOrEmpty(p.Nickname))
            {
                room.Ready[p.Nickname] = false;
            }
        }
        foreach (var player in room.ObIDs)
        {
            if (player is Player p && !string.IsNullOrEmpty(p.Nickname))
            {
                room.Ready[p.Nickname] = false;
            }
        }

        // 全员「准备」已在校验里通过；这里再等各客户端"加载就绪"（ReadinessCollect），
        // 由 Set_startTrigger 统一调度：下限 StartDelayMs + 全员就绪即走 + 上限 StartReadyTimeoutMs。
        Log.Info($"[Start] 房间 {roomId} 全员已准备，进入开局等待（下限 {ProfileService.SettingConfig.StartDelayMs}ms）");
        Set_startTrigger(Parent, room);
    }

    // 检查房间内所有存活玩家是否已就绪；假死玩家（TCP/UDP 双通道近期无活动）未就绪不阻塞开始
    static bool IsAllReady(GameRoom room)
    {
        try
        {
            foreach (KeyValuePair<string, bool> kv in room.Ready)
            {
                // 假死玩家未就绪不影响其他玩家，直接跳过
                if (!kv.Value && HeartbeatMonitor.IsPlayerAlive(kv.Key))
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            // 玩家就绪包并发更新字典导致遍历异常时，返回 false 让下一轮重试
            Log.Info($"就绪状态检查异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>未满足开局条件时回给客户端的拒绝码（与旧版 GrSessionDataPacket 的门槛一致）。</summary>
    private const int DenyStartDenied = 2;

    /// <summary>
    /// 开局校验（对齐旧版 Launcher_P3563 的 GrSessionDataPacket 门槛）：
    /// 房间内每个真人必须是「已准备」（PlayerType = 3），房主除外；观战位与 AI 不参与。
    /// 任何非 3 的状态（含 2 房主值 / 5 未准备 / 客户端没上报的状态）都算没准备——
    /// 旧版就是"不等于 3 即拦"，只在明确等于 5 时拦会漏掉大多数客户端真实上报值。
    /// 返回 false 时已经给请求方回了拒绝包，调用方必须直接 return，不能再往下走开局流程。
    /// </summary>
    internal static bool EnsureAllPlayersReady(GameRoom room, SessionGroup Parent, string path)
    {
        if (room == null)
        {
            return false;
        }

        int readyCount = 0;
        List<string> notReady = new List<string>();
        foreach (RoomMember member in room._IDs)
        {
            if (member is not Player p || string.IsNullOrEmpty(p.Nickname))
            {
                continue;
            }
            // 房主自己不需要准备
            if (p.ID == room.RoomMaster || p.PlayerType == GameRoom.PlayerTypeReady)
            {
                readyCount++;
                continue;
            }
            // 连接已断的槽位不阻塞开局（由心跳/一致性巡检清理，避免整房被幽灵卡死）
            if (p.Session?.Client == null || p.Session.Client.mDisconnected != 0)
            {
                readyCount++;
                continue;
            }
            notReady.Add($"{p.Nickname}(state={p.PlayerType})");
        }

        int playerCount = room.GetPlayerCount();
        if (playerCount < 1 || readyCount >= playerCount)
        {
            return true;
        }

        Log.Info($"[Start] 房间 {room.RoomId} 拒绝开局（{path}）：未准备 {string.Join(", ", notReady)}，ready={readyCount}/{playerCount}");
        if (Parent?.Client != null)
        {
            using (OutPacket deny = new OutPacket("GrReplyStartPacket"))
            {
                deny.WriteInt(DenyStartDenied);
                Parent.Client.Send(deny);
            }
        }
        return false;
    }

    /// <summary>
    /// 开局调度：固定下限（StartDelayMs）+ 全员就绪即走 + 超时上限（StartReadyTimeoutMs）。
    /// 超时仍未就绪的玩家按掉线移除；房间无真人则取消开局并销毁房间。
    /// </summary>
    static void Set_startTrigger(SessionGroup Parent, GameRoom room)
    {
        int delayMs = Math.Clamp(ProfileService.SettingConfig.StartDelayMs, 0, 10000);
        int timeoutMs = Math.Clamp(ProfileService.SettingConfig.StartReadyTimeoutMs, delayMs, 60000);

        // 等待期就绪采集：先清空，再靠“客户端任意包”置位
        room.Ready.Clear();
        foreach (RoomMember member in room._slots)
        {
            if (member is Player pl && !string.IsNullOrEmpty(pl.Nickname))
            {
                room.Ready[pl.Nickname] = false;
            }
        }
        room.ReadinessCollect = true;

        var timer = new System.Timers.Timer();
        timer.Interval = 200;
        timer.AutoReset = true;
        long startTicks = Environment.TickCount64;
        bool sawSignal = false;
        long lastLog = 0;

        void Finish(string reason)
        {
            room.ReadinessCollect = false;
            try
            {
                timer.Stop();
                timer.Dispose();
            }
            catch
            {
            }
            Log.Info($"[Start] 房间 {room.RoomId} 开局（{reason}，等待 {Environment.TickCount64 - startTicks}ms）");
            startTrigger(Parent, room, null, null);
        }

        timer.Elapsed += (s, e) =>
        {
            try
            {
                if (RoomManager.GetRoom(room.RoomId) != room)
                {
                    timer.Stop();
                    timer.Dispose();
                    return;
                }

                long elapsed = Environment.TickCount64 - startTicks;
                if (room.Ready.Values.Any(v => v))
                {
                    sawSignal = true;
                }
                bool allReady = IsAllReady(room);

                // 下限已到：全员就绪直接走；若始终没有任何就绪信号，按下限直接走（避免硬等 15 秒）
                if (elapsed >= delayMs && (allReady || !sawSignal))
                {
                    Finish(allReady ? "全员就绪" : "无就绪信号，按下限开局");
                    return;
                }

                // 超时：未就绪的按掉线剔除
                if (elapsed >= timeoutMs)
                {
                    KickNotReady(room);
                    if (room.GetPlayerCount() == 0)
                    {
                        timer.Stop();
                        timer.Dispose();
                        room.ReadinessCollect = false;
                        room.Started = false;
                        room.StartedPlayerCount = 0;
                        Log.Warn($"[Start] 房间 {room.RoomId} 超时后已无真人，取消开局");
                        RoomManager.RemoveRoom(room);
                        return;
                    }
                    Finish("超时剔除未就绪玩家");
                    return;
                }

                if (elapsed - lastLog >= 1000)
                {
                    lastLog = elapsed;
                    int readyCount = room.Ready.Values.Count(v => v);
                    Log.Debug($"[Start] 就绪进度 ready={readyCount}/{room.Ready.Count} elapsed={elapsed}ms");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[Start] 就绪检测异常，直接开局", ex);
                Finish("异常兜底");
            }
        };

        timer.Start();
    }

    /// <summary>开局超时：未就绪玩家按掉线移出房间（保留连接），并处理房主转移。</summary>
    static void KickNotReady(GameRoom room)
    {
        foreach (var pair in room.Ready.ToArray())
        {
            if (pair.Value)
            {
                continue;
            }
            string nickname = pair.Key;
            // 只剔除确实没有活动的玩家：加载慢但有 TCP/UDP 心跳的人不能踢
            if (HeartbeatMonitor.IsPlayerAlive(nickname))
            {
                continue;
            }
            int slotId = RoomManager.GetPlayerSlotId(room.RoomId, nickname);
            if (slotId == -1)
            {
                room.Ready.TryRemove(nickname, out _);
                continue;
            }
            Log.Warn($"[Start] 房间 {room.RoomId} 玩家 {nickname} 未就绪，按掉线移出房间");
            RoomManager.RemovePlayer(room.RoomId, (byte)slotId, nickname);
            room.Ready.TryRemove(nickname, out _);
        }

        // 房主被移除时，把房主转给剩余玩家中 slot 最小者
        bool masterAlive = room._slots.Any(m => m is Player p && p.ID == room.RoomMaster);
        if (!masterAlive && room.GetPlayerCount() > 0)
        {
            Player next = room._slots.OfType<Player>().OrderBy(p => p.ID).FirstOrDefault();
            if (next != null)
            {
                room.RoomMaster = next.ID;
                next.PlayerType = GameRoom.PlayerTypeRoomMaster;
                Log.Info($"[Start] 房主已转移给 {next.Nickname}");
            }
        }
    }

    static void startTrigger(SessionGroup Parent, GameRoom room, object sender, System.Timers.ElapsedEventArgs e)
    {
        if (room.StartTicks != 0)
        {
            Log.Info($"startTrigger: room.StartTicks 已经设置,跳过执行");
            return;
        }
        room.StartTicks = ConvertTick() + 3000;
        room.TimeData.Clear();
        room.Ranking.Clear();
        room.EndTicks = 0;
        room.SettleTriggered = false;
        room.TeamGauge.Clear();
        room.Ready.Clear();
        room.ResetModeScores();

        // 挑战者模式由指定的 AI 主机模拟 AI（抓包值 = 房内最后的真人 ID）；
        // 其余模式保持原来的 0。
        int aiMaster = 0;
        if (GameModes.IsChallenger(room.GameType))
        {
            foreach (RoomMember member in room._slots)
            {
                if (member is Player p && p.ID > aiMaster)
                {
                    aiMaster = p.ID;
                }
            }
        }
        using (OutPacket oPacket = new OutPacket("GameAiMasterSlotNoticePacket"))
        {
            oPacket.WriteInt(aiMaster);
            BroadCast(room.RoomId, oPacket);
        }
        using (OutPacket oPacket = new OutPacket("GameControlPacket"))
        {
            oPacket.WriteInt(1);
            oPacket.WriteUInt(room.StartTicks);
            BroadCast(room.RoomId, oPacket);
        }

        // 夺旗赛：抓包中服务端在开局后立即下发 0:0 初始化比分板
        if (GameModes.IsTeamFlagMode(room.GameType))
        {
            BroadcastFlagScore(room);
        }
        ArmModeDeadline(room);
        Log.Debug($"StartTicks = {room.StartTicks}");
    }

    /// <summary>夺旗赛比分广播（GameFlagSetScorePacket：红队夺旗数、蓝队夺旗数）。</summary>
    internal static void BroadcastFlagScore(GameRoom room)
    {
        using (OutPacket oPacket = new OutPacket("GameFlagSetScorePacket"))
        {
            oPacket.WriteUInt((uint)Math.Max(0, room.RedFlagScore));
            oPacket.WriteUInt((uint)Math.Max(0, room.BlueFlagScore));
            BroadCast(room.RoomId, oPacket);
        }
    }

    /// <summary>
    /// 夺旗赛/死斗没有冲线，正赛时长由服务端计时：
    /// 到时下发 GameControl(3)，客户端回包后走原有结算流程。
    /// </summary>
    internal static void ArmModeDeadline(GameRoom room)
    {
        int duration = GameModes.RaceDurationMs(room.GameType);
        if (duration <= 0)
        {
            return;
        }
        if (Interlocked.CompareExchange(ref room.ModeDeadlineArmed, 1, 0) != 0)
        {
            return;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(duration + 3000).ConfigureAwait(false);
            }
            catch
            {
                return;
            }
            EndModeRace(room, "时间到");
        });
    }

    /// <summary>结束积分制比赛（夺旗赛打平会先进入加时）。</summary>
    internal static void EndModeRace(GameRoom room, string reason)
    {
        if (room == null || RoomManager.GetRoom(room.RoomId) != room)
        {
            return;
        }
        lock (room.Sync)
        {
            if (!room.Started || room.SettleTriggered || room.EndTicks != 0)
            {
                return;
            }

            if (GameModes.IsTeamFlagMode(room.GameType) && !room.FlagOvertime
                && room.RedFlagScore == room.BlueFlagScore)
            {
                // 正赛打平：GameControl state=6 触发官方的 flag_draw 流程，比分清零重新计时
                room.FlagRegulationTotal = room.RedFlagScore + room.BlueFlagScore;
                room.FlagOvertime = true;
                room.RedFlagScore = 0;
                room.BlueFlagScore = 0;
                uint overtimeTick = ConvertTick() + 2000 + 3000;
                using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                {
                    oPacket.WriteInt(6);
                    oPacket.WriteUInt(overtimeTick);
                    BroadCast(room.RoomId, oPacket);
                }
                BroadcastFlagScore(room);
                Interlocked.Exchange(ref room.ModeDeadlineArmed, 0);
                ArmModeDeadline(room);
                Log.Info($"[Mode] 房间 {room.RoomId} 夺旗赛正赛打平，进入加时 startTick={overtimeTick}");
                return;
            }

            room.SnapshotMembers = DeepCopyMembers(room._IDs);
            if (!room.FlagOvertime)
            {
                // 正赛直接结束时同样记下总夺旗数（加时结束时保留正赛的值）
                room.FlagRegulationTotal = room.RedFlagScore + room.BlueFlagScore;
            }
            room.EndTicks = ConvertTick() + 3000;
            using (OutPacket oPacket = new OutPacket("GameControlPacket"))
            {
                oPacket.WriteInt(3);
                oPacket.WriteUInt(room.EndTicks);
                BroadCast(room.RoomId, oPacket);
            }
            Log.Info($"[Mode] 房间 {room.RoomId} {GameModes.Name(room.GameType)} 结束（{reason}）：" +
                     $"夺旗 {room.RedFlagScore}:{room.BlueFlagScore} 死斗 {room.DeathMatchRedScore}:{room.DeathMatchBlueScore} 加时={room.FlagOvertime}");
            // 积分制模式没有冲线时间可等，客户端收到 GameControl(3) 回报 state=2 后要立刻结算，
            // 否则客户端已经在结算时间点把界面建好，只显示默认的 0:0。定时器仅作兜底。
            Set_settleTrigger(room, GameModes.IsScoreMode(room.GameType) ? 4000 : 10000);
        }
    }

    internal static void Set_settleTrigger(GameRoom room, int intervalMs = 10000)
    {
        var onceTimer = new System.Timers.Timer();
        onceTimer.Interval = intervalMs;
        onceTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, _event) => settleTrigger(room, s, _event));
        onceTimer.AutoReset = false;
        onceTimer.Start();
    }

    static void settleTrigger(GameRoom room, object sender, System.Timers.ElapsedEventArgs e)
    {
        SettleRoom(room);
    }

    /// <summary>
    /// 结算一个房间：先发 GameNextStage + GameResult，最后才发 GameControl(state=4)。
    /// 抓包（夺旗赛/死斗/挑战者）都是这个顺序；反过来先发 state=4 时，客户端会用默认值
    /// 把结算阶段建好，夺旗赛的队伍比分就变成 0:0。
    /// </summary>
    internal static void SettleRoom(GameRoom room)
    {
        if (room == null)
        {
            return;
        }

        lock (room.Sync)
        {
            if (room.SettleTriggered)
            {
                return;
            }
            room.SettleTriggered = true;
        }

        bool scoreMode = GameModes.IsScoreMode(room.GameType);
        bool teamFlag = GameModes.IsTeamFlagMode(room.GameType);
        var menbers = room.SnapshotMembers ?? room._IDs; // 优先用结束瞬间快照
        var timeData = new Dictionary<int, uint>(room.TimeData);
        room.SnapshotMembers = null; // 用后清除

        uint settleTick = room.EndTicks + 6000;
        int retryRoomId = room.RoomId;
        byte retryMode = room.ModeByte;

        InitRoom(room);

        if (scoreMode)
        {
            // 结算前再同步一次最终比分，保证客户端进结算阶段时缓存的是终局比分
            if (teamFlag)
            {
                BroadcastFlagScore(room);
            }
            GameResultPacket(room, menbers, timeData);
        }

        using (OutPacket outPacket = new OutPacket("GameControlPacket"))
        {
            outPacket.WriteInt(4);
            outPacket.WriteUInt(settleTick);
            BroadCast(retryRoomId, outPacket);
        }

        if (!scoreMode)
        {
            GameResultPacket(room, menbers, timeData);
        }
        else if (teamFlag)
        {
            // 部分客户端在 state=4 之后才创建结算界面，这里补一帧终局比分
            BroadcastFlagScore(room);
        }

        int firstID = room.Ranking.FirstOrDefault(x => x.Value == 0).Key;
        if (room.RoomMaster < 8 && RoomManager.TryGetIdDetail(room.RoomId, firstID) is Player p1)
        {
            room.RoomMaster = firstID;
            p1.PlayerType = GameRoom.PlayerTypeRoomMaster;
        }
        else if (room.GetOBCount() < 1 && RoomManager.TryGetIdDetail(room.RoomId, firstID) is Player p2)
        {
            room.RoomMaster = firstID;
            p2.PlayerType = GameRoom.PlayerTypeRoomMaster;
        }
        Log.Info($"EndTicks = {room.EndTicks + 6000}");
    }

    /// <summary>
    /// 建房失败必须回包。客户端发完 ChCreateRoomRequestPacket 后一直在等 ChCreateRoomReplyPacket，
    /// 静默 return 会让客户端永远卡在"创建中"，表现就是第二次建房（房主还没从上个房间的映射里清干净）
    /// 时游戏卡死/崩溃。同时把刚创建出来的空房间回收掉，避免残留幽灵房。
    /// </summary>
    internal static void ChCreateRoomFailReply(SessionGroup Parent, GameRoom room, byte gameType)
    {
        RoomManager.RemoveRoom(room);
        using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
        {
            oPacket.WriteByte(0);
            oPacket.WriteByte(0);
            oPacket.WriteByte(0);
            oPacket.WriteEncByte(gameType);
            Parent.Client.Send(oPacket);
        }
    }

    public static void GrSlotDataPacket(int roomId)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null || room.Started)
        {
            return;
        }
        using (OutPacket outPacket = new OutPacket("GrSlotDataPacket"))
        {
            GrSlotDataPacket(roomId, outPacket);
            BroadCast(roomId, outPacket);
        }
    }

    internal static void StopGame(int roomId, SessionGroup Parent)
    {
        var room = RoomManager.GetRoom(roomId);
        using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
        {
            outPacket.WriteString("结束游戏");
            BroadCast(roomId, outPacket, Parent.Client.Nickname);
        }
        using (OutPacket outPacket = new OutPacket("GameControlPacket"))
        {
            outPacket.WriteInt(4);
            outPacket.WriteUInt(ConvertTick());
            BroadCast(roomId, outPacket);
        }
        InitRoom(room);
        using (OutPacket outPacket = new OutPacket("GameResultPacket"))
        {
            outPacket.WriteByte(0);
            outPacket.WriteInt(0); // player count
            outPacket.WriteInt(0); // AI count
            outPacket.WriteBytes(new byte[34]);
            outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00");
            BroadCast(roomId, outPacket);
        }
    }

    static void InitRoom(GameRoom room)
    {
        foreach (RoomMember member in room._IDs)
        {
            if (member is Player p)
            {
                p.PlayerType = 2;
                p.LastPacketReceived = 0;
            }
        }
        foreach (RoomMember member in room.ObIDs)
        {
            if (member is Player p)
            {
                p.LastPacketReceived = 0;
            }
        }
        room.Started = false;
        room.StartTicks = 0;
        room.StartedPlayerCount = 0;
        Interlocked.Exchange(ref room.StartArmed, 0);
        room.Ready.Clear();
        room.TimeData.Clear();
        room.Ranking.Clear();
        room.redGauge = 0f;
        room.blueGauge = 0f;
        // 注意：夺旗/死斗积分必须保留到 GameResultPacket 结算之后再清理，
        // 因此这里不调用 ResetModeScores（下一局 startTrigger 会重置）。
        Interlocked.Exchange(ref room.StartGate, 0);
    }

    static void GrSlotDataPacket(int roomId, OutPacket outPacket, bool enter = false, string nickname = "", uint? trackOverride = null)
    {
        bool ob = false;
        if (!string.IsNullOrEmpty(nickname))
        {
            var profileConfig = Profile.ProfileService.GetProfileConfig(nickname);
            if (profileConfig?.Rider != null)
            {
                uint pmap = profileConfig.Rider.pmap;
                ob = (pmap == 718 || pmap == 590);
            }
        }

        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }

        outPacket.WriteUInt(trackOverride ?? room.track); // track name hash（开局包用实际随机到的地图）
        outPacket.WriteInt(0);
        outPacket.WriteBytes(room.RoomData); // 32
        outPacket.WriteInt(room.RoomMaster); // RoomMaster

        outPacket.WriteBytes(new byte[11]);
        outPacket.WriteInt(room.CloseSlotIds.Count); // 房间格子锁定数量 格子ID byte
        foreach (var slotId in room.CloseSlotIds)
        {
            outPacket.WriteByte(slotId);
        }
        outPacket.WriteBytes(new byte[16]);

        /* ---- Player ---- */
        foreach (RoomMember member in room._IDs)
        {
            if (member is Player p)
            {
                var rawConfig = ProfileService.GetProfileConfig(p.Nickname);
                // 档案反序列化不完整（Rider/RiderItem 缺失）时用空档案兜底，仅对缺失内容补默认值
                bool profileComplete = rawConfig?.Rider != null && rawConfig?.RiderItem != null;
                var pConfig = profileComplete ? rawConfig : new ProfileConfig();
                if (!profileComplete)
                {
                    Log.Info($"[GrSlotDataPacket] Warning: ProfileConfig incomplete for player {p.Nickname}, using defaults");
                }

                if (Log.IsPacketTraceEnabled)
                {
                    Log.Debug($"Player Nickname = {p.Nickname}, ID = {p.ID}, SlotId = {p.SlotId}");
                }
                if (enter)
                {
                    outPacket.WriteInt(3);
                }
                else
                {
                    outPacket.WriteInt(p.PlayerType); // Player Type, 2 = RoomMaster, 3 = AutoReady, 4 = Observer, 5 = Preparing, 7 = AI
                }
                outPacket.WriteUInt(ClientManager.GetUserNO(p.Nickname));
                IPEndPoint client = ClientManager.ClientToIPEndPoint(pConfig.Rider.ClientId);
                // 端点无法解析时仅对该字段补占位，其余内容照常写入
                outPacket.WriteEndPoint(client == null ? new IPEndPoint(IPAddress.Any, 0) : new IPEndPoint(client.Address, pConfig.Rider.P2pPort));
                outPacket.WriteEndPoint(new IPEndPoint(IPAddress.Any, 0));
                // 房间名关键字功能已移除：不再识别“比赛”（隐藏昵称/装备）
                outPacket.WriteString(p.Nickname);
                outPacket.WriteShort(pConfig.Rider.Emblem1);
                outPacket.WriteShort(pConfig.Rider.Emblem2);
                outPacket.WriteShort(0);
                GameSupport.GetRider(p.Nickname, outPacket);
                outPacket.WriteString(pConfig.Rider.Card);
                outPacket.WriteUInt(pConfig.Rider.RP);
                if (room.IsTeamMode || GameModes.IsChallenger(room.GameType))
                {
                    outPacket.WriteByte(p.Team);
                }
                else
                {
                    outPacket.WriteByte(0);
                }

                if (room.Ranking.ContainsKey(p.ID))
                {
                    outPacket.WriteInt(room.Ranking[p.ID]);
                }
                else
                {
                    int nextValue = room.Ranking.Count;
                    room.Ranking[p.ID] = nextValue;
                    outPacket.WriteInt(nextValue);
                }

                outPacket.WriteBytes(new byte[30]);

                outPacket.WriteInt(1500);
                outPacket.WriteInt(1499);
                outPacket.WriteInt(0);
                outPacket.WriteInt(2000);
                outPacket.WriteInt(5);
                outPacket.WriteHexString("FF 00 00 00");

                outPacket.WriteByte(pConfig.Rider.LicenseLevel); // 玩家真实驾照等级
                if (pConfig.Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteString("");
                    outPacket.WriteInt(0);
                }
                else
                {
                    outPacket.WriteString(pConfig.Rider.ClubName);
                    outPacket.WriteInt(pConfig.Rider.ClubMark_LOGO);
                }
                outPacket.WriteBytes(new byte[19]);
            }
            else if (member is Ai a)
            {
                Log.Debug($"Ai ID = {a.ID}, SlotId = {a.SlotId}");
                outPacket.WriteInt(7);
                outPacket.WriteShort(a.Character);
                outPacket.WriteShort(a.Rid);
                outPacket.WriteShort(a.Kart);
                outPacket.WriteShort(a.Balloon);
                outPacket.WriteShort(a.HeadBand);
                outPacket.WriteShort(a.Goggle);
                if (room.IsTeamMode || GameModes.IsChallenger(room.GameType))
                {
                    outPacket.WriteByte(a.Team);
                }
                else
                {
                    outPacket.WriteByte(0);
                }
            }
            else if (member is Close close)
            {
                outPacket.WriteInt(close.PlayerType);
            }
            else
            {
                outPacket.WriteInt(0);
            }
        }

        /* ---- Observer ---- */
        foreach (RoomMember member in room.ObIDs)
        {
            if (member is Player p)
            {
                var rawConfig = ProfileService.GetProfileConfig(p.Nickname);
                // 档案反序列化不完整时用空档案兜底，仅对缺失内容补默认值
                bool profileComplete = rawConfig?.Rider != null;
                var pConfig = profileComplete ? rawConfig : new ProfileConfig();
                if (!profileComplete)
                {
                    Log.Info($"[GrSlotDataPacket] Warning: ProfileConfig incomplete for observer {p.Nickname}, using defaults");
                }
                outPacket.WriteInt(p.PlayerType);
                outPacket.WriteUInt(ClientManager.GetUserNO(p.Nickname));
                IPEndPoint client = ClientManager.ClientToIPEndPoint(pConfig.Rider.ClientId);
                // 端点无法解析时仅对该字段补占位，其余内容照常写入
                outPacket.WriteEndPoint(client == null ? new IPEndPoint(IPAddress.Any, 0) : new IPEndPoint(client.Address, pConfig.Rider.P2pPort));
                outPacket.WriteEndPoint(new IPEndPoint(IPAddress.Any, 0));
                outPacket.WriteString(p.Nickname);
            }
            else
            {
                outPacket.WriteInt(0);
            }
        }

        Position(roomId, outPacket);
    }

    internal static void GrSessionDataPacket(SessionGroup Parent)
    {
        int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }

        int playerCount = room.GetPlayerCount();
        int totalMembers = room.GetCount();

        // 按需求放开所有人数限制：组队/个人都不再校验参赛人数与队伍平衡，
        // 单人（含个人竞速赛单跑）也能直接开局。
        Log.Info($"[Start] 房间 {room.RoomId} 不校验人数（参赛者 {totalMembers} 人，红队 {room.GetTeamMemberCount(1)} / 蓝队 {room.GetTeamMemberCount(2)}，模式 {room.GameType}），直接开局");

        if (playerCount < 1)
        {
            using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
            {
                oPacket.WriteInt(2);
                Parent.Client.Send(oPacket);
            }
            return;
        }

        // 全员准备校验：还有队友没点准备时不允许开局（单跑/AI 不受影响）
        if (!EnsureAllPlayersReady(room, Parent, "GrRequestStart"))
        {
            return;
        }

        // 原子开局门闩：两人同时点开始、或客户端重发开始包时，只有第一个请求能真正开局。
        // 原实现没有这层保护，两条路径会各自 GetRandomTrack 并覆盖 room.trackTemp，
        // 导致不同客户端进不同赛道。
        if (Interlocked.CompareExchange(ref room.StartGate, 1, 0) != 0)
        {
            Log.Warn($"[Start] 房间 {room.RoomId} 正在开局，忽略重复请求（{Parent.Client.Nickname}）");
            return;
        }

        try
        {
            room.Started = true;
            room.StartedPlayerCount = playerCount;
            RoomScore.OnRaceStart(room);   // 记录本局参赛者，掉线者结算时按未完成计
            room.ResetRareItems(); // 每局重置「天使/道具锁/闪电」计数

            bool ai = room.GetAiCount() > 0;
            uint track = RandomTrack.GetRandomTrack(Parent, $"[{room.RoomName}][{room.RoomId.ToString()}]", room.RandomTrackGameType, room.track, ai, GameModes.TrackGameType(room.GameType));
            room.trackTemp = track;
            // 抽到的地图必须留痕：出问题时能一眼看出是哪张图（随机图导致闪退的排查依赖这条）
            Log.Info($"[Start] 房间 {room.RoomId} 抽到地图：{RandomTrack.GetTrackName(track)} (0x{track:X8})，随机选项={room.track}");

            // AI 参数必须"每局生成一次、所有人复用"：原实现放在按玩家发送的循环里，
            // 每个客户端拿到的 AI 速度都不同，必然不同步。
            room.PrepareAiSpecs();

            using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
            {
                oPacket.WriteInt(0);
                Parent.Client.Send(oPacket);
            }

            foreach (RoomMember member in room.ObIDs)
            {
                if (member is Player p)
                {
                    GrCommandStartPacket(roomId, p);
                }
            }

            foreach (RoomMember member in room._IDs)
            {
                if (member is Player p)
                {
                    GrCommandStartPacket(roomId, p);
                }
            }
        }
        catch (Exception ex)
        {
            // 开局失败时复位房间状态，避免房间永久卡在 Started 导致无法进入/无法解散
            Log.Error($"[GrSessionDataPacket] 开局失败 room={room.RoomId}", ex);
            room.Started = false;
            room.StartedPlayerCount = 0;
        }
        finally
        {
            Interlocked.Exchange(ref room.StartGate, 0);
        }
    }

    static void GrCommandStartPacket(int roomId, Player p)
    {
        var room = RoomManager.GetRoom(roomId);
        // 挑战者模式下发 GrCommandStartAiPacket（抓包确认），正文结构与普通开局包一致
        string startPacketName = GameModes.UsesAiStartPacket(room.GameType)
            ? "GrCommandStartAiPacket"
            : "GrCommandStartPacket";
        using (OutPacket oPacket = new OutPacket(startPacketName))
        {
            oPacket.WriteUInt((uint)PacketName.GrSessionDataPacket);
            GrSessionDataPacket(p.Nickname, oPacket);

            oPacket.WriteUInt((uint)PacketName.GrSlotDataPacket);
            // 夺旗/死斗开局包里的槽位数据必须带“实际随机到的地图”，
            // 否则客户端会按 track=0 自行随机到非本模式地图，进图即闪退。
            bool forceTrackType = GameModes.TrackGameType(room.GameType) != null;
            GrSlotDataPacket(roomId, oPacket, true, p.Nickname, forceTrackType ? room.trackTemp : null);
            oPacket.WriteInt();

            //kart data
            ushort KartID = ProfileService.GetProfileConfig(p.Nickname)?.RiderItem?.Set_Kart ?? 0;
            ushort FlyingPetID = ProfileService.GetProfileConfig(p.Nickname)?.RiderItem?.Set_FlyingPet ?? 0;
            // 房间名关键字功能已移除：不再识别“原版”（全员使用原版车辆数据）
            StartGameData.GetKartSpac(oPacket, p.Nickname, room.SpeedType, KartID, FlyingPetID);

            oPacket.WriteInt(room.GetAiCount()); //AI count
            if (room.GetAiCount() > 0)
            {
                // 复用开局时生成的一份 AI 参数（原实现在这里逐人重新随机 → 各家 AI 速度不一致）
                for (int j = 0; j < room.GetAiCount(); j++)
                {
                    float[] spec = room.GetAiSpec(j);
                    oPacket.WriteEncFloat(spec[0]);
                    oPacket.WriteEncFloat(spec[1]);
                    oPacket.WriteEncFloat(spec[2]);
                    oPacket.WriteEncFloat(spec[3]);
                    oPacket.WriteEncFloat(spec[4]);
                    oPacket.WriteEncFloat(spec[5]);
                }
            }
            oPacket.WriteUInt(room.trackTemp); //track name hash
            oPacket.WriteString("008a000445d4dd3c21fc030e");
            oPacket.WriteInt(10000);

            oPacket.WriteInt();
            oPacket.WriteUInt((uint)PacketName.MissionInfo);
            oPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00");
            if (GameModes.UsesAiStartPacket(room.GameType))
            {
                // GrCommandStartAiPacket 在基础开局包后追加：AI 主机/保留字段 + AI 列表长度
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
            }
            //oPacket.WriteString("[applied param]\r\ntransAccelFactor='1.8555' driftEscapeForce='4720' steerConstraint='24.95' normalBoosterTime='3860' \r\npartsBoosterLock='1' \r\n\r\n[equipped / default parts param]\r\ntransAccelFactor='1.86' driftEscapeForce='2120' steerConstraint='2.7' normalBoosterTime='860' \r\n\r\n\r\n[gamespeed param]\r\ntransAccelFactor='-0.0045' driftEscapeForce='2600' steerConstraint='22.25' normalBoosterTime='3000' \r\n\r\n\r\n[factory enchant param]\r\n");
            Log.Debug($"Track : {RandomTrack.GetTrackName(room.trackTemp)}");
            p.Session.Client.Send(oPacket);
        }
    }

    internal static void GrSessionDataPacket(SessionGroup Parent, string Nickname)
    {
        using (OutPacket oPacket = new OutPacket("GrSessionDataPacket"))
        {
            GrSessionDataPacket(Nickname, oPacket);
            Parent.Client.Send(oPacket);
        }
    }

    static void GrSessionDataPacket(string Nickname, OutPacket outPacket)
    {
        int roomId = RoomManager.TryGetRoomId(Nickname);
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Log.Info($"GetRoom Failed, roomId = {roomId}");
            return;
        }
        outPacket.WriteString(room.RoomName);
        outPacket.WriteString(room.LockPwd);
        outPacket.WriteByte(room.ModeByte);
        outPacket.WriteByte(room.SpeedType); //7
        outPacket.WriteInt(0);
        outPacket.WriteByte(0);
        outPacket.WriteInt(0);
        outPacket.WriteBytes(new byte[7]);
    }

    internal static void ChJoinRoomReplyPacket(SessionGroup Parent, int roomId, String pwd)
    {
        // 1-无法进入房间；2-房间已满；3-密码错误；4-匹配失败；5-创建新房间
        var room = RoomManager.GetRoom(roomId);

        // 观战身份（590 观战者 / 718 观战者超级房主）不校验房间密码：
        // 观战客户端入口不会弹密码框，固定发送空串；若仍走密码校验必然回 3（密码错误），
        // 表现为“进加密房直接提示密码错误、且永远没有输入密码的机会”。
        uint joinerPmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
        bool isObserverJoin = joinerPmap == 590 || joinerPmap == 718;

        if (room == null || room.Started)
        {
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteEncByte(0);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (!isObserverJoin && !string.IsNullOrEmpty(room.LockPwd) && pwd != room.LockPwd)
        {
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(3);
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteEncByte(room.ModeByte);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            // 密码错误不再动房间：
            // 客户端收到 3 后会自己发 ChGetRoomListRequestPacket 刷新房间列表（日志已确认），
            // 原来这里调 RemoveRoom(room) 对非空房间只会给房内玩家多发一次 GrSlotDataPacket，
            // 既不是列表刷新，也没有实际意义。
            return;
        }

        int playerCount = room.GetPlayerCount();
        byte slot = room.DuoTeam
            ? RoomManager.AddDuoPlayer(roomId, Parent.Client.Nickname, 2, Parent)
            : RoomManager.AddPlayer(roomId, Parent.Client.Nickname, 0, 2, Parent);
        Player player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
        if (slot == 255 || player == null)
        {
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(2);
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteEncByte(room.ModeByte);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (room.DuoTeam)
        {
            uint pmapDuo = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
            if (pmapDuo == 718 || (playerCount < 1 && room.RoomMaster < 8))
            {
                room.RoomMaster = player.ID;
                if (player.PlayerType != 4)
                {
                    player.PlayerType = GameRoom.PlayerTypeRoomMaster;
                }
            }
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(0);
                outPacket.WriteByte(1);
                outPacket.WriteByte((byte)(room.DuoTeam ? 4 : 2));
                outPacket.WriteEncByte(room.ModeByte);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (GameModes.IsTeamGameType(room.GameType) || GameModes.IsChallenger(room.GameType))
        {
            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
            if (pmap == 718 || (playerCount < 1 && room.RoomMaster < 8))
            {
                room.RoomMaster = player.ID;
                if (player.PlayerType != 4)
                {
                    player.PlayerType = GameRoom.PlayerTypeRoomMaster;
                }
            }
            if (GameModes.IsChallenger(room.GameType))
            {
                // 挑战者模式：所有人同队（蓝队 2），落 0~3 号格
                player.Team = 2;
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(2);
                    outPacket.WriteByte(2);
                    outPacket.WriteEncByte(room.ModeByte);
                    outPacket.WriteBytes(new byte[5]);
                    Parent.Client.Send(outPacket);
                }
                return;
            }
            if (slot < 4)
            {
                player.Team = 2;
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(player.Team); // 抓包：该字节为入座队伍
                    outPacket.WriteByte(2);
                    outPacket.WriteEncByte(room.ModeByte);
                    outPacket.WriteBytes(new byte[5]);
                    Parent.Client.Send(outPacket);
                }
                return;
            }
            else if (slot > 3 && slot < 8)
            {
                player.Team = 1;
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(player.Team); // 抓包：该字节为入座队伍
                    outPacket.WriteByte(2);
                    outPacket.WriteEncByte(room.ModeByte);
                    outPacket.WriteBytes(new byte[5]);
                    Parent.Client.Send(outPacket);
                }
                return;
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(1);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(0);
                    outPacket.WriteEncByte(room.ModeByte);
                    outPacket.WriteBytes(new byte[5]);
                    Parent.Client.Send(outPacket);
                }
                return;
            }
        }
        else
        {
            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
            if (pmap == 718 || (playerCount < 1 && room.RoomMaster < 8))
            {
                room.RoomMaster = player.ID;
                if (player.PlayerType != 4)
                {
                    player.PlayerType = GameRoom.PlayerTypeRoomMaster;
                }
            }
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(0);
                outPacket.WriteByte(1);
                outPacket.WriteByte(8);
                outPacket.WriteEncByte(room.ModeByte);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
        }
    }

    public static void BroadCast(int roomId, OutPacket outPacket, string Self = "", byte team = 0)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }

        foreach (RoomMember member in room.ObIDs)
        {
            if (member is Player p)
            {
                if (Self != p.Nickname)
                {
                    try
                    {
                        p.Session?.Client?.Send(outPacket);
                    }
                    catch (Exception ex)
                    {
                        Log.Info($"BroadCast OB Send Error [{p.Nickname}]: {ex.Message}");
                    }
                }
            }
        }

        foreach (RoomMember member in room._slots)
        {
            if (member is Player p)
            {
                if (Self != p.Nickname)
                {
                    if (team == 0)
                    {
                        try
                        {
                            p.Session?.Client?.Send(outPacket);
                        }
                        catch (Exception ex)
                        {
                            Log.Info($"BroadCast Send Error [{p.Nickname}]: {ex.Message}");
                        }
                    }
                    else if (p.Team == team)
                    {
                        try
                        {
                            p.Session?.Client?.Send(outPacket);
                        }
                        catch (Exception ex)
                        {
                            Log.Info($"BroadCast TeamSend Error [{p.Nickname}]: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    // 添加指定数量的 Ai
    /// <summary>
    /// 从建房包里读出房间人数。
    ///
    /// 结构解析结果：人数固定位于**包尾第 85 个字节**（raw.Length - 85）。
    /// 实测 2/4/6/8 人房、个人/组队、不同房间名长度都成立（115 字节包 offset=30、113 字节包 offset=28）。
    /// 保留旧的标记扫描作为兜底，两者都失败才回退 8。
    /// </summary>
    /// <summary>
    /// 解析 ChGetRoomListRequestPacket（GetRawBuffer 带 4 字节包头，字段紧随其后）。
    ///
    /// 字段顺序在不同客户端上并不统一，「[页码 int][模式 ushort]」和「[模式 ushort][页码 int]」都存在。
    /// 若把模式号当成页码，就会 Skip(模式号 * 10) 把整页数据跳过去 ——
    /// 表现为房间列表永远空白、别人建的房"看不到"。这里两种解释都试，取合理的那个。
    /// </summary>
    internal static void ParseRoomListRequest(byte[] raw, out int page, out int gameType)
    {
        page = 0;
        gameType = -1;
        if (raw == null || raw.Length < 6)
        {
            return;
        }

        int offset = 4; // 跳过包头
        int avail = raw.Length - offset;
        if (avail <= 0)
        {
            return;
        }

        if (avail >= 6)
        {
            int pageFirst = BitConverter.ToInt32(raw, offset);
            int modeLast = BitConverter.ToUInt16(raw, offset + 4);
            if (IsPlausiblePage(pageFirst) && IsPlausibleGameType(modeLast))
            {
                page = pageFirst;
                gameType = modeLast;
                return;
            }

            int modeFirst = BitConverter.ToUInt16(raw, offset);
            int pageLast = BitConverter.ToInt32(raw, offset + 2);
            if (IsPlausiblePage(pageLast) && IsPlausibleGameType(modeFirst))
            {
                page = pageLast;
                gameType = modeFirst;
                return;
            }
        }

        // 兜底：按「页码 + 模式」的老结构理解
        if (avail >= 4)
        {
            page = BitConverter.ToInt32(raw, offset);
        }
        if (avail >= 6)
        {
            gameType = BitConverter.ToUInt16(raw, offset + 4);
        }
    }

    /// <summary>
    /// 房间列表里给“还没选图”的房间用的占位赛道 hash（优先取一张真实存在的图）。
    /// </summary>
    internal static uint ListFallbackTrack(byte gameType = 0)
    {
        // 夺旗/死斗房间的列表占位图也要用对应类型的地图，避免客户端按房间模式预览时拿到不匹配的图
        string required = GameModes.TrackGameType(gameType);
        if (required != null)
        {
            foreach (var kv in RandomTrack.TrackList)
            {
                if (RandomTrack.IsTrackOfGameType(kv.Key, required))
                {
                    return kv.Key;
                }
            }
        }
        foreach (var kv in RandomTrack.TrackList)
        {
            return kv.Key;
        }
        return Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
    }

    static bool IsPlausiblePage(int page) => page >= 0 && page <= 1000;

    /// <summary>合法模式号：1~99（含双人组队 67/68）；0 与超大值都视为解析错位。</summary>
    static bool IsPlausibleGameType(int gameType) => gameType > 0 && gameType <= 99;

    internal static int ParseMaxPlayers(byte[] raw)
    {
        if (raw == null || raw.Length < 20)
        {
            return 8;
        }

        // 双人模式：7C 70 89 <人数>（优先，避免被后面的通用偏移误判成 8）
        for (int i = 0; i + 3 < raw.Length; i++)
        {
            if (raw[i] == 0x7C && raw[i + 1] == 0x70)
            {
                int off = (raw[i + 2] == 0x89) ? 3 : 2;
                int n = raw[i + off];
                if (n >= 2 && n <= 8)
                {
                    return n;
                }
            }
        }

        // 组队/个人：BA 7C 70 后一字节
        for (int i = 0; i + 4 < raw.Length; i++)
        {
            if (raw[i] == 0xBA && raw[i + 1] == 0x7C && raw[i + 2] == 0x70)
            {
                int n2 = raw[i + 4];
                if (n2 >= 2 && n2 <= 8)
                {
                    return n2;
                }
            }
        }

        // 通用兜底
        if (raw.Length >= 90)
        {
            int value = raw[raw.Length - 85];
            if (value >= 2 && value <= 8) return value;
        }
        return 8;
    }

    /// <summary>
    /// 按房间人数锁定多余席位：
    /// 个人赛锁掉超出人数的格子；组队赛每排 4 格，按每队人数逐排锁尾部的格子
    /// （客户端会按镜像关系展示，锁一侧对面同步显示）。
    /// </summary>
    internal static void ApplyRoomSize(GameRoom room, int maxPlayers)
    {
        if (room == null)
        {
            return;
        }
        // 双人组队：按「队伍=一列（上主下副）」整列关闭，而不是按 slot%4 的组队规则
        if (room.DuoTeam)
        {
            int teams = room.DuoTeamCount;
            for (int team = teams + 1; team <= 4; team++)
            {
                int top = GameRoom.DuoTopSlots[team - 1];
                int bottom = GameRoom.DuoBottomSlots[team - 1];
                if (room._slots[top] == null && room._IDs[top] == null) room.AddClose((byte)top, top);
                if (room._slots[bottom] == null && room._IDs[bottom] == null) room.AddClose((byte)bottom, bottom);
            }
            GrSlotDataPacket(room.RoomId);
            return;
        }

        // 挑战者模式：4 人同队，直接锁掉右半场（4~7 号格）
        if (GameModes.IsChallenger(room.GameType))
        {
            for (byte slot = GameModes.ChallengerMaxPlayers; slot < 8; slot++)
            {
                if (room._slots[slot] == null && room._IDs[slot] == null)
                {
                    room.AddClose(slot, slot);
                }
            }
            GrSlotDataPacket(room.RoomId);
            return;
        }

        bool teamMode = GameModes.IsTeamGameType(room.GameType);
        int perSide = Math.Max(1, maxPlayers / 4 + (maxPlayers % 4 == 0 ? 0 : 0));
        // 组队赛：每队人数 = maxPlayers/2（2/4/6/8 都是偶数），每排 4 格
        int columnLimit = teamMode ? Math.Max(1, maxPlayers / 2) : maxPlayers;

        for (byte slot = 0; slot < 8; slot++)
        {
            int column = slot % 4;
            bool closed = teamMode ? column >= columnLimit : slot >= maxPlayers;
            if (closed)
            {
                // 必须同时建立 Close 成员（_slots/_IDs），只改 CloseSlotIds 会让客户端槽位数据不一致 → 黑屏
                room.AddClose(slot, slot);
            }
        }
        GrSlotDataPacket(room.RoomId);
    }

    internal static void AddAis(GameRoom room, int count, byte randomTrackGameType)
    {
        if (room == null || count <= 0)
        {
            return;
        }
        // 双人模式没有 AI
        if (room.DuoTeam)
        {
            return;
        }
        var selector = new DictionaryRandomSelector();
        // 要补的 AI 数量与角色/卡丁车字典容量无关：字典可能只有 6 个角色，
        // 而 ai=8 建房要补 7 个 AI。字典不够时必须跨轮重复抽取，
        // 否则只会加出 min(count, 字典数) 个（表现为“AI 数量少一个”）。
        int want = Math.Min(count, 8);
        var charPool = aiCharacterDict.Keys.ToList();
        if (charPool.Count == 0)
        {
            Log.Warn($"[Room] 无可用 AI 角色，跳过添加 {count} 个 AI");
            return;
        }
        var rng = new Random();
        List<short> randomCharIds = new List<short>(want);
        while (randomCharIds.Count < want)
        {
            foreach (short id in charPool.OrderBy(_ => rng.Next()))
            {
                randomCharIds.Add(id);
                if (randomCharIds.Count >= want)
                {
                    break;
                }
            }
        }
        List<short> kartPool;
        if (randomTrackGameType == 0)
        {
            kartPool = selector.GetRandomKartIds(aiKartDict, want, true, false);
        }
        else if (randomTrackGameType == 1)
        {
            kartPool = selector.GetRandomKartIds(aiKartDict, want, false, true);
        }
        else
        {
            kartPool = new List<short>();
        }
        // 可用卡丁车不足时同样跨轮重复；一辆都没有时用 0（默认车）
        List<short> randomKartIds = new List<short>(want);
        while (kartPool.Count > 0 && randomKartIds.Count < want)
        {
            foreach (short id in kartPool.OrderBy(_ => rng.Next()))
            {
                randomKartIds.Add(id);
                if (randomKartIds.Count >= want)
                {
                    break;
                }
            }
        }
        int aiCount = 0;
        int redCount = room.GetTeamMemberCount(1);
        int blueCount = room.GetTeamMemberCount(2);
        for (int i = 0; i < randomCharIds.Count && aiCount < count; i++)
        {
            short targetCharId = randomCharIds[i];
            // 卡丁车数量可能少于角色数量：不足时用 0（默认/随机车），不能按下标硬取
            short targetKartId = i < randomKartIds.Count ? randomKartIds[i] : (short)0;
            if (aiCharacterDict.TryGetValue(targetCharId, out var targetChar))
            {
                short? ridIndex = selector.GetRandomRidIndex(targetChar);
                short? balloonId = 0;
                short? headbandId = 0;
                short? goggleId = 0;
                if (randomTrackGameType == 1)
                {
                    balloonId = selector.GetRandomAccessoryId(targetChar.Balloons);
                    headbandId = selector.GetRandomAccessoryId(targetChar.Headbands);
                    goggleId = selector.GetRandomAccessoryId(targetChar.Goggles);
                }
                // 按当前红蓝人数均衡分配（原来固定前 4 个蓝、后 4 个红，
                // 少于 4 个 AI 时会全在蓝方）。
                // 注意：个人赛建房时房主是 AddPlayer(team:0)，不属于任何一队，
                // GetTeamMemberCount(2) 为 0 会一直往 0-3 槽塞；该区被房主+3 个 AI 占满后
                // TrySetAi 返回 255，结果 7 个 AI 只加了 6 个。这里以实际空槽为准。
                int freeBlue = 0;
                for (int s = 0; s < 4; s++)
                {
                    if (room._slots[s] == null)
                    {
                        freeBlue++;
                    }
                }
                int freeRed = 0;
                for (int s = 4; s < 8; s++)
                {
                    if (room._slots[s] == null)
                    {
                        freeRed++;
                    }
                }
                if (freeBlue == 0 && freeRed == 0)
                {
                    break; // 房间已满
                }
                byte team = freeBlue == 0 ? (byte)1
                          : freeRed == 0 ? (byte)2
                          : (blueCount <= redCount ? (byte)2 : (byte)1);
                if (team == 2) { blueCount++; } else { redCount++; }
                Ai ai = new Ai
                {
                    Character = targetCharId,
                    Rid = ridIndex ?? 0,
                    Kart = targetKartId,
                    Balloon = balloonId ?? 0,
                    HeadBand = headbandId ?? 0,
                    Goggle = goggleId ?? 0,
                    Team = team
                };
                byte slot = room.TrySetAi(ai, team);
                if (slot == 255)
                {
                    // 均衡计算与槽位分区不一致时（房主 team=0 / 双人组队）兜底换另一队
                    slot = room.TrySetAi(ai, team == 2 ? (byte)1 : (byte)2);
                }
                if (slot != 255)
                {
                    aiCount++;
                }
                if (aiCount == count)
                {
                    break;
                }
            }
        }
        Log.Debug($"[Room] AI 添加完成：请求 {count}，实际 {aiCount}，角色池 {charPool.Count}，车池 {kartPool.Count}");
    }

    internal static void AddAi(SessionGroup Parent, int roomId, int ID)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Log.Info($"GetRoom Failed, roomId = {roomId}");
            return;
        }
        var selector = new DictionaryRandomSelector();
        List<short> randomCharIds = selector.GetRandomCharacterIds(aiCharacterDict, 2);
        if (randomCharIds.Count == 0)
        {
            Log.Warn("[Room] 无可用 AI 角色，跳过添加 AI");
            return;
        }
        List<short> randomKartIds = new List<short>();
        if (room.RandomTrackGameType == 0)
        {
            randomKartIds = selector.GetRandomKartIds(aiKartDict, 2, true, false);
        }
        else if (room.RandomTrackGameType == 1)
        {
            randomKartIds = selector.GetRandomKartIds(aiKartDict, 2, false, true);
        }
        if (room.IsTeamMode || GameModes.IsChallenger(room.GameType))
        {
            var Ais = new List<Ai>();
            for (int i = 0; i < 2 && i < randomCharIds.Count; i++)
            {
                short targetCharId = randomCharIds[i];
                // 卡丁车筛选后可能为空：不足时用 0，不能按下标硬取
                short targetKartId = i < randomKartIds.Count ? randomKartIds[i] : (short)0;
                if (aiCharacterDict.TryGetValue(targetCharId, out var targetChar))
                {
                    short? ridIndex = selector.GetRandomRidIndex(targetChar);
                    short? balloonId = 0;
                    short? headbandId = 0;
                    short? goggleId = 0;
                    if (room.RandomTrackGameType == 1)
                    {
                        balloonId = selector.GetRandomAccessoryId(targetChar.Balloons);
                        headbandId = selector.GetRandomAccessoryId(targetChar.Headbands);
                        goggleId = selector.GetRandomAccessoryId(targetChar.Goggles);
                    }
                    Ais.Add(new Ai
                    {
                        Character = targetCharId,
                        Rid = ridIndex ?? 0,
                        Kart = targetKartId,
                        Balloon = balloonId ?? 0,
                        HeadBand = headbandId ?? 0,
                        Goggle = goggleId ?? 0
                    });
                }
            }
            if (Ais.Count < 2)
            {
                // 角色字典不足 2 个时无法凑齐组队 AI，直接放弃，避免 Ais[1] 越界
                Log.Warn($"[Room] AI 角色不足（{Ais.Count}/2），跳过组队 AI 添加");
                return;
            }
            byte slot0 = room.TrySetAi(Ais[0], 2);
            // 挑战者模式全员蓝队（右半场已锁），AI 也只能进蓝队
            byte slot1 = room.TrySetAi(Ais[1], GameModes.IsChallenger(room.GameType) ? (byte)2 : (byte)1);
            if (slot0 != 255 && slot1 != 255)
            {
                var ai0 = RoomManager.TryGetSlotDetail(roomId, slot0) as Ai;
                var ai1 = RoomManager.TryGetSlotDetail(roomId, slot1) as Ai;
                using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                {
                    oPacket.WriteInt(0);
                    oPacket.WriteByte(2);
                    oPacket.WriteInt(ai0.ID);
                    oPacket.WriteShort(ai0.Character);
                    oPacket.WriteShort(ai0.Rid);
                    oPacket.WriteShort(ai0.Kart);
                    oPacket.WriteShort(ai0.Balloon);
                    oPacket.WriteShort(ai0.HeadBand);
                    oPacket.WriteShort(ai0.Goggle);
                    oPacket.WriteByte(ai0.Team);
                    oPacket.WriteInt(ai1.ID);
                    oPacket.WriteShort(ai1.Character);
                    oPacket.WriteShort(ai1.Rid);
                    oPacket.WriteShort(ai1.Kart);
                    oPacket.WriteShort(ai1.Balloon);
                    oPacket.WriteShort(ai1.HeadBand);
                    oPacket.WriteShort(ai1.Goggle);
                    oPacket.WriteByte(ai1.Team);
                    Position(roomId, oPacket);
                    BroadCast(roomId, oPacket);
                }
            }
        }
        else
        {
            short targetCharId = randomCharIds[0];
            short targetKartId = randomKartIds.Count > 0 ? randomKartIds[0] : (short)0;
            if (aiCharacterDict.TryGetValue(targetCharId, out var targetChar))
            {
                short? ridIndex = selector.GetRandomRidIndex(targetChar);
                short? balloonId = 0;
                short? headbandId = 0;
                short? goggleId = 0;
                if (room.RandomTrackGameType == 1)
                {
                    balloonId = selector.GetRandomAccessoryId(targetChar.Balloons);
                    headbandId = selector.GetRandomAccessoryId(targetChar.Headbands);
                    goggleId = selector.GetRandomAccessoryId(targetChar.Goggles);
                }
                byte slot2 = room.TrySetAi(new Ai
                {
                    Character = targetCharId,
                    Rid = ridIndex ?? 0,
                    Kart = targetKartId,
                    Balloon = balloonId ?? 0,
                    HeadBand = headbandId ?? 0,
                    Goggle = goggleId ?? 0,
                    Team = 0
                }, 0);
                if (slot2 != 255)
                {
                    var ai2 = RoomManager.TryGetSlotDetail(roomId, slot2) as Ai;
                    using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                    {
                        oPacket.WriteInt(0);
                        oPacket.WriteByte(1);
                        oPacket.WriteInt(ai2.ID);
                        oPacket.WriteShort(ai2.Character);
                        oPacket.WriteShort(ai2.Rid);
                        oPacket.WriteShort(ai2.Kart);
                        oPacket.WriteShort(ai2.Balloon);
                        oPacket.WriteShort(ai2.HeadBand);
                        oPacket.WriteShort(ai2.Goggle);
                        oPacket.WriteByte(0);
                        Position(roomId, oPacket);
                        BroadCast(roomId, oPacket);
                    }
                }
            }
        }
    }

    internal static void Position(int roomId, OutPacket outPacket)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }
        foreach (RoomMember member in room._slots)
        {
            if (member is Player player)
            {
                outPacket.WriteInt(player.ID);
            }
            else if (member is Ai ai)
            {
                outPacket.WriteInt(ai.ID);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
        }
    }

    internal static void BroadcastSlotState(int roomId)
    {
        GrSlotStatePacket(roomId);
    }

    internal static void GrSlotStatePacket(int roomId)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }
        using (OutPacket oPacket = new OutPacket("GrSlotStatePacket"))
        {
            foreach (RoomMember member in room._IDs)
            {
                if (member is Player player)
                {
                    // 6 = out（掉线未离开槽位）
                    oPacket.WriteInt(player.PlayerType);
                }
                else if (member is Ai ai)
                {
                    oPacket.WriteInt(7);
                }
                else
                {
                    oPacket.WriteInt(0);
                }
            }
            oPacket.WriteBytes(new byte[32]);
            BroadCast(roomId, oPacket);
        }
    }

    /// <summary>双人胜队：总分最高；并列取第一名所在队伍</summary>
    static byte GetDuoWinnerTeam(Dictionary<byte, int> teamScores, byte firstTeam)
    {
        if (teamScores.Count == 0) return firstTeam;
        byte winner = teamScores.OrderByDescending(kv => kv.Value)
                                .ThenBy(kv => kv.Key == firstTeam ? 0 : 1)
                                .ThenBy(kv => kv.Key)
                                .First().Key;
        return winner;
    }

    /// <summary>夺旗赛/死斗的胜方队伍；平局返回 0。</summary>
    internal static byte WinnerTeam(GameRoom room)
    {
        if (GameModes.IsTeamFlagMode(room.GameType))
        {
            if (room.RedFlagScore == room.BlueFlagScore) return 0;
            return (byte)(room.RedFlagScore > room.BlueFlagScore ? 1 : 2);
        }
        if (room.GameType == GameModes.DeathMatchTeam)
        {
            if (room.DeathMatchRedScore == room.DeathMatchBlueScore) return 0;
            return (byte)(room.DeathMatchRedScore > room.DeathMatchBlueScore ? 1 : 2);
        }
        return 0;
    }

    /// <summary>
    /// 结算记录 +15 的 short：普通组队写队伍数 2，夺旗赛写正赛总夺旗数（抓包 3:1 → 4），
    /// 死斗/挑战者/个人写 0（均与官方抓包一致）。
    /// </summary>
    static short ResultTeamField(GameRoom room)
    {
        if (GameModes.IsTeamFlagMode(room.GameType))
        {
            return (short)Math.Max(0, room.FlagRegulationTotal);
        }
        if (room.GameType == 3 || room.GameType == 4)
        {
            return 2;
        }
        return 0;
    }

    /// <summary>
    /// 积分制模式的名次：胜队优先 → 个人积分降序 → 格子顺序。
    /// 客户端的名次/颁奖界面直接读该名次，因此必须与阵营比分一致。
    /// </summary>
    static Dictionary<int, int> BuildModeRanking(GameRoom room, RoomMember[] members)
    {
        var list = new List<(int Id, int Score, byte Team, int Slot)>();
        foreach (RoomMember member in members)
        {
            if (member is Player p)
            {
                list.Add((p.ID, room.ModeScore(p.ID), p.Team, p.SlotId));
            }
            else if (member is Ai a)
            {
                list.Add((a.ID, room.ModeScore(a.ID), a.Team, a.SlotId));
            }
        }

        bool teamScored = GameModes.IsTeamFlagMode(room.GameType)
            || room.GameType == GameModes.DeathMatchTeam;
        byte winningTeam = WinnerTeam(room);
        list.Sort((left, right) =>
        {
            if (teamScored && winningTeam != 0)
            {
                int leftWin = left.Team == winningTeam ? 1 : 0;
                int rightWin = right.Team == winningTeam ? 1 : 0;
                if (leftWin != rightWin)
                {
                    return rightWin - leftWin;
                }
            }
            if (left.Score != right.Score)
            {
                return right.Score - left.Score;
            }
            return left.Slot - right.Slot;
        });

        var ranking = new Dictionary<int, int>();
        for (int i = 0; i < list.Count; i++)
        {
            ranking[list[i].Id] = i;
        }
        return ranking;
    }

    static void GameResultPacket(GameRoom room, RoomMember[] members, Dictionary<int, uint> timeData)
    {
        int playerCount = 0;
        int aiCount = 0;
        foreach (RoomMember member in members)
        {
            if (member is Player p1)
            {
                playerCount++;
                if (!timeData.ContainsKey(p1.ID))
                {
                    timeData[p1.ID] = uint.MaxValue;
                }
            }
            else if (member is Ai a1)
            {
                aiCount++;
                if (!timeData.ContainsKey(a1.ID))
                {
                    timeData[a1.ID] = uint.MaxValue;
                }
            }
        }

        bool scoreMode = GameModes.IsScoreMode(room.GameType);
        if (scoreMode)
        {
            // 夺旗赛/死斗没有冲线时间：结算时间统一写正赛时长（抓包：180000 / 150000）
            uint modeTime = (uint)Math.Max(0, GameModes.RaceDurationMs(room.GameType));
            foreach (RoomMember member in members)
            {
                if (member is Player p0)
                {
                    timeData[p0.ID] = modeTime;
                }
                else if (member is Ai a0)
                {
                    timeData[a0.ID] = modeTime;
                }
            }
        }

        Dictionary<int, int> ranking = scoreMode
            ? BuildModeRanking(room, members)
            : GetAllRanks(timeData);
        room.Ranking.Clear();
        foreach (var pair in ranking)
        {
            room.Ranking[pair.Key] = pair.Value;
        }

        var firstId = ranking.First(kv => kv.Value == 0).Key;
        byte firstTeam = 0;
        if (members[firstId] is Player p2)
        {
            firstTeam = p2.Team;
        }
        else if (members[firstId] is Ai a2)
        {
            firstTeam = a2.Team;
        }
        Log.Info($"第一名 ID: {firstId} Team: {firstTeam}");

        int redTeam = 0;
        int blueTeam = 0;
        Dictionary<byte, int> teamScores = new Dictionary<byte, int>();
        foreach (RoomMember member in members)
        {
            if (member is Player p3)
            {
                if (p3.Team == 2 && timeData[p3.ID] != uint.MaxValue)
                {
                    blueTeam += teamPoints[ranking[p3.ID]];
                }
                else if (p3.Team == 1 && timeData[p3.ID] != uint.MaxValue)
                {
                    redTeam += teamPoints[ranking[p3.ID]];
                }
                if (p3.Team >= 1 && p3.Team <= 4 && timeData[p3.ID] != uint.MaxValue)
                {
                    teamScores.TryGetValue(p3.Team, out int sc0);
                    teamScores[p3.Team] = sc0 + RankPoint(room, ranking[p3.ID]);
                }
            }
            if (member is Ai a3)
            {
                if (a3.Team == 2 && timeData[a3.ID] != uint.MaxValue)
                {
                    blueTeam += teamPoints[ranking[a3.ID]];
                }
                else if (a3.Team == 1 && timeData[a3.ID] != uint.MaxValue)
                {
                    redTeam += teamPoints[ranking[a3.ID]];
                }
                if (a3.Team >= 1 && a3.Team <= 4 && timeData[a3.ID] != uint.MaxValue)
                {
                    teamScores.TryGetValue(a3.Team, out int sc1);
                    teamScores[a3.Team] = sc1 + RankPoint(room, ranking[a3.ID]);
                }
            }
        }

        using (OutPacket outPacket = new OutPacket("GameNextStagePacket"))
        {
            outPacket.WriteByte(room.ModeByte);
            outPacket.WriteInt();
            outPacket.WriteInt();
            BroadCast(room.RoomId, outPacket);
        }

        byte winnerTeam;
        if (room.GameType == 3)
        {
            if (room.DuoTeam)
            {
                winnerTeam = GetDuoWinnerTeam(teamScores, firstTeam);
            }
            else if (redTeam == blueTeam)
            {
                winnerTeam = firstTeam;
            }
            else
            {
                winnerTeam = (byte)(redTeam > blueTeam ? 1 : 2);
            }
        }
        else if (room.GameType == 4)
        {
            winnerTeam = firstTeam;
        }
        else if (scoreMode)
        {
            // 夺旗赛/死斗：胜方按阵营积分判定；平局沿用“第一名所在队”
            byte winner = WinnerTeam(room);
            winnerTeam = winner != 0 ? winner : firstTeam;
        }
        else if (GameModes.IsChallenger(room.GameType))
        {
            winnerTeam = firstTeam;
        }
        else
        {
            winnerTeam = 0;
        }

        // 自动计分：把本局结果交给计分器（个人累计名次分 / 组队累计胜场）
        RoomScore.OnRoundEnd(room, members, ranking, timeData, winnerTeam);

        using (OutPacket outPacket = new OutPacket("GameResultPacket"))
        {
            outPacket.WriteByte(winnerTeam);

            outPacket.WriteInt(playerCount); // player count
            foreach (RoomMember member in members)
            {
                if (member is Player p4)
                {
                    var rawConfig = ProfileService.GetProfileConfig(p4.Nickname);
                    // 档案不完整时用空档案兜底：记录照常写入，仅档案相关字段（装备/俱乐部/奖励）补默认值
                    bool p4Complete = rawConfig?.Rider != null && rawConfig?.RiderItem != null;
                    var p4Config = p4Complete ? rawConfig : new ProfileConfig();
                    if (!p4Complete)
                    {
                        Log.Info($"[GrGameResultPacket] Warning: ProfileConfig incomplete for {p4.Nickname}, using defaults");
                    }

                    outPacket.WriteInt(p4.ID); // player id
                    outPacket.WriteUInt(timeData[p4.ID]);
                    outPacket.WriteByte();
                    outPacket.WriteUShort(p4Config.RiderItem.Set_Kart);
                    int playerRanking = ranking[p4.ID];
                    int playerPoint = scoreMode
                        ? room.ModeScore(p4.ID)
                        : timeData[p4.ID] == uint.MaxValue ? 0 : RankPoint(room, playerRanking);
                    Log.Debug($"Player {p4.ID} 排名 {playerRanking} 得分 {playerPoint}");
                    outPacket.WriteInt(playerRanking);
                    // 抓包：该 short 在普通组队=2（队伍数），夺旗赛=正赛总夺旗数（3:1 → 4），
                    // 死斗/挑战者/个人=0。写成固定 2 会让夺旗赛按普通组队结算处理。
                    outPacket.WriteShort(ResultTeamField(room));
                    outPacket.WriteByte();

                    var reward = TimeReward.Reward(playerRanking);
                    if (p4Complete)
                    {
                        p4Config.Rider.RP += reward.RP;
                        outPacket.WriteUInt(p4Config.Rider.RP);
                        outPacket.WriteUInt(reward.RP); // Earned RP
                        outPacket.WriteUInt(reward.Lucci); // Earned Lucci
                        p4Config.Rider.Lucci += reward.Lucci;
                        outPacket.WriteUInt(p4Config.Rider.Lucci);
                        ProfileService.Save(p4.Nickname, p4Config);
                    }
                    else
                    {
                        // 档案缺失：奖励相关字段无法获取，仅补 0
                        outPacket.WriteUInt(0); // RP
                        outPacket.WriteUInt(0); // Earned RP
                        outPacket.WriteUInt(0); // Earned Lucci
                        outPacket.WriteUInt(0); // Lucci
                    }
                    outPacket.WriteBytes(new byte[29]);

                    // 计分模式（夺旗/组队死斗）的 TP 列写个人积分；个人夺旗没有阵营，team 写 0
                    if (room.IsTeamMode || scoreMode || GameModes.IsChallenger(room.GameType))
                    {
                        outPacket.WriteInt(scoreMode ? room.ModeScore(p4.ID) : playerPoint);
                        outPacket.WriteByte(p4.Team); // Team
                    }
                    else
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteByte(0);
                    }
                    outPacket.WriteBytes(new byte[12]);
                    outPacket.WriteInt(1);
                    outPacket.WriteByte(0);
                    outPacket.WriteUShort(p4Config.RiderItem.Set_Character);
                    // 抓包：+92 偏移是该玩家所在队伍的夺旗数（3:1 时红队记录=3、蓝队记录=1），
                    // 客户端结算界面的队伍比分就读这里；整段写 0 就会显示 0:0。
                    outPacket.WriteBytes(new byte[5]);
                    outPacket.WriteInt(GameModes.IsTeamFlagMode(room.GameType)
                        ? room.TeamModeScore(p4.Team)
                        : 0);
                    outPacket.WriteBytes(new byte[40]);
                    outPacket.WriteHexString("FF");
                    outPacket.WriteBytes(new byte[37]);
                    outPacket.WriteInt(p4Config.Rider.ClubMark_LOGO);
                    outPacket.WriteBytes(new byte[39]);
                }
            }

            outPacket.WriteInt(aiCount); // AI count
            foreach (RoomMember member in members)
            {
                if (member is Ai a4)
                {
                    outPacket.WriteInt(a4.ID);
                    outPacket.WriteUInt(timeData[a4.ID]);
                    outPacket.WriteByte();

                    // 获取 kart 属性值
                    outPacket.WriteShort(a4.Kart);
                    int AiRanking = ranking[a4.ID];
                    int AiPoint = timeData[a4.ID] == uint.MaxValue ? 0 : RankPoint(room, AiRanking);
                    Log.Debug($"AI {a4.ID} 排名 {AiRanking} 得分 {AiPoint}");
                    outPacket.WriteInt(AiRanking);
                    outPacket.WriteShort(0);
                    if (room.IsTeamMode || scoreMode || GameModes.IsChallenger(room.GameType))
                    {
                        outPacket.WriteByte(a4.Team); // Team
                        outPacket.WriteInt(scoreMode ? room.ModeScore(a4.ID) : AiPoint);
                    }
                    else
                    {
                        outPacket.WriteByte(0);
                        outPacket.WriteInt(0);
                    }
                }
            }
            Log.Info($"红队得分 {redTeam} 蓝队得分 {blueTeam}");
            outPacket.WriteBytes(new byte[34]);
            outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00");
            BroadCast(room.RoomId, outPacket);
        }

    }

    internal static RoomMember[] DeepCopyMembers(RoomMember[] source)
    {
        var copy = new RoomMember[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            copy[i] = source[i] switch
            {
                Player p => new Player
                {
                    SlotId = p.SlotId,
                    ID = p.ID,
                    Nickname = p.Nickname,
                    PlayerType = p.PlayerType,
                    Team = p.Team,
                    Session = p.Session,
                    LastPacketReceived = p.LastPacketReceived,
                },
                Ai a => new Ai
                {
                    SlotId = a.SlotId,
                    ID = a.ID,
                    Character = a.Character,
                    Rid = a.Rid,
                    Kart = a.Kart,
                    Balloon = a.Balloon,
                    HeadBand = a.HeadBand,
                    Goggle = a.Goggle,
                    Team = a.Team,
                },
                Close c => new Close
                {
                    SlotId = c.SlotId,
                    ID = c.ID,
                    PlayerType = c.PlayerType,
                },
                _ => null, // null 保持 null
            };
        }
        return copy;
    }
}
