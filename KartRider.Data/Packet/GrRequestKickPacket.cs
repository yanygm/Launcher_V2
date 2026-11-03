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
            if (RoomManager.TryGetIdDetail(roomId, ID) is Player p)
            {
                var player = RoomManager.RemovePlayer(roomId, p.SlotId, p.Nickname);
                using (OutPacket outPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                {
                    outPacket.WriteBool(player);
                    p.Session.Client.Send(outPacket);
                }
                if (player)
                {
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
            }
            return true;
        }
    }
}
