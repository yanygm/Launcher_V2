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
    /// <summary>处理 <see cref="PacketName.PqGetClubRealCrewListPacket"/> 数据包。</summary>
    internal sealed class PqGetClubRealCrewListPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetClubRealCrewListPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int ClubID = inPacket.ReadInt();
            // IDA: PqGetClubRealCrewListPacket = int + int
            if (inPacket.Available >= 4) inPacket.ReadInt();
            int ClubMemberCount = 1;
            using (OutPacket outPacket = new OutPacket("PrGetClubRealCrewListPacket"))
            {
                outPacket.WriteInt(ClubMemberCount);
                for (int i = 0; i < ClubMemberCount; i++)
                {
                    outPacket.WriteInt(ClubID);
                    outPacket.WriteUInt(ClientManager.GetUserNO(session.Client.Nickname));
                    outPacket.WriteString(session.Client.Nickname);
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.RP);
                    outPacket.WriteShort(5);//职位
                    outPacket.WriteUInt(3000000);//周活跃度
                    outPacket.WriteUInt(3000000);//累计活跃度
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteHexString("FFFF0000");
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteInt(0);
                }
                outPacket.WriteInt(300);
                outPacket.WriteInt(ClubID);
                outPacket.WriteUInt(ClientManager.GetUserNO(session.Client.Nickname));
                outPacket.WriteString(session.Client.Nickname);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.RP);
                outPacket.WriteShort(5);//职位
                outPacket.WriteUInt(3000000);//周活跃度
                outPacket.WriteUInt(3000000);//累计活跃度
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteHexString("FFFF0000");
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteHexString("22394703A87F0B00");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
