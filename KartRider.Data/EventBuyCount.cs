using System;
using KartRider.IO.Packet;

namespace KartRider
{
	public static class EventBuyCount
	{
		public static int BuyCount = 0;
		public static int ShopItem1 = 0;
		public static int ShopItem2 = 0;
		public static int ShopItem3 = 0;
		public static int ShopItem4 = 0;
		public static int ShopItem5 = 0;
		public static int ShopItem6 = 0;
		public static int ShopItem7 = 0;
		public static int ShopItem8 = 0;
		public static int ShopItem9 = 0;
		public static int ShopItem10 = 0;
		public static int ShopItem11 = 0;
		public static int ShopItem12 = 0;
		public static int ShopItem13 = 0;
		public static int ShopItem14 = 0;
		public static int ShopItem15 = 0;
		public static int ShopItem16 = 0;
		public static int ShopItem17 = 0;
		public static int ShopItem18 = 0;
		public static int ShopItem19 = 0;
		public static int ShopItem20 = 0;

		public static void PrEventBuyCount()
		{
			using (OutPacket outPacket = new OutPacket("PrEventBuyCount"))
			{
				outPacket.WriteInt(EventBuyCount.BuyCount);
				if (EventBuyCount.BuyCount >= 1)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem1);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 2)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem2);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 3)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem3);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 4)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem4);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 5)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem5);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 6)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem6);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 7)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem7);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 8)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem8);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 9)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem9);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 10)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem10);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 11)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem11);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 12)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem12);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 13)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem13);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 14)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem14);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 15)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem15);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 16)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem16);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 17)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem17);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 18)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem18);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 19)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem19);
					outPacket.WriteInt(0);
				}
				if (EventBuyCount.BuyCount >= 20)
				{
					outPacket.WriteInt(EventBuyCount.ShopItem20);
					outPacket.WriteInt(0);
				}
				RouterListener.MySession.Client.Send(outPacket);
			}
		}
	}
}