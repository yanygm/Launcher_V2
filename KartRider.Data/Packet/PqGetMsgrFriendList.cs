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
    /// <summary>处理 <see cref="PacketName.PqGetMsgrFriendList"/> 数据包。</summary>
    internal sealed class PqGetMsgrFriendList : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetMsgrFriendList;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            uint unk1 = inPacket.ReadUInt();
            var users = ClientManager.UserNOToNickname.Where(u => u.Value != session.Client.Nickname).ToList();

            // 3571：即使没有好友也必须回一次空列表，否则客户端会一直等
            if (users.Count == 0)
            {
                using (OutPacket emptyPacket = new OutPacket("PrGetMsgrFriendList"))
                {
                    // 对齐官服尾部：总人数 0 + 段号 0 + 结尾 13 字节
                    emptyPacket.WriteInt(0);
                    emptyPacket.WriteInt(0);
                    emptyPacket.WriteHexString("00 00 00 00 01 00 00 00 00 00 00 00 00");
                    session.Client.Send(emptyPacket);
                }
            }

            for (int start = 0; start < users.Count; start += 100)
            {
                int take = Math.Min(100, users.Count - start);
                var segment = users.Skip(start).Take(take).ToList();
                using (OutPacket outPacket = new OutPacket("PrGetMsgrFriendList"))
                {
                    GameSupport.GetMsgrFriendList(outPacket, users.Count, segment);
                    outPacket.WriteInt(0);
                    outPacket.WriteByte(1);
                    outPacket.WriteBytes(new byte[8]);
                    session.Client.Send(outPacket);
                }
            }

            return true;
        }
    }
}
