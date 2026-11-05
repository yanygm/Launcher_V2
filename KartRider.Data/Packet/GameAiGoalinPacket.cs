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
    /// <summary>处理 <see cref="PacketName.GameAiGoalinPacket"/> 数据包。</summary>
    internal sealed class GameAiGoalinPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameAiGoalinPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
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
            Log.Debug($"GameAiGoalinPacket, Id = {Id}, Time = {Time}");
            if (room.EndTicks == 0)
            {
                // 积分制比赛（夺旗/死斗）由服务端计时，AI 冲线只记录成绩，不结束比赛
                if (GameModes.IsScoreMode(room.GameType))
                {
                    return true;
                }
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
            return true;
        }
    }
}
