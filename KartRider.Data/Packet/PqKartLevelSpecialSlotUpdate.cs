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
    /// <summary>处理 <see cref="PacketName.PqKartLevelSpecialSlotUpdate"/> 数据包。</summary>
    internal sealed class PqKartLevelSpecialSlotUpdate : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKartLevelSpecialSlotUpdate;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Kart = inPacket.ReadShort();
            short SN = inPacket.ReadShort();
            short Effect = inPacket.ReadShort();

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

            using (OutPacket outPacket = new OutPacket("PrKartLevelSpecialSlotUpdate"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteShort(Kart);
                outPacket.WriteShort(SN);
                if (existingLevelList != null)
                {
                    outPacket.WriteShort(existingLevelList.Grade);
                    outPacket.WriteShort(existingLevelList.Points);
                    outPacket.WriteShort(existingLevelList.Level1);
                    outPacket.WriteShort(existingLevelList.Level2);
                    outPacket.WriteShort(existingLevelList.Level3);
                    outPacket.WriteShort(existingLevelList.Level4);
                    outPacket.WriteShort(Effect);
                    KartExcData.AddLevelList(session.Client.Nickname, Kart, SN, existingLevelList.Grade, existingLevelList.Points, existingLevelList.Level1, existingLevelList.Level2, existingLevelList.Level3, existingLevelList.Level4, Effect);
                }
                else
                {
                    outPacket.WriteShort(5);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(10);
                    outPacket.WriteShort(10);
                    outPacket.WriteShort(10);
                    outPacket.WriteShort(5);
                    outPacket.WriteShort(Effect);
                    KartExcData.AddLevelList(session.Client.Nickname, Kart, SN, 5, 0, 10, 10, 10, 5, Effect);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
