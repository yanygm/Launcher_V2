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
    /// <summary>处理 <see cref="PacketName.PqSearchClubListPacket"/> 数据包。</summary>
    internal sealed class PqSearchClubListPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqSearchClubListPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int ClubID = inPacket.ReadInt();
            // IDA: PqSearchClubListPacket = int + int + string + string
            if (inPacket.Available >= 4) inPacket.ReadInt();
            if (inPacket.Available >= 4) inPacket.ReadString();
            if (inPacket.Available >= 4) inPacket.ReadString();
            int ClubCount = 1;
            var searchClubConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrSearchClubListPacket"))
            {
                outPacket.WriteInt(ClubCount);
                for (int i = 0; i < ClubCount; i++)
                {
                    outPacket.WriteInt(searchClubConfig.Rider.ClubCode);
                    outPacket.WriteString(searchClubConfig.Rider.ClubName);
                    outPacket.WriteByte(5);
                    outPacket.WriteInt(500);//最大成员数
                    outPacket.WriteHexString("FFFFFFFF");
                    outPacket.WriteInt(3000000);//活跃度
                    outPacket.WriteInt(3000000);//最大活跃度
                    outPacket.WriteInt(searchClubConfig.Rider.ClubMark_LOGO);
                    outPacket.WriteInt(0);
                    outPacket.WriteString(session.Client.Nickname);
                    outPacket.WriteInt(500);//成员数
                    outPacket.WriteDateTime(DateTime.Now);
                    outPacket.WriteString(searchClubConfig.Rider.ClubIntro);
                    outPacket.WriteShort(0);
                    outPacket.WriteInt(3000000);//周活跃度
                }
                outPacket.WriteByte(1);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
