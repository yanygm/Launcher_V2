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
                ? GameType
                : (DuoTeamMode.IsBaseGameType(rawGameType)
                    ? (Room.DuoTeam ? DuoTeamMode.ToWireGameType(GameType) : rawGameType)
                    : rawGameType);
            Room.RoomData = RoomData;
            Log.Info($"[Room] create gameType={rawGameType} -> base={GameType} duo={Room.DuoTeam} wire={Room.WireGameType} channel={Room.Channel}({channelInfo?.Name ?? "未知频道"} cfgGameType={channelInfo?.WireGameType.ToString() ?? "-"} cfgDuo={channelInfo?.DuoTeam.ToString() ?? "-"})");
            Log.Debug($"CreateRoom = {RoomId}");
            byte randomTrackGameType = 0;
            if (GameType == 2 || GameType == 4 || GameType == 14 || GameType == 54 || GameModes.IsItemEnabled(GameType))
            {
                randomTrackGameType = 1;
            }
            Room.RandomTrackGameType = randomTrackGameType;

            // 挑战者模式是 4 人同队（抓包：全部坐在蓝队 0~3 号格），人数上限固定 4
            maxPlayers = GameModes.ClampMaxPlayers(GameType, maxPlayers);
            Room.MaxPlayers = maxPlayers;

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
