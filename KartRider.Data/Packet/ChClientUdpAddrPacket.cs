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
    /// <summary>处理 <see cref="PacketName.ChClientUdpAddrPacket"/> 数据包。</summary>
    internal sealed class ChClientUdpAddrPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChClientUdpAddrPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var ClientUdpAddr = inPacket.ReadEndPoint();
            var udpConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            udpConfig.Rider.UdpPort = (ushort)ClientUdpAddr.Port;
            ProfileService.Save(session.Client.Nickname, udpConfig);
            Log.Info($"[{session.Client.Nickname}] {ClientUdpAddr.Address}:{ClientUdpAddr.Port}");
            return true;
        }
    }
}
