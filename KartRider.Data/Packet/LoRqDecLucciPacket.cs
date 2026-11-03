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
    /// <summary>处理 <see cref="PacketName.LoRqDecLucciPacket"/> 数据包。</summary>
    internal sealed class LoRqDecLucciPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqDecLucciPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            inPacket.ReadByte();
            uint Lucci = inPacket.ReadUInt();
            var lucciConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            if (Lucci <= lucciConfig.Rider.Lucci)
            {
                lucciConfig.Rider.Lucci -= Lucci;
            }
            else
            {
                lucciConfig.Rider.Lucci = 0;
            }
            ProfileService.Save(session.Client.Nickname, lucciConfig);
            return true;
        }
    }
}
