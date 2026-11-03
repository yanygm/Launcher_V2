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
    /// <summary>处理 <see cref="PacketName.SpRqBingoGachaPacket"/> 数据包。</summary>
    internal sealed class SpRqBingoGachaPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.SpRqBingoGachaPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int BingoType = inPacket.ReadInt();
            var bingo = Bingo.Get(session.Client.Nickname);
            Log.Info($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{session.Client.Nickname}] 请求面板 SpRqBingoGachaPacket type={BingoType} 次数={bingo.BingoCount} 上次数字={bingo.BingoNum} 已达成道具={bingo.BingoItems.Count(i => i.Value == 1)}");
            using (OutPacket outPacket = new OutPacket("SpRpBingoGachaPacket"))
            {
                outPacket.WriteInt(BingoType);
                outPacket.WriteInt(bingo.BingoNumsList.Count);//Bingo格子数量
                foreach (var num in bingo.BingoNumsList)
                {
                    outPacket.WriteByte(num);//Bingo格子数字
                    byte obtainedNum = 0;
                    bingo.BingoNums.TryGetValue(num, out obtainedNum);
                    outPacket.WriteByte(obtainedNum);//数字是否获得
                }
                outPacket.WriteInt(0);
                outPacket.WriteInt(bingo.BingoItemsList.Count);//Bingo道具数量
                foreach (var item in bingo.BingoItemsList)
                {
                    outPacket.WriteInt(item);//Bingo道具
                    byte obtainedItem = 0;
                    bingo.BingoItems.TryGetValue(item, out obtainedItem);
                    outPacket.WriteByte(obtainedItem);//道具是否获得
                }
                outPacket.WriteInt(0);
                outPacket.WriteInt(0);
                outPacket.WriteShort(bingo.BingoCount);
                outPacket.WriteByte(0);
                outPacket.WriteByte(bingo.BingoNum);//获取的数字
                byte lastObtained = 0;
                bingo.BingoNums.TryGetValue(bingo.BingoNum, out lastObtained);
                outPacket.WriteByte(lastObtained);//获取的数字是否已点亮
                bingo.BingoLight(session, bingo.BingoNum);//点亮数字
                outPacket.WriteByte(0);
                outPacket.WriteByte(0);
                outPacket.WriteByte(1);
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
