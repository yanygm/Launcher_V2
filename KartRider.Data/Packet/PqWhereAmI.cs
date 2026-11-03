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
    /// <summary>处理 <see cref="PacketName.PqWhereAmI"/> 数据包。</summary>
    internal sealed class PqWhereAmI : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqWhereAmI;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            uint UserID = iPacket.ReadUInt();
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            // 不在房间时 room 为 null：原实现直接取 room.GameType → 空引用 → 踢线
            byte channel = 0;
            if (room != null)
            {
                var matched = GameSupport.Channels.FirstOrDefault(c => c.Value.GameType == room.GameType);
                channel = matched.Key;
            }
            using (OutPacket outPacket = new OutPacket("PrWhereAmI"))
            {
                outPacket.WriteUInt(UserID);
                outPacket.WriteInt(roomId);
                outPacket.WriteByte(channel);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
            }
            return true;
        }
    }
}
