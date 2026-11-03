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
    /// <summary>处理 <see cref="PacketName.GrRequestSetSlotStatePacket"/> 数据包。</summary>
    internal sealed class GrRequestSetSlotStatePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrRequestSetSlotStatePacket;

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

            if (!room.Started)
            {
                player.PlayerType = iPacket.ReadInt();
                GrSlotStatePacket(roomId);
                using (OutPacket oPacket = new OutPacket("GrReplySetSlotStatePacket"))
                {
                    oPacket.WriteUInt(ClientManager.GetUserNO(Parent.Client.Nickname));
                    oPacket.WriteByte(1);
                    oPacket.WriteInt(player.ID);
                    oPacket.WriteInt(player.PlayerType);
                    BroadCast(roomId, oPacket);
                }
                GrSlotDataPacket(roomId);
            }
            return true;
        }
    }
}
