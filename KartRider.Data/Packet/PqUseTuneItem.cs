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
    /// <summary>处理 <see cref="PacketName.PqUseTuneItem"/> 数据包。</summary>
    internal sealed class PqUseTuneItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUseTuneItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Item = inPacket.ReadShort();
            short Item_Id = inPacket.ReadShort();
            short Kart = inPacket.ReadShort();
            inPacket.ReadShort();
            short KartSN = inPacket.ReadShort();

            Random random = new Random();
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
                if (Item == 5)
                {
                    using (OutPacket outPacket = new OutPacket("PrUseTuneItem"))
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteShort(Item);
                        outPacket.WriteShort(Item_Id);
                        outPacket.WriteShort(Kart);
                        outPacket.WriteShort(KartSN);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(603);
                        outPacket.WriteShort(703);
                        outPacket.WriteShort(903);
                        outPacket.WriteShort(existingList.Slot1);
                        outPacket.WriteShort(existingList.Count1);
                        outPacket.WriteShort(existingList.Slot2);
                        outPacket.WriteShort(existingList.Count2);
                        session.Client.Send(outPacket);
                    }
                    existingList.Tune1 = 603;
                    existingList.Tune2 = 703;
                    existingList.Tune3 = 903;
                    KartExcData.AddTuneList(session.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
                }
                else
                {
                    List<short> tuneList1 = new List<short> { existingList.Tune1, existingList.Tune2, existingList.Tune3 };
                    List<short> tuneList2 = GameSupport.GetTuns(tuneList1, Item);
                    using (OutPacket outPacket = new OutPacket("PrUseTuneItem"))
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteShort(Item);
                        outPacket.WriteShort(Item_Id);
                        outPacket.WriteShort(Kart);
                        outPacket.WriteShort(KartSN);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(tuneList2[0]);
                        existingList.Tune1 = tuneList2[0];
                        outPacket.WriteShort(tuneList2[1]);
                        existingList.Tune2 = tuneList2[1];
                        outPacket.WriteShort(tuneList2[2]);
                        existingList.Tune3 = tuneList2[2];
                        outPacket.WriteShort(existingList.Slot1);
                        outPacket.WriteShort(existingList.Count1);
                        outPacket.WriteShort(existingList.Slot2);
                        outPacket.WriteShort(existingList.Count2);
                        session.Client.Send(outPacket);
                    }
                }
                KartExcData.AddTuneList(session.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
            }
            return true;
        }
    }
}
