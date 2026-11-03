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
    /// <summary>处理 <see cref="PacketName.PqGetRiderInfo"/> 数据包。</summary>
    internal sealed class PqGetRiderInfo : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetRiderInfo;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            uint UserID = inPacket.ReadUInt();
            inPacket.ReadInt();
            string nickname = ClientManager.GetNickname(UserID);
            if (UserID == 0)
            {
                nickname = inPacket.ReadString(false);
            }
            if (nickname == session.Client.Nickname)
            {
                GameSupport.PrGetRiderInfo(nickname, session);
            }
            else
            {
                if (string.IsNullOrEmpty(nickname))
                {
                    using (OutPacket outPacket = new OutPacket("PrGetRiderInfo"))
                    {
                        outPacket.WriteByte(0);
                        session.Client.Send(outPacket);
                    }
                }
                else
                {
                    GameSupport.PrGetRiderInfo(nickname, session);
                }
            }
            return true;
        }
    }
}
