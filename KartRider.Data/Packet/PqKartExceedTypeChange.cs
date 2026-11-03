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
    /// <summary>处理 <see cref="PacketName.PqKartExceedTypeChange"/> 数据包。</summary>
    internal sealed class PqKartExceedTypeChange : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKartExceedTypeChange;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart1 = inPacket.ReadShort();
            short sn1 = inPacket.ReadShort();
            short kart2 = inPacket.ReadShort();
            short sn2 = inPacket.ReadShort();
            short Spanner = inPacket.ReadShort();
            uint Lucci = inPacket.ReadUInt();

            int ExceedType1 = 0;
            if (!FileName.FileNames.ContainsKey(session.Client.Nickname))
            {
                FileName.Load(session.Client.Nickname);
            }
            var filename = FileName.FileNames[session.Client.Nickname];
            var Parts12List = new List<Parts12>();
            if (File.Exists(filename.Level12Data_LoadFile))
            {
                Parts12List = JsonHelper.DeserializeNoBom<List<Parts12>>(filename.Parts12Data_LoadFile) ?? new List<Parts12>();
            }
            var targetPart = Parts12List.FirstOrDefault(parts => parts.ID == kart1 && parts.SN == sn1);
            if (targetPart != null)
            {
                ExceedType1 = targetPart.ExceedType;
            }
            Random random = new Random();
            int index = random.Next(V2Specs.ExceedTypes.Length);
            int ExceedType2 = V2Specs.ExceedTypes[index];
            while (ExceedType2 == ExceedType1)
            {
                index = random.Next(V2Specs.ExceedTypes.Length);
                ExceedType2 = V2Specs.ExceedTypes[index];
            }
            //int ExceedType = Random.Shared.Next(2, 5);
            using (OutPacket outPacket = new OutPacket("PrKartExceedTypeChange"))
            {
                outPacket.WriteShort(0);//kart2
                outPacket.WriteShort(0);//sn2
                outPacket.WriteShort(Spanner);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.Lucci);
                outPacket.WriteInt(ExceedType2);
                session.Client.Send(outPacket);
            }
            KartExcData.AddPartsList(session.Client.Nickname, kart1, sn1, 0, (short)ExceedType2, 0, 0);
            Log.Info($"ExceedType: " + ExceedType2);
            return true;
        }
    }
}
