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
    /// <summary>处理 <see cref="PacketName.ChClientP2pAddrPacket"/> 数据包。</summary>
    internal sealed class ChClientP2pAddrPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChClientP2pAddrPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var ClientP2pAddr = inPacket.ReadEndPoint();
            var p2pConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            p2pConfig.Rider.P2pPort = (ushort)ClientP2pAddr.Port;
            ProfileService.Save(session.Client.Nickname, p2pConfig);
            Log.Info($"[{session.Client.Nickname}] {ClientP2pAddr.Address}:{ClientP2pAddr.Port}");
            return true;
        }
    }
}
