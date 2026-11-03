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
    /// <summary>处理 <see cref="PacketName.RmNotiMyRoomInfoPacket"/> 数据包。</summary>
    internal sealed class RmNotiMyRoomInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.RmNotiMyRoomInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var myRoomConfig = ProfileService.GetProfileConfig(session.Client.Nickname);
            myRoomConfig.MyRoom.MyRoom = inPacket.ReadShort();
            myRoomConfig.MyRoom.MyRoomBGM = inPacket.ReadByte();
            myRoomConfig.MyRoom.UseRoomPwd = inPacket.ReadByte();
            inPacket.ReadByte();
            myRoomConfig.MyRoom.UseItemPwd = inPacket.ReadByte();
            myRoomConfig.MyRoom.TalkLock = inPacket.ReadByte();
            myRoomConfig.MyRoom.RoomPwd = inPacket.ReadString(false);
            inPacket.ReadString(false);
            myRoomConfig.MyRoom.ItemPwd = inPacket.ReadString(false);
            myRoomConfig.MyRoom.MyRoomKart1 = inPacket.ReadShort();
            myRoomConfig.MyRoom.MyRoomKart2 = inPacket.ReadShort();
            ProfileService.Save(session.Client.Nickname, myRoomConfig);
            MyRoomData.RmNotiMyRoomInfoPacket(session, session.Client.Nickname);
            return true;
        }
    }
}
