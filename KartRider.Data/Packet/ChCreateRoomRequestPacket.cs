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
    /// <summary>处理 <see cref="PacketName.ChCreateRoomRequestPacket"/> 数据包。</summary>
    internal sealed class ChCreateRoomRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChCreateRoomRequestPacket;

        /// <summary>房间频道路径的标准值（32 字节 UTF-16："PopKart\M01\Chan"）。</summary>
        private static byte[] DefaultRoomData()
        {
            return Encoding.Unicode.GetBytes("PopKart\\M01\\Chan");
        }

        /// <summary>校验客户端送来的房间数据；不可信时回退到标准值，避免回显后客户端解析异常。</summary>
        private static byte[] SanitizeRoomData(byte[] data)
        {
            if (data == null || data.Length != 32)
            {
                return DefaultRoomData();
            }
            string text;
            try
            {
                text = Encoding.Unicode.GetString(data);
            }
            catch
            {
                return DefaultRoomData();
            }
            foreach (char ch in text)
            {
                if (ch < 0x20 || ch > 0xD7A3)   // ASCII + 韩文区以外（乱码/指针值）一律视为无效
                {
                    return DefaultRoomData();
                }
            }
            if (text.IndexOf("PopKart", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return DefaultRoomData();
            }
            return data;
        }

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
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

            // 客户端偶尔会送一段"内存垃圾"当房间数据（实测出现过 0x17D2898888 这类指针值）。
            // 这份数据会被原样回显到 GrSlotDataPacket，客户端解析时可能直接闪退，
            // 所以这里做一次白名单校验：不是「PopKart\M01\Chan」这种字符串就换成标准值。
            RoomData = SanitizeRoomData(RoomData);
            iPacket.ReadBytes(5);
            byte AiSwitch = iPacket.ReadByte();
            Log.Info($"[Room] create raw1={raw1} ai={AiCount} aiSwitch={AiSwitch} raw2={raw2} raw3={raw3} data={BitConverter.ToString(RoomData)}");

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
                return true;
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
            var channelInfo = GameSupport.GetChannelInfo(playerChannel);

            // 双人组队判定：频道配置 > 建房包模式号 > 频道名/房间名关键字。
            // 建房包里的模式号并不总是基础模式（1~4）：双人组队频道实测下发 51，早期抓包是 68，
            // 若直接拿去当 3/4 用，双人房会被建成"个人模式"——没有队伍位、模式号也和频道对不上，
            // 房主进房后界面错乱（客户端崩溃），其他人房间列表里也匹配不到这个房间。
            bool duoRequested = (channelInfo != null && channelInfo.DuoTeam)
                                || DuoTeamMode.IsDuoGameType(rawGameType)
                                || DuoTeamMode.IsDuoRoomName(channelInfo?.Name)
                                || DuoTeamMode.IsDuoRoomName(RoomName);

            // 基础模式优先级：客户端模式号 → 频道配置（channel.xml，客户端与服务端共用）→ 双人频道按名称判断
            byte baseGameType = DuoTeamMode.ToBaseGameType(rawGameType);
            bool rawSpecialMode = GameModes.IsFlagMode(rawGameType)
                || GameModes.IsDeathMatch(rawGameType)
                || GameModes.IsChallenger(rawGameType);
            // 星期模式（channel.xml gameType=71）：7 个子频道各自有自己的模式号，
            // 官方房间列表条目尾部（第 3 字节）给的就是它们的基础模式：
            //   68(双人竞速) -> 3 组队竞速
            //   51(迷你地板赛) -> 7 夺旗组队
            //    8(双人1v1?) -> 1 个人竞速
            if (channelInfo != null && channelInfo.WireGameType == 71
                && !DuoTeamMode.IsBaseGameType(baseGameType))
            {
                baseGameType = rawGameType switch
                {
                    68 => (byte)3,
                    51 => (byte)4,   // 迷你地板赛：玩法等同组队道具（写 7 会变成夺旗赛）
                    50 => (byte)2,   // 迷你个人加速赛：玩法等同个人道具
                    8 => (byte)1,
                    _ => (byte)3,
                };
                Log.Debug($"[Room] 星期子频道号={rawGameType} → 基础模式={baseGameType}");
            }
            if (!DuoTeamMode.IsBaseGameType(baseGameType))
            {
                if (channelInfo != null && DuoTeamMode.IsBaseGameType(channelInfo.GameType))
                {
                    baseGameType = channelInfo.GameType;
                }
                else if (duoRequested)
                {
                    baseGameType = DuoTeamMode.GuessDuoBaseGameType(channelInfo?.Name, RoomName);
                }
            }
            // 夺旗/死斗/挑战者频道：建房包没有给出特殊模式号时，以频道配置为准
            if (!rawSpecialMode && channelInfo != null
                && (GameModes.IsFlagMode(channelInfo.GameType)
                    || GameModes.IsDeathMatch(channelInfo.GameType)
                    || GameModes.IsChallenger(channelInfo.GameType)))
            {
                baseGameType = channelInfo.GameType;
            }
            GameType = baseGameType;

            // RP 赛（福不福）：房名带「RP赛」的组队房 → 建成 RP 房。
            // 官服抓包确认模式号：组队竞速无限RP = 30；组队道具RP = 28（暂未启用，先不转）。
            Room.IsRpRoom = false;
            if (RoomName != null && RoomName.Contains("RP赛") && GameType == 3)
            {
                GameType = GameModes.RpSpeedTeam;
                Room.IsRpRoom = true;
                Log.Debug($"[RP] 房间 {RoomId} 按房名创建为「组队竞速无限RP赛」（模式 30，房名={RoomName}）");
            }
            else if (RoomName != null && RoomName.Contains("RP赛") && GameType == 1)
            {
                GameType = GameModes.RpSpeedIndi;
                Room.IsRpRoom = true;
                Log.Debug($"[RP] 房间 {RoomId} 按房名创建为「个人竞速无限RP赛」（模式 29，房名={RoomName}）");
            }
            else if (RoomName != null && RoomName.Contains("RP赛") && GameType == 4)
            {
                GameType = GameModes.RpItemTeam;
                Room.IsRpRoom = true;
                Log.Debug($"[RP] 房间 {RoomId} 按房名创建为「组队道具RP赛」（模式 28，房名={RoomName}）");
            }
            else if (RoomName != null && RoomName.Contains("RP赛") && GameType == 2)
            {
                GameType = GameModes.RpItemIndi;
                Room.IsRpRoom = true;
                Log.Debug($"[RP] 房间 {RoomId} 按房名创建为「个人道具RP赛」（模式 27，房名={RoomName}）");
            }

            // 客户端在 RP 频道建房时会直接带模式 30（channel.xml: bokbulbokSpeedTeamInfinit）
            if (GameModes.IsRpMode(GameType))
            {
                Room.IsRpRoom = true;
                Log.Debug($"[RP] 房间 {RoomId} 识别为「{GameModes.Name(GameType)}」（模式 {GameType}，频道 {Room.Channel}）");
            }
            Room.GameType = GameType;
            Room.DuoTeam = duoRequested && (GameType == 3 || GameType == 4) && ProfileService.SettingConfig.DuoTeamChannel;
            Room.DuoTeamSize = Room.DuoTeam
                ? DuoTeamMode.NormalizeTeamSize(ProfileService.SettingConfig.DuoTeamSize)
                : (byte)2;
            // 下发给客户端的模式号：基础模式（3/4）时才按官方值换成 67/68；
            // 客户端给的是扩展模式号（如 51）时原样回显，避免客户端收到自己不认识的模式而卡死。
            // 夺旗/死斗/挑战者等特殊模式始终按解析后的模式号下发。
            Room.WireGameType = GameModes.IsFlagMode(GameType)
                || GameModes.IsDeathMatch(GameType)
                || GameModes.IsChallenger(GameType)
                || GameModes.IsRpMode(GameType)
                ? GameType
                : (DuoTeamMode.IsBaseGameType(rawGameType)
                    ? (Room.DuoTeam ? DuoTeamMode.ToWireGameType(GameType) : rawGameType)
                    : rawGameType);
            // 迷你地板赛（子频道号 51）：客户端不认识 51 这个房间模式，
            // GrSessionDataPacket / GrSlotDataPacket 里带 51 会在进房时闪退。
            // 这里对客户端统一按「组队道具(4)」下发，只在本服内部打标记走迷你道具表。
            if (channelInfo != null && channelInfo.WireGameType == 71 && rawGameType == ItemRule.MiniWireGameType)
            {
                Room.MiniItemMode = true;
                Room.WireGameType = Room.GameType;
                Log.Info($"[Room] 迷你地板赛：对客户端按组队道具下发（wire={Room.WireGameType}）");
            }
            // 迷你个人加速赛（子频道号 50）：对客户端按「个人道具(2)」下发，内部走加速赛表
            else if (channelInfo != null && channelInfo.WireGameType == 71 && rawGameType == ItemRule.MiniAccelWireGameType)
            {
                Room.MiniAccelMode = true;
                Room.WireGameType = Room.GameType;
                Log.Info($"[Room] 迷你个人加速赛：对客户端按个人道具下发（wire={Room.WireGameType}）");
            }
            // 普通组队道具房：房名带「地板赛」时同样按迷你地板赛处理（道具走迷你表）
            else if (Room.GameType == 4 && !string.IsNullOrEmpty(RoomName) && RoomName.Contains("地板赛"))
            {
                Room.MiniItemMode = true;
                Log.Info($"[Room] 房名含「地板赛」→ 按迷你地板赛处理（room={RoomName}）");
            }
            // 普通个人道具房：房名带「加速赛」时按迷你个人加速赛处理
            else if (Room.GameType == 2 && !string.IsNullOrEmpty(RoomName) && RoomName.Contains("加速赛"))
            {
                Room.MiniAccelMode = true;
                Log.Info($"[Room] 房名含「加速赛」→ 按迷你个人加速赛处理（room={RoomName}）");
            }
            // 【测试】星期模式房间给一条真实赛道：列表里官方可见的条目都是真实 hash，
            // 我们没选图时是 0 → 列表回填占位图 forest_B01，客户端很可能按"该子频道允许的赛道"过滤掉。
            if (channelInfo != null && channelInfo.WireGameType == 71)
            {
                // 官服建房后第一份 GrSlotDataPacket 的这个字段是 00000007（不是赛道 hash），
                // 写赛道 hash 时客户端解析槽位包会闪退。列表条目另用真实赛道（见 ChGetRoomListRequestPacket）。
                Room.track = 7;
                Log.Debug("[Room] 星期模式房间默认赛道 = 0x1C1802BE");
            }
            Room.RoomData = RoomData;
            Log.Info($"[Room] create gameType={rawGameType} -> base={GameType} duo={Room.DuoTeam} wire={Room.WireGameType} channel={Room.Channel}({channelInfo?.Name ?? "未知频道"} cfgGameType={channelInfo?.WireGameType.ToString() ?? "-"} cfgDuo={channelInfo?.DuoTeam.ToString() ?? "-"})");
            Log.Debug($"CreateRoom = {RoomId}");
            byte randomTrackGameType = 0;
            if (GameType == 2 || GameType == 4 || GameType == 14 || GameType == 54 || GameModes.IsItemEnabled(GameType))
            {
                randomTrackGameType = 1;
            }
            Room.RandomTrackGameType = randomTrackGameType;

            // 挑战者模式是 4 人同队（抓包：我方 4 人坐 4~7 号格），人数上限固定 4
            maxPlayers = GameModes.ClampMaxPlayers(GameType, maxPlayers);
            Room.MaxPlayers = maxPlayers;

            if (GameModes.IsChallenger(GameType))
            {
                // 官服抓包：刚进挑战赛房间时客户端默认选中的就是战队 id=19（深渊队）、难度 1（普通），
                // 槽位包必须回同样的值，客户端才会把房间画成「我方 4 格 + 对面 AI 战队」。
                Room.CampaignId = 19;
                Room.CampaignDifficulty = 1;
                Log.Info($"[挑战赛] 房间 {RoomId} 默认战队 id={Room.CampaignId}，难度={Room.CampaignDifficulty}");
            }

            if (GameModes.IsTeamGameType(GameType) || GameModes.IsChallenger(GameType))
            {
                // 团队模式房主默认蓝队（2）；挑战者模式同样全员蓝队
                byte slot = Room.DuoTeam
                    ? RoomManager.AddDuoPlayer(RoomId, Parent.Client.Nickname, 2, Parent)
                    : RoomManager.AddPlayer(RoomId, Parent.Client.Nickname, 2, 2, Parent);
                if (slot == 255)
                {
                    Log.Info($"CreateRoom Failed, 房主入座失败（room={RoomId}, nickname={Parent.Client.Nickname}）");
                    ChCreateRoomFailReply(Parent, Room, rawGameType);
                    return true;
                }
                Player player = RoomManager.GetPlayer(RoomId, Parent.Client.Nickname);
                if (player == null)
                {
                    Log.Info($"GetPlayer Failed");
                    ChCreateRoomFailReply(Parent, Room, rawGameType);
                    return true;
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
                    oPacket.WriteByte(player.Team); // 抓包：该字节为房主所在队伍
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
                    Log.Info($"CreateRoom Failed, 房主入座失败（room={RoomId}, nickname={Parent.Client.Nickname}）");
                    ChCreateRoomFailReply(Parent, Room, rawGameType);
                    return true;
                }
                Player player = RoomManager.GetPlayer(RoomId, Parent.Client.Nickname);
                if (player == null)
                {
                    Log.Info($"GetPlayer Failed");
                    ChCreateRoomFailReply(Parent, Room, rawGameType);
                    return true;
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
            return true;
        }
    }
}
