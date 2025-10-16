using System;
using KartRider.IO.Packet;

namespace KartRider
{
    public static class TestServer
    {
        public static short Type = 0;
        public static short ItemID = 0;
        public static short Amount = 0;

        public static void TestServerAddItem(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteInt(1);
                outPacket.WriteShort(TestServer.Type);
                outPacket.WriteShort(TestServer.ItemID);
                outPacket.WriteShort(0);
                outPacket.WriteShort(TestServer.Amount);
                outPacket.WriteShort(0);
                outPacket.WriteShort(-1);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                Parent.Client.Send(outPacket);
            }
            TestServer.Type = 0;
            TestServer.ItemID = 0;
            TestServer.Amount = 0;
        }
    }
}
