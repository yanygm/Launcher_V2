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
    /// <summary>处理 <see cref="PacketName.PqItemPresetUpdateSlotData"/> 数据包。</summary>
    internal sealed class PqItemPresetUpdateSlotData : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqItemPresetUpdateSlotData;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            using (OutPacket outPacket = new OutPacket("PrItemPresetUpdateSlotData"))
            {
                outPacket.WriteInt(0);
                session.Client.Send(outPacket);
            }
            var itemPresetConfig = ItemPresetsService.GetItemPresetConfig(session.Client.Nickname);
            inPacket.ReadInt();
            short id1 = inPacket.ReadShort();
            short id2 = inPacket.ReadShort();
            // 原实现用 (ID == id1 && ID == id2) 比较同一个属性，除非两值相等否则永远不命中，
            // 导致整个预设更新分支实际失效。这里优先按 id1 匹配，未命中再退回 id2。
            var ItemPreset = itemPresetConfig.ItemPresets.FirstOrDefault(p => p.ID == id1)
                             ?? itemPresetConfig.ItemPresets.FirstOrDefault(p => p.ID == id2);
            if (ItemPreset != null)
            {
                ItemPreset.Badge = inPacket.ReadByte();
                inPacket.ReadBytes(8);
                ItemPreset.Enable = inPacket.ReadByte();
                ItemPreset.Name = inPacket.ReadString();
                ItemPreset.Character = inPacket.ReadUShort();
                ItemPreset.Paint = inPacket.ReadUShort();
                ItemPreset.Kart = inPacket.ReadUShort();
                ItemPreset.Plate = inPacket.ReadUShort();
                ItemPreset.Goggle = inPacket.ReadUShort();
                ItemPreset.Balloon = inPacket.ReadUShort();
                ItemPreset.Unknown1 = inPacket.ReadUShort();
                ItemPreset.HeadBand = inPacket.ReadUShort();
                ItemPreset.HeadPhone = inPacket.ReadUShort();
                ItemPreset.HandGearL = inPacket.ReadUShort();
                ItemPreset.Unknown2 = inPacket.ReadUShort();
                ItemPreset.Uniform = inPacket.ReadUShort();
                ItemPreset.Decal = inPacket.ReadUShort();
                ItemPreset.Pet = inPacket.ReadUShort();
                ItemPreset.FlyingPet = inPacket.ReadUShort();
                ItemPreset.Aura = inPacket.ReadUShort();
                ItemPreset.SkidMark = inPacket.ReadUShort();
                ItemPreset.SpecialKit = inPacket.ReadUShort();
                ItemPreset.RidColor = inPacket.ReadUShort();
                ItemPreset.BonusCard = inPacket.ReadUShort();
                ItemPreset.BossModeCard = inPacket.ReadUShort();
                ItemPreset.KartPlant1 = inPacket.ReadUShort();
                ItemPreset.KartPlant2 = inPacket.ReadUShort();
                ItemPreset.KartPlant3 = inPacket.ReadUShort();
                ItemPreset.KartPlant4 = inPacket.ReadUShort();
                ItemPreset.Unknown3 = inPacket.ReadUShort();
                ItemPreset.FishingPole = inPacket.ReadUShort();
                ItemPreset.Tachometer = inPacket.ReadUShort();
                ItemPreset.Dye = inPacket.ReadUShort();
                ItemPreset.KartSN = inPacket.ReadUShort();
                ItemPreset.Unknown4 = inPacket.ReadByte();
                ItemPreset.KartCoating = inPacket.ReadUShort();
                ItemPreset.KartTailLamp = inPacket.ReadUShort();
                ItemPreset.slotBg = inPacket.ReadUShort();
                ItemPreset.KartCoating12 = inPacket.ReadUShort();
                ItemPreset.KartTailLamp12 = inPacket.ReadUShort();
                ItemPreset.KartBoosterEffect12 = inPacket.ReadUShort();
                ItemPreset.Unknown5 = inPacket.ReadUShort();
                ItemPresetsService.Save(session.Client.Nickname, itemPresetConfig);
            }
            return true;
        }
    }
}
