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
    /// <summary>处理 <see cref="PacketName.SpRqRemainCashPacket"/> 数据包。</summary>
    internal sealed class SpRqRemainCashPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqRemainCashPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("SpRpRemainCashPacket"))
            {
                outPacket.WriteUInt(0);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.Cash);
                session.Client.Send(outPacket);
            }
            using (OutPacket outPacket = new OutPacket("SpRpRemainTcCashPacket"))
            {
                outPacket.WriteUInt(99);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.TcCash);
                outPacket.WriteUInt(0);
                outPacket.WriteUInt(0);
                outPacket.WriteUInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
