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
    /// <summary>处理 <see cref="PacketName.SpRqBingoGachaSelectSetPacket"/> 数据包。</summary>
    internal sealed class SpRqBingoGachaSelectSetPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqBingoGachaSelectSetPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            var bingo = Bingo.Get(session.Client.Nickname);
            bingo.BingoItem = inPacket.ReadByte();//选择的Bingo道具
            Log.Info($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{session.Client.Nickname}] 选择道具 SpRqBingoGachaSelectSetPacket 道具={bingo.BingoItem}");
            bingo.Save();
            using (OutPacket outPacket = new OutPacket("SpRpBingoGachaSelectSetPacket"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteByte(0);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
