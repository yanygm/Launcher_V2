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

    // 参与频道隔离的模式（gameType 取自客户端 channel.xml 枚举）：
    //   1/2/3/4 = 个人竞速/个人道具/组队竞速/组队道具（无限加速频道 9/10 请求的 gameType 同样是 1/3）
    //   7/8     = 夺旗赛（组队/个人）
    //   17      = 挑战赛（kAiItemTeam）
    // 其余模式（俱乐部 13-16、星期 71 等）不做隔离，返回全局列表。
    static readonly HashSet<int> RacingGameTypes = new HashSet<int> { 1, 2, 3, 4, 7, 8, 17 };

    static void Start(SessionGroup Parent, int roomId)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Log.Info($"房间 {roomId} 不存在");
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

        // 直接开局：客户端收到 GameControl 后才进入加载与 321 计时。
        // 原先"等全员 UDP 就绪"会把 8 人房的开局时间拖到最慢玩家的加载时间（最长 30 秒）。
        // 就绪标记仅保留作诊断，不再阻塞开局。
        Log.Info($"[Start] 房间 {roomId} 直接开局（不等待全员就绪）");
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
        using (OutPacket oPacket = new OutPacket("GameAiMasterSlotNoticePacket"))
        {
            oPacket.WriteInt();
            BroadCast(room.RoomId, oPacket);
        }
        using (OutPacket oPacket = new OutPacket("GameControlPacket"))
        {
            oPacket.WriteInt(1);
            oPacket.WriteUInt(room.StartTicks);
            BroadCast(room.RoomId, oPacket);
        }
        room.TimeData.Clear();
        room.Ranking.Clear();
        room.EndTicks = 0;
        room.SettleTriggered = false;
        room.TeamGauge.Clear();
        room.Ready.Clear();
        room.Tracks.Clear(); // 新一局重置位置跟踪（坐标/圈数/里程）
        Log.Debug($"StartTicks = {room.StartTicks}");
    }

    static void Set_settleTrigger(GameRoom room)
    {
        var onceTimer = new System.Timers.Timer();
        onceTimer.Interval = 10000;
        onceTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, _event) => settleTrigger(room, s, _event));
        onceTimer.AutoReset = false;
        onceTimer.Start();
    }

    static void settleTrigger(GameRoom room, object sender, System.Timers.ElapsedEventArgs e)
    {
        if (room == null)
        {
            return;
        }

        if (room.SettleTriggered)
        {
            return;
        }
        room.SettleTriggered = true;

        var menbers = room.SnapshotMembers ?? room._IDs; // 优先用结束瞬间快照
        var timeData = new Dictionary<int, uint>(room.TimeData);
        room.SnapshotMembers = null; // 用后清除

        uint settleTick = room.EndTicks + 6000;
        int retryRoomId = room.RoomId;
        byte retryMode = room.ModeByte;
        using (OutPacket outPacket = new OutPacket("GameControlPacket"))
        {
            outPacket.WriteInt(4);
            outPacket.WriteUInt(settleTick);
            BroadCast(retryRoomId, outPacket);
        }

        InitRoom(room);

        GameResultPacket(room, menbers, timeData);

        int firstID = room.Ranking.FirstOrDefault(x => x.Value == 0).Key;
        if (room.RoomMaster < 8 && RoomManager.TryGetIdDetail(room.RoomId, firstID) is Player p1)
        {
            room.RoomMaster = firstID;
            p1.PlayerType = 2;
        }
        else if (room.GetOBCount() < 1 && RoomManager.TryGetIdDetail(room.RoomId, firstID) is Player p2)
        {
            room.RoomMaster = firstID;
            p2.PlayerType = 2;
        }
        Log.Info($"EndTicks = {room.EndTicks + 6000}");
    }

    public static void Clientsession(SessionGroup Parent, string packetName, InPacket iPacket)
    {
        if (!string.IsNullOrEmpty(Parent.Client.Nickname))
        {
            if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
            {
                FileName.Load(Parent.Client.Nickname);
            }
            ProfileService.Load(Parent.Client.Nickname);
        }

        if (packetName == "GameSlotPacket")
        {
            SlotData.GameSlotPacket(Parent, iPacket);
            return;
        }
        else if (packetName == "GameControlPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            var state = iPacket.ReadInt();
            //start
            if (state == 0 && room.StartTicks == 0)
            {
                Start(Parent, roomId);
            }
            //finish
            else if (state == 2)
            {
                var time = iPacket.ReadUInt();
                var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
                if (player != null)
                {
                    using (OutPacket oPacket = new OutPacket("GameRaceTimePacket"))
                    {
                        oPacket.WriteInt(player.ID);
                        oPacket.WriteUInt(time);
                        BroadCast(roomId, oPacket);
                    }
                    room.TimeData.TryAdd(player.ID, time);
                    Log.Info($"GameControlPacket, ID = {player.ID}, Time = {time}");
                }
                if (room.EndTicks == 0)
                {
                    room.SnapshotMembers = DeepCopyMembers(room._IDs); // 结束瞬间拍快照
                    room.EndTicks = ConvertTick() + 10000;
                    using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                    {
                        oPacket.WriteInt(3);
                        oPacket.WriteUInt(room.EndTicks);
                        BroadCast(roomId, oPacket, Parent.Client.Nickname);
                    }
                    Set_settleTrigger(room);
                }
            }
            return;
        }
        else if (packetName == "ChGetRoomListRequestPacket")
        {
            int page = iPacket.ReadInt();
            // 请求体第二个字段是模式(gameType)，不是频道；频道由 PqChannelMovein 记录在玩家档案里
            int gameType = (iPacket.Length - iPacket.Position >= 2) ? iPacket.ReadUShort() : -1;
            int channel = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.Channel ?? -1;
            // 回列表前先清掉 0 人房，避免客户端看到无法进入的幽灵房
            RoomManager.PurgeEmptyRooms();
            // v2 测试：只按频道隔离；模式过滤先关闭（避免模式字段语义不一致导致列表空白）
            // 俱乐部道具/竞速、星期模式暂不隔离（频道语义待确认），这三个频道维持全局列表
            bool isolate = channel >= 0 && RacingGameTypes.Contains(gameType);
            var rooms = RoomManager.GetRoomsByPage(page, isolate ? channel : -1, gameType, out int total, false);
            Log.Info($"[RoomList] {Parent.Client.Nickname} channel={channel} gameType={gameType} isolate={isolate} page={page} -> total={total}");
            using (OutPacket oPacket = new OutPacket("ChGetRoomListReplyPacket"))
            {
                Log.Info($"Room Count: {total} (channel={channel}, isolate={isolate})");
                oPacket.WriteInt(total); // 房间总数（本频道）
                oPacket.WriteInt(page);
                oPacket.WriteInt(rooms.Count); // 房间数量
                foreach (var _room in rooms)
                {
                    oPacket.WriteShort((short)_room.Key);
                    // RoomName 理论上非空；但一旦为 null，WriteString 会抛异常 → 处理器中断 → 不回包 →
                    // 客户端在房间列表页无限等待（表现为卡死）。这里做一次兜底。
                    oPacket.WriteString(_room.Value.RoomName ?? string.Empty); // 房间名称
                    oPacket.WriteUInt(_room.Value.track); // 赛道
                    oPacket.WriteBool(_room.Value.Lock); // 是否上锁
                    oPacket.WriteByte(_room.Value.ModeByte); // 模式
                    oPacket.WriteByte(_room.Value.SpeedType); // 速度模式
                    oPacket.WriteBool(_room.Value.Started); // 房间状态
                    oPacket.WriteByte((byte)(8 - _room.Value.CloseSlotIds.Count)); // 房间最大人数
                    oPacket.WriteByte((byte)_room.Value.GetCount()); // 房间人数
                    oPacket.WriteHexString("00 00 00 00 00 00");
                }
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (packetName == "PqChannelSwitch")
        {
            int length = iPacket.ReadInt();
            iPacket.ReadBytes(length);
            byte channel = (byte)(iPacket.ReadByte() - 1);
            var channelData = GameSupport.Channels.ContainsKey(channel) ? GameSupport.Channels[channel] : null;
            if (channelData == null) return;
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
            return;
        }
        else if (packetName == "PqChannelMovein")
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
                return;
            }
            Log.Info($"PqChannelMovein nickname = {nickname} (UserNO={UserNO})");
            IPEndPoint clientEndPoint = Parent.Client.Socket.RemoteEndPoint as IPEndPoint;
            if (clientEndPoint == null) return;
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
                    return;
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
            return;
        }
        else if (packetName == "ChCreateRoomRequestPacket")
        {
            // 房间人数不在已解析的字段里，而是紧跟在标记 BA 7C 70 之后的一个字节（2/4/6/8）
            byte[] rawCreate = iPacket.GetRawBuffer();
            int maxPlayers = ParseMaxPlayers(rawCreate);
            string RoomName = iPacket.ReadString();    //room name
            Log.Debug($"RoomName = {RoomName}, len = {RoomName.Length}");
            string Password = iPacket.ReadString();
            Log.Debug($"Password len = {Password.Length}");
            byte GameType = iPacket.ReadEncodedByte(); //7c
            int raw1 = iPacket.ReadInt();
            var AiCount = iPacket.ReadInt();
            int raw2 = iPacket.ReadInt();
            int raw3 = iPacket.ReadInt();
            byte[] RoomData = iPacket.ReadBytes(32);
            iPacket.ReadBytes(5);
            byte AiSwitch = iPacket.ReadByte();
            Log.Info($"[Room] create raw1={raw1} ai={AiCount} raw2={raw2} raw3={raw3} data={BitConverter.ToString(RoomData)}");

            if (UdpServer.GetUdp(Parent.Client.Nickname).Item2 == 0)
            {
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(0);
                    oPacket.WriteEncByte(GameType);
                    Parent.Client.Send(oPacket);
                }
                return;
            }

            var RoomId = RoomManager.CreateRoom();
            var Room = RoomManager.GetRoom(RoomId);
            Room.RoomName = RoomName;
            if (!string.IsNullOrEmpty(Password))
            {
                Room.Lock = true;
            }
            Room.LockPwd = Password;
            if (StartTimeAttack.ContainsKey(Parent.Client.Nickname))
            {
                Room.SpeedType = StartTimeAttack[Parent.Client.Nickname];
            }
            else
            {
                Room.SpeedType = 7;
            }
            Room.Channel = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.Channel ?? -1;
            Room.MaxPlayers = maxPlayers;
            byte rawGameType = GameType;
            byte playerChannel = (byte)(Room.Channel >= 0 ? Room.Channel : 255);
            bool duoRequested = GameSupport.IsDuoTeamChannel(playerChannel)
                                || DuoTeamMode.IsDuoGameType(rawGameType)
                                || DuoTeamMode.IsDuoRoomName(RoomName);
            GameType = DuoTeamMode.ToBaseGameType(rawGameType);
            Room.GameType = GameType;
            Room.DuoTeam = duoRequested && (GameType == 3 || GameType == 4) && ProfileService.SettingConfig.DuoTeamChannel;
            Room.DuoTeamSize = Room.DuoTeam
                ? DuoTeamMode.NormalizeTeamSize(ProfileService.SettingConfig.DuoTeamSize)
                : (byte)2;
            Room.WireGameType = Room.DuoTeam ? DuoTeamMode.ToWireGameType(GameType) : GameType;
            Room.RoomData = RoomData;
            Log.Debug($"CreateRoom = {RoomId}");
            byte randomTrackGameType = 0;
            if (GameType == 2 || GameType == 4 || GameType == 14 || GameType == 54)
            {
                randomTrackGameType = 1;
            }
            Room.RandomTrackGameType = randomTrackGameType;

            if (GameType == 3 || GameType == 4)
            {
                byte slot = Room.DuoTeam
                    ? RoomManager.AddDuoPlayer(RoomId, Parent.Client.Nickname, 2, Parent)
                    : RoomManager.AddPlayer(RoomId, Parent.Client.Nickname, 2, 2, Parent);
                if (slot == 255)
                {
                    Log.Info($"CreateRoom Failed");
                    return;
                }
                Player player = RoomManager.GetPlayer(RoomId, Parent.Client.Nickname);
                if (player == null)
                {
                    Log.Info($"GetPlayer Failed");
                    return;
                }
                Room.RoomMaster = player.ID;
                uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
                if (pmap == 590)
                {
                    Room.RoomMaster = 0;
                }
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte((byte)(Room.DuoTeam ? 4 : 2));
                    oPacket.WriteEncByte(Room.ModeByte);
                    Parent.Client.Send(oPacket);
                }
            }
            else
            {
                byte slot = RoomManager.AddPlayer(RoomId, Parent.Client.Nickname, 0, 2, Parent);
                if (slot == 255)
                {
                    Log.Info($"CreateRoom Failed");
                    return;
                }
                Player player = RoomManager.GetPlayer(RoomId, Parent.Client.Nickname);
                if (player == null)
                {
                    Log.Info($"GetPlayer Failed");
                    return;
                }
                Room.RoomMaster = player.ID;
                uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
                if (pmap == 590)
                {
                    Room.RoomMaster = 0;
                }
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte((byte)maxPlayers);
                    oPacket.WriteEncByte(Room.ModeByte);
                    Parent.Client.Send(oPacket);
                }
            }
            // 建房时只记录人数：等客户端真正进入房间（GrFirstRequestPacket）后再锁，
            // 立刻发槽位包会因为房间界面还没建好而黑屏。
            Room.PendingMaxPlayers = maxPlayers;

            if (AiCount > 0 && AiSwitch == 6)
            {
                // 新增 AI 数量
                AddAis(Room, AiCount - 1, randomTrackGameType);
            }
            return;
        }
        else if (packetName == "GrFirstRequestPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return;
            }
            GrSessionDataPacket(Parent, Parent.Client.Nickname);
            //Thread.Sleep(10);
            GrSlotDataPacket(roomId);

            // 进房后按建房时选的人数锁掉多余格子（只做一次）
            var enterRoom = RoomManager.GetRoom(roomId);
            if (enterRoom != null && !enterRoom.RoomSizeApplied)
            {
                enterRoom.RoomSizeApplied = true;
                ApplyRoomSize(enterRoom, enterRoom.PendingMaxPlayers > 0 ? enterRoom.PendingMaxPlayers : 8);
            }
            return;
        }
        else if (packetName == "GrChangeTrackPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            room.track = iPacket.ReadUInt();
            Log.Debug($"Gr Track Changed : {RandomTrack.GetTrackName(room.track)}");
            GrSlotDataPacket(roomId);
            return;
        }
        else if (packetName == "GrRequestSetSlotStatePacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }

            var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
            if (player == null)
            {
                Log.Info($"GetPlayer Failed, roomId = {roomId}, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return;
            }

            if (!room.Started)
            {
                player.PlayerType = iPacket.ReadInt();
                GrSlotStatePacket(roomId);
                using (OutPacket oPacket = new OutPacket("GrReplySetSlotStatePacket"))
                {
                    oPacket.WriteUInt(ClientManager.GetUserNO(Parent.Client.Nickname));
                    oPacket.WriteByte(1);
                    oPacket.WriteInt(player.ID);
                    oPacket.WriteInt(player.PlayerType);
                    BroadCast(roomId, oPacket);
                }
                GrSlotDataPacket(roomId);
            }
            return;
        }
        else if (packetName == "GrRequestClosePacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            uint unk1 = iPacket.ReadUInt();
            byte type = iPacket.ReadByte();
            uint slotId1 = iPacket.ReadUInt();
            uint unk2 = iPacket.ReadUInt();
            uint slotId2 = iPacket.ReadUInt();
            using (OutPacket oPacket = new OutPacket("GrReplyClosePacket"))
            {
                oPacket.WriteUInt(ClientManager.GetUserNO(Parent.Client.Nickname));
                if (room.GameType == 3 || room.GameType == 4)
                {
                    if (unk1 < 8 && slotId1 < 8 && unk2 < 8 && slotId2 < 8 && type == 1 && !room.CloseSlotIds.Contains((byte)slotId1) && !room.CloseSlotIds.Contains((byte)slotId2))
                    {
                        if (room.AddClose((byte)slotId1, (int)unk1) && room.AddClose((byte)slotId2, (int)unk2))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(1);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else if (unk1 < 8 && slotId1 < 8 && unk2 < 8 && slotId2 < 8 && type == 0 && room.CloseSlotIds.Contains((byte)slotId1) && room.CloseSlotIds.Contains((byte)slotId2))
                    {
                        if (room.RemoveClose((byte)slotId1, (int)unk1) && room.RemoveClose((byte)slotId2, (int)unk2))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk2);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                        oPacket.WriteUInt(unk1);
                        oPacket.WriteUInt(unk2);
                        int closeCount = room.CloseSlotIds.Count;
                        oPacket.WriteInt(0);
                        oPacket.WriteInt(closeCount);
                        foreach (byte slotId in room.CloseSlotIds)
                        {
                            oPacket.WriteByte(slotId);
                        }
                    }
                    BroadCast(roomId, oPacket);
                }
                else
                {
                    if (unk1 < 8 && slotId1 < 8 && type == 1 && !room.CloseSlotIds.Contains((byte)slotId1))
                    {
                        if (room.AddClose((byte)slotId1, (int)unk1))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(1);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else if (unk1 < 8 && slotId1 < 8 && type == 0 && room.CloseSlotIds.Contains((byte)slotId1))
                    {
                        if (room.RemoveClose((byte)slotId1, (int)unk1))
                        {
                            oPacket.WriteByte(1);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                            oPacket.WriteUInt(unk1);
                            oPacket.WriteUInt(unk1);
                            int closeCount = room.CloseSlotIds.Count;
                            oPacket.WriteInt(0);
                            oPacket.WriteInt(closeCount);
                            foreach (byte slotId in room.CloseSlotIds)
                            {
                                oPacket.WriteByte(slotId);
                            }
                        }
                    }
                    else
                    {
                        oPacket.WriteByte(0);
                        oPacket.WriteUInt(unk1);
                        oPacket.WriteUInt(unk1);
                        int closeCount = room.CloseSlotIds.Count;
                        oPacket.WriteInt(0);
                        oPacket.WriteInt(closeCount);
                        foreach (byte slotId in room.CloseSlotIds)
                        {
                            oPacket.WriteByte(slotId);
                        }
                    }
                    BroadCast(roomId, oPacket);
                }
            }
            return;
        }
        else if (packetName == "GrRequestStartPacket")
        {
            GrSessionDataPacket(Parent);
            return;
        }
        else if (packetName == "PcReportStateInGame")
        {
            return;
        }
        else if (packetName == "ChLeaveRoomRequestPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            int slotId = RoomManager.GetPlayerSlotId(roomId, Parent.Client.Nickname);
            if (slotId != -1)
            {
                Log.Info($"Leave roomId: {roomId} slotId: {slotId}");
                var Leave = RoomManager.RemovePlayer(roomId, (byte)slotId, Parent.Client.Nickname);
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteBool(Leave);
                    Parent.Client.Send(oPacket);
                }
            }
            else
            {
                Log.Info($"Leave Failed roomId: {roomId} slotId: {slotId}");
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteBool(false);
                    Parent.Client.Send(oPacket);
                }
            }
            return;
        }
        else if (packetName == "GrRequestBasicAiPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            byte ID = iPacket.ReadByte();
            if (RoomManager.TryGetIdDetail(roomId, ID) is Ai ai)
            {
                room.RemoveMember(ai.SlotId, "");
                using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                {
                    oPacket.WriteInt(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteInt(ID);
                    oPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00");
                    Position(roomId, oPacket);
                    BroadCast(roomId, oPacket);
                }
            }
            else
            {
                AddAi(Parent, roomId, ID);
            }
            using (OutPacket oPacket = new OutPacket("GrReplyBasicAiPacket"))
            {
                oPacket.WriteByte(1);
                oPacket.WriteHexString("00 00 00 00");
                BroadCast(roomId, oPacket);
            }
            return;
        }
        else if (packetName == "GameAiGoalinPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            var Id = iPacket.ReadInt();
            var Time = iPacket.ReadUInt();
            using (OutPacket oPacket = new OutPacket("GameRaceTimePacket"))
            {
                oPacket.WriteInt(Id);
                oPacket.WriteUInt(Time);
                BroadCast(roomId, oPacket);
            }
            room.TimeData.TryAdd(Id, Time);
            Log.Info($"GameAiGoalinPacket, Id = {Id}, Time = {Time}");
            if (room.EndTicks == 0)
            {
                room.SnapshotMembers = DeepCopyMembers(room._IDs);
                room.EndTicks = ConvertTick() + 10000;
                using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                {
                    oPacket.WriteInt(3);
                    oPacket.WriteUInt(room.EndTicks);
                    BroadCast(roomId, oPacket);
                }
                Set_settleTrigger(room);
            }
            return;
        }
        else if (packetName == "GameTeamBoosterRequestAddGaugePacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            var team = iPacket.ReadByte();
            var value = iPacket.ReadFloat();
            Log.Info($"GameTeamBoosterRequestAddGaugePacket, teams = {team}, value = {value}");

            if (!room.IsValidTeam(team))
            {
                return;
            }
            int gaugeMembers = room.GetPlayerCount(team);
            if (gaugeMembers < 1) gaugeMembers = 1;
            float gauge = room.AddTeamGauge(team, value * 0.000125f / gaugeMembers);
            using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
            {
                oPacket.WriteByte(team);
                oPacket.WriteFloat(gauge);
                BroadCast(roomId, oPacket, "", team);
            }
            if (gauge >= 1f) room.ResetTeamGauge(team);
            return;
        }
        else if (packetName == "GrChangeTeamPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
            if (player == null)
            {
                Log.Info($"GetPlayer Failed, roomId = {roomId}, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return;
            }
            byte team = iPacket.ReadByte();
            bool Bool;
            if (room.DuoTeam) Bool = RoomManager.ChangeDuoTeam(roomId, player.SlotId, team);
            else Bool = RoomManager.ChangeMemberTeam(roomId, player.SlotId, team);
            Log.Info($"ChangeMemberTeam, roomId = {roomId}, SlotId = {player.SlotId}, Team = {team}, {Bool}");
            using (OutPacket oPacket = new OutPacket("GrChangeTeamPacketReply"))
            {
                oPacket.WriteInt(player.ID);
                oPacket.WriteByte(player.Team);
                Position(roomId, oPacket);
                Parent.Client.Send(oPacket);
            }
            GrSlotDataPacket(roomId);
            return;
        }
        else if (packetName == "ChJoinRoomRequestPacket")
        {
            var roomId = iPacket.ReadByte();
            var unk = iPacket.ReadByte();
            var pwd = iPacket.ReadString();
            Log.Info($"ChJoinRoomRequestPacket, roomId = {roomId}, unk = {unk}, pwd = {pwd}");

            ChJoinRoomReplyPacket(Parent, roomId, pwd);
            return;
        }
        else if (packetName == "GrRiderTalkPacket")
        {
            string value = iPacket.ReadString();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"GetRoom Failed, roomId = {roomId}");
                return;
            }

            var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
            if (player == null)
            {
                Log.Info($"GetPlayer Failed, roomId = {roomId}, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return;
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
                return;
            }
            else if (value.StartsWith("开始游戏", StringComparison.OrdinalIgnoreCase) || value.StartsWith("開始遊戲", StringComparison.OrdinalIgnoreCase))
            {
                GrSessionDataPacket(Parent);
                return;
            }
            else if (value.StartsWith("结束游戏", StringComparison.OrdinalIgnoreCase) || value.StartsWith("結束遊戲", StringComparison.OrdinalIgnoreCase))
            {
                StopGame(roomId, Parent);
                return;
            }
            return;
        }
        else if (packetName == "PqRoomMasterChangePacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"GetRoom Failed, roomId = {roomId}");
                return;
            }
            if (!room.Started)
            {
                string Target = iPacket.ReadString();
                var player = RoomManager.GetPlayer(roomId, Target);
                if (player != null)
                {
                    room.RoomMaster = player.ID;
                    player.PlayerType = 2;
                    GrSlotDataPacket(roomId);
                }
            }
            return;
        }
        else if (packetName == "PcStartMatching" || packetName == "PcCancelMatching")
        {
            var roomList = RoomManager._rooms.Values.Where(r => !r.Lock && !r.Started && r.GetCount() < (8 - r.CloseSlotIds.Count)).ToList();
            if (roomList.Count > 0)
            {
                Random random = new Random();
                GameRoom room = roomList[random.Next(roomList.Count)];
                ChJoinRoomReplyPacket(Parent, room.RoomId, "");
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("PcMatchingFound"))
                {
                    outPacket.WriteInt(0);
                    Parent.Client.Send(outPacket);
                }
            }
            return;
        }
        else if (packetName == "ChGetCurrentCmpRequestPacket")
        {
            using (OutPacket outPacket = new OutPacket("ChGetCurrentCmpReplyPacket"))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (packetName == "PqRotationModeDataPacket")
        {
            using (OutPacket outPacket = new OutPacket("PrRotationModeDataPacket"))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (packetName == "PqChangeRoomInfoPacket")
        {
            string RoomName = iPacket.ReadString();
            string RoomPassword = iPacket.ReadString();

            int LimitTime = iPacket.ReadInt();
            byte RKeyAllowed = iPacket.ReadByte();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                Log.Info($"TryGetRoomId Failed, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"GetRoom Failed, roomId = {roomId}");
                return;
            }
            room.RoomName = RoomName;
            if (RoomPassword.Length > 0)
            {
                room.Lock = true;
            }
            else
            {
                room.Lock = false;
            }
            room.LockPwd = RoomPassword;

            using (OutPacket outPacket = new OutPacket("PrChangeRoomInfoPacket"))
            {
                outPacket.WriteBool(true);
                outPacket.WriteString(RoomName);
                outPacket.WriteString(RoomPassword);
                outPacket.WriteInt(LimitTime);
                outPacket.WriteByte(RKeyAllowed);
                BroadCast(roomId, outPacket);
            }
            return;
        }
        else if (packetName == "GrRequestKickPacket")
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                Log.Info($"TryGetRoomId Failed, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return;
            }
            int ID = iPacket.ReadInt();
            if (RoomManager.TryGetIdDetail(roomId, ID) is Player p)
            {
                var player = RoomManager.RemovePlayer(roomId, p.SlotId, p.Nickname);
                using (OutPacket outPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    outPacket.WriteBool(player);
                    p.Session.Client.Send(outPacket);
                }
                if (player)
                {
                    using (OutPacket outPacket = new OutPacket("GrKickBroadcastPacket"))
                    {
                        outPacket.WriteString(p.Nickname);
                        BroadCast(roomId, outPacket);
                    }
                    using (OutPacket outPacket = new OutPacket("GrReplyKickPacket"))
                    {
                        outPacket.WriteByte(0);
                        Parent.Client.Send(outPacket);
                    }
                }
            }
            return;
        }
        else if (packetName == "ChGetCurrentGpRequestPacket")
        {
            using (OutPacket outPacket = new OutPacket("ChGetCurrentGpReplyPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(1);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (packetName == "PqWhereIsRider")
        {
            uint UserID = iPacket.ReadUInt();
            string nickname = ClientManager.GetNickname(UserID);
            if (string.IsNullOrEmpty(nickname)) return;
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (roomId == -1)
            {
                using (OutPacket outPacket = new OutPacket("PrWhereIsRider"))
                {
                    outPacket.WriteUInt(UserID);
                    outPacket.WriteBytes(new byte[10]);
                    Parent.Client.Send(outPacket);
                }
                return;
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("PrWhereIsRider"))
                {
                    outPacket.WriteUInt(UserID);
                    outPacket.WriteInt(roomId);
                    var channel = GameSupport.Channels.FirstOrDefault(c => c.Value.GameType == room.GameType).Key;
                    outPacket.WriteInt(channel);
                    outPacket.WriteBool(room.Lock);
                    outPacket.WriteByte(1);
                    Parent.Client.Send(outPacket);
                }
            }
            return;
        }
        else if (packetName == "PqWhereAmI")
        {
            uint UserID = iPacket.ReadUInt();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            // 不在房间时 room 为 null：原实现直接取 room.GameType → 空引用 → 踢线
            byte channel = 0;
            if (room != null)
            {
                var matched = GameSupport.Channels.FirstOrDefault(c => c.Value.GameType == room.GameType);
                channel = matched.Key;
            }
            using (OutPacket outPacket = new OutPacket("PrWhereAmI"))
            {
                outPacket.WriteUInt(UserID);
                outPacket.WriteInt(roomId);
                outPacket.WriteByte(channel);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (packetName == "PqInviteGamePacket")
        {
            uint UserID = iPacket.ReadUInt();
            string nickname = ClientManager.GetNickname(UserID);
            if (string.IsNullOrEmpty(nickname))
            {
                return; // 无效用户
            }

            // 被邀请者已在房间中，不接受邀请
            int targetRoomId = RoomManager.TryGetRoomId(nickname);
            if (targetRoomId != -1)
            {
                return;
            }

            var parent = ClientManager.GetParent(nickname);
            if (parent != null)
            {
                using (OutPacket outPacket = new OutPacket("PrInviteGamePacket"))
                {
                    outPacket.WriteBytes(iPacket.ReadBytes(iPacket.Available));
                    parent.Client.Send(outPacket);
                }
            }
            return;
        }
        else if (packetName == "PqSendMacroChat")
        {
            var roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (roomId == -1)
            {
                return;
            }

            int type = iPacket.ReadInt();
            byte id = iPacket.ReadByte();
            Player player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);

            using (OutPacket outPacket = new OutPacket("PcSendMacroChat"))
            {
                outPacket.WriteUInt(ClientManager.GetUserNO(Parent.Client.Nickname));
                outPacket.WriteInt(type);
                outPacket.WriteByte(id);
                if (type == 0)
                {
                    outPacket.WriteString(ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.QuickMsg.GetValueOrDefault(id) ?? "");
                    BroadCast(roomId, outPacket, Parent.Client.Nickname);
                }
                else
                {
                    outPacket.WriteString(ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.TeamQuickMsg.GetValueOrDefault(id) ?? "");
                    BroadCast(roomId, outPacket, Parent.Client.Nickname, player.Team);
                }
                if (room.GetPlayerCount(0) == 1)
                {
                    if (ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.QuickMsg.GetValueOrDefault(id) == "结束游戏" ||
                        ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.QuickMsg.GetValueOrDefault(id) == "結束遊戲" ||
                        ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.TeamQuickMsg.GetValueOrDefault(id) == "结束游戏" ||
                        ProfileService.GetProfileConfig(Parent.Client.Nickname)?.GameOption?.TeamQuickMsg.GetValueOrDefault(id) == "結束遊戲")
                    {
                        StopGame(roomId, Parent);
                    }
                }
            }
            return;
        }
        else if (packetName == "ChGetCurrentMpRequestPacket")
        {
            using (OutPacket outPacket = new OutPacket("ChGetCurrentMpReplyPacket"))
            {
                outPacket.WriteHexString("83 08 00 00 83 08 00 00 08 00 00 00 04 01 00 00 C8 00 00 00 00 00 68 41 01 00 00 01 00 01");
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else
        {
            return;
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

    static void StopGame(int roomId, SessionGroup Parent)
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
        room.Tracks.Clear();
        room.redGauge = 0f;
        room.blueGauge = 0f;
        Interlocked.Exchange(ref room.StartGate, 0);
    }

    static void GrSlotDataPacket(int roomId, OutPacket outPacket, bool enter = false, string nickname = "")
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

        outPacket.WriteUInt(room.track); // track name hash
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
                if (room.GameType == 3 || room.GameType == 4)
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

                outPacket.WriteByte(RiderData.RiderSchool.catLevel); //3
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
                Log.Info($"Ai ID = {a.ID}, SlotId = {a.SlotId}");
                outPacket.WriteInt(7);
                outPacket.WriteShort(a.Character);
                outPacket.WriteShort(a.Rid);
                outPacket.WriteShort(a.Kart);
                outPacket.WriteShort(a.Balloon);
                outPacket.WriteShort(a.HeadBand);
                outPacket.WriteShort(a.Goggle);
                if (room.GameType == 3 || room.GameType == 4)
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

    static void GrSessionDataPacket(SessionGroup Parent)
    {
        int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }

        int readyCount = 0;
        foreach (RoomMember member in room._IDs)
        {
            if (member is Player player)
            {
                if (player.PlayerType == 3 || player.ID == room.RoomMaster)
                {
                    readyCount++;
                    continue;
                }
            }
        }

        int playerCount = room.GetPlayerCount();
        int totalMembers = room.GetCount();

        // 只有 1 个参赛者（没有 AI）时不允许开局。
        // 注意：拒绝码 1 会被客户端当成“开始匹配”，所以这里用 3（需要 2 人以上）。
        if (totalMembers < 2)
        {
            Log.Info($"[Start] 房间 {room.RoomId} 只有 {totalMembers} 名参赛者，拒绝开局（码 3）");
            using (OutPacket denyPacket = new OutPacket("GrReplyStartPacket"))
            {
                denyPacket.WriteInt(3);
                Parent.Client.Send(denyPacket);
            }
            return;
        }

        // 组队赛/双人：每个非空队伍人数必须相同，且至少 2 支队伍（客户端对应弹窗：至少2队 / 两组人数相同）
        if (room.GameType == 3 || room.GameType == 4)
        {
            int team1, team2;
            if (room.DuoTeam)
            {
                var counts = new System.Collections.Generic.List<int>();
                for (byte t = 1; t <= 4; t++)
                {
                    int c = room.GetTeamMemberCount(t);
                    if (c > 0) counts.Add(c);
                }
                if (counts.Count < 2)
                {
                    // 码 18 = 客户端“房间内至少有2个以上队伍才可开始游戏。”
                    Log.Info($"[Start] 双人房 {room.RoomId} 不足 2 支队伍，拒绝开局（码 18）");
                    using (OutPacket denyPacket = new OutPacket("GrReplyStartPacket"))
                    {
                        denyPacket.WriteInt(18);
                        Parent.Client.Send(denyPacket);
                    }
                    return;
                }
                team1 = counts[0];
                team2 = counts[0];
                foreach (int c in counts)
                {
                    if (c != team1) team2 = c;
                }
                if (team2 != team1)
                {
                    Log.Info($"[Start] 双人房 {room.RoomId} 队伍人数不一致（{string.Join("/", counts)}），拒绝开局（码 4）");
                    using (OutPacket denyPacket = new OutPacket("GrReplyStartPacket"))
                    {
                        denyPacket.WriteInt(4);
                        Parent.Client.Send(denyPacket);
                    }
                    return;
                }
                // 人数一致，继续后续流程
                team1 = team2 = counts[0];
            }
            team1 = room.GetTeamMemberCount(2);
            team2 = room.GetTeamMemberCount(1);
            if (!room.DuoTeam && (team1 != team2 || team1 == 0))
            {
                Log.Info($"[Start] 房间 {room.RoomId} 组队人数不匹配（{team1} vs {team2}），拒绝开局（码 4）");
                using (OutPacket denyPacket = new OutPacket("GrReplyStartPacket"))
                {
                    denyPacket.WriteInt(4);
                    Parent.Client.Send(denyPacket);
                }
                return;
            }
        }

        if (readyCount < playerCount || playerCount < 1)
        {
            using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
            {
                oPacket.WriteInt(2);
                Parent.Client.Send(oPacket);
            }
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
            room.ResetRareItems(); // 每局重置「天使/道具锁/闪电」计数

            bool ai = room.GetAiCount() > 0;
            uint track = RandomTrack.GetRandomTrack(Parent, $"[{room.RoomName}][{room.RoomId.ToString()}]", room.RandomTrackGameType, room.track, ai);
            room.trackTemp = track;

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
        using (OutPacket oPacket = new OutPacket("GrCommandStartPacket"))
        {
            oPacket.WriteUInt((uint)PacketName.GrSessionDataPacket);
            GrSessionDataPacket(p.Nickname, oPacket);

            oPacket.WriteUInt((uint)PacketName.GrSlotDataPacket);
            GrSlotDataPacket(roomId, oPacket, true, p.Nickname);
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
            //oPacket.WriteString("[applied param]\r\ntransAccelFactor='1.8555' driftEscapeForce='4720' steerConstraint='24.95' normalBoosterTime='3860' \r\npartsBoosterLock='1' \r\n\r\n[equipped / default parts param]\r\ntransAccelFactor='1.86' driftEscapeForce='2120' steerConstraint='2.7' normalBoosterTime='860' \r\n\r\n\r\n[gamespeed param]\r\ntransAccelFactor='-0.0045' driftEscapeForce='2600' steerConstraint='22.25' normalBoosterTime='3000' \r\n\r\n\r\n[factory enchant param]\r\n");
            Log.Debug($"Track : {RandomTrack.GetTrackName(room.trackTemp)}");
            p.Session.Client.Send(oPacket);
        }
    }

    static void GrSessionDataPacket(SessionGroup Parent, string Nickname)
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

    static void ChJoinRoomReplyPacket(SessionGroup Parent, int roomId, String pwd)
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
        else if (room.GameType == 3 || room.GameType == 4)
        {
            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
            if (pmap == 718 || (playerCount < 1 && room.RoomMaster < 8))
            {
                room.RoomMaster = player.ID;
            }
            if (slot < 4)
            {
                player.Team = 2;
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(1);
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
                    outPacket.WriteByte(1);
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
    static int ParseMaxPlayers(byte[] raw)
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
    static void ApplyRoomSize(GameRoom room, int maxPlayers)
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

        bool teamMode = room.GameType == 3 || room.GameType == 4;
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

    static void AddAis(GameRoom room, int count, byte randomTrackGameType)
    {
        if (room == null || count <= 0)
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

    static void AddAi(SessionGroup Parent, int roomId, int ID)
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
        if (room.GameType == 3 || room.GameType == 4)
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
            byte slot1 = room.TrySetAi(Ais[1], 1);
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

    static void Position(int roomId, OutPacket outPacket)
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

    static void GrSlotStatePacket(int roomId)
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
        int best = -1;
        byte winner = firstTeam;
        foreach (var kv in teamScores)
        {
            if (kv.Value > best) { best = kv.Value; winner = kv.Key; }
            else if (kv.Value == best && kv.Key == firstTeam) { winner = firstTeam; }
        }
        return winner;
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

        Dictionary<int, int> ranking = GetAllRanks(timeData);
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
                    teamScores[p3.Team] = sc0 + teamPoints[ranking[p3.ID]];
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
                    teamScores[a3.Team] = sc1 + teamPoints[ranking[a3.ID]];
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

        using (OutPacket outPacket = new OutPacket("GameResultPacket"))
        {
            if (room.GameType == 3)
            {
                if (room.DuoTeam)
                {
                    outPacket.WriteByte(GetDuoWinnerTeam(teamScores, firstTeam));
                }
                else if (redTeam == blueTeam)
                {
                    outPacket.WriteByte(firstTeam);
                }
                else
                {
                    outPacket.WriteByte((byte)(redTeam > blueTeam ? 1 : 2));
                }
            }
            else if (room.GameType == 4)
            {
                outPacket.WriteByte(firstTeam);
            }
            else
            {
                outPacket.WriteByte(0);
            }

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
                    int playerPoint = timeData[p4.ID] == uint.MaxValue ? 0 : teamPoints[playerRanking];
                    Log.Info($"Player {p4.ID} 排名 {playerRanking} 得分 {playerPoint}");
                    outPacket.WriteInt(playerRanking);
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteShort(2); //2
                    }
                    else
                    {
                        outPacket.WriteShort(0);
                    }
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

                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteInt(playerPoint);
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
                    outPacket.WriteBytes(new byte[49]);
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
                    int AiPoint = timeData[a4.ID] == uint.MaxValue ? 0 : teamPoints[AiRanking];
                    Log.Info($"AI {a4.ID} 排名 {AiRanking} 得分 {AiPoint}");
                    outPacket.WriteInt(AiRanking);
                    outPacket.WriteShort(0);
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteByte(a4.Team); // Team
                        outPacket.WriteInt(AiPoint);
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

    static RoomMember[] DeepCopyMembers(RoomMember[] source)
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
