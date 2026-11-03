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
    /// <summary>处理 <see cref="PacketName.PqUpdateRiderSchoolLevelPacket"/> 数据包。</summary>
    internal sealed class PqUpdateRiderSchoolLevelPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUpdateRiderSchoolLevelPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // PqUpdateRiderSchoolLevelPacket 只有一个字节：[u8 新等级]
            byte requestedLevel = inPacket.Available >= 1 ? inPacket.ReadByte() : (byte)0;
            byte level = RiderSchool.RecordPromotion(session, requestedLevel);
            using (OutPacket outPacket = new OutPacket("PrUpdateRiderSchoolLevelPacket"))
            {
                // 本客户端的应答结构：[u8 等级][u16 时间(天)][u16 时间(刻)][u8 标志]，共 6 字节
                outPacket.WriteByte(level);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteByte(1);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
