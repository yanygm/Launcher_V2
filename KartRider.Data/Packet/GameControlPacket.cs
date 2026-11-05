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
    /// <summary>处理 <see cref="PacketName.GameControlPacket"/> 数据包。</summary>
    internal sealed class GameControlPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameControlPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
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
                // 抓包：客户端在 state 后额外写了一个保留字节，真正的完赛时间在下一处 uint。
                // 旧实现直接 ReadUInt() 会读到 time<<8，结算显示的用时/名次全部偏移。
                var time = ReadFinishTime(iPacket);
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
                    // 夺旗赛/死斗按服务端计时结算：个别客户端提前上报完赛不能直接结束比赛
                    if (GameModes.IsScoreMode(room.GameType))
                    {
                        Log.Debug($"[Mode] 房间 {roomId} 积分制比赛忽略提前完赛上报（{time}ms）");
                        return true;
                    }
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
                else if (GameModes.IsScoreMode(room.GameType))
                {
                    // 官方抓包：服务端发 GameControl(3) 后客户端回报 state=2，服务端立即结算。
                    // 不等待定时器，否则客户端会赶在结算包到达前把结算界面建好（比分 0:0）。
                    SettleRoom(room);
                }
            }
            return true;
        }

        /// <summary>读取 GameControlPacket(state=2) 的完赛时间，兼容有/无保留字节两种布局。</summary>
        static uint ReadFinishTime(InPacket iPacket)
        {
            byte[] raw = iPacket.GetRawBuffer();
            int pos = iPacket.Position;
            if (raw != null && pos >= 0 && raw.Length >= pos + 5 && raw[pos] == 0)
            {
                uint shifted = BitConverter.ToUInt32(raw, pos + 1);
                if (shifted <= 4u * 60 * 60 * 1000)
                {
                    iPacket.Skip(5);
                    return shifted;
                }
            }
            return iPacket.ReadUInt();
        }
    }
}
