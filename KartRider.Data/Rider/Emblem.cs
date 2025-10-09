using ExcData;
using KartRider;
using KartRider.IO.Packet;
using System;
using System.Collections.Generic;
using System.Xml;

namespace RiderData
{
    public static class Emblem
    {
        public static List<short> emblem = new List<short>();

        public static void RmOwnerEmblemPacket()
        {
            int All_Emblem = emblem.Count;
            using (OutPacket outPacket = new OutPacket("RmOwnerEmblemPacket"))
            {
                outPacket.WriteInt(1);
                outPacket.WriteInt(1);
                outPacket.WriteInt(All_Emblem);
                foreach (var item in emblem)
                {
                    outPacket.WriteShort(item);
                }
                RouterListener.MySession.Client.Send(outPacket);
            }
        }
    }
}
