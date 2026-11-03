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
    /// <summary>处理 <see cref="PacketName.GrChangeTrackPacket"/> 数据包。</summary>
    internal sealed class GrChangeTrackPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrChangeTrackPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            room.track = iPacket.ReadUInt();
            // IDA: GrChangeTrackPacket = int + int
            if (iPacket.Available >= 4) iPacket.ReadInt();
            Log.Debug($"Gr Track Changed : {RandomTrack.GetTrackName(room.track)}");
            GrSlotDataPacket(roomId);
            return true;
        }
    }
}
