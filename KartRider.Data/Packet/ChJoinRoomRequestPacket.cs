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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static KartRider.MultyPlayer;

namespace KartRider.Packet
{
    /// <summary>处理 <see cref="PacketName.ChJoinRoomRequestPacket"/> 数据包。</summary>
    internal sealed class ChJoinRoomRequestPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.ChJoinRoomRequestPacket;

        protected override bool Handle(SessionGroup Parent, InPacket iPacket)
        {
            // IDA: ChJoinRoomRequestPacket = ushort roomId + string password + byte + int
            ushort roomId = iPacket.ReadUShort();
            string pwd = iPacket.ReadString();
            byte unk1 = iPacket.Available >= 1 ? iPacket.ReadByte() : (byte)0;
            int unk2 = iPacket.Available >= 4 ? iPacket.ReadInt() : 0;
            Log.Info($"ChJoinRoomRequestPacket, roomId = {roomId}, pwd = {pwd}, unk1 = {unk1}, unk2 = {unk2}");

            ChJoinRoomReplyPacket(Parent, roomId, pwd);
            return true;
        }
    }
}
