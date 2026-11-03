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
    /// <summary>处理 <see cref="PacketName.PqKart12RestictTuningSkill"/> 数据包。</summary>
    internal sealed class PqKart12RestictTuningSkill : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKart12RestictTuningSkill;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart = inPacket.ReadShort();
            short sn = inPacket.ReadShort();
            // IDA: PqKart12RestictTuningSkill = ushort + ushort + int
            int field = inPacket.ReadInt();
            if (!FileName.FileNames.ContainsKey(session.Client.Nickname))
            {
                FileName.Load(session.Client.Nickname);
            }
            var filename = FileName.FileNames[session.Client.Nickname];
            var Level12List = new List<Level12>();
            if (File.Exists(filename.Level12Data_LoadFile))
            {
                Level12List = JsonHelper.DeserializeNoBom<List<Level12>>(filename.Level12Data_LoadFile) ?? new List<Level12>();
            }
            var existingParts = Level12List.FirstOrDefault(level12 => level12.ID == kart && level12.SN == sn);
            if (existingParts != null)
            {
                using (OutPacket outPacket = new OutPacket("PrKart12RestictTuningSkill"))
                {
                    outPacket.WriteShort(kart);
                    outPacket.WriteShort(sn);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(field);
                    session.Client.Send(outPacket);
                }
                short point = existingParts.SkillPoints;
                short skill = 0;
                short skilllevel = 0;
                switch (field)
                {
                    case 1:
                        skill = existingParts.Skill1;
                        skilllevel = existingParts.SkillGrade1;
                        break;
                    case 2:
                        skill = existingParts.Skill2;
                        skilllevel = existingParts.SkillGrade2;
                        break;
                    case 3:
                        skill = existingParts.Skill3;
                        skilllevel = existingParts.SkillGrade3;
                        break;
                }
                KartExcData.AddLevel12List(session.Client.Nickname, kart, sn, -1, (short)field, skill, 0, (short)(point + skilllevel));
            }
            return true;
        }
    }
}
