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
    /// <summary>处理 <see cref="PacketName.PqUseProtectSpannerItem"/> 数据包。</summary>
    internal sealed class PqUseProtectSpannerItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUseProtectSpannerItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Protect = inPacket.ReadShort();
            short v2 = inPacket.ReadShort();
            short Item_Id = inPacket.ReadShort();
            short Kart = inPacket.ReadShort();
            short KartSN = inPacket.ReadShort();
            inPacket.ReadShort();
            short slot = inPacket.ReadShort();

            if (!FileName.FileNames.ContainsKey(session.Client.Nickname))
            {
                FileName.Load(session.Client.Nickname);
            }
            var filename = FileName.FileNames[session.Client.Nickname];
            var TuneList = new List<Tune>();
            if (File.Exists(filename.TuneData_LoadFile))
            {
                TuneList = JsonHelper.DeserializeNoBom<List<Tune>>(filename.TuneData_LoadFile) ?? new List<Tune>();
            }
            var existingList = TuneList.FirstOrDefault(tune => tune.ID == Kart && tune.SN == KartSN);
            if (existingList != null)
            {
                using (OutPacket outPacket = new OutPacket("PrUseProtectSpannerItem"))
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteShort(Protect);
                    outPacket.WriteShort(v2);
                    outPacket.WriteShort(Item_Id);
                    outPacket.WriteShort(Kart);
                    outPacket.WriteShort(KartSN);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(existingList.Tune1);
                    outPacket.WriteShort(existingList.Tune2);
                    outPacket.WriteShort(existingList.Tune3);
                    if (Protect == 49)
                    {
                        outPacket.WriteShort(slot);
                        outPacket.WriteShort(4);
                        outPacket.WriteShort(existingList.Slot2);
                        outPacket.WriteShort(existingList.Count2);
                        existingList.Slot1 = slot;
                        existingList.Count1 = 4;
                    }
                    else if (Protect == 53)
                    {
                        outPacket.WriteShort(existingList.Slot1);
                        outPacket.WriteShort(existingList.Count1);
                        outPacket.WriteShort(slot);
                        outPacket.WriteShort(3);
                        existingList.Slot2 = slot;
                        existingList.Count2 = 3;
                    }
                    else
                    {
                        outPacket.WriteShort(existingList.Slot1);
                        outPacket.WriteShort(existingList.Count1);
                        outPacket.WriteShort(existingList.Slot2);
                        outPacket.WriteShort(existingList.Count2);
                    }
                    session.Client.Send(outPacket);
                    KartExcData.AddTuneList(session.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
                }
            }
            return true;
        }
    }
}
