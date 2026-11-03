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
    /// <summary>处理 <see cref="PacketName.PqClubInitFirstPacket"/> 数据包。</summary>
    internal sealed class PqClubInitFirstPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqClubInitFirstPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int ClubMemberCount = 1;
            var initFirstConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrClubInitFirstPacket"))
            {
                outPacket.WriteInt(initFirstConfig.Rider.ClubCode);
                outPacket.WriteString(initFirstConfig.Rider.ClubName);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteByte(5);
                outPacket.WriteByte(1);
                outPacket.WriteUInt(ClientManager.GetUserNO(session.Client.Nickname));
                outPacket.WriteString(session.Client.Nickname);
                outPacket.WriteInt(500);//最大成员数
                outPacket.WriteHexString("00000000E803000004FFFF0000");
                outPacket.WriteString(initFirstConfig.Rider.ClubIntro);
                outPacket.WriteInt(initFirstConfig.Rider.ClubMark_LOGO);
                outPacket.WriteInt(initFirstConfig.Rider.ClubMark_LINE);
                outPacket.WriteInt(3000000);//周活跃度
                outPacket.WriteInt(3000000);//俱乐部活跃度
                outPacket.WriteHexString("C0C62D0000000000");
                outPacket.WriteInt(500);//成员数
                outPacket.WriteHexString("00000000A11100008B170000D20A000004100000");
                outPacket.WriteInt(ClubMemberCount);
                for (int i = 0; i < ClubMemberCount; i++)
                {
                    outPacket.WriteInt(initFirstConfig.Rider.ClubCode);
                    outPacket.WriteUInt(ClientManager.GetUserNO(session.Client.Nickname));
                    outPacket.WriteString(session.Client.Nickname);
                    outPacket.WriteUInt(initFirstConfig.Rider.RP);
                    outPacket.WriteShort(5);//职位
                    outPacket.WriteUInt(3000000);//周活跃度
                    outPacket.WriteUInt(3000000);//累计活跃度
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteHexString("FFFF0000");
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteInt(0);
                }
                outPacket.WriteInt(initFirstConfig.Rider.ClubCode);
                outPacket.WriteUInt(ClientManager.GetUserNO(session.Client.Nickname));
                outPacket.WriteString(session.Client.Nickname);
                outPacket.WriteUInt(initFirstConfig.Rider.RP);
                outPacket.WriteShort(5);//职位
                outPacket.WriteUInt(3000000);//周活跃度
                outPacket.WriteUInt(3000000);//累计活跃度
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteHexString("FFFF0000");
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteHexString("D43C1B01ED7E0B00");
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteInt(0);
                outPacket.WriteHexString("22394703A87F0B00880AFB03A67F0B0000000000");
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
