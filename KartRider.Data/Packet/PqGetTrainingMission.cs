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
    /// <summary>处理 <see cref="PacketName.PqGetTrainingMission"/> 数据包。</summary>
    internal sealed class PqGetTrainingMission : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetTrainingMission;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int type = inPacket.ReadInt();
            uint track = inPacket.ReadUInt();
            //byte Level = TimeAttack.GetTrackLevel(Nickname, track);
            //PrGetTrainingMission 00 08 B7 51 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            using (OutPacket outPacket = new OutPacket("PrGetTrainingMission"))
            {
                outPacket.WriteInt(type);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteByte(0); //完成赛道
                outPacket.WriteByte(0); //使用加速器道具（完成时累积）
                outPacket.WriteByte(0); //撞击次数%s次以内
                outPacket.WriteByte(0); //达成赛道纪录
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
