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
    /// <summary>处理 <see cref="PacketName.PqUseResetSocketItem"/> 数据包。</summary>
    internal sealed class PqUseResetSocketItem : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqUseResetSocketItem;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short Item = inPacket.ReadShort();
            short Item_Id = inPacket.ReadShort();
            short Kart = inPacket.ReadShort();
            inPacket.ReadShort();
            short KartSN = inPacket.ReadShort();

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
                using (OutPacket outPacket = new OutPacket("PrUseResetSocketItem"))
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteShort(Item);
                    outPacket.WriteShort(Item_Id);
                    outPacket.WriteShort(Kart);
                    outPacket.WriteShort(KartSN);
                    outPacket.WriteShort(34);
                    outPacket.WriteShort(76);
                    outPacket.WriteShort(1);
                    outPacket.WriteShort(1);
                    outPacket.WriteShort(existingList.Tune1);
                    outPacket.WriteShort(existingList.Tune2);
                    outPacket.WriteShort(existingList.Tune3);
                    outPacket.WriteShort(existingList.Slot1);
                    outPacket.WriteShort(existingList.Count1);
                    outPacket.WriteShort(existingList.Slot2);
                    outPacket.WriteShort(existingList.Count2);
                    session.Client.Send(outPacket);
                }
                List<short> secure = new List<short>();
                if (existingList.Count1 == 0)
                {
                    existingList.Slot1 = -1;
                }
                else
                {
                    existingList.Count1 = (short)((int)existingList.Count1 - 1);
                }
                if (existingList.Count2 == 0)
                {
                    existingList.Slot2 = -1;
                }
                else
                {
                    existingList.Count2 = (short)((int)existingList.Count2 - 1);
                }
                Log.Info($"TuneProtect: " + existingList.Slot1 + ", " + existingList.Slot2);
                Log.Info($"TuneProtectCount: " + existingList.Count1 + ", " + existingList.Count2);
                for (int i = 2; i <= 4; i++)
                {
                    if (existingList.Slot1 != -1 && (int)(existingList.Slot1) + 2 == i)
                    {
                    }
                    else if (existingList.Slot2 != -1 && (int)(existingList.Slot2) + 2 == i)
                    {
                    }
                    else
                    {
                        if (i == 2) existingList.Tune1 = 0;
                        else if (i == 3) existingList.Tune2 = 0;
                        else if (i == 4) existingList.Tune3 = 0;
                    }
                }
                Log.Info($"TuneList: " + existingList.Tune1 + ", " + existingList.Tune2 + ", " + existingList.Tune3);
                KartExcData.AddTuneList(session.Client.Nickname, Kart, KartSN, existingList.Tune1, existingList.Tune2, existingList.Tune3, existingList.Slot1, existingList.Count1, existingList.Slot2, existingList.Count2);
            }
            return true;
        }
    }
}
