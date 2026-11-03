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
    /// <summary>处理 <see cref="PacketName.GameTeamBoosterRequestAddGaugePacket"/> 数据包。</summary>
    internal sealed class GameTeamBoosterRequestAddGaugePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameTeamBoosterRequestAddGaugePacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            var team = iPacket.ReadByte();
            var value = iPacket.ReadFloat();
            Log.Info($"GameTeamBoosterRequestAddGaugePacket, teams = {team}, value = {value}");

            if (!room.IsValidTeam(team))
            {
                return true;
            }

            int gaugeMembers = room.GetPlayerCount(team);
            if (gaugeMembers < 1) gaugeMembers = 1;
            float gauge = room.AddTeamGauge(team, value * 0.000125f / gaugeMembers);
            using (OutPacket oPacket = new OutPacket("GameTeamBoosterSetGaugePacket"))
            {
                oPacket.WriteByte(team);
                oPacket.WriteFloat(gauge);
                BroadCast(roomId, oPacket, "", team);
            }
            if (gauge >= 1f) room.ResetTeamGauge(team);
            return true;
        }
    }
}
