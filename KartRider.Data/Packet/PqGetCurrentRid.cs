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
    /// <summary>处理 <see cref="PacketName.PqGetCurrentRid"/> 数据包。</summary>
    internal sealed class PqGetCurrentRid : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetCurrentRid;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            string UserName = inPacket.ReadString();
            if (Directory.Exists(Path.GetFullPath(Path.Combine(FileName.ProfileDir, UserName))))
            {
                using (OutPacket outPacket = new OutPacket("PrGetCurrentRid"))
                {
                    outPacket.WriteString(UserName);
                    session.Client.Send(outPacket);
                }
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("PrGetCurrentRid"))
                {
                    outPacket.WriteInt(0);
                    session.Client.Send(outPacket);
                }
            }
            return true;
        }
    }
}
