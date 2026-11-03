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
    /// <summary>处理 <see cref="PacketName.PqKart12TuningLevelUp"/> 数据包。</summary>
    internal sealed class PqKart12TuningLevelUp : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKart12TuningLevelUp;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart = inPacket.ReadShort();
            short sn = inPacket.ReadShort();
            uint[] money = new uint[] { 0, 10, 12, 15, 20, 30 };
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
                using (OutPacket outPacket = new OutPacket("PrKart12TuningLevelUp"))
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(1);
                    outPacket.WriteShort(3);
                    outPacket.WriteShort(kart);
                    outPacket.WriteShort(sn);
                    short Level = (short)((int)existingParts.Grade + 1);
                    if (Level < 1 || Level > money.Length - 1)
                    {
                        // 原实现不夹紧等级：第 5 次升级时 money[6] 直接越界踢线
                        Log.Warn($"[Tuning] 等级 {Level} 超出上限，拒绝升级");
                        return true;
                    }
                    existingParts.Grade = Level;
                    outPacket.WriteShort(Level);//1-1,2-3,3-6,4-10,5-15
                    short Point = (short)((int)existingParts.SkillPoints + (int)Level);
                    existingParts.SkillPoints = Point;
                    outPacket.WriteShort(Point);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(4);
                    outPacket.WriteShort(4);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteUInt(money[(int)Level]);
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.Lucci);
                    session.Client.Send(outPacket);
                    KartExcData.AddLevel12List(session.Client.Nickname, kart, sn, Level, -1, -1, -1, Point);
                }
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("PrKart12TuningLevelUp"))
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(1);
                    outPacket.WriteShort(3);
                    outPacket.WriteShort(kart);
                    outPacket.WriteShort(sn);
                    outPacket.WriteShort(5);//1-1,2-3,3-6,4-10,5-15
                    outPacket.WriteShort(15);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(4);
                    outPacket.WriteShort(4);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteUInt(10);
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.Lucci);
                    session.Client.Send(outPacket);
                    KartExcData.AddLevel12List(session.Client.Nickname, kart, sn, 5, 0, 0, 0, 15);
                }
            }
            return true;
        }
    }
}
