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
    /// <summary>处理 <see cref="PacketName.PqChangeRoomInfoPacket"/> 数据包。</summary>
    internal sealed class PqChangeRoomInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqChangeRoomInfoPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            string RoomName = iPacket.ReadString();
            string RoomPassword = iPacket.ReadString();

            int LimitTime = iPacket.ReadInt();
            byte RKeyAllowed = iPacket.ReadByte();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                Log.Info($"TryGetRoomId Failed, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return true;
            }
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"GetRoom Failed, roomId = {roomId}");
                return true;
            }
            room.RoomName = RoomName;
            if (RoomPassword.Length > 0)
            {
                room.Lock = true;
            }
            else
            {
                room.Lock = false;
            }
            room.LockPwd = RoomPassword;

            using (OutPacket outPacket = new OutPacket("PrChangeRoomInfoPacket"))
            {
                outPacket.WriteBool(true);
                outPacket.WriteString(RoomName);
                outPacket.WriteString(RoomPassword);
                outPacket.WriteInt(LimitTime);
                outPacket.WriteByte(RKeyAllowed);
                BroadCast(roomId, outPacket);
            }
            return true;
        }
    }
}
