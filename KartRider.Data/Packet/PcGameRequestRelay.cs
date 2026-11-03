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
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms.Design;
using System.Xml;
using System.Xml.Linq;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.PcGameRequestRelay"/> 数据包。</summary>
    internal sealed class PcGameRequestRelay : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PcGameRequestRelay;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }

            int val1 = inPacket.ReadInt();
            // IDA: PcGameRequestRelay = int + int
            if (inPacket.Available >= 4) inPacket.ReadInt();

            // using (OutPacket outPacket = new OutPacket("GameRelayBroadcastingPacket"))
            // {
            //     outPacket.WriteInt(0);
            //     outPacket.WriteInt(val1);
            //     MultyPlayer.BroadCast(roomId, outPacket);
            // }
            return true;
        }
    }
}
