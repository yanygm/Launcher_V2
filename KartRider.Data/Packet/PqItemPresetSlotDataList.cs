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
    /// <summary>处理 <see cref="PacketName.PqItemPresetSlotDataList"/> 数据包。</summary>
    internal sealed class PqItemPresetSlotDataList : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqItemPresetSlotDataList;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var itemPresetConfig = ItemPresetsService.GetItemPresetConfig(session.Client.Nickname);
            using (OutPacket outPacket = new OutPacket("PrItemPresetSlotDataList"))
            {
                outPacket.WriteInt(itemPresetConfig.ItemPresets.Count);
                foreach (var ItemPreset in itemPresetConfig.ItemPresets)
                {
                    outPacket.WriteUShort(ItemPreset.ID);
                    outPacket.WriteUShort(ItemPreset.ID);
                    outPacket.WriteByte(ItemPreset.Badge);
                    if (ItemPreset.ID < 4)
                    {
                        outPacket.WriteInt(0);
                        outPacket.WriteHexString("44 B0 46 23");
                    }
                    else
                    {
                        outPacket.WriteInt(27125 + ItemPreset.ID);
                        outPacket.WriteHexString("44 B0 46 23");
                    }
                    outPacket.WriteByte(ItemPreset.Enable);
                    outPacket.WriteString(ItemPreset.Name);
                    outPacket.WriteUShort(ItemPreset.Character);
                    outPacket.WriteUShort(ItemPreset.Paint);
                    outPacket.WriteUShort(ItemPreset.Kart);
                    outPacket.WriteUShort(ItemPreset.Plate);
                    outPacket.WriteUShort(ItemPreset.Goggle);
                    outPacket.WriteUShort(ItemPreset.Balloon);
                    outPacket.WriteUShort(ItemPreset.Unknown1);
                    outPacket.WriteUShort(ItemPreset.HeadBand);
                    outPacket.WriteUShort(ItemPreset.HeadPhone);
                    outPacket.WriteUShort(ItemPreset.HandGearL);
                    outPacket.WriteUShort(ItemPreset.Unknown2);
                    outPacket.WriteUShort(ItemPreset.Uniform);
                    outPacket.WriteUShort(ItemPreset.Decal);
                    outPacket.WriteUShort(ItemPreset.Pet);
                    outPacket.WriteUShort(ItemPreset.FlyingPet);
                    outPacket.WriteUShort(ItemPreset.Aura);
                    outPacket.WriteUShort(ItemPreset.SkidMark);
                    outPacket.WriteUShort(ItemPreset.SpecialKit);
                    outPacket.WriteUShort(ItemPreset.RidColor);
                    outPacket.WriteUShort(ItemPreset.BonusCard);
                    outPacket.WriteUShort(ItemPreset.BossModeCard);
                    outPacket.WriteUShort(ItemPreset.KartPlant1);
                    outPacket.WriteUShort(ItemPreset.KartPlant2);
                    outPacket.WriteUShort(ItemPreset.KartPlant3);
                    outPacket.WriteUShort(ItemPreset.KartPlant4);
                    outPacket.WriteUShort(ItemPreset.Unknown3);
                    outPacket.WriteUShort(ItemPreset.FishingPole);
                    outPacket.WriteUShort(ItemPreset.Tachometer);
                    outPacket.WriteUShort(ItemPreset.Dye);
                    outPacket.WriteUShort(ItemPreset.KartSN);
                    outPacket.WriteByte(ItemPreset.Unknown4);
                    outPacket.WriteUShort(ItemPreset.KartCoating);
                    outPacket.WriteUShort(ItemPreset.KartTailLamp);
                    outPacket.WriteUShort(ItemPreset.slotBg);
                    outPacket.WriteUShort(ItemPreset.KartCoating12);
                    outPacket.WriteUShort(ItemPreset.KartTailLamp12);
                    outPacket.WriteUShort(ItemPreset.KartBoosterEffect12);
                    outPacket.WriteUShort(ItemPreset.Unknown5);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
