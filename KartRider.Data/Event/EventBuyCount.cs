using System;
using KartRider.IO.Packet;

namespace KartRider
{
    public static class EventBuyCount
    {
        public static int BuyCount = 0;
        public static int[] ShopItem = new int[0];

        public static void PrEventBuyCount(SessionGroup Parent)
        {
            using (OutPacket outPacket = new OutPacket("PrEventBuyCount"))
            {
                outPacket.WriteInt(EventBuyCount.BuyCount);
                for (int i = 0; i < EventBuyCount.ShopItem.Length; i++)
                {
                    outPacket.WriteInt(EventBuyCount.ShopItem[i]);
                    outPacket.WriteInt(0);
                }
                Parent.Client.Send(outPacket);
            }
        }
    }
}
