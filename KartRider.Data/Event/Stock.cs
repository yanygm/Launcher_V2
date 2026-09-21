using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcData;
using KartRider.IO.Packet;
using Profile;
using RiderData;

namespace KartRider
{
    public static class Stock
    {
        // key为stockId，value为该stock下的item列表
        public static Dictionary<uint, List<item>> StockList = new Dictionary<uint, List<item>>();
        public static Dictionary<uint, price> PriceList = new Dictionary<uint, price>();

        /**
         * 尝试获取指定stockId的item列表
         * @param stockId 商品Id
         * @param itemList 输出的item列表
         */
        public static bool TryGet(uint stockId, out List<item> itemList)
        {
            return StockList.TryGetValue(stockId, out itemList);
        }

        public static void GetStockItem(SessionGroup Parent, uint stockId)
        {
            TryGet(stockId, out List<item> itemList);
            Console.WriteLine(itemList.Count);
            foreach (item Item in itemList)
            {
                if (Item.itemCatId == 3 && Item.expireDay == 0)
                {
                    AddNewKart(Parent, Item.itemId);
                }
                else
                {
                    AddNewItem(Parent.Client.Nickname, Item);
                }
            }
        }

        public static void ShopBuy(SessionGroup Parent, uint stockId, byte priceType)
        {
            if (Stock.PriceList.ContainsKey(stockId))
            {
                var priceConfig = ProfileService.GetProfileConfig(Parent.Client.Nickname);
                var PayBool = Pay(Parent.Client.Nickname, stockId, priceConfig);
                ShopBool(Parent, PayBool, stockId);
            }
        }

        public static bool Pay(string Nickname, uint stockId, ProfileConfig priceConfig)
        {
            var price = Stock.PriceList[stockId];
            if (price.priceType == 0)
            {
                if (price.salePrice <= priceConfig.Rider.Cash)
                {
                    priceConfig.Rider.Cash -= price.salePrice;
                    ProfileService.Save(Nickname, priceConfig);
                    return true;
                }
            }
            else if (price.priceType == 1)
            {
                if (price.salePrice <= priceConfig.Rider.Lucci)
                {
                    priceConfig.Rider.Lucci -= price.salePrice;
                    ProfileService.Save(Nickname, priceConfig);
                    return true;
                }
            }
            else if (price.priceType == 2)
            {
                if (price.salePrice <= priceConfig.Rider.TcCash)
                {
                    priceConfig.Rider.TcCash -= price.salePrice;
                    ProfileService.Save(Nickname, priceConfig);
                    return true;
                }
            }
            else if (price.priceType == 3)
            {
                if (price.salePrice <= priceConfig.Rider.Koin)
                {
                    priceConfig.Rider.Koin -= price.salePrice;
                    ProfileService.Save(Nickname, priceConfig);
                    return true;
                }
            }
            return false;
        }

