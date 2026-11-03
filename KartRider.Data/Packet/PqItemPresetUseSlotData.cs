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
    /// <summary>处理 <see cref="PacketName.PqItemPresetUseSlotData"/> 数据包。</summary>
    internal sealed class PqItemPresetUseSlotData : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqItemPresetUseSlotData;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short id = inPacket.ReadShort();
            using (OutPacket outPacket = new OutPacket("PrItemPresetUseSlotData"))
            {
                outPacket.WriteInt(1);
                session.Client.Send(outPacket);
            }
            var usePresetConfig = ItemPresetsService.GetItemPresetConfig(session.Client.Nickname);
            foreach (var ItemPreset in usePresetConfig.ItemPresets)
            {
                if (ItemPreset.ID == id)
                {
                    ItemPreset.Enable = 1;
                }
                else
                {
                    ItemPreset.Enable = 0;
                }
            }
            ItemPresetsService.Save(session.Client.Nickname, usePresetConfig);
            return true;
        }
    }
}
