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
    /// <summary>处理 <see cref="PacketName.PqKart12TuningPointUpdate"/> 数据包。</summary>
    internal sealed class PqKart12TuningPointUpdate : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKart12TuningPointUpdate;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart = inPacket.ReadShort();
            short sn = inPacket.ReadShort();
            short field = inPacket.ReadShort();
            byte AddDel = inPacket.ReadByte();
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
                short point = existingParts.SkillPoints;
                short skilllevel = 0;
                switch (field)
                {
                    case 1:
                        skilllevel = existingParts.SkillGrade1;
                        break;
                    case 2:
                        skilllevel = existingParts.SkillGrade2;
                        break;
                    case 3:
                        skilllevel = existingParts.SkillGrade3;
                        break;
                }
                if (AddDel == 1)
                {
                    using (OutPacket outPacket = new OutPacket("PrKart12TuningPointUpdate"))
                    {
                        outPacket.WriteShort(kart);
                        outPacket.WriteShort(sn);
                        outPacket.WriteShort(field);
                        outPacket.WriteShort((short)((int)skilllevel + 1));
                        outPacket.WriteByte(1);
                        outPacket.WriteShort((short)((int)point - 1));
                        outPacket.WriteByte(1);
                        session.Client.Send(outPacket);
                    }
                    KartExcData.AddLevel12List(session.Client.Nickname, kart, sn, -1, field, -1, (short)((int)skilllevel + 1), (short)((int)point - 1));
                }
                else if (AddDel == 0)
                {
                    using (OutPacket outPacket = new OutPacket("PrKart12TuningPointUpdate"))
                    {
                        outPacket.WriteShort(kart);
                        outPacket.WriteShort(sn);
                        outPacket.WriteShort(field);
                        outPacket.WriteShort((short)((int)skilllevel - 1));
                        outPacket.WriteByte(0);
                        outPacket.WriteShort((short)((int)point + 1));
                        outPacket.WriteByte(0);
                        session.Client.Send(outPacket);
                    }
                    KartExcData.AddLevel12List(session.Client.Nickname, kart, sn, -1, field, -1, (short)((int)skilllevel - 1), (short)((int)point + 1));
                }
            }
            return true;
        }
    }
}
