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
    /// <summary>处理 <see cref="PacketName.SpRqBingoGachaInfoPacket"/> 数据包。</summary>
    internal sealed class SpRqBingoGachaInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqBingoGachaInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            // IDA: SpRqBingoGachaInfoPacket = int + int
            if (inPacket.Available >= 4) inPacket.ReadInt();
            if (inPacket.Available >= 4) inPacket.ReadInt();
            var bingo = Bingo.Get(session.Client.Nickname);
            Log.Info($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{session.Client.Nickname}] 请求面板 SpRqBingoGachaInfoPacket 格子={bingo.BingoNumsList.Count} 已获取={bingo.BingoNums.Count(n => n.Value == 1)} 道具={bingo.BingoItem}");
            using (OutPacket outPacket = new OutPacket("SpRpBingoGachaInfoPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteByte(bingo.BingoItem);//选择的Bingo道具
                outPacket.WriteBytes(new byte[3]);
                outPacket.WriteShort(0);
                outPacket.WriteInt(bingo.BingoNumsList.Count);//Bingo格子数量
                foreach (var num in bingo.BingoNumsList)
                {
                    outPacket.WriteByte(num);//Bingo格子数字
                    byte obtainedNum = 0;
                    bingo.BingoNums.TryGetValue(num, out obtainedNum);
                    outPacket.WriteByte(obtainedNum);//数字是否获得
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
