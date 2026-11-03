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
    /// <summary>处理 <see cref="PacketName.PqKartLevelPointUpdate"/> 数据包。</summary>
    internal sealed class PqKartLevelPointUpdate : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKartLevelPointUpdate;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Kart = inPacket.ReadShort();
            short SN = inPacket.ReadShort();
            short v1 = inPacket.ReadShort();
            short v2 = inPacket.ReadShort();
            short v3 = inPacket.ReadShort();
            short v4 = inPacket.ReadShort();
            short pointleft = 0;
            short Effect = 0;
            using (OutPacket outPacket = new OutPacket("PrKartLevelPointUpdate"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteShort(Kart);
                outPacket.WriteShort(SN);
                outPacket.WriteShort(5);
                outPacket.WriteShort(pointleft);
                outPacket.WriteShort(v1);
                outPacket.WriteShort(v2);
                outPacket.WriteShort(v3);
                outPacket.WriteShort(v4);
                outPacket.WriteShort(Effect);
                session.Client.Send(outPacket);
            }
            if (!FileName.FileNames.ContainsKey(session.Client.Nickname))
            {
                FileName.Load(session.Client.Nickname);
            }
            var filename = FileName.FileNames[session.Client.Nickname];
            var LevelList = new List<Level>();
            if (File.Exists(filename.LevelData_LoadFile))
            {
                LevelList = JsonHelper.DeserializeNoBom<List<Level>>(filename.LevelData_LoadFile) ?? new List<Level>();
            }
            var existingLevelList = LevelList.FirstOrDefault(level => level.ID == Kart && level.SN == SN);
            if (existingLevelList == null)
            {
                pointleft = (short)(35 - v1 - v2 - v3 - v4);
                KartExcData.AddLevelList(session.Client.Nickname, Kart, SN, 5, pointleft, v1, v2, v3, v4, 0);
            }
            else
            {
                pointleft = (short)(existingLevelList.Points - v1 - v2 - v3 - v4);
                short v1New = (short)(existingLevelList.Level1 + v1);
                short v2New = (short)(existingLevelList.Level2 + v2);
                short v3New = (short)(existingLevelList.Level3 + v3);
                short v4New = (short)(existingLevelList.Level4 + v4);
                short effect = existingLevelList.Effect;
                KartExcData.AddLevelList(session.Client.Nickname, Kart, SN, 5, pointleft, v1New, v2New, v3New, v4New, effect);
            }
            return true;
        }
    }
}
