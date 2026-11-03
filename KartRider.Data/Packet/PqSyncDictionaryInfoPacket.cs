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
    /// <summary>处理 <see cref="PacketName.PqSyncDictionaryInfoPacket"/> 数据包。</summary>
    internal sealed class PqSyncDictionaryInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqSyncDictionaryInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int Dictionary = inPacket.ReadInt();
            int Count = GameSupport.Dictionary.Count;
            using (OutPacket outPacket = new OutPacket("PrSyncDictionaryInfoPacket"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteInt(1);
                outPacket.WriteInt(Count);
                foreach (var item in GameSupport.Dictionary)
                {
                    outPacket.WriteShort(item[0]);
                    outPacket.WriteShort(item[1]);
                }
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
