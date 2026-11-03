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
    /// <summary>处理 <see cref="PacketName.PqFavoriteTrackUpdate"/> 数据包。</summary>
    internal sealed class PqFavoriteTrackUpdate : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqFavoriteTrackUpdate;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            inPacket.ReadByte();
            int tracks = inPacket.ReadInt(); //赛道数量
            for (int i = 0; i < tracks; i++)
            {
                short theme = inPacket.ReadShort(); //主题代码
                uint track = inPacket.ReadUInt(); //赛道代码
                byte Add_Del = inPacket.ReadByte(); //1添加，2删除
                if (Add_Del == 1)
                {
                    FavoriteItem.Favorite_Track_Add(session.Client.Nickname, theme, track);
                }
                else if (Add_Del == 2)
                {
                    FavoriteItem.Favorite_Track_Del(session.Client.Nickname, theme, track);
                }
            }
            return true;
        }
    }
}
