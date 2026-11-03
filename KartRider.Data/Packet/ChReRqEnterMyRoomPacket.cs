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
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms.Design;
using System.Xml;
using System.Xml.Linq;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.ChReRqEnterMyRoomPacket"/> 数据包。</summary>
    internal sealed class ChReRqEnterMyRoomPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChReRqEnterMyRoomPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
            => HandleCore(session, inPacket);

        internal static bool HandleCore(SessionGroup session, InPacket inPacket)
        {
            var availableNicknames = ClientManager.NicknameToUserNO.Keys.Where(n => n != session.Client.Nickname).ToList();
            if (availableNicknames.Count == 0)
            {
                MyRoomData.ChRpEnterMyRoomPacket(session, 5);
                return true;
            }
            Random random = new Random();
            int randomIndex = random.Next(availableNicknames.Count);
            string targetNickname = availableNicknames[randomIndex];
            MyRoomData.ChRpEnterMyRoomPacket(session, session.Client.Nickname, targetNickname);
            return true;
        }
    }
}
