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
    public static List<short> itemProb_indi = new List<short>();
    public static List<short> itemProb_team = new List<short>();
    public static Dictionary<short, AICharacter> aiCharacterDict = new Dictionary<short, AICharacter>();
    public static Dictionary<short, AIKart> aiKartDict = new Dictionary<short, AIKart>();
    public static Dictionary<string, byte> StartTimeAttack = new Dictionary<string, byte>();
    public static Dictionary<string, bool> Ready = new Dictionary<string, bool>();
    public static int[] teamPoints = { 10, 8, 6, 5, 4, 3, 2, 1 };

    public static IPEndPoint GetServerEndPoint(SessionGroup Parent)
    {
        IPEndPoint serverEndPoint = Parent.Client.Socket.LocalEndPoint as IPEndPoint;
        IPAddress clientEndPoint = ((IPEndPoint)Parent.Client.Socket.RemoteEndPoint).Address;
        var ipInfo = Task.Run(async () => await Update.GetCountryAsync()).Result;
        string ip = ipInfo?.Ip ?? "";
        if (RouterListener.RouterIPList.Contains(clientEndPoint.ToString()) || LanIpGetter.IsInLocalSubnet(clientEndPoint.ToString()))
        {
            return serverEndPoint;
        }
        else if(!string.IsNullOrEmpty(ip))
        {
            int serverPort = ProfileService.SettingConfig.ServerPort;
            return new IPEndPoint(IPAddress.Parse(ip), serverPort);
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

        Ready = new Dictionary<string, bool>();

        foreach (var player in room._slots)
        {
            if (player is Player p && !string.IsNullOrEmpty(p.Nickname))
            {
                Ready[p.Nickname] = false;
            }
        }
        foreach (var player in room.ObIDs)
        {
            if (player is Player p && !string.IsNullOrEmpty(p.Nickname))
            {
                Ready[p.Nickname] = false;
            }
        }

        // 标记是否所有值都为true
        bool allReady = true;

        // 第一步：遍历字典值，检查是否有false
        foreach (bool value in Ready.Values)
        {
            if (!value) // 只要有一个值为false，标记为未全部就绪
            {
                allReady = false;
                break; // 找到false后提前退出遍历，提升效率
            }
        }

        // 可选：添加退出条件，防止无限循环（比如超时）
        // 示例：累计等待10秒后退出
        int waitCount = 0;

        // 第二步：用while循环判断（根据allReady的值执行逻辑）
        // 场景1：等待所有值变为true（循环直到全部为true）
        while (!allReady)
        {
            Console.WriteLine("存在未就绪的玩家，等待中...");

            // 模拟：重新检查字典值（实际场景中可替换为刷新数据的逻辑）
            allReady = true;
            foreach (bool value in Ready.Values)
            {
                if (!value)
                {
                    allReady = false;
                    break;
                }
            }

            // 模拟等待（避免死循环，实际场景可替换为业务逻辑）
            System.Threading.Thread.Sleep(1000);

            waitCount++;
            if (waitCount >= 30)
            {
                Set_startTrigger(Parent, room);
                return;
            }
        }

        // 循环结束后输出结果
        if (allReady)
        {
            Set_startTrigger(Parent, room);
            return;
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
            oPacket.WriteByte(0);
            oPacket.WriteUInt(room.StartTicks);
            BroadCast(room.RoomId, oPacket);
        }
        room.TimeData = new Dictionary<int, uint>();
        room.Ranking = new Dictionary<int, int>();
        room.EndTicks = 0;
        Ready = new Dictionary<string, bool>();
        Console.WriteLine("StartTicks = {0}", room.StartTicks);
    }

    static void Set_settleTrigger(SessionGroup Parent)
    {
        var onceTimer = new System.Timers.Timer();
        onceTimer.Interval = 11000;
        onceTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, _event) => settleTrigger(Parent, s, _event));
        onceTimer.AutoReset = false;
        onceTimer.Start();
    }

    static void settleTrigger(SessionGroup Parent, object sender, System.Timers.ElapsedEventArgs e)
    {
        var roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
        if (roomId == -1)
        {
            return;
        }
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            return;
        }

        if (room.TimeData.Count < room.GetCount())
        {
            foreach (RoomMember member in room._slots)
            {
                if (member is Player player)
                {
                    if (!room.TimeData.ContainsKey(player.ID))
                    {
                        room.TimeData[player.ID] = uint.MaxValue;
                    }
                }
                else if (member is Ai ai)
                {
                    if (!room.TimeData.ContainsKey(ai.ID))
                    {
                        room.TimeData[ai.ID] = uint.MaxValue;
                    }
                }
            }
        }
        room.Ranking = GetAllRanks(room.TimeData);
        int redTeam = 0;
        int blueTeam = 0;
        var firstId = room.Ranking.First(kv => kv.Value == 0).Key;
        byte firstTeam = 0;
        if (RoomManager.TryGetIdDetail(roomId, firstId) is Player p)
        {
            firstTeam = p.Team;
        }
        else if (RoomManager.TryGetIdDetail(roomId, firstId) is Ai ai)
        {
            firstTeam = ai.Team;
        }
        Console.WriteLine("第一名 ID: {0} Team: {1}", firstId, firstTeam);
        foreach (RoomMember member in room._slots)
        {
            if (member is Player p2)
            {
                if (p2.Team == 2 && room.TimeData[p2.ID] != uint.MaxValue)
                {
                    blueTeam += teamPoints[room.Ranking[p2.ID]];
                }
                else if (p2.Team == 1 && room.TimeData[p2.ID] != uint.MaxValue)
                {
                    redTeam += teamPoints[room.Ranking[p2.ID]];
                }
            }
            if (member is Ai a2)
            {
                if (a2.Team == 2 && room.TimeData[a2.ID] != uint.MaxValue)
                {
                    blueTeam += teamPoints[room.Ranking[a2.ID]];
                }
                else if (a2.Team == 1 && room.TimeData[a2.ID] != uint.MaxValue)
                {
                    redTeam += teamPoints[room.Ranking[a2.ID]];
                }
            }
        }

        using (OutPacket outPacket = new OutPacket("GameNextStagePacket"))
        {
            outPacket.WriteByte(room.GameType);
            outPacket.WriteInt();
            outPacket.WriteInt();
            BroadCast(roomId, outPacket);
        }

        using (OutPacket outPacket = new OutPacket("GameResultPacket"))
        {
            if (room.GameType == 3)
            {
                if (redTeam == blueTeam)
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

            outPacket.WriteInt(room.GetPlayerCount()); // player count
            foreach (RoomMember member in room._IDs)
            {
                if (member is Player p3)
                {
                    p3.PlayerType = 2; // 初始玩家状态
                    var p3Config = ProfileService.GetProfileConfig(p3.Nickname);

                    outPacket.WriteInt(p3.ID); // player id
                    outPacket.WriteUInt(room.TimeData[p3.ID]);
                    outPacket.WriteByte();
                    outPacket.WriteUShort(p3Config.RiderItem.Set_Kart);
                    int playerRanking = room.Ranking[p3.ID];
                    int playerPoint = room.TimeData[p3.ID] == uint.MaxValue ? 0 : teamPoints[playerRanking];
                    Console.WriteLine("Player {0} 排名 {1} 得分 {2}", p3.ID, playerRanking, playerPoint);
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

                    Random rankRandom = new Random(playerRanking);
                    uint earnedRP = (uint)rankRandom.Next(0, 51); // RP范围: 0-50
                    uint earnedLucci = (uint)rankRandom.Next(0, 501); // Lucci范围: 0-500
                    p3Config.Rider.RP += earnedRP;
                    outPacket.WriteUInt(p3Config.Rider.RP);
                    outPacket.WriteUInt(earnedRP); // Earned RP
                    outPacket.WriteUInt(earnedLucci); // Earned Lucci
                    p3Config.Rider.Lucci += earnedLucci;
                    outPacket.WriteUInt(p3Config.Rider.Lucci);
                    ProfileService.Save(p3.Nickname, p3Config);
                    outPacket.WriteBytes(new byte[29]);

                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteInt(playerPoint);
                        outPacket.WriteByte(p3.Team); // Team
                    }
                    else
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteByte(0);
                    }
                    outPacket.WriteBytes(new byte[12]);
                    outPacket.WriteInt(1);
                    outPacket.WriteByte(0);
                    outPacket.WriteUShort(p3Config.RiderItem.Set_Character);
                    outPacket.WriteBytes(new byte[49]);
                    outPacket.WriteHexString("FF");
                    outPacket.WriteBytes(new byte[37]);
                    outPacket.WriteInt(p3Config.Rider.ClubMark_LOGO);
                    outPacket.WriteBytes(new byte[39]);
                }
            }

            outPacket.WriteInt(room.GetAiCount()); // AI count
            foreach (RoomMember member in room._IDs)
            {
                if (member is Ai a3)
                {
                    outPacket.WriteInt(a3.ID);
                    outPacket.WriteUInt(room.TimeData[a3.ID]);
                    outPacket.WriteByte();

                    // 获取 kart 属性值
                    outPacket.WriteShort(a3.Kart);
                    int AiRanking = room.Ranking[a3.ID];
                    int AiPoint = room.TimeData[a3.ID] == uint.MaxValue ? 0 : teamPoints[AiRanking];
                    Console.WriteLine("AI {0} 排名 {1} 得分 {2}", a3.ID, AiRanking, AiPoint);
                    outPacket.WriteInt(AiRanking);
                    outPacket.WriteShort(0);
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteByte(a3.Team); // Team
                        outPacket.WriteInt(AiPoint);
                    }
                    else
                    {
                        outPacket.WriteByte(0);
                        outPacket.WriteInt(0);
                    }
                }
            }
            Console.WriteLine("红队得分 {0} 蓝队得分 {1}", redTeam, blueTeam);
            outPacket.WriteBytes(new byte[34]);
            outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00");
            BroadCast(roomId, outPacket);
        }

        using (OutPacket outPacket = new OutPacket("GameControlPacket"))
        {
            outPacket.WriteInt(4);
            outPacket.WriteByte(0);
            outPacket.WriteUInt(room.EndTicks + 6000);
            BroadCast(roomId, outPacket);
        }

        room.StartTicks = 0;
        room.Started = false;

        int firstID = room.Ranking.FirstOrDefault(x => x.Value == 0).Key;
        if (room.RoomMaster < 8 && RoomManager.TryGetIdDetail(roomId, firstID) is Player p4)
        {
            room.RoomMaster = firstID;
            p4.PlayerType = 2;
        }
        else if (room.GetOBCount() < 1 && RoomManager.TryGetIdDetail(roomId, firstID) is Player p5)
        {
            room.RoomMaster = firstID;
            p5.PlayerType = 2;
        }
        Console.WriteLine("EndTicks = {0}", room.EndTicks + 5000);
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
            var state = iPacket.ReadByte();
            //start
            if (state == 0 && room.StartTicks == 0)
            {
                Start(Parent, roomId);
            }
            //finish
            else if (state == 2)
            {
                iPacket.ReadInt();
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
                    room.EndTicks = ConvertTick() + 10000;
                    using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                    {
                        oPacket.WriteInt(3);
                        oPacket.WriteByte(0);
                        oPacket.WriteUInt(room.EndTicks);
                        BroadCast(roomId, oPacket, Parent.Client.Nickname);
                    }
                    Set_settleTrigger(Parent);
                }
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
                oPacket.WriteInt(0);
                oPacket.WriteInt(rooms.Count); // 房间数量
                foreach (var _room in rooms)
                {
                    oPacket.WriteShort((short)_room.Key);
                    oPacket.WriteString(_room.Value.RoomName); // 房间名称
                    oPacket.WriteUInt(_room.Value.track); // 赛道
                    oPacket.WriteBool(_room.Value.Lock); // 是否上锁
                    oPacket.WriteByte(_room.Value.GameType); // 模式
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
            Console.WriteLine("Channel Switch, channel = {0}", channelData.Name);

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
            iPacket.ReadBytes(29);
            byte AiSwitch = iPacket.ReadByte();
            Console.WriteLine("AiSwitch = {0}", AiSwitch);

            var RoomId = RoomManager.CreateRoom();
            var Room = RoomManager.GetRoom(RoomId);
            Room.RoomName = RoomName;
            if (Password != "")
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
            Room.GameType = GameType;
            Room.RoomData = RoomData;
            Console.WriteLine("CreateRoom = {0}", RoomId);
            byte randomTrackGameType = 0;
            if (GameType == 2 || GameType == 4 || GameType == 14 || GameType == 54)
            {
                randomTrackGameType = 1;
            }
            Room.RandomTrackGameType = randomTrackGameType;

            if (GameType == 3 || GameType == 4)
            {
                byte slot = RoomManager.AddPlayer(RoomId, Parent.Client.Nickname, 2, 2, Parent);
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
                uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.pmap;
                if (pmap == 590)
                {
                    Room.RoomMaster = 0;
                }
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(2);
                    oPacket.WriteEncByte(GameType);
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
                uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.pmap;
                if (pmap == 590)
                {
                    Room.RoomMaster = 0;
                }
                using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
                {
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteByte(8);
                    oPacket.WriteEncByte(GameType);
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
                room.RemoveMember(ai.SlotId, "", out bool DeleteAi);
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
                room.EndTicks = ConvertTick() + 10000;
                using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                {
                    oPacket.WriteInt(3);
                    oPacket.WriteByte(0);
                    oPacket.WriteUInt(room.EndTicks);
                    BroadCast(roomId, oPacket);
                }
                Set_settleTrigger(Parent);
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

            if (team == 1)
            {
                room.redGauge += (value * 0.000125f / room.GetPlayerCount(team));
                if (room.redGauge > 1f) room.redGauge = 1f;
                using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
                {
                    oPacket.WriteByte(team);
                    oPacket.WriteFloat(room.redGauge);
                    BroadCast(roomId, oPacket, "", team);
                }
                if (room.redGauge == 1f) room.redGauge = 0f;
            }
            else if (team == 2)
            {
                room.blueGauge += (value * 0.000125f / room.GetPlayerCount(team));
                if (room.blueGauge > 1f) room.blueGauge = 1f;
                using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
                {
                    oPacket.WriteByte(team);
                    oPacket.WriteFloat(room.blueGauge);
                    BroadCast(roomId, oPacket, "", team);
                }
                if (room.blueGauge == 1f) room.blueGauge = 0f;
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
                oPacket.WriteByte(player.Team);
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

            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.pmap;
            if (pmap == 718 || pmap == 590)
            {
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
                    using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
                    {
                        outPacket.WriteString("结束游戏");
                        BroadCast(roomId, outPacket, Parent.Client.Nickname);
                    }
                    using (OutPacket outPacket = new OutPacket("GameResultPacket"))
                    {
                        outPacket.WriteByte(0);
                        outPacket.WriteInt(0); // player count
                        outPacket.WriteInt(0); // AI count
                        outPacket.WriteBytes(new byte[34]);
                        outPacket.WriteHexString("FF FF FF FF 00 00 00 00 00");
                        BroadCast(roomId, outPacket);
                    }
                    using (OutPacket outPacket = new OutPacket("GameControlPacket"))
                    {
                        outPacket.WriteInt(4);
                        outPacket.WriteByte(0);
                        outPacket.WriteUInt(ConvertTick());
                        BroadCast(roomId, outPacket);
                    }
                    room.Started = false;
                    room.StartTicks = 0;
                    return;
                }
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
            string Target = iPacket.ReadString();
            var player = RoomManager.GetPlayer(roomId, Target);
            if (player != null)
            {
                room.RoomMaster = player.ID;
                player.PlayerType = 2;
                GrSlotDataPacket(roomId);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PcStartMatching") || hash == Adler32Helper.GenerateAdler32_ASCII("PcCancelMatching"))
        {
            var roomList = RoomManager._rooms.Values.Where(r => !r.Lock).ToList();
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
                if (player)
                {
                    using (OutPacket outPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                    {
                        outPacket.WriteByte(1);
                        p.Session.Client.Send(outPacket);
                    }
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
                    var channel = GameSupport.Channels.FirstOrDefault(c => c.Value.GameType == room.GameType).Key;
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
            var channel = GameSupport.Channels.FirstOrDefault(c => c.Value.GameType == room.GameType).Key;
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
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqInviteGamePacket"))
        {
            uint UserID = iPacket.ReadUInt();
            string nickname = ClientManager.GetNickname(UserID);
            if (string.IsNullOrEmpty(nickname))
            {
                return; // 无效用户
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
                    outPacket.WriteString(ProfileService.GetProfileConfig(Parent.Client.Nickname).GameOption.QuickMsg.GetValueOrDefault(id) ?? "");
                    BroadCast(roomId, outPacket, Parent.Client.Nickname);
                }
                else
                {
                    outPacket.WriteString(ProfileService.GetProfileConfig(Parent.Client.Nickname).GameOption.TeamQuickMsg.GetValueOrDefault(id) ?? "");
                    BroadCast(roomId, outPacket, Parent.Client.Nickname, player.Team);
                }
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
        using (OutPacket outPacket = new OutPacket("GrSlotDataPacket"))
        {
            GrSlotDataPacket(roomId, outPacket);
            BroadCast(roomId, outPacket);
        }
    }

    static void GrSlotDataPacket(int roomId, OutPacket outPacket, bool enter = false)
    {
        var room = RoomManager.GetRoom(roomId);
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
                var pConfig = ProfileService.GetProfileConfig(p.Nickname);

                Console.WriteLine("Player Nickname = {0}, ID = {1}, SlotId = {2}", p.Nickname, p.ID, p.SlotId);
                if (enter)
                {
                    if (p.ID == room.RoomMaster && p.PlayerType == 2)
                    {
                        outPacket.WriteInt(3);
                    }
                    else
                    {
                        outPacket.WriteInt(p.PlayerType);
                    }
                }
                else
                {
                    outPacket.WriteInt(p.PlayerType); // Player Type, 2 = RoomMaster, 3 = AutoReady, 4 = Observer, 5 = Preparing, 7 = AI
                }
                outPacket.WriteUInt(ClientManager.GetUserNO(p.Nickname));
                IPEndPoint client = ClientManager.ClientToIPEndPoint(pConfig.Rider.ClientId);
                outPacket.WriteEndPoint(new IPEndPoint(client.Address, pConfig.Rider.P2pPort));
                outPacket.WriteEndPoint(new IPEndPoint(IPAddress.Any, 0));
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
                var pConfig = ProfileService.GetProfileConfig(p.Nickname);
                outPacket.WriteInt(p.PlayerType);
                outPacket.WriteUInt(ClientManager.GetUserNO(p.Nickname));
                IPEndPoint client = ClientManager.ClientToIPEndPoint(pConfig.Rider.ClientId);
                outPacket.WriteEndPoint(new IPEndPoint(client.Address, pConfig.Rider.P2pPort));
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

        bool ai = false;
        if (room.GetAiCount() > 0)
        {
            ai = true;
        }
        uint track = RandomTrack.GetRandomTrack(Parent.Client.Nickname, room.RandomTrackGameType, room.track, ai);
        room.trackTemp = track;

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

    static void GrCommandStartPacket(int roomId, Player p)
    {
        var room = RoomManager.GetRoom(roomId);
        using (OutPacket oPacket = new OutPacket("GrCommandStartPacket"))
        {
            oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSessionDataPacket")));
            GrSessionDataPacket(p.Nickname, oPacket);

            oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSlotDataPacket")));
            GrSlotDataPacket(roomId, oPacket, true);
            oPacket.WriteInt();

            //kart data
            StartGameData.GetKartSpac(oPacket, p.Nickname, room.SpeedType);

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
        }
        outPacket.WriteString(room.RoomName);
        outPacket.WriteString(room.LockPwd);
        outPacket.WriteByte(room.GameType);
        outPacket.WriteByte(room.SpeedType); //7
        outPacket.WriteInt(0);
        outPacket.WriteByte(0);
        outPacket.WriteInt(0);
        outPacket.WriteBytes(new byte[7]);
    }

    static void ChJoinRoomReplyPacket(SessionGroup Parent, int roomId, String pwd)
    {
        if (roomId == -1)
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
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteEncByte(room.GameType);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        if (room.Started)
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
        if (!string.IsNullOrEmpty(room.LockPwd) && pwd != room.LockPwd)
        {
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(2);
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteEncByte(room.GameType);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            return;
        }

        int playerCount = room.GetPlayerCount();
        byte slot = RoomManager.AddPlayer(roomId, Parent.Client.Nickname, 0, 2, Parent);
        Player player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
        if (slot == 255 || player == null)
        {
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteEncByte(room.GameType);
                outPacket.WriteBytes(new byte[5]);
                Parent.Client.Send(outPacket);
            }
            return;
        }
        else if (room.GameType == 3 || room.GameType == 4)
        {
            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.pmap;
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
                    outPacket.WriteEncByte(room.GameType);
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
                    outPacket.WriteEncByte(room.GameType);
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
                    outPacket.WriteEncByte(room.GameType);
                    outPacket.WriteBytes(new byte[5]);
                    Parent.Client.Send(outPacket);
                }
                return;
            }
        }
        else
        {
            uint pmap = ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.pmap;
            if (pmap == 718 || (playerCount < 1 && room.RoomMaster < 8))
            {
                room.RoomMaster = player.ID;
            }
            using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
            {
                outPacket.WriteByte(0);
                outPacket.WriteByte(1);
                outPacket.WriteByte(8);
                outPacket.WriteEncByte(room.GameType);
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
                    p.Session.Client.Send(outPacket);
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
                        p.Session.Client.Send(outPacket);
                    }
                    else if (p.Team == team)
                    {
                        p.Session.Client.Send(outPacket);
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
}
