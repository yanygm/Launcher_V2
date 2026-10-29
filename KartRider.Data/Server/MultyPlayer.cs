using ExcData;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Profile;
using System;
using System.Collections.Generic;
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

    static void Start(SessionGroup Parent, int roomId)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine($"房间 {roomId} 不存在");
            return;
        }

        room.Ready = new Dictionary<string, bool>();

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

        // 防止同一房间重复启动就绪检测（多个玩家可能同时触发 Start）
        if (!_readyCheckRooms.TryAdd(room, 0))
        {
            Console.WriteLine($"房间 {roomId} 已有就绪检测在进行，跳过");
            return;
        }

        // 立即检查一次：若开局就全部就绪（如无存活玩家），直接启动，无需起定时器
        if (IsAllReady(room))
        {
            _readyCheckRooms.TryRemove(room, out _);
            Set_startTrigger(Parent, room);
            return;
        }

        // 定时器驱动就绪检测：每秒检查一次，全部就绪或超过 30 秒后启动游戏。
        // 相比原 while + Thread.Sleep，不再占用玩家会话线程，等待期间该玩家 TCP 通道畅通
        var readyTimer = new System.Timers.Timer();
        readyTimer.Interval = 1000;
        readyTimer.AutoReset = true;
        int waitCount = 0;
        readyTimer.Elapsed += (s, e) =>
        {
            try
            {
                waitCount++;
                // 房间已解散则停止检测
                if (RoomManager.GetRoom(room.RoomId) != room)
                {
                    readyTimer.Stop();
                    readyTimer.Dispose();
                    _readyCheckRooms.TryRemove(room, out _);
                    Console.WriteLine($"房间 {room.RoomId} 已解散，就绪检测停止");
                    return;
                }

                if (IsAllReady(room) || waitCount >= 30)
                {
                    readyTimer.Stop();
                    readyTimer.Dispose();
                    _readyCheckRooms.TryRemove(room, out _);
                    Set_startTrigger(Parent, room);
                }
                else
                {
                    Console.WriteLine($"存在未就绪的玩家，等待中... ({waitCount}/30)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"就绪检测异常: {ex.Message}");
                readyTimer.Stop();
                readyTimer.Dispose();
                _readyCheckRooms.TryRemove(room, out _);
                Set_startTrigger(Parent, room);
            }
        };
        readyTimer.Start();
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
            Console.WriteLine($"就绪状态检查异常: {ex.Message}");
            return false;
        }
    }

    static void Set_startTrigger(SessionGroup Parent, GameRoom room)
    {
        var onceTimer = new System.Timers.Timer();
        onceTimer.Interval = 1000;
        onceTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, _event) => startTrigger(Parent, room, s, _event));
        onceTimer.AutoReset = false;
        onceTimer.Start();
    }

    static void startTrigger(SessionGroup Parent, GameRoom room, object sender, System.Timers.ElapsedEventArgs e)
    {
        if (room.StartTicks != 0)
        {
            Console.WriteLine("startTrigger: room.StartTicks 已经设置,跳过执行");
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
        room.TimeData = new Dictionary<int, uint>();
        room.Ranking = new Dictionary<int, int>();
        room.EndTicks = 0;
        room.SettleTriggered = false;
        room.Ready = new Dictionary<string, bool>();
        room.Tracks = new Dictionary<int, TrackPos>(); // 新一局重置位置跟踪（坐标/圈数/里程）
        room.AiTracks = new Dictionary<int, AiTrack>(); // 新一局重置 AI 里程跟踪
        room.TeamGauge.Clear(); // 新一局重置各队组队加速槽
        Console.WriteLine("StartTicks = {0}", room.StartTicks);
    }

    static void Set_settleTrigger(GameRoom room, int delayMs = 10000)
    {
        var onceTimer = new System.Timers.Timer();
        onceTimer.Interval = delayMs;
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
            return; // 已经结算过（AI 收尾倒计时 / 玩家冲线两条路径可能各挂了一个定时器）
        }
        room.SettleTriggered = true;

        var menbers = room.SnapshotMembers ?? room._IDs; // 优先用结束瞬间快照
        var timeData = new Dictionary<int, uint>(room.TimeData);
        room.SnapshotMembers = null; // 用后清除

        uint settleTick = room.EndTicks + 6000;
        using (OutPacket outPacket = new OutPacket("GameControlPacket"))
        {
            outPacket.WriteInt(4);
            outPacket.WriteUInt(settleTick);
            BroadCast(room.RoomId, outPacket);
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
        Console.WriteLine("EndTicks = {0}", settleTick);
    }

    public static void Clientsession(SessionGroup Parent, uint hash, InPacket iPacket)
    {
        if (!string.IsNullOrEmpty(Parent.Client.Nickname))
        {
            if (!FileName.FileNames.ContainsKey(Parent.Client.Nickname))
            {
                FileName.Load(Parent.Client.Nickname);
            }
            ProfileService.Load(Parent.Client.Nickname);
        }

        if (hash == Adler32Helper.GenerateAdler32_ASCII("GameSlotPacket", 0))
        {
            SlotData.GameSlotPacket(Parent, iPacket);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameControlPacket"))
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
                    Console.WriteLine("GameControlPacket, ID = {0}, Time = {1}", player.ID, time);
                }
                if (room.EndTicks == 0)
                {
                    // 玩家冲线：和「第一个 AI 冲线」走同一条路 —— 开始 10 秒读秒，到点就结算。
                    BeginCountdown(room, CountdownWindowMs, "玩家到达终点");
                }
                // 读秒进行中有人冲线：不重新计时，也不提前结算 —— 从第一个完赛者算起固定 10 秒，
                // 没跑完的人（AI 或玩家）一律按未完赛处理，结算前会给 AI 补成绩，结算板照样完整。
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChGetRoomListRequestPacket"))
        {
            int page = iPacket.ReadInt();
            var rooms = RoomManager.GetRoomsByPage(page);
            using (OutPacket oPacket = new OutPacket("ChGetRoomListReplyPacket"))
            {
                Console.WriteLine($"Room Count: {RoomManager._rooms.Count}");
                oPacket.WriteInt(RoomManager._rooms.Count); // 房间总数
                oPacket.WriteInt(page);
                oPacket.WriteInt(rooms.Count); // 房间数量
                foreach (var _room in rooms)
                {
                    oPacket.WriteShort((short)_room.Key);
                    oPacket.WriteString(_room.Value.RoomName); // 房间名称
                    oPacket.WriteUInt(_room.Value.track); // 赛道
                    oPacket.WriteBool(_room.Value.Lock); // 是否上锁
                    oPacket.WriteByte(_room.Value.ModeByte); // 模式（双人组队 = 68）
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelSwitch", 0))
        {
            int length = iPacket.ReadInt();
            iPacket.ReadBytes(length);
            byte channel = (byte)(iPacket.ReadByte() - 1);
            var channelData = GameSupport.Channels.ContainsKey(channel) ? GameSupport.Channels[channel] : null;
            if (channelData == null) return;
            StartTimeAttack[Parent.Client.Nickname] = channelData.CreateSpeed;
            // 记录玩家当前频道：建房时若处于「双人组队」频道，则房间自动启用双人组队模式
            GameSupport.SetPlayerChannel(Parent.Client.Nickname, channel);
            Console.WriteLine("Channel Switch, channel = {0}, duo = {1}", channelData.Name, channelData.DuoTeam);

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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelMovein", 0))
        {
            uint UserNO = iPacket.ReadUInt();
            string nickname = ClientManager.GetNickname(UserNO);
            if (string.IsNullOrEmpty(nickname)) return;
            Console.WriteLine("PqChannelMovein nickname = {0}", nickname);
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
                    Console.WriteLine("[PrChannelMoveIn] Warning: ProfileConfig or Rider is null for {0}", nickname);
                    return;
                }
                nicknameConfig.Rider.ClientId = clientId;
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChCreateRoomRequestPacket", 0))
        {
            string RoomName = iPacket.ReadString();    //room name
            Console.WriteLine("RoomName = {0}, len = {1}", RoomName, RoomName.Length);
            string Password = iPacket.ReadString();
            Console.WriteLine("Password = {0}, len = {1}", Password, Password.Length);
            byte GameType = iPacket.ReadEncodedByte(); //7c
            iPacket.ReadInt();
            var AiCount = iPacket.ReadInt();
            Console.WriteLine("AiCount = {0}", AiCount);
            iPacket.ReadInt();
            iPacket.ReadInt();
            byte[] RoomData = iPacket.ReadBytes(32);
            iPacket.ReadBytes(5);
            byte AiSwitch = iPacket.ReadByte();
            Console.WriteLine("AiSwitch = {0}", AiSwitch);

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
            // 双人组队判定（模式号以官方抓包为准：68 = 双人组队）：
            // 1) 玩家当前所在频道是双人组队频道（channel.xml 中 gameType 68，或服务端追加的频道）
            //    此时无论客户端上报什么 gameType，都以频道定义的模式为准
            // 2) 客户端直接上报 gameType 68（客户端 channel.xml 已配置该模式的频道）
            // 3) 房间名带关键字（便于不进频道也能快速测试）
            byte playerChannel = GameSupport.GetPlayerChannel(Parent.Client.Nickname);
            bool inDuoChannel = GameSupport.IsDuoTeamChannel(playerChannel);

            // 客户端上报的原始模式号（可能是 68，也可能是普通组队的 3/4）
            byte rawGameType = GameType;
            byte channelWireGameType = 0;
            if (inDuoChannel)
            {
                var channelInfo = GameSupport.Channels[playerChannel];
                rawGameType = channelInfo.GameType;
                channelWireGameType = channelInfo.WireGameType != 0 ? channelInfo.WireGameType : DuoTeamMode.GameType;
            }

            // 判定是否双人组队：频道命中，或客户端直接上报 68，或房间名带关键字
            bool duoTeam = inDuoChannel
                           || DuoTeamMode.IsDuoGameType(rawGameType)
                           || DuoTeamMode.IsDuoRoomName(RoomName);

            // 服务端内部统一用组队模式的 3/4 处理（道具、结算、分队逻辑都按 3/4 判断）
            GameType = DuoTeamMode.ToBaseGameType(rawGameType);

            Room.GameType = GameType;
            Room.RoomData = RoomData;
            Room.DuoTeam = duoTeam && (GameType == 3 || GameType == 4);
            if (Room.DuoTeam)
            {
                // 双人组队：客户端需要收到官方模式号 68 才会渲染多队伍房间（1~4 队可选）。
                // 频道里显式配置了别的双人模式号时以频道为准。
                Room.WireGameType = DuoTeamMode.IsDuoGameType(channelWireGameType)
                    ? channelWireGameType
                    : DuoTeamMode.ToWireGameType(GameType);
            }
            else
            {
                Room.WireGameType = GameType;
            }
            Room.DuoTeamSize = Room.DuoTeam
                ? DuoTeamMode.NormalizeTeamSize(ProfileService.SettingConfig.DuoTeamSize)
                : DuoTeamMode.DefaultTeamSize;
            Console.WriteLine("CreateRoom = {0}, GameType = {1}, WireGameType = {2}, DuoTeam = {3}, TeamSize = {4}",
                RoomId, Room.GameType, Room.WireGameType, Room.DuoTeam, Room.DuoTeamSize);
            byte randomTrackGameType = 0;
            if (GameType == 2 || GameType == 4 || GameType == 14 || GameType == 54)
            {
                randomTrackGameType = 1;
            }
            Room.RandomTrackGameType = randomTrackGameType;

            if (GameType == 3 || GameType == 4)
            {
                // 双人组队按格子顺序入座（0,1 → 1 队 …）；普通组队固定进入蓝队（0-3 格）
                byte teamArg = Room.DuoTeam ? (byte)0 : (byte)2;
                byte slot = RoomManager.AddPlayer(RoomId, Parent.Client.Nickname, teamArg, 2, Parent);
                if (slot == 255)
                {
                    Console.WriteLine("CreateRoom Failed");
                    return;
                }
                Player player = RoomManager.GetPlayer(RoomId, Parent.Client.Nickname);
                if (player == null)
                {
                    Console.WriteLine("GetPlayer Failed");
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
                    // 队伍数量：官方双人组队抓包为 04（4 支队伍），客户端据此生成 4 张队伍色卡片。
                    // 原先固定写 2，导致换队面板只有红蓝两色。
                    oPacket.WriteByte(Room.WireTeamCount);
                    oPacket.WriteEncByte(Room.WireGameType);
                    Parent.Client.Send(oPacket);
                }
            }
            else
            {
                byte slot = RoomManager.AddPlayer(RoomId, Parent.Client.Nickname, 0, 2, Parent);
                if (slot == 255)
                {
                    Console.WriteLine("CreateRoom Failed");
                    return;
                }
                Player player = RoomManager.GetPlayer(RoomId, Parent.Client.Nickname);
                if (player == null)
                {
                    Console.WriteLine("GetPlayer Failed");
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
                    oPacket.WriteByte(8);
                    oPacket.WriteEncByte(Room.WireGameType);
                    Parent.Client.Send(oPacket);
                }
            }
            if (AiCount > 0 && AiSwitch == 6)
            {
                // 新增 AI 数量
                AddAis(Room, AiCount - 1, randomTrackGameType);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrFirstRequestPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return;
            }
            GrSessionDataPacket(Parent, Parent.Client.Nickname);
            //Thread.Sleep(10);
            GrSlotDataPacket(roomId);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrChangeTrackPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            room.track = iPacket.ReadUInt();
            Console.WriteLine("Gr Track Changed : {0}", RandomTrack.GetTrackName(room.track));
            GrSlotDataPacket(roomId);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestSetSlotStatePacket"))
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
                Console.WriteLine("GetPlayer Failed, roomId = {0}, Parent.Client.Nickname = {1}", roomId, Parent.Client.Nickname);
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestClosePacket"))
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestStartPacket"))
        {
            GrSessionDataPacket(Parent);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PcReportStateInGame", 0))
        {
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChLeaveRoomRequestPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            int slotId = RoomManager.GetPlayerSlotId(roomId, Parent.Client.Nickname);
            if (slotId != -1)
            {
                Console.WriteLine($"Leave roomId: {roomId} slotId: {slotId}");
                var Leave = RoomManager.RemovePlayer(roomId, (byte)slotId, Parent.Client.Nickname);
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteBool(Leave);
                    Parent.Client.Send(oPacket);
                }
            }
            else
            {
                Console.WriteLine($"Leave Failed roomId: {roomId} slotId: {slotId}");
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteBool(false);
                    Parent.Client.Send(oPacket);
                }
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestBasicAiPacket"))
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameAiGoalinPacket"))
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
            Console.WriteLine("GameAiGoalinPacket, Id = {0}, Time = {1}", Id, Time);
            if (room.EndTicks == 0)
            {
                // Ai冲线：和「第一个 AI 冲线」走同一条路 —— 开始 10 秒读秒，到点就结算。
                BeginCountdown(room, CountdownWindowMs, "Ai到达终点");
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameAiReportPacket"))
        {
            // AI 在「AI 主机」客户端本地模拟，客户端只会周期性发这个包上报 7 个 AI 的里程，
            // 全程不发 GameAiGoalinPacket（AI 冲线包）——所以 AI 到终点这件事必须服务端自己判。
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null || room.StartTicks == 0)
            {
                return;
            }
            iPacket.Skip(4); // 浮点数组前有 4 字节无意义数据（客户端未初始化的缓冲）
            var aiDists = new List<float>();
            while (iPacket.Available >= 4)
            {
                aiDists.Add(iPacket.ReadFloat());
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameTeamBoosterRequestAddGaugePacket"))
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return;
            }
            var team = iPacket.ReadByte();
            var value = iPacket.ReadFloat();
            Console.WriteLine("GameTeamBoosterRequestAddGaugePacket, teams = {0}, value = {1}", team, value);

            // 组队加速槽按队伍独立累计：普通组队是 1/2 两队，双人组队是 1..N 队
            if (!room.IsValidTeam(team))
            {
                return; // 非法队伍号（含 team=0）直接忽略
            }
            int members = room.GetPlayerCount(team);
            if (members < 1)
            {
                members = 1; // 防止除零（该队玩家已离场时仍可能收到残留包）
            }
            float gauge = room.AddTeamGauge(team, value * 0.000125f / members);
            using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
            {
                oPacket.WriteByte(team);
                oPacket.WriteFloat(gauge);
                BroadCast(roomId, oPacket, "", team);
            }
            if (gauge >= 1f)
            {
                room.ResetTeamGauge(team);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrChangeTeamPacket"))
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
                Console.WriteLine("GetPlayer Failed, roomId = {0}, Parent.Client.Nickname = {1}", roomId, Parent.Client.Nickname);
                return;
            }
            byte team = iPacket.ReadByte();
            var Bool = RoomManager.ChangeMemberTeam(roomId, player.SlotId, team);
            Console.WriteLine("ChangeMemberTeam, roomId = {0}, SlotId = {1}, Team = {2}, {3}", roomId, player.SlotId, team, Bool);
            using (OutPacket oPacket = new OutPacket("GrChangeTeamPacketReply"))
            {
                oPacket.WriteInt(player.ID);
                oPacket.WriteByte(room.WireTeam(player.Team));
                Position(roomId, oPacket);
                Parent.Client.Send(oPacket);
            }
            GrSlotDataPacket(roomId);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChJoinRoomRequestPacket"))
        {
            var roomId = iPacket.ReadByte();
            var unk = iPacket.ReadByte();
            var pwd = iPacket.ReadString();
            Console.WriteLine("ChJoinRoomRequestPacket, roomId = {0}, unk = {1}, pwd = {2}", roomId, unk, pwd);

            ChJoinRoomReplyPacket(Parent, roomId, pwd);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRiderTalkPacket"))
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
                Console.WriteLine("GetRoom Failed, roomId = {0}", roomId);
                return;
            }

            var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
            if (player == null)
            {
                Console.WriteLine("GetPlayer Failed, roomId = {0}, Parent.Client.Nickname = {1}", roomId, Parent.Client.Nickname);
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRoomMasterChangePacket"))
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("GetRoom Failed, roomId = {0}", roomId);
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PcStartMatching") || hash == Adler32Helper.GenerateAdler32_ASCII("PcCancelMatching"))
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChGetCurrentCmpRequestPacket"))
        {
            using (OutPacket outPacket = new OutPacket("ChGetCurrentCmpReplyPacket"))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqRotationModeDataPacket"))
        {
            using (OutPacket outPacket = new OutPacket("PrRotationModeDataPacket"))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChangeRoomInfoPacket"))
        {
            string RoomName = iPacket.ReadString();
            string RoomPassword = iPacket.ReadString();

            int LimitTime = iPacket.ReadInt();
            byte RKeyAllowed = iPacket.ReadByte();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                Console.WriteLine("TryGetRoomId Failed, Parent.Client.Nickname = {0}", Parent.Client.Nickname);
                return;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("GetRoom Failed, roomId = {0}", roomId);
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestKickPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                Console.WriteLine("TryGetRoomId Failed, Parent.Client.Nickname = {0}", Parent.Client.Nickname);
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChGetCurrentGpRequestPacket"))
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqWhereIsRider", 0))
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
                    var channel = GameSupport.FindChannelIndex(room.WireGameType != 0 ? room.WireGameType : room.GameType, room.DuoTeam);
                    outPacket.WriteInt(channel);
                    outPacket.WriteBool(room.Lock);
                    outPacket.WriteByte(1);
                    Parent.Client.Send(outPacket);
                }
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqWhereAmI"))
        {
            uint UserID = iPacket.ReadUInt();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            var channel = GameSupport.FindChannelIndex(room.WireGameType != 0 ? room.WireGameType : room.GameType, room.DuoTeam);
            using (OutPacket outPacket = new OutPacket("PrWhereAmI"))
            {
                outPacket.WriteUInt(UserID);
                outPacket.WriteInt(roomId);
                outPacket.WriteByte((byte)channel);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInviteGamePacket"))
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqSendMacroChat"))
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChGetCurrentMpRequestPacket"))
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
        room.Ready = new Dictionary<string, bool>();
        room.TeamGauge.Clear(); // 新一局重置各队组队加速槽
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
                    Console.WriteLine("[GrSlotDataPacket] Warning: ProfileConfig incomplete for player {0}, using defaults", p.Nickname);
                }

                Console.WriteLine("Player Nickname = {0}, ID = {1}, SlotId = {2}", p.Nickname, p.ID, p.SlotId);
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
                if (room.RoomName.Contains("比赛") && !string.IsNullOrEmpty(nickname) && nickname != p.Nickname && !ob)
                {
                    outPacket.WriteString("跑跑卡丁车");
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    GameSupport.GetRider(nickname, outPacket);
                    outPacket.WriteString("");
                }
                else
                {
                    outPacket.WriteString(p.Nickname);
                    outPacket.WriteShort(pConfig.Rider.Emblem1);
                    outPacket.WriteShort(pConfig.Rider.Emblem2);
                    outPacket.WriteShort(0);
                    GameSupport.GetRider(p.Nickname, outPacket);
                    outPacket.WriteString(pConfig.Rider.Card);
                }
                outPacket.WriteUInt(pConfig.Rider.RP);
                if (room.GameType == 3 || room.GameType == 4)
                {
                    outPacket.WriteByte(room.WireTeam(p.Team));
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
                Console.WriteLine("Ai ID = {0}, SlotId = {1}", a.ID, a.SlotId);
                outPacket.WriteInt(7);
                outPacket.WriteShort(a.Character);
                outPacket.WriteShort(a.Rid);
                outPacket.WriteShort(a.Kart);
                outPacket.WriteShort(a.Balloon);
                outPacket.WriteShort(a.HeadBand);
                outPacket.WriteShort(a.Goggle);
                if (room.GameType == 3 || room.GameType == 4)
                {
                    outPacket.WriteByte(room.WireTeam(a.Team));
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
                    Console.WriteLine("[GrSlotDataPacket] Warning: ProfileConfig incomplete for observer {0}, using defaults", p.Nickname);
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
        if (readyCount < playerCount || playerCount < 1)
        {
            using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
            {
                oPacket.WriteInt(2);
                Parent.Client.Send(oPacket);
            }
            return;
        }

        room.Started = true;
        room.StartedPlayerCount = playerCount;

        bool ai = false;
        if (room.GetAiCount() > 0)
        {
            ai = true;
        }
        uint track = RandomTrack.GetRandomTrack(Parent, $"[{room.RoomName}][{room.RoomId.ToString()}]", room.RandomTrackGameType, room.track, ai);
        room.trackTemp = track;

        using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
        {
            oPacket.WriteInt(0);
            Parent.Client.Send(oPacket);
        }

        try
        {
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
            Console.WriteLine("[GrSessionDataPacket] Start failed: {0}", ex);
            room.Started = false;
            room.StartedPlayerCount = 0;
        }
    }

    static void GrCommandStartPacket(int roomId, Player p)
    {
        var room = RoomManager.GetRoom(roomId);
        using (OutPacket oPacket = new OutPacket("GrCommandStartPacket"))
        {
            oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSessionDataPacket")));
            GrSessionDataPacket(p.Nickname, oPacket);

            oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSlotDataPacket")));
            GrSlotDataPacket(roomId, oPacket, true, p.Nickname);
            oPacket.WriteInt();

            //kart data
            ushort KartID = ProfileService.GetProfileConfig(p.Nickname)?.RiderItem?.Set_Kart ?? 0;
            ushort FlyingPetID = ProfileService.GetProfileConfig(p.Nickname)?.RiderItem?.Set_FlyingPet ?? 0;
            if (room.RoomName.Contains("原版"))
            {
                StartGameData.GetDefaultSpac(oPacket, p.Nickname, room.SpeedType, KartID, 0);
            }
            else
            {
                StartGameData.GetKartSpac(oPacket, p.Nickname, room.SpeedType, KartID, FlyingPetID);
            }

            oPacket.WriteInt(room.GetAiCount()); //AI count
            if (room.GetAiCount() > 0)
            {
                for (int j = 0; j < room.GetAiCount(); j++)
                {
                    var AiSpec = AI.GetAISpec(room.RandomTrackGameType);
                    oPacket.WriteEncFloat(AiSpec[0]);
                    oPacket.WriteEncFloat(AiSpec[1]);
                    oPacket.WriteEncFloat(AiSpec[2]);
                    oPacket.WriteEncFloat(AiSpec[3]);
                    oPacket.WriteEncFloat(AiSpec[4]);
                    oPacket.WriteEncFloat(AiSpec[5]);
                }
            }
            oPacket.WriteUInt(room.trackTemp); //track name hash
            oPacket.WriteString("008a000445d4dd3c21fc030e");
            oPacket.WriteInt(10000);

            oPacket.WriteInt();
            oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("MissionInfo")));
            oPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00");
            //oPacket.WriteString("[applied param]\r\ntransAccelFactor='1.8555' driftEscapeForce='4720' steerConstraint='24.95' normalBoosterTime='3860' \r\npartsBoosterLock='1' \r\n\r\n[equipped / default parts param]\r\ntransAccelFactor='1.86' driftEscapeForce='2120' steerConstraint='2.7' normalBoosterTime='860' \r\n\r\n\r\n[gamespeed param]\r\ntransAccelFactor='-0.0045' driftEscapeForce='2600' steerConstraint='22.25' normalBoosterTime='3000' \r\n\r\n\r\n[factory enchant param]\r\n");
            Console.WriteLine("Track : {0}", RandomTrack.GetTrackName(room.trackTemp));
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
            Console.WriteLine("GetRoom Failed, roomId = {0}", roomId);
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
        else if (!string.IsNullOrEmpty(room.LockPwd) && pwd != room.LockPwd)
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
            RoomManager.RemoveRoom(room);
            return;
        }

        int playerCount = room.GetPlayerCount();
        byte slot = RoomManager.AddPlayer(roomId, Parent.Client.Nickname, 0, 2, Parent);
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
        else if (room.GameType == 3 || room.GameType == 4)
        {
            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.pmap ?? 0;
            if (pmap == 718 || (playerCount < 1 && room.RoomMaster < 8))
            {
                room.RoomMaster = player.ID;
            }
            if (room.DuoTeam)
            {
                // 双人组队：入座时已在 TryAddPlayer 里按格子顺序分配好队伍号（每队最多 DuoTeamSize 人），
                // 这里不再强制改成红/蓝队，直接回执即可
                Console.WriteLine("[DuoTeam] {0} 加入房间 {1}，格子 {2}，队伍 {3}", player.Nickname, roomId, slot, player.Team);
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(0);
                    outPacket.WriteByte(1);
                    outPacket.WriteByte(room.WireTeamCount); // 队伍数量：双人房 4 队，普通组队 2 队
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
                    outPacket.WriteByte(1);
                    outPacket.WriteByte(room.WireTeamCount);
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
                    outPacket.WriteByte(room.WireTeamCount);
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
                        Console.WriteLine($"BroadCast OB Send Error [{p.Nickname}]: {ex.Message}");
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
                            Console.WriteLine($"BroadCast Send Error [{p.Nickname}]: {ex.Message}");
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
                            Console.WriteLine($"BroadCast TeamSend Error [{p.Nickname}]: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    // 添加指定数量的 Ai
    static void AddAis(GameRoom room, int count, byte randomTrackGameType)
    {
        var selector = new DictionaryRandomSelector();
        List<short> randomCharIds = selector.GetRandomCharacterIds(aiCharacterDict, 8);
        List<short> randomKartIds = null;
        if (randomTrackGameType == 0)
        {
            randomKartIds = selector.GetRandomKartIds(aiKartDict, 8, true, false);
        }
        else if (randomTrackGameType == 1)
        {
            randomKartIds = selector.GetRandomKartIds(aiKartDict, 8, false, true);
        }
        int aiCount = 0;
        for (int i = 0; i < 8; i++)
        {
            short targetCharId = randomCharIds[i];
            short targetKartId = randomKartIds[i];
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
                byte team = i < 4 ? (byte)2 : (byte)1;
                // 双人组队：AI 不按固定红蓝队，而是按格子顺序自动入座并由格子推导队伍号，
                // 这样 7 个 AI 能依次填满各队（每队 2 人），不会被「队伍已满」挡掉
                if (room.DuoTeam)
                {
                    team = 0;
                }
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
                if (room.TrySetAi(ai, team) != 255)
                {
                    aiCount++;
                }
                if (aiCount == count)
                {
                    break;
                }
            }
        }
    }

    static void AddAi(SessionGroup Parent, int roomId, int ID)
    {
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine("GetRoom Failed, roomId = {0}", roomId);
        }
        var selector = new DictionaryRandomSelector();
        List<short> randomCharIds = selector.GetRandomCharacterIds(aiCharacterDict, 2);
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
            for (int i = 0; i < 2; i++)
            {
                short targetCharId = randomCharIds[i];
                short targetKartId = randomKartIds[i];
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
            byte slot0 = room.TrySetAi(Ais[0], room.DuoTeam ? (byte)0 : (byte)2);
            byte slot1 = room.TrySetAi(Ais[1], room.DuoTeam ? (byte)0 : (byte)1);
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
                    oPacket.WriteByte(room.WireTeam(ai0.Team));
                    oPacket.WriteInt(ai1.ID);
                    oPacket.WriteShort(ai1.Character);
                    oPacket.WriteShort(ai1.Rid);
                    oPacket.WriteShort(ai1.Kart);
                    oPacket.WriteShort(ai1.Balloon);
                    oPacket.WriteShort(ai1.HeadBand);
                    oPacket.WriteShort(ai1.Goggle);
                    oPacket.WriteByte(room.WireTeam(ai1.Team));
                    Position(roomId, oPacket);
                    BroadCast(roomId, oPacket);
                }
            }
        }
        else
        {
            short targetCharId = randomCharIds[0];
            short targetKartId = randomKartIds[0];
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
                        // 队伍号必须下发真实值：写死 0 会让客户端把该 AI 渲染成「无队伍」，
                        // 双人组队下看起来就是「只有红蓝两色」，第三、四队永远不上色。
                        oPacket.WriteByte(room.WireTeam(ai2.Team));
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

    /// <summary>
    /// 读秒窗口（毫秒）：赛道上出现第一个完赛者（玩家或 AI）就开始读秒，
    /// 固定 10 秒后强制结算 —— 不管剩下的参赛者有没有跑完。
    /// 这个值同时写进 state=3 包里的秒数字节，客户端读秒就是按它来的。
    /// </summary>
    const int CountdownWindowMs = 10000;

    /// <summary>
    /// 开始读秒：广播 state=3（带剩余秒数 + 结束时刻），并挂上结算定时器。
    /// 房间已经进入读秒/已结算时直接返回，保证一局只读一次秒。
    /// </summary>
    static void BeginCountdown(GameRoom room, int windowMs, string reason)
    {
        if (room == null || room.EndTicks != 0 || room.SettleTriggered)
        {
            return;
        }

        room.SnapshotMembers = DeepCopyMembers(room._IDs); // 结束瞬间拍快照
        room.EndTicks = ConvertTick() + (uint)windowMs;

        using (OutPacket oPacket = new OutPacket("GameControlPacket"))
        {
            oPacket.WriteInt(3);
            oPacket.WriteUInt(room.EndTicks);
            BroadCast(room.RoomId, oPacket);
        }

        int delay = Math.Max(500, windowMs);
        Set_settleTrigger(room, delay);
        Console.WriteLine("[开始读秒] {0}：客户端显示 {1} 秒，{2} 毫秒后结算", reason, (windowMs + 999) / 1000, delay);
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
        room.Ranking = ranking;

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
        Console.WriteLine("第一名 ID: {0} Team: {1}", firstId, firstTeam);

        // 按队伍累计积分（普通组队是 1/2 两队，双人组队是 1..N 队，同一套逻辑）
        Dictionary<byte, int> teamScores = new Dictionary<byte, int>();
        foreach (RoomMember member in members)
        {
            byte memberTeam = 0;
            int memberId = -1;
            if (member is Player p3)
            {
                memberTeam = p3.Team;
                memberId = p3.ID;
            }
            else if (member is Ai a3)
            {
                memberTeam = a3.Team;
                memberId = a3.ID;
            }

            if (memberTeam == 0 || memberId < 0 || timeData[memberId] == uint.MaxValue)
            {
                continue; // 未组队 / 未完成比赛（DNF）不计分
            }

            teamScores.TryGetValue(memberTeam, out int score);
            teamScores[memberTeam] = score + teamPoints[ranking[memberId]];
        }

        int redTeam = teamScores.TryGetValue(1, out int r) ? r : 0;  // 普通组队：红队 = 1 队
        int blueTeam = teamScores.TryGetValue(2, out int b) ? b : 0;  // 普通组队：蓝队 = 2 队
        Console.WriteLine("队伍积分：{0}", string.Join(" / ", teamScores.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}队={kv.Value}")));

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
                    // 双人组队竞速：总分最高的队伍获胜（并列时取第一名所在队伍）
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
                // 道具组队：先到终点者所在队伍获胜（双人组队沿用同一规则）
                outPacket.WriteByte(firstTeam);
            }
            else
            {
                outPacket.WriteByte(0);
            }

            // 条目总数 = 玩家 + AI：AI 也按「玩家条目」下发（见下面的 Ai 分支）
            outPacket.WriteInt(playerCount + aiCount); // count
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
                        Console.WriteLine("[GrGameResultPacket] Warning: ProfileConfig incomplete for {0}, using defaults", p4.Nickname);
                    }

                    outPacket.WriteInt(p4.ID); // player id
                    outPacket.WriteUInt(timeData[p4.ID]);
                    outPacket.WriteByte();
                    outPacket.WriteUShort(p4Config.RiderItem.Set_Kart);
                    int playerRanking = ranking[p4.ID];
                    int playerPoint = timeData[p4.ID] == uint.MaxValue ? 0 : teamPoints[playerRanking];
                    Console.WriteLine("Player {0} 排名 {1} 得分 {2}", p4.ID, playerRanking, playerPoint);
                    outPacket.WriteInt(playerRanking);
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        // 组队模式填队伍数量：普通组队固定 2 队；
                        // 双人组队按官方抓包（GameResultPacket 该字段为 0）保持 0，避免客户端按 2 队解析
                        outPacket.WriteShort(room.DuoTeam ? (short)0 : (short)2);
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
                        outPacket.WriteByte(room.WireTeam(p4.Team)); // Team
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
                else if (member is Ai a5)
                {
                    // AI 也写成「玩家条目」（结构必须和玩家一模一样，217 字节）：
                    // 客户端在组队模式（68 双人 / 3 竞速组队 / 4 道具组队）里并不解析单独的 AI 段，
                    // 只按玩家条目逐条渲染。AI 留在 AI 段时整段被忽略 ——
                    // 结算板/领奖台只剩自己一行、黄/绿/蓝队伍全是 000，队伍积分也不累加。
                    outPacket.WriteInt(a5.ID);                  // id
                    outPacket.WriteUInt(timeData[a5.ID]);       // 完赛时间
                    outPacket.WriteByte();                      // 未知（同玩家）
                    outPacket.WriteUShort((ushort)a5.Kart);     // 卡丁车
                    int aiRanking = ranking[a5.ID];
                    int aiPoint = timeData[a5.ID] == uint.MaxValue ? 0 : teamPoints[aiRanking];
                    Console.WriteLine("AI {0} 排名 {1} 得分 {2}", a5.ID, aiRanking, aiPoint);
                    outPacket.WriteInt(aiRanking);
                    outPacket.WriteShort(room.GameType == 3 || room.GameType == 4
                        ? (room.DuoTeam ? (short)0 : (short)2)
                        : (short)0);
                    outPacket.WriteByte();
                    outPacket.WriteUInt(0); // RP
                    outPacket.WriteUInt(0); // Earned RP
                    outPacket.WriteUInt(0); // Earned Lucci
                    outPacket.WriteUInt(0); // Lucci
                    outPacket.WriteBytes(new byte[29]);
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteInt(aiPoint);
                        outPacket.WriteByte(room.WireTeam(a5.Team)); // Team
                    }
                    else
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteByte(0);
                    }
                    outPacket.WriteBytes(new byte[12]);
                    outPacket.WriteInt(1);
                    outPacket.WriteByte(0);
                    outPacket.WriteUShort((ushort)a5.Character); // 角色
                    outPacket.WriteBytes(new byte[49]);
                    outPacket.WriteHexString("FF");
                    outPacket.WriteBytes(new byte[37]);
                    outPacket.WriteInt(0);                      // 俱乐部标志
                    outPacket.WriteBytes(new byte[39]);
                }
            }

            // AI 段留空：AI 已并入上面的玩家条目
            outPacket.WriteInt(0); // AI count
            Console.WriteLine("红队得分 {0} 蓝队得分 {1}", redTeam, blueTeam);
            outPacket.WriteBytes(new byte[34]);
            outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00");
            BroadCast(room.RoomId, outPacket);
        }
    }

    // 双人组队竞速：取总分最高的队伍；并列时由第一名所在队伍获胜，再并列则取队号小的
    static byte GetDuoWinnerTeam(Dictionary<byte, int> teamScores, byte firstTeam)
    {
        if (teamScores.Count == 0)
        {
            return firstTeam;
        }
        int best = teamScores.Values.Max();
        var winners = teamScores.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
        if (winners.Count == 1)
        {
            return winners[0];
        }
        if (winners.Contains(firstTeam))
        {
            return firstTeam;
        }
        return winners.OrderBy(t => t).First();
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
