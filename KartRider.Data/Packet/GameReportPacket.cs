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
    /// <summary>处理 <see cref="PacketName.GameReportPacket"/> 数据包。</summary>
    internal sealed class GameReportPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.GameReportPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            byte[] DateTime1 = inPacket.ReadBytes(18);
            int GetItem = inPacket.ReadInt();
            int UseItem = inPacket.ReadInt();
            int UseBooster = inPacket.ReadEncodedInt();
            int[][] hashArray1 = new int[20][];
            for (int k = 0; k < 20; k++)
            {
                hashArray1[k] = new int[3];
                hashArray1[k][0] = inPacket.ReadEncodedInt();
                hashArray1[k][1] = inPacket.ReadEncodedInt();
                hashArray1[k][2] = inPacket.ReadEncodedInt();
            }
            int hash1 = inPacket.ReadEncodedInt();
            int hash2 = inPacket.ReadEncodedInt();
            int hash3 = inPacket.ReadEncodedInt();
            float single1 = inPacket.ReadEncodedFloat();
            float single2 = inPacket.ReadEncodedFloat();
            float single3 = inPacket.ReadEncodedFloat();
            int PlaneCheck = inPacket.ReadInt();
            byte[] hashArray2 = inPacket.ReadBytes(20);
            int hash4 = inPacket.ReadInt();
            byte[] hashArray3 = inPacket.ReadBytes(16);
            session.TotalSendPlaneCount += PlaneCheck;
            Log.Debug($"PlaneCheck: {PlaneCheck}, Total: {session.TotalSendPlaneCount}, Max: {session.SendPlaneCount}, Dist: {single3}");
            return true;
        }
    }
}
