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
    /// <summary>处理 <see cref="PacketName.PqExChangePacket"/> 数据包。</summary>
    internal sealed class PqExChangePacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqExChangePacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // 原实现直接断线且不回包；改为记录并忽略，避免玩家正常操作被踢
            Log.Warn($"[Exchange] PqExChangePacket 未实现，已忽略（{session.Client.Nickname}）");
            return true;
        }
    }
}
