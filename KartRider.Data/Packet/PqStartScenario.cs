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
    /// <summary>处理 <see cref="PacketName.PqStartScenario"/> 数据包。</summary>
    internal sealed class PqStartScenario : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqStartScenario;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var scenarioConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            // IDA: PqStartScenario = ushort + byte + byte（不是 int）
            ushort scenarioType = inPacket.ReadUShort();
            byte scenarioSubType = inPacket.Available >= 1 ? inPacket.ReadByte() : (byte)0;
            byte scenarioUnk = inPacket.Available >= 1 ? inPacket.ReadByte() : (byte)0;
            scenarioConfig.Rider.ScenarioType = scenarioType;
            using (OutPacket outPacket = new OutPacket("PrStartScenario"))
            {
                // IDA: PrStartScenario = ushort + byte + byte
                outPacket.WriteUShort(scenarioType);
                outPacket.WriteByte(scenarioSubType);
                outPacket.WriteByte(scenarioUnk);
                session.Client.Send(outPacket);
            }
            ProfileService.Save(session.Client.Nickname, scenarioConfig);
            return true;
        }
    }
}
