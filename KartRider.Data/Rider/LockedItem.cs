using System.Collections.Generic;
using System.IO;
using System.Linq;
using KartRider;
using KartRider.IO.Packet;
using Profile;

namespace RiderData
{
    public static class LockedItem
    {
        public static void LockedItemGet(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var LockedItemList = new List<Locked_Item>();
            if (File.Exists(filename.Locked_LoadFile))
            {
                LockedItemList = JsonHelper.DeserializeNoBom<List<Locked_Item>>(filename.Locked_LoadFile) ?? new List<Locked_Item>();
            }
            using (OutPacket outPacket = new OutPacket("PrLockedItemGet"))
            {
                outPacket.WriteInt(LockedItemList.Count);
                foreach (var LockedItem in LockedItemList)
                {
                    outPacket.WriteUShort(LockedItem.ItemCatID);
                    outPacket.WriteUShort(LockedItem.ItemID);
                    outPacket.WriteUShort(LockedItem.ItemSN);
                    outPacket.WriteByte(0);
                }
                Parent.Client.Send(outPacket);
            }
        }

        public static void LockedItem_Add(string Nickname, ushort itemCatID, ushort itemID, ushort itemSN)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var locked = new List<Locked_Item>();
            if (File.Exists(filename.Locked_LoadFile))
            {
                locked = JsonHelper.DeserializeNoBom<List<Locked_Item>>(filename.Locked_LoadFile);
            }
            bool targetItems = locked.Any(kart => kart.ItemCatID == itemCatID && kart.ItemID == itemID && kart.ItemSN == itemSN);
            if (!targetItems)
            {
                locked.Add(new Locked_Item { ItemCatID = itemCatID, ItemID = itemID, ItemSN = itemSN });
                File.WriteAllText(filename.Locked_LoadFile, JsonHelper.Serialize(locked));
            }
        }

        public static void LockedItem_Del(string Nickname, ushort itemCatID, ushort itemID, ushort itemSN)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var locked = new List<Locked_Item>();
            if (File.Exists(filename.Locked_LoadFile))
            {
                locked = JsonHelper.DeserializeNoBom<List<Locked_Item>>(filename.Locked_LoadFile) ?? new List<Locked_Item>();
                var targetItems = locked.Where(kart => kart.ItemCatID == itemCatID && kart.ItemID == itemID && kart.ItemSN == itemSN).ToList();
                foreach (var item in targetItems)
                {
                    locked.Remove(item);
                }
                File.WriteAllText(filename.Locked_LoadFile, JsonHelper.Serialize(locked));
            }
        }
    }

    public class Locked_Item
    {
        public ushort ItemCatID { get; set; } = 0;
        public ushort ItemID { get; set; } = 0;
        public ushort ItemSN { get; set; } = 0;
    }
}