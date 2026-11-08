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
    /// <summary>处理 <see cref="PacketName.GrRequestKickPacket"/> 数据包。</summary>
    internal sealed class GrRequestKickPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GrRequestKickPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            int roomId = RoomManager.TryGetRoomId(Parent.Client.Nickname);
            if (roomId == -1)
            {
                Log.Info($"TryGetRoomId Failed, Parent.Client.Nickname = {Parent.Client.Nickname}");
                return true;
            }
            int ID = iPacket.ReadInt();

            // 竞技位在 _IDs 里（0~7），观战位在 ObIDs 里（固定 8~15）：
            // 只查 _IDs 时踢观战者会悄悄失败，被踢的人就一直残留在房间里。
            Player p = RoomManager.TryGetIdDetail(roomId, ID) as Player;
            var room = RoomManager.GetRoom(roomId);
            if (p == null && room != null)
            {
                foreach (RoomMember member in room.ObIDs)
                {
                    if (member is Player ob && ob.ID == ID)
                    {
                        p = ob;
                        break;
                    }
                }
            }

            if (p != null)
            {
                // 按昵称移除（内部自动判别竞技位/观战位），避免 slotId 过期删不到人
                var player = RoomManager.RemovePlayerByNickname(roomId, p.Nickname);
                try
                {
                    if (p.Session?.Client != null)
                    {
                        using (OutPacket outPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                        {
                            outPacket.WriteBool(player);
                            p.Session.Client.Send(outPacket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Info($"[Kick] 通知被踢玩家 {p.Nickname} 失败：{ex.Message}");
                }

                // 无论服务端是否找到了槽位，都要给房主回包并广播，否则双方都会"卡住"
                using (OutPacket outPacket = new OutPacket("GrKickBroadcastPacket"))
                {
                    outPacket.WriteString(p.Nickname);
                    BroadCast(roomId, outPacket);
                }
                using (OutPacket outPacket = new OutPacket("GrReplyKickPacket"))
                {
                    outPacket.WriteByte(0);
                    Parent.Client.Send(outPacket);
                }
            }
            return true;
        }
    }
}
