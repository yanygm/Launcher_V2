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
    /// <summary>处理 <see cref="PacketName.ChGetRoomListRequestPacket"/> 数据包。</summary>
    internal sealed class ChGetRoomListRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChGetRoomListRequestPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            byte[] rawList = iPacket.GetRawBuffer();
            ParseRoomListRequest(rawList, out int page, out int gameType);
            // 频道由 PqChannelMovein / PqChannelSwitch 记录在玩家档案里，仅用于日志排查
            int channel = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.Channel ?? -1;
            // 回列表前先清掉 0 人房，避免客户端看到无法进入的幽灵房
            RoomManager.PurgeEmptyRooms();
            // 需求：所有模式/频道都显示全部房间（不做频道隔离、不做模式过滤）。
            // 原实现 isolate = channel >= 0 && RacingGameTypes.Contains(gameType) 会把别的频道的房间滤掉，
            // 表现为同一个房间在组队频道能看到、在别的模式/频道就看不到。
            var rooms = RoomManager.GetRoomsByPage(page, -1, gameType, out int total, false);
            Log.Info($"[RoomList] {Parent.Client.Nickname} req={BitConverter.ToString(rawList)} channel={channel} gameType={gameType} page={page} -> total={total}");
            using (OutPacket oPacket = new OutPacket("ChGetRoomListReplyPacket"))
            {
                Log.Info($"Room Count: {total} (channel={channel}, 全局列表)");
                oPacket.WriteInt(total); // 房间总数（本频道）
                oPacket.WriteInt(page);
                oPacket.WriteInt(rooms.Count); // 房间数量
                foreach (var _room in rooms)
                {
                    // 逐个房间打一条日志：出现“别人看不到房间”时，能直接比对
                    // 房间的 mode/channel 与请求方频道是否一致
                    Log.Info($"[RoomList]  -> room={_room.Key} name={_room.Value.RoomName} mode={_room.Value.ModeByte} duo={_room.Value.DuoTeam} channel={_room.Value.Channel} players={_room.Value.GetPlayerCount()} started={_room.Value.Started}");
                    // 列表条目的模式字段按「本次列表的上下文模式」下发。
                    // 客户端是按频道模式渲染列表的：实测在 RotationMode 频道（gameType=71）里建的双人组队房
                    // （mode=68），条目原样下发时会被客户端整条丢弃（服务端 total=1，客户端列表依旧空白）。
                    // 房间的真实模式仍由 ChJoinRoomReplyPacket / GrSessionDataPacket 下发，这里只影响列表显示。
                    // 【测试】列表条目写「房间自己的模式号」，而不是本次请求的频道号。
                    // 官服抓包：星期模式-双人竞速 的列表里 10 条中正好 2 条 mode=68，
                    // 客户端只显示这两条；写 71 会一条都不显示。
                    // 迷你地板赛：对外仍然写 51（官方列表里 mode=51），内部是组队道具
                    byte listMode = _room.Value.MiniItemMode ? (byte)ItemRule.MiniWireGameType
                                    : _room.Value.MiniAccelMode ? (byte)ItemRule.MiniAccelWireGameType
                                    : _room.Value.ModeByte;
                    // 刚建好的房间还没选图，track 是 0。真实房间一定带合法赛道 hash，
                    // 客户端拿到 0（不存在的赛道）时很可能整条丢弃该房间条目，这里补一个可用占位图。
                    uint listTrack = _room.Value.track;
                    if (listTrack == 7) listTrack = 0x1C1802BE;   // 星期模式：房间内字段是 7，列表要给真实赛道
                    if (listTrack == 0)
                    {
                        listTrack = ListFallbackTrack(_room.Value.GameType);
                        Log.Info($"[RoomList] 房间 {_room.Key} 尚未选图，列表赛道用占位图 {RandomTrack.GetTrackName(listTrack)} (0x{listTrack:X8})");
                    }
                    oPacket.WriteShort((short)_room.Key);
                    // RoomName 理论上非空；但一旦为 null，WriteString 会抛异常 → 处理器中断 → 不回包 →
                    // 客户端在房间列表页无限等待（表现为卡死）。这里做一次兜底。
                    oPacket.WriteString(_room.Value.RoomName ?? string.Empty); // 房间名称
                    oPacket.WriteUInt(listTrack); // 赛道
                    oPacket.WriteBool(_room.Value.Lock); // 是否上锁
                    oPacket.WriteByte(listMode); // 模式（列表上下文模式）
                    oPacket.WriteByte(_room.Value.SpeedType); // 速度模式
                    oPacket.WriteBool(_room.Value.Started); // 房间状态
                    // 挑战赛不锁格（官服槽位包 closeCount 恒为 0），人数上限要单独写 4，
                    // 否则列表会显示成 8 人房
                    oPacket.WriteByte(GameModes.IsChallenger(_room.Value.GameType)
                        ? (byte)GameModes.ChallengerMaxPlayers
                        : (byte)(8 - _room.Value.CloseSlotIds.Count)); // 房间最大人数
                    oPacket.WriteByte((byte)_room.Value.GetCount()); // 房间人数
                    // 官方条目尾部 6 字节不是全 0：
                    //   mode=68(双人竞速) 的条目是 00 00 03 00 00 00
                    //   mode=51(迷你地板赛) 的条目是 XX 00 07 00 00 00
                    // 第三字节正好是该房间的「基础模式」(3=组队竞速 / 7=夺旗)，
                    // 写 0 会让客户端无法归类 → 整条不显示。
                    oPacket.WriteUShort(0);
                    // 官服：mode=51（迷你地板赛）的条目尾部是 07，mode=68 是 03，mode=8 是 01。
                    // 迷你房内部按组队道具(4)跑，但列表里要写官方那个值，否则客户端列表不显示。
                    // 迷你赛房间对外仍是 51/50，但内部 GameType 是 4/2，所以按标记判断
                    ushort listBase = _room.Value.MiniItemMode ? (ushort)7
                                    : _room.Value.MiniAccelMode ? (ushort)12
                                    : _room.Value.GameType;
                    oPacket.WriteUShort(listBase);
                    oPacket.WriteUShort(0);
                }
                Parent.Client.Send(oPacket);
            }
            return true;
        }
    }
}
