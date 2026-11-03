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
                    byte listMode = (gameType > 0 && gameType <= 255) ? (byte)gameType : _room.Value.ModeByte;
                    if (listMode != _room.Value.ModeByte)
                    {
                        Log.Warn($"[RoomList] 房间 {_room.Key} 实际模式={_room.Value.ModeByte}，列表按请求模式 {listMode} 下发（模式不一致时客户端会丢弃条目）");
                    }
                    // 刚建好的房间还没选图，track 是 0。真实房间一定带合法赛道 hash，
                    // 客户端拿到 0（不存在的赛道）时很可能整条丢弃该房间条目，这里补一个可用占位图。
                    uint listTrack = _room.Value.track;
                    if (listTrack == 0)
                    {
                        listTrack = ListFallbackTrack();
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
                    oPacket.WriteByte((byte)(8 - _room.Value.CloseSlotIds.Count)); // 房间最大人数
                    oPacket.WriteByte((byte)_room.Value.GetCount()); // 房间人数
                    oPacket.WriteHexString("00 00 00 00 00 00");
                }
                Parent.Client.Send(oPacket);
            }
            return true;
        }
    }
}
