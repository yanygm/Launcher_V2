using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using KartRider.IO.Packet;

namespace KartRider
{
    public class mixEntity
    {
        public uint id { get; set; }
        public string type { get; set; }
        public ushort resultItemCatId { get; set; }
        public ushort resultItemId { get; set; }
        public int resultItemCount { get; set; }
        public uint resultStockId { get; set; }
        public List<requireItem> requireItemList { get; set; }
    }

    public class requireItem
    {
        public ushort itemCatId { get; set; }
        public ushort itemId { get; set; }
        public int itemCount { get; set; }
    }

    public static class Mix
    {
        public static List<mixEntity> mixList = new List<mixEntity>();

        public static void ParseDocument(XDocument doc)
        {
            if (doc.Root == null)
                mixList = new List<mixEntity>();

            mixList = doc.Root
                .Elements("mixEntity")
                .Select(e => new mixEntity
                {
                    id = GetUInt(e, "id"),
                    type = GetString(e, "type"),

                    resultItemCatId = GetUShort(e, "resultItemCatId"),
                    resultItemId = GetUShort(e, "resultItemId"),
                    resultItemCount = GetInt(e, "resultItemCount"),
                    resultStockId = GetUInt(e, "resultStockId"),

                    requireItemList = e.Elements("requireItem")
                        .Select(r => new requireItem
                        {
                            itemCatId = GetUShort(r, "itemCatId"),
                            itemId = GetUShort(r, "itemId"),
                            itemCount = GetInt(r, "itemCount")
                        })
                        .ToList()
                })
                .ToList();
        }

        private static string GetString(XElement element, string attributeName)
        {
            XAttribute attr = element.Attribute(attributeName);
            return attr == null ? string.Empty : attr.Value;
        }

        private static uint GetUInt(XElement element, string attributeName)
        {
            XAttribute attr = element.Attribute(attributeName);
            uint value;

            if (attr != null && uint.TryParse(attr.Value, out value))
                return value;

            return 0;
        }

        private static ushort GetUShort(XElement element, string attributeName)
        {
            XAttribute attr = element.Attribute(attributeName);
            ushort value;

            if (attr != null && ushort.TryParse(attr.Value, out value))
                return value;

            return 0;
        }

        private static int GetInt(XElement element, string attributeName)
        {
            XAttribute attr = element.Attribute(attributeName);
            int value;

            if (attr != null && int.TryParse(attr.Value, out value))
                return value;

            return 0;
        }

        public static void GrtMixItem(SessionGroup Parent, uint MixID, int Type)
        {
            mixEntity entity = mixList.FirstOrDefault(x => x.id == MixID);
            if (entity != null)
            {
                using (OutPacket outPacket = new OutPacket("PrMixItem"))
                {
                    outPacket.WriteUInt(MixID);
                    outPacket.WriteByte(0);
                    outPacket.WriteUInt(entity.resultStockId);
                    outPacket.WriteBytes(new byte[10]);
                    Parent.Client.Send(outPacket);
                }
                if (entity.resultStockId != 0)
                {
                    Stock.GetStockItem(Parent, entity.resultStockId);
                }
                else if (entity.resultItemCatId != 0 && entity.resultItemId != 0)
                {
                    if (entity.resultItemCatId == 3)
                    {
                        Stock.AddNewKart(Parent, entity.resultItemId);
                    }
                    else
                    {
                        item Item = new item(entity.resultItemCatId, entity.resultItemId, (ushort)entity.resultItemCount, 0);
                        Stock.AddNewItem(Parent.Client.Nickname, Item);
                    }
                }
                foreach (requireItem item in entity.requireItemList)
                {
                    Stock.DelNewItem(Parent.Client.Nickname, item.itemCatId, item.itemId, (ushort)item.itemCount);
                }
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("PrMixItem"))
                {
                    outPacket.WriteUInt(MixID);
                    outPacket.WriteByte(1);
                    Parent.Client.Send(outPacket);
                }
            }
        }
    }
}