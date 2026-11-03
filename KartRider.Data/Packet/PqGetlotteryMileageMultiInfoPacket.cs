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
    /// <summary>处理 <see cref="PacketName.PqGetlotteryMileageMultiInfoPacket"/> 数据包。</summary>
    internal sealed class PqGetlotteryMileageMultiInfoPacket : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqGetlotteryMileageMultiInfoPacket;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            int Count = inPacket.ReadInt();
            if (Count < 0 || Count > 64 || Count * 4 > inPacket.Available)
            {
                Log.Warn($"[Lottery] 非法数量 {Count}，已忽略");
                return true;
            }
            uint[] Data = new uint[Count];
            for (int i = 0; i < Count; i++)
            {
                Data[i] = inPacket.ReadUInt();
            }
            using (OutPacket outPacket = new OutPacket("PrGetlotteryMileageMultiInfoPacket"))
            {
                outPacket.WriteInt(Count);
                for (int i = 0; i < Count; i++)
                {
                    outPacket.WriteUInt(Data[i]);
                    outPacket.WriteShort(1);
                }
                session.Client.Send(outPacket);
            }
            return true;
        }
    }
}
