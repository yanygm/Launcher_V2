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
    /// <summary>处理 <see cref="PacketName.PqExpeditionFriendInfoListPacket"/> 数据包。</summary>
    /// <remarks>返回空的探险队成员列表（数量 0），避免主页渲染真实队员数据。</remarks>
    internal sealed class PqExpeditionFriendInfoListPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqExpeditionFriendInfoListPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrExpeditionFriendInfoListPacket"))
            {
                // 空探险队列表：成员数 0
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
