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
    /// <summary>处理 <see cref="PacketName.LoRqSetRiderItemOnPacket"/> 数据包。</summary>
    internal sealed class LoRqSetRiderItemOnPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.LoRqSetRiderItemOnPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var riderItemConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            riderItemConfig.RiderItem.Set_Character = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Paint = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Kart = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Plate = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Goggle = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Balloon = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Unknown1 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_HeadBand = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_HeadPhone = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_HandGearL = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Unknown2 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Uniform = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Decal = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Pet = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_FlyingPet = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Aura = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_SkidMark = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_SpecialKit = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_RidColor = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_BonusCard = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_BossModeCard = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartPlant1 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartPlant2 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartPlant3 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartPlant4 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Unknown3 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_FishingPole = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Tachometer = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Dye = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartSN = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Unknown4 = inPacket.ReadByte();
            riderItemConfig.RiderItem.Set_KartCoating = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartTailLamp = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_slotBg = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartCoating12 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartTailLamp12 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_KartBoosterEffect12 = inPacket.ReadUShort();
            riderItemConfig.RiderItem.Set_Unknown5 = inPacket.ReadUShort();
            // 车膜排查用：把客户端上报的装备字段直接展开（只列非零），
            // 免去事后对着十六进制数 45 个字段。对比“迅车膜/V1 车膜”时看这一行即可。
            var itemSet = riderItemConfig.RiderItem;
            var nonZero = new List<string>();
            foreach (var prop in typeof(RiderItemData).GetProperties())
            {
                if (!prop.Name.StartsWith("Set_", StringComparison.Ordinal))
                {
                    continue;
                }
                long value = Convert.ToInt64(prop.GetValue(itemSet));
                if (value != 0)
                {
                    nonZero.Add(prop.Name.Substring(4) + "=" + value);
                }
            }
            ProfileService.Save(session.Client.Nickname, riderItemConfig);
            int roomId = RoomManager.TryGetRoomId(session.Client.Nickname);
            if (roomId != -1)
            {
                Player player = RoomManager.GetPlayer(roomId, session.Client.Nickname);
                if (player != null)
                {
                    using (OutPacket outPacket = new OutPacket("GrSlotItemOnPacket"))
                    {
                        outPacket.WriteInt(player.ID);
                        GameSupport.GetRider(session.Client.Nickname, outPacket);
                        MultyPlayer.BroadCast(roomId, outPacket, session.Client.Nickname);
                    }
                }
            }
            return true;
        }
    }
}
