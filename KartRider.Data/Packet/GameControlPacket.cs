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
            return true;
        }
    }
}
