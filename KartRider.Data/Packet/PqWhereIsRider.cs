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
    /// <summary>处理 <see cref="PacketName.PqWhereIsRider"/> 数据包。</summary>
    internal sealed class PqWhereIsRider : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqWhereIsRider;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            uint UserID = iPacket.ReadUInt();
            string nickname = ClientManager.GetNickname(UserID);
            if (string.IsNullOrEmpty(nickname)) return true;
            int roomId = RoomManager.TryGetRoomId(nickname);
            var room = RoomManager.GetRoom(roomId);
            if (roomId == -1)
            {
                using (OutPacket outPacket = new OutPacket("PrWhereIsRider"))
                {
                    outPacket.WriteUInt(UserID);
                    outPacket.WriteBytes(new byte[10]);
                    Parent.Client.Send(outPacket);
                }
                return true;
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("PrWhereIsRider"))
                {
                    outPacket.WriteUInt(UserID);
                    outPacket.WriteInt(roomId);
                    var channel = GameSupport.Channels.FirstOrDefault(c => c.Value.GameType == room.GameType).Key;
                    outPacket.WriteInt(channel);
                    outPacket.WriteBool(room.Lock);
                    outPacket.WriteByte(1);
                    Parent.Client.Send(outPacket);
                }
            }
            return true;
        }
    }
}
