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
    /// <summary>处理 <see cref="PacketName.GrChangeTeamPacket"/> 数据包。</summary>
    internal sealed class GrChangeTeamPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrChangeTeamPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            var player = RoomManager.GetPlayer(roomId, Parent.Client.Nickname);
            if (player == null)
            {
                Log.Info($"GetPlayer Failed, roomId = {roomId}, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return true;
            }
            byte team = iPacket.ReadByte();
            bool Bool;
            if (room.DuoTeam) Bool = RoomManager.ChangeDuoTeam(roomId, player.SlotId, team);
            else Bool = RoomManager.ChangeMemberTeam(roomId, player.SlotId, team);
            Log.Info($"ChangeMemberTeam, roomId = {roomId}, SlotId = {player.SlotId}, Team = {team}, {Bool}");
            using (OutPacket oPacket = new OutPacket("GrChangeTeamPacketReply"))
            {
                oPacket.WriteInt(player.ID);
                oPacket.WriteByte(player.Team);
                Position(roomId, oPacket);
                Parent.Client.Send(oPacket);
            }
            GrSlotDataPacket(roomId);
            return true;
        }
    }
}
