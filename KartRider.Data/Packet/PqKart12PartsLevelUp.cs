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
    /// <summary>处理 <see cref="PacketName.PqKart12PartsLevelUp"/> 数据包。</summary>
    internal sealed class PqKart12PartsLevelUp : BuiltInPacketHandler
    {
        public override PacketName TargetPacket => PacketName.PqKart12PartsLevelUp;

        protected override bool Handle(SessionGroup session, InPacket inPacket)
        {
            short kart = inPacket.ReadShort();
            short sn = inPacket.ReadShort();
            inPacket.ReadShort();
            byte parts = inPacket.ReadByte();
            byte old = inPacket.ReadByte();
            inPacket.ReadByte();
            byte leve = inPacket.ReadByte();
            using (OutPacket outPacket = new OutPacket("PrKart12PartsLevelUp"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteUInt(ProfileService.GetProfileConfig(session.Client.Nickname).Rider.Lucci);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                session.Client.Send(outPacket);
            }
            short Item_Cat_Id = 0;
            switch (parts)
            {
                case 0:
                    Item_Cat_Id = 72;
                    break;
                case 1:
                    Item_Cat_Id = 73;
                    break;
                case 2:
                    Item_Cat_Id = 74;
                    break;
                case 3:
                    Item_Cat_Id = 75;
                    break;
            }
            if (kart != 0 && sn != 0 && Item_Cat_Id != 0)
            {
                KartExcData.AddPartsList(session.Client.Nickname, kart, sn, Item_Cat_Id, (short)(old + 1), V2Specs.GetGrade((byte)(old + 1)), V2Specs.Get12Parts((short)(old + 1)));
            }
            return true;
        }
    }
}
