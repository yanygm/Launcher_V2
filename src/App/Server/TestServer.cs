using Launcher.Library.IO;

namespace Launcher.App.Server
{
    public static class TestServer
    {
        public static short Type = 0;
        public static short ItemID = 0;
        public static short Amount = 0;

        public static void TestServerAddItem()
        {
            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteInt(1);
                outPacket.WriteShort(Type);
                outPacket.WriteShort(ItemID);
                outPacket.WriteShort(0);
                outPacket.WriteShort(Amount);
                outPacket.WriteShort(0);
                outPacket.WriteShort(-1);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                RouterListener.MySession.Client.Send(outPacket);
            }
            Type = 0;
            ItemID = 0;
            Amount = 0;
        }
    }
}