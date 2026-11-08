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
    /// <summary>处理 <see cref="PacketName.PqRoomMasterChangePacket"/> 数据包。</summary>
    internal sealed class PqRoomMasterChangePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqRoomMasterChangePacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            var room = RoomManager.GetRoom(roomId);
            if (room == null)
            {
                Log.Info($"GetRoom Failed, roomId = {roomId}");
                return true;
            }
            if (!room.Started)
            {
                string Target = iPacket.ReadString();
                var player = RoomManager.GetPlayer(roomId, Target);
                if (player != null)
                {
                    room.RoomMaster = player.ID;
                    // 新老房主的状态一起刷：旧房主回到「未准备」，否则房间里会出现两个"房主"，
                    // 客户端也会误以为大家都准备好了
                    ResetReadyStates(room);
                    GrSlotDataPacket(roomId);
                    GrSlotStatePacket(roomId);
                }
            }
            return true;
        }
    }
}
