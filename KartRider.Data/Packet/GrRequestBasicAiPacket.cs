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
    /// <summary>处理 <see cref="PacketName.GrRequestBasicAiPacket"/> 数据包。</summary>
    internal sealed class GrRequestBasicAiPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrRequestBasicAiPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                return true;
            }
            // IDA: GrRequestBasicAiPacket = int + byte
            if (iPacket.Available >= 4) iPacket.ReadInt();
            byte ID = iPacket.ReadByte();
            if (RoomManager.TryGetIdDetail(roomId, ID) is Ai ai)
            {
                room.RemoveMember(ai.SlotId, "");
                using (OutPacket oPacket = new OutPacket("GrSlotDataBasicAi"))
                {
                    oPacket.WriteInt(1);
                    oPacket.WriteByte(1);
                    oPacket.WriteInt(ID);
                    oPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00");
                    Position(roomId, oPacket);
                    BroadCast(roomId, oPacket);
                }
            }
            else
            {
                AddAi(Parent, roomId, ID);
            }
            using (OutPacket oPacket = new OutPacket("GrReplyBasicAiPacket"))
            {
                oPacket.WriteByte(1);
                oPacket.WriteHexString("00 00 00 00");
                BroadCast(roomId, oPacket);
            }
            return true;
        }
    }
}
