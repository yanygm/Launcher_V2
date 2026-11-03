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
    /// <summary>处理 <see cref="PacketName.PqKartSpec"/> 数据包。</summary>
    internal sealed class PqKartSpec : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKartSpec;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var StartTimeAttack_SpeedType = inPacket.ReadByte();
            var Kart_id = inPacket.ReadUShort();
            var FlyingPet_id = inPacket.ReadUShort();
            // IDA: PqKartSpec 还有 uint + byte + byte，读取以完整消费请求（当前逻辑不使用）
            if (inPacket.Available >= 4) inPacket.ReadUInt();
            if (inPacket.Available >= 1) inPacket.ReadByte();
            if (inPacket.Available >= 1) inPacket.ReadByte();
            byte StartType = 1;
            StartGameData.Start_KartSpac(session, session.Client.Nickname, StartType, 0, 0, 0, StartTimeAttack_SpeedType, Kart_id, FlyingPet_id);
            return true;
        }
    }
}
