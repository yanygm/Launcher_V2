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
    /// <summary>处理 <see cref="PacketName.PqInviteGamePacket"/> 数据包。</summary>
    internal sealed class PqInviteGamePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqInviteGamePacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            uint UserID = iPacket.ReadUInt();
            string nickname = ClientManager.GetNickname(UserID);
            if (string.IsNullOrEmpty(nickname))
            {
                return true; // 无效用户
            }

            // 被邀请者已在房间中，不接受邀请
            int targetRoomId = RoomManager.TryGetRoomId(nickname);
            if (targetRoomId != -1)
            {
                return true;
            }

            var parent = ClientManager.GetParent(nickname);
            if (parent != null)
            {
                using (OutPacket outPacket = new OutPacket("PrInviteGamePacket"))
                {
                    outPacket.WriteBytes(iPacket.ReadBytes(iPacket.Available));
                    parent.Client.Send(outPacket);
                }
            }
            return true;
        }
    }
}
