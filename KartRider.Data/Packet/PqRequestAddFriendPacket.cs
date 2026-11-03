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
    /// <summary>处理 <see cref="PacketName.PqRequestAddFriendPacket"/> 数据包。</summary>
    internal sealed class PqRequestAddFriendPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqRequestAddFriendPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            uint UserID = inPacket.ReadUInt();
            string UserName = ClientManager.GetNickname(UserID);
            if (string.IsNullOrEmpty(UserName)) return true;
            var pConfig = ProfileService.GetProfileConfig(UserName);
            IPEndPoint client = ClientManager.ClientToIPEndPoint(pConfig.Rider.ClientId);
            using (OutPacket outPacket = new OutPacket("PrRequestAddFriendPacket"))
            {
                outPacket.WriteHexString("00 44 77 3D");
                outPacket.WriteBytes(new byte[28]);
                outPacket.WriteUInt(UserID);
                outPacket.WriteString(UserName);
                outPacket.WriteEndPoint(new IPEndPoint(client.Address, pConfig.Rider.P2pPort));
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
