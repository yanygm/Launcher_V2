using ExcData;
using KartLibrary.IO;
using KartRider.Common.Network;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace KartRider;

public static class MultyPlayer
{
    static string Nickname;
    public static List<short> itemProb_indi = new List<short>();
    public static List<short> itemProb_team = new List<short>();
    public static Dictionary<short, AICharacter> aiCharacterDict = new Dictionary<short, AICharacter>();
    public static Dictionary<short, AIKart> aiKartDict = new Dictionary<short, AIKart>();
    public static Dictionary<string, RoomList> roomList = new Dictionary<string, RoomList>();
    public static SpecialKartConfig kartConfig = new SpecialKartConfig();
    public static Dictionary<string, long> diff = new Dictionary<string, long>();
    public static int[] teamPoints = { 10, 8, 6, 5, 4, 3, 2, 1 };

    public static void milTime(uint time)
    {
        uint min = time / 60000;
        uint sec = time - min * 60000;
        sec = sec / 1000;
        uint mil = time % 1000;
        Console.WriteLine($"成绩: {min}:{sec}:{mil}");
    }

    public static long GetUpTime()
    {
        var Time = Environment.TickCount64;
        Console.WriteLine($"系统已运行总毫秒数: {Time} ms");
        return Time;
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

    static void Set_settleTrigger(SessionGroup Parent, string nickname, fileName filename)
    {
        var onceTimer = new System.Timers.Timer();
        onceTimer.Interval = 10000;
        onceTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, _event) => settleTrigger(Parent, nickname, filename, s, _event));
        onceTimer.AutoReset = false;
        onceTimer.Start();
    }

    static void settleTrigger(SessionGroup Parent, string nickname, fileName filename, object sender, System.Timers.ElapsedEventArgs e)
    {
        int roomId = RoomManager.TryGetRoomId(nickname);
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
        }
        using (OutPacket outPacket = new OutPacket("GameNextStagePacket"))
        {
            outPacket.WriteByte(room.GameType);
            outPacket.WriteInt();
            outPacket.WriteInt();
            Parent.Client.Send(outPacket);
        }
        using (OutPacket outPacket = new OutPacket("GameResultPacket"))
        {
            if (room.TimeData.Count < room.GetCount())
            {
                for (int i = 0; i < 8; i++)
                {
                    if (RoomManager.TryGetSlotStatus(roomId, (byte)i) != SlotStatus.Empty)
                    {
                        var Object = RoomManager.TryGetSlotDetail(roomId, (byte)i);
                        if (Object is Player player)
                        {
                            if (!room.TimeData.ContainsKey(player.ID))
                            {
                                room.TimeData[player.ID] = 4294967295;
                            }
                        }
                        else if (Object is Ai ai)
                        {
                            if (!room.TimeData.ContainsKey(ai.ID))
                            {
                                room.TimeData[ai.ID] = 4294967295;
                            }
                        }
                    }
                }
            }
            room.Ranking = GetAllRanks(room.TimeData);
            int redTeam = 0;
            int blueTeam = 0;
            var firstId = room.Ranking.First(kv => kv.Value == 0).Key;
            byte firstTeam = 0;
            if (RoomManager.TryGetSlotDetail(roomId, (byte)firstId) is Player p)
            {
                firstTeam = p.Team;
            }
            else if (RoomManager.TryGetSlotDetail(roomId, (byte)firstId) is Ai ai)
            {
                firstTeam = ai.Team;
            }
            Console.WriteLine("第一名 ID: {0} Team: {1}", firstId, firstTeam);
            for (int i = 0; i < 8; i++)
            {
                if (RoomManager.TryGetSlotDetail(roomId, (byte)i) is Player p2)
                {
                    if (p2.Team == 2)
                    {
                        blueTeam += teamPoints[room.Ranking[i]];
                    }
                    else if (p2.Team == 1)
                    {
                        redTeam += teamPoints[room.Ranking[i]];
                    }
                }
                if (RoomManager.TryGetSlotDetail(roomId, (byte)i) is Ai a2)
                {
                    if (a2.Team == 2)
                    {
                        blueTeam += teamPoints[room.Ranking[i]];
                    }
                    else if (a2.Team == 1)
                    {
                        redTeam += teamPoints[room.Ranking[i]];
                    }
                }
            }
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
            for (int i = 0; i < 8; i++)
            {
                if (RoomManager.TryGetSlotDetail(roomId, (byte)i) is Player p3)
                {
                    outPacket.WriteInt(p3.ID); // player id
                    outPacket.WriteUInt(room.TimeData[p3.ID]);
                    outPacket.WriteByte();
                    outPacket.WriteShort(ProfileService.ProfileConfigs[p3.Nickname].RiderItem.Set_Kart);
                    int playerRanking = room.Ranking[i];
                    int playerPoint = teamPoints[playerRanking];
                    Console.WriteLine("Player {0} 排名 {1} 得分 {2}", p3.ID, playerRanking, playerPoint);
                    outPacket.WriteInt(playerRanking);
                    outPacket.WriteShort();
                    outPacket.WriteByte();
                    outPacket.WriteUInt(ProfileService.ProfileConfigs[p3.Nickname].Rider.RP += 10000);
                    outPacket.WriteInt(10000); // Earned RP
                    outPacket.WriteInt(10000); // Earned Lucci
                    outPacket.WriteUInt(ProfileService.ProfileConfigs[p3.Nickname].Rider.Lucci += 10000);
                    outPacket.WriteBytes(new byte[29]);
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        if (room.TimeData[p3.ID] == 4294967295)
                        {
                            outPacket.WriteInt(0);
                        }
                        else
                        {
                            outPacket.WriteInt(playerPoint);
                        }
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
                    outPacket.WriteShort(ProfileService.ProfileConfigs[p3.Nickname].RiderItem.Set_Character);
                    outPacket.WriteBytes(new byte[49]);
                    outPacket.WriteHexString("FF");
                    outPacket.WriteHexString("00 00 00 00 00 00 00 E3 23 07 40 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                    outPacket.WriteInt(ProfileService.ProfileConfigs[p3.Nickname].Rider.ClubMark_LOGO);
                    outPacket.WriteBytes(new byte[39]);
                }
            }

            outPacket.WriteInt(room.GetAiCount()); // AI count
            for (int i = 0; i < 8; i++)
            {
                if (RoomManager.TryGetSlotDetail(roomId, (byte)i) is Ai a3)
                {
                    outPacket.WriteInt(a3.ID);
                    if (room.TimeData.ContainsKey(a3.ID) && room.TimeData[a3.ID] > 0)
                    {
                        outPacket.WriteUInt(room.TimeData[a3.ID]);
                    }
                    else
                    {
                        outPacket.WriteHexString("FFFFFFFF");
                    }
                    outPacket.WriteByte();

                    // 获取 kart 属性值
                    outPacket.WriteShort(a3.Kart);
                    int AiRanking = room.Ranking[a3.ID];
                    int AiPoint = teamPoints[AiRanking];
                    Console.WriteLine("AI {0} 排名 {1} 得分 {2}", a3.ID, AiRanking, AiPoint);
                    outPacket.WriteInt(AiRanking);
                    outPacket.WriteHexString("A0 60");
                    if (room.GameType == 3 || room.GameType == 4)
                    {
                        outPacket.WriteByte(a3.Team); // Team
                        if (room.TimeData[a3.ID] == 4294967295)
                        {
                            outPacket.WriteInt(0);
                        }
                        else
                        {
                            outPacket.WriteInt(AiPoint);
                        }
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
            Parent.Client.Send(outPacket);
        }
        using (OutPacket outPacket = new OutPacket("GameControlPacket"))
        {
            outPacket.WriteInt(4);
            outPacket.WriteByte(0);
            outPacket.WriteLong(room.EndTicks + 5000);
            Parent.Client.Send(outPacket);
        }
        Console.WriteLine("EndTicks = {0}", room.EndTicks + 5000);
    }

    static short ParseShort(XAttribute attribute)
    {
        if (attribute == null || !short.TryParse(attribute.Value, out short result))
        {
            return 0; // 默认值或错误处理
        }
        return result;
    }

    public static void Clientsession(SessionGroup Parent, string nickname, uint hash, InPacket iPacket)
    {
        fileName filename = new fileName();
        if (nickname != "")
        {
            if (!FileName.FileNames.ContainsKey(nickname))
            {
                FileName.Load(nickname);
            }
            filename = FileName.FileNames[nickname];
        }
        if (hash == Adler32Helper.GenerateAdler32_ASCII("GameSlotPacket", 0))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            iPacket.ReadInt();
            uint item = iPacket.ReadUInt();
            byte type = iPacket.ReadByte();
            if (item == 0 && type == 12)
            {
                iPacket.ReadBytes(11);
                var GopCourseHash = iPacket.ReadUInt();
                var GoCourseHash = iPacket.ReadUInt();
                if (GopCourseHash == Adler32Helper.GenerateAdler32_ASCII("GopCourse", 0) && GoCourseHash == Adler32Helper.GenerateAdler32_ASCII("GoCourse", 0))
                {
                    iPacket.ReadBytes(8);
                    var goal = iPacket.ReadString(false);
                    if (goal == "goal")
                    {
                        var ArrivalTicks = iPacket.ReadUInt();
                        var slotId = RoomManager.GetPlayerSlotId(roomId, nickname);
                        if (slotId != -1)
                        {
                            if (room.EndTicks == 0)
                            {
                                room.EndTicks = ArrivalTicks + 5000;
                                using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                                {
                                    oPacket.WriteInt(3);
                                    oPacket.WriteByte(0);
                                    oPacket.WriteLong(room.EndTicks);
                                    Parent.Client.Send(oPacket);
                                }
                                Set_settleTrigger(Parent, nickname, filename);
                            }
                        }
                        Console.WriteLine("GameSlotPacket, Arrivaled. Ticks = {0}", ArrivalTicks);
                    }
                }
            }
            else if (item == 4294967295 && iPacket.Length == 77)
            {
                byte[] data1 = iPacket.ReadBytes(25);
                short id1 = iPacket.ReadShort();
                byte unk1 = iPacket.ReadByte();
                byte[] data2 = iPacket.ReadBytes(4);
                iPacket.ReadByte();
                iPacket.ReadShort();
                byte[] data3 = iPacket.ReadBytes(29);
                short skill = GameSupport.RandomItemSkill(nickname, room.GameType);
                using (OutPacket oPacket = new OutPacket("GameSlotPacket"))
                {
                    oPacket.WriteInt();
                    oPacket.WriteUInt(item);
                    oPacket.WriteByte(type);
                    oPacket.WriteBytes(data1);
                    oPacket.WriteShort(skill);
                    oPacket.WriteByte(1);
                    oPacket.WriteBytes(data2);
                    oPacket.WriteByte(2);
                    oPacket.WriteShort(skill);
                    oPacket.WriteBytes(data3);
                    Parent.Client.Send(oPacket);
                }
            }
            if (type == 11)
            {
                var uni = iPacket.ReadByte();
                var skill = iPacket.ReadShort();
                List<short> skills = V2Specs.GetSkills(nickname);
                if (skills.Contains(13) && skill == 3)
                {
                    GameSupport.AttackedSkill(Parent, nickname, type, uni, 10);
                }
                if (kartConfig.SkillAttacked.TryGetValue(ProfileService.ProfileConfigs[nickname].RiderItem.Set_Kart, out var kartSkills))
                {
                    if (kartSkills.TryGetValue(skill, out var targetSkill))
                    {
                        GameSupport.AttackedSkill(Parent, nickname, type, uni, targetSkill);
                    }
                }
                Console.WriteLine("GameSlotPacket, Attacked. Skill = {0}", skill);
            }
            else if (type == 18)
            {
                var uni = iPacket.ReadByte();
                iPacket.ReadShort();
                iPacket.ReadByte();
                var skill = iPacket.ReadShort();
                List<short> skills = V2Specs.GetSkills(nickname);
                if (skills.Contains(14) && skill == 5)
                {
                    GameSupport.AddItemSkill(Parent, nickname, 6);
                }
                if (kartConfig.SkillMappings.TryGetValue(ProfileService.ProfileConfigs[nickname].RiderItem.Set_Kart, out var kartSkills))
                {
                    if (kartSkills.TryGetValue(skill, out var targetSkill))
                    {
                        GameSupport.AddItemSkill(Parent, nickname, targetSkill);
                    }
                }
                Console.WriteLine("GameSlotPacket, Mapping. Skill = {0}", skill);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameControlPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            var state = iPacket.ReadByte();
            //start
            long StartTicks = GetUpTime() + 10000;
            room.StartTicks = StartTicks;
            if (state == 0)
            {
                using (OutPacket oPacket = new OutPacket("GameAiMasterSlotNoticePacket"))
                {
                    oPacket.WriteInt();
                    Parent.Client.Send(oPacket);
                }
                using (OutPacket oPacket = new OutPacket("GameControlPacket"))
                {
                    oPacket.WriteInt(1);
                    oPacket.WriteByte(0);
                    oPacket.WriteLong(StartTicks - MultyPlayer.diff[nickname]);
                    Parent.Client.Send(oPacket);
                }
                room.TimeData = new Dictionary<int, uint>();
                room.EndTicks = 0;
                Console.WriteLine("StartTicks = {0}", StartTicks);
            }
            //finish
            else if (state == 2)
            {
                iPacket.ReadInt();
                var time = iPacket.ReadUInt();
                using (OutPacket oPacket = new OutPacket("GameRaceTimePacket"))
                {
                    oPacket.WriteInt();
                    oPacket.WriteUInt(time);
                    Parent.Client.Send(oPacket);
                }
                var slotId = RoomManager.GetPlayerSlotId(roomId, nickname);
                if (slotId != -1)
                {
                    var player = RoomManager.GetPlayer(roomId, nickname);
                    room.TimeData.TryAdd(player.ID, time);
                    Console.WriteLine("GameControlPacket, slotId = {0}, Time = {1}", slotId, time);
                }
                if (room.EndTicks == 0)
                {
                    room.EndTicks = StartTicks + time + 5000;
                    Set_settleTrigger(Parent, nickname, filename);
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
                    oPacket.WriteByte(_room.Value.Lock); // 是否上锁
                    oPacket.WriteByte(_room.Value.GameType); // 模式
                    oPacket.WriteByte(_room.Value.SpeedType); // 速度模式
                    oPacket.WriteByte(0);
                    oPacket.WriteByte(8); // 房间最大人数
                    oPacket.WriteByte((byte)_room.Value.GetCount()); // 房间人数
                    oPacket.WriteHexString("00 00 00 00 00 00");
                }
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelSwitch", 0))
        {
            Nickname = nickname;

            IPEndPoint serverEndPoint = Parent.Client.Socket.LocalEndPoint as IPEndPoint;
            if (serverEndPoint == null) return;

            int length = iPacket.ReadInt();
            iPacket.ReadBytes(length);
            byte channel = iPacket.ReadByte();
            Console.WriteLine("Channel Switch, channel = {0}", channel);
            //StartGameRacing.GameRacing_SpeedType = 4;
            if (channel == 72 || channel == 55 || channel == 63)
            {
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(0);
                    //oPacket.WriteInt(channeldata1);
                    oPacket.WriteInt(4);
                    oPacket.WriteEndPoint(serverEndPoint);
                    Parent.Client.Send(oPacket);
                }
                roomList.TryAdd(nickname, new RoomList());
                roomList[nickname].RandomTrackGameType = 0;
                roomList[nickname].SpeedType = 7;
                roomList[nickname].GameType = 1;
            }
            else if (channel == 73 || channel == 35)
            {
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(0);
                    //oPacket.WriteInt(channeldata1);
                    oPacket.WriteInt(3);
                    oPacket.WriteEndPoint(serverEndPoint);
                    Parent.Client.Send(oPacket);
                }
                roomList.TryAdd(nickname, new RoomList());
                roomList[nickname].RandomTrackGameType = 0;
                roomList[nickname].SpeedType = 7;
                roomList[nickname].GameType = 3;
            }
            else if (channel == 25)
            {
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(4);
                    oPacket.WriteEndPoint(serverEndPoint);
                    Parent.Client.Send(oPacket);
                }
                roomList.TryAdd(nickname, new RoomList());
                roomList[nickname].RandomTrackGameType = 0;
                roomList[nickname].SpeedType = 4;
                roomList[nickname].GameType = 1;
            }
            else if (channel == 26)
            {
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(3);
                    oPacket.WriteEndPoint(serverEndPoint);
                    Parent.Client.Send(oPacket);
                }
                roomList.TryAdd(nickname, new RoomList());
                roomList[nickname].RandomTrackGameType = 0;
                roomList[nickname].SpeedType = 4;
                roomList[nickname].GameType = 3;
            }
            else if (channel == 70 || channel == 57 || channel == 64)
            {
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(1);
                    oPacket.WriteInt(2);
                    oPacket.WriteEndPoint(serverEndPoint);
                    Parent.Client.Send(oPacket);
                }
                roomList.TryAdd(nickname, new RoomList());
                roomList[nickname].RandomTrackGameType = 1;
                roomList[nickname].SpeedType = 7;
                roomList[nickname].GameType = 2;
            }
            else if (channel == 71 || channel == 38)
            {
                using (OutPacket oPacket = new OutPacket("PrChannelSwitch"))
                {
                    oPacket.WriteInt(0);
                    oPacket.WriteInt(1);
                    oPacket.WriteEndPoint(serverEndPoint);
                    Parent.Client.Send(oPacket);
                }
                roomList.TryAdd(nickname, new RoomList());
                roomList[nickname].RandomTrackGameType = 1;
                roomList[nickname].SpeedType = 7;
                roomList[nickname].GameType = 4;
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("ChGetCurrentGpReplyPacket"))
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteByte(0);
                    Parent.Client.Send(outPacket);
                }
            }
            //GameSupport.OnDisconnect();
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqChannelMovein", 0))
        {
            IPEndPoint clientEndPoint = Parent.Client.Socket.RemoteEndPoint as IPEndPoint;
            if (clientEndPoint == null) return;
            string clientId = ClientManager.GetClientId(clientEndPoint);
            var ClientGroup = ClientManager.ClientGroups[clientId];
            if (ClientGroup.Nickname == "" && Nickname != "")
            {
                ClientGroup.Nickname = Nickname;
            }
            using (OutPacket oPacket = new OutPacket("PrChannelMoveIn"))
            {
                //oPacket.WriteHexString("01 3d a4 3d 49 8f 99 3d a4 3d 49 90 99");
                oPacket.WriteByte(1);
                oPacket.WriteEndPoint(IPAddress.Any, 39311);
                oPacket.WriteEndPoint(IPAddress.Any, 39312);
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PqMissionAttendPacket", 0))
        {
            using (OutPacket oPacket = new OutPacket("PrMissionAttendPacket"))
            {
                oPacket.WriteInt(3);
                oPacket.WriteInt(0);
                oPacket.WriteInt(15);
                oPacket.WriteInt(0);
                oPacket.WriteInt(-1);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                oPacket.WriteInt(109);
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChCreateRoomRequestPacket", 0))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            string RoomName = iPacket.ReadString();    //room name
            Console.WriteLine("RoomName = {0}, len = {1}", RoomName, RoomName.Length);
            string Password = iPacket.ReadString();
            Console.WriteLine("Password = {0}, len = {1}", Password, Password.Length);
            var unk1 = iPacket.ReadByte(); //7c
            iPacket.ReadInt();
            var AiCount = iPacket.ReadInt();
            Console.WriteLine("AiCount = {0}", AiCount);
            iPacket.ReadInt();
            iPacket.ReadInt();
            byte[] RoomUnkBytes = iPacket.ReadBytes(32);
            var unk2 = iPacket.ReadBytes(29);
            byte AiSwitch = iPacket.ReadByte();
            Console.WriteLine("AiSwitch = {0}", AiSwitch);
            using (OutPacket oPacket = new OutPacket("ChCreateRoomReplyPacket"))
            {
                oPacket.WriteByte(1);
                oPacket.WriteByte(1);
                oPacket.WriteByte(2);
                oPacket.WriteByte(unk1);
                Parent.Client.Send(oPacket);
            }
            var roomData = roomList[nickname];
            var RoomId = RoomManager.CreateRoom();
            var Room = RoomManager.GetRoom(RoomId);
            Console.WriteLine("CreateRoom = {0}", RoomId);
            if (roomData.GameType == 3 || roomData.GameType == 4)
            {
                bool CreateBool = RoomManager.AddPlayer(RoomId, nickname, 2, 2);
                if (CreateBool == false)
                {
                    Console.WriteLine("CreateRoom Failed");
                }
            }
            else
            {
                bool CreateBool = RoomManager.AddPlayer(RoomId, nickname, 0, 2);
                if (CreateBool == false)
                {
                    Console.WriteLine("CreateRoom Failed");
                }
            }
            Room.RoomName = RoomName;
            if (Password != "")
            {
                Room.Lock = 1;
            }
            Room.LockPwd = Password;
            Room.RoomUnkBytes = RoomUnkBytes;
            Room.SpeedType = roomData.SpeedType;
            Room.GameType = roomData.GameType;
            Room.RandomTrackGameType = roomData.RandomTrackGameType;
            if (AiCount > 0 && AiSwitch == 6)
            {
                // 新增 AI 数量
                AddAi(Room, AiCount - 1, roomData.RandomTrackGameType);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrFirstRequestPacket"))
        {
            GrSessionDataPacket(Parent, nickname);
            //Thread.Sleep(10);
            GrSlotDataPacket(Parent, nickname);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrChangeTrackPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            room.track = iPacket.ReadUInt();
            iPacket.ReadInt();
            room.RoomUnkBytes = iPacket.ReadBytes(32);
            Console.WriteLine("Gr Track Changed : {0}", RandomTrack.GetTrackName(room.track));
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestSetSlotStatePacket"))
        {
            int Data = iPacket.ReadInt();
            GrSlotDataPacket(Parent, nickname);
            //GrSlotStatePacket(Parent, Data);
            GrReplySetSlotStatePacket(Parent, nickname, Data);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestClosePacket"))
        {
            using (OutPacket oPacket = new OutPacket("GrReplyClosePacket"))
            {
                //oPacket.WriteHexString("ff 76 05 5d 01");
                oPacket.WriteUInt(Adler32Helper.GenerateAdler32_ASCII(nickname, 0));
                oPacket.WriteByte(1);
                oPacket.WriteInt(7);
                oPacket.WriteInt(7);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestStartPacket"))
        {
            using (OutPacket oPacket = new OutPacket("GrReplyStartPacket"))
            {
                oPacket.WriteInt(0);
                Parent.Client.Send(oPacket);
            }
            using (OutPacket oPacket = new OutPacket("GrCommandStartPacket"))
            {
                int roomId = RoomManager.TryGetRoomId(nickname);
                var room = RoomManager.GetRoom(roomId);
                if (room == null)
                {
                    Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
                }
                oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSessionDataPacket")));
                GrSessionDataPacket(Parent, nickname, oPacket);

                oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("GrSlotDataPacket")));
                GrSlotDataPacket(Parent, nickname, oPacket);
                oPacket.WriteInt();

                //kart data
                StartGameData.GetKartSpac(oPacket, nickname, room.SpeedType);

                oPacket.WriteInt(room.GetAiCount()); //AI count
                if (room.GetAiCount() > 0)
                {
                    for (int i = 0; i < room.GetAiCount(); i++)
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
                uint track = RandomTrack.GetRandomTrack(nickname, room.RandomTrackGameType, room.track);
                oPacket.WriteUInt(track); //track name hash
                oPacket.WriteInt(10000);

                oPacket.WriteInt();
                oPacket.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes("MissionInfo")));
                oPacket.WriteHexString("00000000000000000000FFFFFFFF000000000000000000");
                //oPacket.WriteString("[applied param]\r\ntransAccelFactor='1.8555' driftEscapeForce='4720' steerConstraint='24.95' normalBoosterTime='3860' \r\npartsBoosterLock='1' \r\n\r\n[equipped / default parts param]\r\ntransAccelFactor='1.86' driftEscapeForce='2120' steerConstraint='2.7' normalBoosterTime='860' \r\n\r\n\r\n[gamespeed param]\r\ntransAccelFactor='-0.0045' driftEscapeForce='2600' steerConstraint='22.25' normalBoosterTime='3000' \r\n\r\n\r\n[factory enchant param]\r\n");
                Console.WriteLine("Track : {0}", RandomTrack.GetTrackName(track));
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("PcReportStateInGame", 0))
        {
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChLeaveRoomRequestPacket"))
        {
            using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
            {
                oPacket.WriteByte(1);
                Parent.Client.Send(oPacket);
            }
            int roomId = RoomManager.TryGetRoomId(nickname);
            int slotId = RoomManager.GetPlayerSlotId(roomId, nickname);
            if (slotId != -1)
            {
                RoomManager.RemovePlayer(roomId, (byte)slotId);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrRequestBasicAiPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            int unk1 = iPacket.ReadInt();
            Console.WriteLine("GrRequestBasicAiPacket, unk1 = {0}", unk1);
            byte team = (byte)((unk1 < 4) ? 2 : 1);
            var selector = new DictionaryRandomSelector();
            List<short> randomCharIds = selector.GetRandomCharacterIds(aiCharacterDict, 1);
            List<short> randomKartIds = new List<short>();
            if (room.RandomTrackGameType == 0)
            {
                randomKartIds = selector.GetRandomKartIds(aiKartDict, 1, true, false);
            }
            else if (room.RandomTrackGameType == 1)
            {
                randomKartIds = selector.GetRandomKartIds(aiKartDict, 1, false, true);
            }
            if (RoomManager.TryGetSlotStatus(roomId, (byte)unk1) == SlotStatus.Ai)
            {
                using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                {
                    oPacket.WriteInt(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteInt(unk1);
                    oPacket.WriteHexString("0000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                    Parent.Client.Send(oPacket);
                }
                room.RemoveMember((byte)unk1, out bool DeleteAi);
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
                    using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                    {
                        oPacket.WriteInt(0);
                        oPacket.WriteByte(1);
                        oPacket.WriteInt(unk1);
                        oPacket.WriteShort(targetCharId);
                        oPacket.WriteShort(ridIndex ?? 0);
                        oPacket.WriteShort(targetKartId);
                        oPacket.WriteShort(balloonId ?? 0);
                        oPacket.WriteShort(headbandId ?? 0);
                        oPacket.WriteShort(goggleId ?? 0);
                        if (room.GameType == 3 || room.GameType == 4)
                        {
                            oPacket.WriteByte(team);
                        }
                        else
                        {
                            oPacket.WriteByte(0);
                        }
                        oPacket.WriteShort(0);
                        oPacket.WriteShort(0);
                        oPacket.WriteHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                        Parent.Client.Send(oPacket);
                    }
                    room.TrySetAi((byte)unk1, new Ai
                    {
                        Character = targetCharId,
                        Rid = ridIndex ?? 0,
                        Kart = targetKartId,
                        Balloon = balloonId ?? 0,
                        HeadBand = headbandId ?? 0,
                        Goggle = goggleId ?? 0,
                        Team = team
                    });
                }
            }
            using (OutPacket oPacket = new OutPacket("GrReplyBasicAiPacket"))
            {
                oPacket.WriteByte(1);
                oPacket.WriteHexString("2CFB6605");
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameAiGoalinPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            var Id = iPacket.ReadInt();
            var Time = iPacket.ReadUInt();
            using (OutPacket oPacket = new OutPacket("GameRaceTimePacket"))
            {
                oPacket.WriteInt(Id);
                oPacket.WriteUInt(Time);
                Parent.Client.Send(oPacket);
            }
            room.TimeData.TryAdd(Id, Time);
            Console.WriteLine("GameAiGoalinPacket, Id = {0}, Time = {1}", Id, Time);
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GameTeamBoosterRequestAddGaugePacket"))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            var team = iPacket.ReadByte();
            var value = iPacket.ReadFloat();
            Console.WriteLine("GameTeamBoosterRequestAddGaugePacket, teams = {0}, value = {1}", team, value);
            if (team == 1)
            {
                room.redGauge += (value / 8000f);
                if (room.redGauge > 1f) room.redGauge = 1f;
                using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
                {
                    oPacket.WriteByte(team);
                    oPacket.WriteFloat(room.redGauge);
                    Parent.Client.Send(oPacket);
                }
                if (room.redGauge == 1f) room.redGauge = 0f;
            }
            else if (team == 2)
            {
                room.blueGauge += (value / 8000f);
                if (room.blueGauge > 1f) room.blueGauge = 1f;
                using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
                {
                    oPacket.WriteByte(team);
                    oPacket.WriteFloat(room.blueGauge);
                    Parent.Client.Send(oPacket);
                }
                if (room.blueGauge == 1f) room.blueGauge = 0f;
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("GrChangeTeamPacket"))
        {
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine("CreateRoom Failed, roomId = {0}", roomId);
            }
            var player = RoomManager.GetPlayer(roomId, nickname);
            if (player == null)
            {
                Console.WriteLine("GetPlayer Failed, roomId = {0}, nickname = {1}", roomId, nickname);
                return;
            }
            byte team = (byte)(3 - player.Team);
            var Bool = RoomManager.ChangeMemberTeam(roomId, player.SlotId, team);
            Console.WriteLine("ChangeMemberTeam, roomId = {0}, ID = {1}, Team = {2}, {3}", roomId, player.ID, team, Bool);
            using (OutPacket oPacket = new OutPacket("GrChangeTeamPacketReply"))
            {
                oPacket.WriteInt(player.ID);
                oPacket.WriteByte(player.Team);
                for (byte i = 0; i < 8; i++)
                {
                    if (i == player.SlotId)
                    {
                        oPacket.WriteInt(player.ID);
                    }
                    else
                    {
                        oPacket.WriteHexString("FFFFFFFF");
                    }
                }
                Parent.Client.Send(oPacket);
            }
            return;
        }
        else if (hash == Adler32Helper.GenerateAdler32_ASCII("ChJoinRoomRequestPacket"))
        {
            var roomId = iPacket.ReadByte();
            var unk = iPacket.ReadByte();
            var pwd = iPacket.ReadString();
            Console.WriteLine("ChJoinRoomRequestPacket, roomId = {0}, unk = {1}, pwd = {2}", roomId, unk, pwd);

            var room = RoomManager.GetRoom(roomId);
            if (pwd == room.LockPwd)
            {
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(0);
                    outPacket.WriteInt();
                    outPacket.WriteInt();
                    Parent.Client.Send(outPacket);
                }
                RoomManager.AddPlayer(roomId, nickname, 2, 2);
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("ChJoinRoomReplyPacket"))
                {
                    outPacket.WriteByte(2);
                    outPacket.WriteInt();
                    outPacket.WriteInt();
                    Parent.Client.Send(outPacket);
                }
            }
            return;
        }
        else
        {
            return;
        }
    }

    static void GrSlotDataPacket(SessionGroup Parent, string nickname)
    {
        using (OutPacket outPacket = new OutPacket("GrSlotDataPacket"))
        {
            GrSlotDataPacket(Parent, nickname, outPacket);
            Parent.Client.Send(outPacket);
        }
    }

    static void GrSlotDataPacket(SessionGroup Parent, string nickname, OutPacket outPacket)
    {
        int roomId = RoomManager.TryGetRoomId(nickname);
        Console.WriteLine("GrSlotDataPacket, roomId = {0}", roomId);
        var room = RoomManager.GetRoom(roomId);
        if (room == null)
        {
            Console.WriteLine("GetRoom Failed, roomId = {0}", roomId);
        }
        var player = RoomManager.GetPlayer(roomId, nickname);
        if (player == null)
        {
            Console.WriteLine("GetPlayer Failed, roomId = {0}, nickname = {1}", roomId, nickname);
        }
        outPacket.WriteUInt(room.track); // track name hash
        outPacket.WriteInt(0);
        outPacket.WriteBytes(room.RoomUnkBytes); // 32
        outPacket.WriteInt(0); // RoomMaster
        outPacket.WriteInt(0); // 2
        outPacket.WriteInt(0); // outPacket.WriteShort(); outPacket.WriteShort(3);
        outPacket.WriteShort(0); // 797
        outPacket.WriteByte(0);
        for (int i = 0; i < 4; i++) outPacket.WriteByte();
        for (int i = 0; i < 4; i++) outPacket.WriteInt();

        /* ---- Player ---- */
        Console.WriteLine("PlayerCount = {0}", room.GetPlayerCount());
        for (int i = 0; i < 8; i++)
        {
            if (RoomManager.TryGetSlotDetail(roomId, (byte)i) is Player p)
            {
                Console.WriteLine("Player Nickname = {0}, SlotId = {1}", p.Nickname, p.SlotId);
                outPacket.WriteInt(p.PlayerType); // Player Type, 2 = RoomMaster, 3 = AutoReady, 4 = Observer, 5 = Preparing, 7 = AI
                outPacket.WriteUInt(Adler32Helper.GenerateAdler32_ASCII(p.Nickname, 0));
                outPacket.WriteEndPoint(ClientManager.ClientToIPEndPoint(ProfileService.ProfileConfigs[p.Nickname].Rider.Client));
                outPacket.WriteInt(ProfileService.ProfileConfigs[p.Nickname].Rider.ClubMark_LOGO);
                outPacket.WriteShort(0);
                outPacket.WriteString(p.Nickname);
                outPacket.WriteShort(ProfileService.ProfileConfigs[p.Nickname].Rider.Emblem1);
                outPacket.WriteShort(ProfileService.ProfileConfigs[p.Nickname].Rider.Emblem2);
                outPacket.WriteShort(0);
                GameSupport.GetRider(Parent, p.Nickname, outPacket);
                outPacket.WriteString(ProfileService.ProfileConfigs[p.Nickname].Rider.Card);
                outPacket.WriteUInt(ProfileService.ProfileConfigs[p.Nickname].Rider.RP);
                if (room.GameType == 3 || room.GameType == 4)
                {
                    outPacket.WriteByte(p.Team);
                }
                else
                {
                    outPacket.WriteByte(0);
                }
                outPacket.WriteByte();
                outPacket.WriteByte();
                for (int j = 0; j < 8; j++) outPacket.WriteInt();
                outPacket.WriteInt(1500); //outPacket.WriteInt(1500);
                outPacket.WriteInt(1500); //outPacket.WriteInt(2000);
                outPacket.WriteInt(0); //outPacket.WriteInt();
                outPacket.WriteInt(2000); //outPacket.WriteInt(2000);
                outPacket.WriteInt(5); //outPacket.WriteInt(5);

                outPacket.WriteHexString("FF 00 00 00"); //"FF 00 00 01"

                outPacket.WriteByte(3); //3
                if (ProfileService.ProfileConfigs[p.Nickname].Rider.ClubMark_LOGO == 0)
                {
                    outPacket.WriteString("");
                    outPacket.WriteInt(0);
                }
                else
                {
                    outPacket.WriteString(ProfileService.ProfileConfigs[p.Nickname].Rider.ClubName);
                    outPacket.WriteInt(ProfileService.ProfileConfigs[p.Nickname].Rider.ClubMark_LOGO);
                }
                outPacket.WriteInt();
                outPacket.WriteInt();
                outPacket.WriteInt();
                outPacket.WriteByte();
                outPacket.WriteInt();
                outPacket.WriteShort();
            }
        }

        /* ---- Ai ---- */
        Console.WriteLine("AiCount = {0}", room.GetAiCount());
        for (int i = 0; i < 8; i++)
        {
            if (RoomManager.TryGetSlotDetail(roomId, (byte)i) is Ai a)
            {
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
            else if (RoomManager.TryGetSlotStatus(roomId, (byte)i) == SlotStatus.Empty)
            {
                outPacket.WriteInt(0);
            }
        }
        outPacket.WriteBytes(new byte[32]);
        for (int i = 0; i < 8; i++)
        {
            var Object = RoomManager.TryGetSlotDetail(roomId, (byte)i);
            if (Object is Player player2 && player2.SlotId == i)
            {
                outPacket.WriteInt(player2.ID);
            }
            else if (Object is Ai ai2 && ai2.SlotId == i)
            {
                outPacket.WriteInt(ai2.ID);
            }
            else
            {
                outPacket.WriteHexString("FFFFFFFF");
            }
        }
    }

    static void GrSlotStatePacket(SessionGroup Parent, int Data)
    {
        using (OutPacket oPacket = new OutPacket("GrSlotStatePacket"))
        {
            oPacket.WriteInt(Data);
            oPacket.WriteBytes(new byte[60]);
            Parent.Client.Send(oPacket);
        }
    }

    static void GrReplySetSlotStatePacket(SessionGroup Parent, string nickname, int Data)
    {
        using (OutPacket oPacket = new OutPacket("GrReplySetSlotStatePacket"))
        {
            oPacket.WriteUInt(Adler32Helper.GenerateAdler32_ASCII(nickname, 0));
            oPacket.WriteInt(1);
            oPacket.WriteByte(0);
            oPacket.WriteInt(Data);
            Parent.Client.Send(oPacket);
        }
    }

    static void GrSessionDataPacket(SessionGroup Parent, string nickname)
    {
        using (OutPacket oPacket = new OutPacket("GrSessionDataPacket"))
        {
            GrSessionDataPacket(Parent, nickname, oPacket);
            Parent.Client.Send(oPacket);
        }
    }

    static void GrSessionDataPacket(SessionGroup Parent, string nickname, OutPacket outPacket)
    {
        int roomId = RoomManager.TryGetRoomId(nickname);
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
        outPacket.WriteByte(8);
        outPacket.WriteInt(0);
        outPacket.WriteInt(0);
        outPacket.WriteByte(0);
        outPacket.WriteByte(0);
        outPacket.WriteByte(0);
    }

    // 添加指定数量的 Ai
    static void AddAi(GameRoom room, int count, byte randomTrackGameType)
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
                byte team = (byte)((i < 4) ? 2 : 1);
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
                if (room.TrySetAi((byte)i, ai))
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
}

public class RoomList
{
    public byte RandomTrackGameType { get; set; }
    public byte SpeedType { get; set; }
    public byte GameType { get; set; }
}
