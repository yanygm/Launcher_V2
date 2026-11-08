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
    /// <summary>处理 <see cref="PacketName.ChLeaveRoomRequestPacket"/> 数据包。</summary>
    internal sealed class ChLeaveRoomRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChLeaveRoomRequestPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            int slotId = RoomManager.GetPlayerSlotId(roomId, Parent.Client.Nickname);
            if (slotId != -1)
            {
                Log.Info($"Leave roomId: {roomId} slotId: {slotId}");
                var Leave = RoomManager.RemovePlayer(roomId, (byte)slotId, Parent.Client.Nickname);
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteBool(Leave);
                    Parent.Client.Send(oPacket);
                }
            }
            else
            {
                // 槽位查不到（档案未加载完整/改名/换观战身份/映射已丢）：按昵称 + 会话兜底再删一次。
                // 直接回 false 会让客户端以为没退出去，而服务端仍保留槽位，
                // 表现就是玩家残留在房间里出不来。
                bool Leave = roomId != -1
                    ? RoomManager.RemovePlayerByNickname(roomId, Parent.Client.Nickname)
                    : RoomManager.RemovePlayerBySession(Parent);
                Log.Info($"Leave by nickname/session roomId: {roomId}, removed: {Leave}");
                using (OutPacket oPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    oPacket.WriteBool(Leave);
                    Parent.Client.Send(oPacket);
                }
            }
            return true;
        }
    }
}
