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
    /// <summary>处理 <see cref="PacketName.SpRqUseInitialCardPacket"/> 数据包。</summary>
    internal sealed class SpRqUseInitialCardPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqUseInitialCardPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var cardConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            cardConfig.Rider.Card = inPacket.ReadString();
            ProfileService.Save(session.Client.Nickname, cardConfig);
            using (OutPacket outPacket = new OutPacket("SpRpUseInitialCardPacket"))
            {
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
