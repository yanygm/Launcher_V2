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
    /// <summary>处理 <see cref="PacketName.PqFavoriteItemUpdate"/> 数据包。</summary>
    internal sealed class PqFavoriteItemUpdate : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqFavoriteItemUpdate;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            inPacket.ReadByte();
            int j = inPacket.ReadInt();
            for (int i = 0; i < j; i++)
            {
                ushort item = inPacket.ReadUShort();
                ushort id = inPacket.ReadUShort();
                ushort sn = inPacket.ReadUShort();
                byte Add_Del = inPacket.ReadByte();
                if (Add_Del == 1)
                {
                    FavoriteItem.Favorite_Item_Add(session.Client.Nickname, item, id, sn);
                }
                else if (Add_Del == 2)
                {
                    FavoriteItem.Favorite_Item_Del(session.Client.Nickname, item, id, sn);
                }
            }
            return true;
        }
    }
}
