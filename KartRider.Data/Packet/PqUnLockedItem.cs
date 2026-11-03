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
    /// <summary>处理 <see cref="PacketName.PqUnLockedItem"/> 数据包。</summary>
    internal sealed class PqUnLockedItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUnLockedItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int useType = inPacket.ReadInt();
            int stringType = inPacket.ReadInt();
            string password = inPacket.ReadString();
            byte Type = inPacket.ReadByte();
            using (OutPacket outPacket = new OutPacket("PrUnLockedItem"))
            {
                outPacket.WriteByte(Type);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