        public static void ShopBool(SessionGroup Parent, bool Shop, uint stockId)
        {
            if (Shop)
            {
                using (OutPacket outPacket = new OutPacket("SpRepBuyItemPacket"))
                {
                    outPacket.WriteInt(0);
                    outPacket.WriteInt(0);
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.Lucci);
                    outPacket.WriteHexString("00 00 00 00");
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.Koin);
                    outPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00");
                    Parent.Client.Send(outPacket);
                }
                GetStockItem(Parent, stockId);
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("SpRepBuyItemPacket"))
                {
                    outPacket.WriteInt(1);
                    outPacket.WriteInt(0);
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.Lucci);
                    outPacket.WriteHexString("00 00 00 00");
                    outPacket.WriteUInt(ProfileService.GetProfileConfig(Parent.Client.Nickname).Rider.Koin);
                    outPacket.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00");
                    Parent.Client.Send(outPacket);
                }
            }
        }

        public static void AddNewKart(SessionGroup Parent, ushort Kart)
        {
            string Nickname = Parent.Client.Nickname;
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var newkart = new List<NewKart>();
            if (File.Exists(filename.NewKart_LoadFile))
            {
                newkart = JsonHelper.DeserializeNoBom<List<NewKart>>(filename.NewKart_LoadFile) ?? new List<NewKart>();
            }
            ushort kartid = ProfileService.GetProfileConfig(Nickname)?.RiderItem?.Set_Kart ?? 0;
            if (kartid == 0)
                kartid = Kart;
            ushort newsn = newkart.Any(kart => kart.KartID == kartid) ? (ushort)newkart.Where(kart => kart.KartID == kartid).Max(kart => kart.KartSN) : (ushort)1;
            var addkart = new NewKart { KartID = kartid, KartSN = (ushort)(newsn + 1) };
            newkart.Add(addkart);
            File.WriteAllText(filename.NewKart_LoadFile, JsonHelper.Serialize(newkart));
            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
            {
                outPacket.WriteByte(1);
                outPacket.WriteInt(1);
                outPacket.WriteShort(3);
                outPacket.WriteUShort(addkart.KartID);
                outPacket.WriteUShort(addkart.KartSN);
                outPacket.WriteUShort(1);//数量
                outPacket.WriteShort(0);
                outPacket.WriteShort(-1);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                outPacket.WriteShort(0);
                Parent.Client.Send(outPacket);
            }
        }

        /**
         * 读取指定昵称的NewItem列表
         * @param Nickname 昵称
         */
        public static List<NewItem> LoadNewItem(fileName filename)
        {
            if (!File.Exists(filename.NewItem_LoadFile))
            {
                return new List<NewItem>();
            }
            return JsonHelper.DeserializeNoBom<List<NewItem>>(filename.NewItem_LoadFile) ?? new List<NewItem>();
        }

        /**
         * 删除已到期的道具并保存到 NewItem.json
         * @param Nickname 昵称
         * @return 删除到期道具后的列表（endTime为DateTime.MinValue表示无期限，不会被删除）
         */
        public static List<NewItem> DelExpiredNewItem(string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            var newitem = LoadNewItem(filename);
            DateTime now = DateTime.Now;
            var expiredItems = newitem.Where(item => item.endTime != DateTime.MinValue && item.endTime <= now).ToList();
            if (expiredItems.Count > 0)
            {
                foreach (var item in expiredItems)
                {
                    newitem.Remove(item);
                }
                File.WriteAllText(filename.NewItem_LoadFile, JsonHelper.Serialize(newitem));
            }
            return newitem;
        }

        /**
         * 添加道具到 NewItem.json（添加前会先清除已到期的道具）
         * @param Nickname 昵称
         * @param Item 道具数据
         */
        public static void AddNewItem(string Nickname, item Item)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            var newitem = DelExpiredNewItem(Nickname);

            if (Item.expireDay == 0)
            {
                // 无期限道具：存在无期限记录时累加数量
                var existingItem = newitem.FirstOrDefault(item => item.itemCatId == Item.itemCatId && item.itemId == Item.itemId && item.endTime == DateTime.MinValue);
                if (existingItem != null)
                {
                    existingItem.itemCount = (ushort)Math.Min(existingItem.itemCount + (int)Item.itemCount, ushort.MaxValue);
                    File.WriteAllText(filename.NewItem_LoadFile, JsonHelper.Serialize(newitem));
                    return;
                }
            }
            else
            {
                // 有期限道具：不增加数量，仅在原endTime上叠加天数（续期）
                var existingItem = newitem.FirstOrDefault(item => item.itemCatId == Item.itemCatId && item.itemId == Item.itemId && item.endTime != DateTime.MinValue);
                if (existingItem != null)
                {
                    existingItem.endTime = existingItem.endTime.AddDays(Item.expireDay);
                    File.WriteAllText(filename.NewItem_LoadFile, JsonHelper.Serialize(newitem));
                    return;
                }
            }

            {
                newitem.Add(new NewItem
                {
                    itemCatId = Item.itemCatId,
                    itemId = Item.itemId,
                    itemCount = Item.itemCount,
                    endTime = Item.expireDay == 0 ? DateTime.MinValue : DateTime.Now.AddDays(Item.expireDay)
                });

                if (Item.itemCatId == 3)
                {
                    var Kart = new KartSpec();
                    Kart.GetKartSpec(Item.itemId);
                    if (Kart.defaultExceedType > 0)
                    {
                        KartExcData.AddPartsList(Nickname, (short)Item.itemId, 0, 72, (short)Kart.defaultEngineType, 0, 0);
                        KartExcData.AddPartsList(Nickname, (short)Item.itemId, 0, 73, (short)Kart.defaultHandleType, 0, 0);
                        KartExcData.AddPartsList(Nickname, (short)Item.itemId, 0, 74, (short)Kart.defaultWheelType, 0, 0);
                        KartExcData.AddPartsList(Nickname, (short)Item.itemId, 0, 75, (short)Kart.defaultBoosterType, 0, 0);
                        KartExcData.AddPartsList(Nickname, (short)Item.itemId, 0, 0, (short)Kart.defaultExceedType, 0, 0);
                    }
                    else if (Kart.TachometerType == "XGenTacho" || Kart.TachometerType == "V1GenTacho")
                    {
                        KartExcData.AddPartsList(Nickname, (short)Item.itemId, 0, 63, 0, 0, 0);
                    }
                }
            }

            File.WriteAllText(filename.NewItem_LoadFile, JsonHelper.Serialize(newitem));
        }

        public static void DelNewKart(string Nickname, ushort ItemID, ushort SN)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            var newkart = new List<NewKart>();
            if (File.Exists(filename.NewKart_LoadFile))
            {
                newkart = JsonHelper.DeserializeNoBom<List<NewKart>>(filename.NewKart_LoadFile) ?? new List<NewKart>();
                var targetItems = newkart.Where(kart => kart.KartID == ItemID && kart.KartSN == SN).ToList();
                foreach (var item in targetItems)
                {
                    newkart.Remove(item);
                }
                File.WriteAllText(filename.NewKart_LoadFile, JsonHelper.Serialize(newkart));
            }

            var newitem = new List<NewItem>();
            if (File.Exists(filename.NewItem_LoadFile))
            {
                newitem = JsonHelper.DeserializeNoBom<List<NewItem>>(filename.NewItem_LoadFile) ?? new List<NewItem>();
                var targetItems = newitem.Where(item => item.itemCatId == 3 && item.itemId == ItemID).ToList();
                foreach (var item in targetItems)
                {
                    newitem.Remove(item);
                }
                File.WriteAllText(filename.NewItem_LoadFile, JsonHelper.Serialize(newitem));
            }
        }

        public static void DelNewItem(string Nickname, ushort ItemType, ushort ItemID, ushort ItemCount = 0)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            var newitem = new List<NewItem>();
            if (File.Exists(filename.NewItem_LoadFile))
            {
                newitem = JsonHelper.DeserializeNoBom<List<NewItem>>(filename.NewItem_LoadFile) ?? new List<NewItem>();
                var targetItems = newitem.Where(item => item.itemCatId == ItemType && item.itemId == ItemID).ToList();
                foreach (var item in targetItems)
                {
                    if (ItemCount == 0)
                    {
                        newitem.Remove(item);
                    }
                    else if (ItemCount > 0 && ItemCount <= item.itemCount)
                    {
                        item.itemCount -= ItemCount;
                    }
                }
                File.WriteAllText(filename.NewItem_LoadFile, JsonHelper.Serialize(newitem));
            }
        }
    }

    public class item
    {
        public ushort itemCatId { get; }
        public ushort itemId { get; }
        public ushort itemCount { get; }
        public double expireDay { get; }

        public item(ushort itemCatId, ushort itemId, ushort itemCount, double expireDay)
        {
            this.itemCatId = itemCatId;
            this.itemId = itemId;
            this.itemCount = itemCount;
            this.expireDay = expireDay;
        }
    }

    public class price
    {
        public byte priceType { get; }
        public uint salePrice { get; }

        public price(byte priceType, uint salePrice)
        {
            this.priceType = priceType;
            this.salePrice = salePrice;
        }
    }

    public class NewKart
    {
        public ushort KartID { get; set; } = 0;
        public ushort KartSN { get; set; } = 0;
    }

    public class NewItem
    {
        public ushort itemCatId { get; set; }
        public ushort itemId { get; set; }
        public ushort itemSn { get; set; } = 0;
        public ushort itemCount { get; set; }
        public DateTime endTime { get; set; }
    }
}
