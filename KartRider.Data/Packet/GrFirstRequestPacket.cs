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
    /// <summary>处理 <see cref="PacketName.GrFirstRequestPacket"/> 数据包。</summary>
    internal sealed class GrFirstRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrFirstRequestPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                return true;
            }
            GrSessionDataPacket(Parent, Parent.Client.Nickname);
            //Thread.Sleep(10);
            GrSlotDataPacket(roomId);

            // 回到房间：如果上一局有未播报的计分结果，这时才发（不在结算界面发）
            RoomScore.OnRoomEntered(roomId, Parent.Client.Nickname);

            // 进房后按建房时选的人数锁掉多余格子（只做一次）
            var enterRoom = RoomManager.GetRoom(roomId);
            if (enterRoom != null && !enterRoom.RoomSizeApplied)
            {
                enterRoom.RoomSizeApplied = true;
                ApplyRoomSize(enterRoom, enterRoom.PendingMaxPlayers > 0 ? enterRoom.PendingMaxPlayers : 8);
            }
            return true;
        }
    }
}
