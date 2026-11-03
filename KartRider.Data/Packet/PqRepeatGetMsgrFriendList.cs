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
    /// <summary>处理 <see cref="PacketName.PqRepeatGetMsgrFriendList"/> 数据包。</summary>
    internal sealed class PqRepeatGetMsgrFriendList : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqRepeatGetMsgrFriendList;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var users = ClientManager.UserNOToNickname.Where(u => u.Value != session.Client.Nickname).ToList();

            if (users.Count == 0)
            {
                using (OutPacket emptyPacket = new OutPacket("PrRepeatGetMsgrFriendList"))
                {
                    GameSupport.GetMsgrFriendList(emptyPacket, 0, new List<KeyValuePair<uint, string>>());
                    emptyPacket.WriteByte(1);
                    emptyPacket.WriteBytes(new byte[8]);
                    session.Client.Send(emptyPacket);
                }
            }

            for (int start = 0; start < users.Count; start += 100)
            {
                int take = Math.Min(100, users.Count - start);
                var segment = users.Skip(start).Take(take).ToList();
                using (OutPacket outPacket = new OutPacket("PrRepeatGetMsgrFriendList"))
                {
                    GameSupport.GetMsgrFriendList(outPacket, users.Count, segment);
                    outPacket.WriteByte(1);
                    outPacket.WriteBytes(new byte[8]);
                    session.Client.Send(outPacket);
                }
            }

            return true;
        }
    }
}
