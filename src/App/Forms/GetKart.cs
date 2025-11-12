using Launcher.App.ExcData;
using Launcher.App.Profile;
using Launcher.App.Server;
using Launcher.Library.IO;
using System.Xml;

namespace KartRider
{
    public partial class GetKart : Form
    {
        private static short Item_Type = 0;
        private static short Item_Code = 0;

        public GetKart()
        {
            InitializeComponent();

            foreach (var outerKey in KartExcData.items.Keys)
            {
                ItemType.Items.Add(outerKey);
            }
            ItemType.SelectedIndex = 2;
        }

        private void button_Add_Click(object sender, EventArgs e)
        {
            // try to parse ItemID when it is a number.
            if (short.TryParse(ItemID.Text, out short tempValue))
            {
                Item_Code = tempValue;
            }

            new Thread(() =>
            {
                short ItemSN = 0; // default value for non-kart items.
                if (Item_Type == 3) // type: kart. Need to handle NewKart.xml.
                {
                    short KartSN = 2; // 1 is the default kart, so start from 2.
                    if (KartExcData.NewKart != null)
                    {
                        var existingItems = KartExcData.NewKart.Where(list => list[0] == Item_Code).ToList();
                        if (existingItems != null)
                        {
                            KartSN = (short)(existingItems.Count + KartSN);
                        }
                    }
                    KartExcData.NewKart.Add([Item_Code, KartSN]);
                    Save_NewKartList(KartExcData.NewKart);
                    ItemSN = KartSN;
                }

                using (OutPacket outPacket = new("PrRequestKartInfoPacket"))
                {
                    outPacket.WriteByte(1);
                    outPacket.WriteInt(1);
                    outPacket.WriteShort(Item_Type);
                    outPacket.WriteShort(Item_Code);
                    outPacket.WriteShort(ItemSN);
                    outPacket.WriteUShort(1); // 数量 수량
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(-1);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    outPacket.WriteShort(0);
                    RouterListener.MySession.Client.Send(outPacket);
                }
            }).Start();
            Console.WriteLine($"[Get Item] Add: Type:{ItemType.Text}, ID:{ItemID.Text}");
        }

        public static void Save_NewKartList(List<List<short>> NewKart)
        {
            File.Delete(FileName.NewKart_LoadFile);
            XmlTextWriter writer = new XmlTextWriter(FileName.NewKart_LoadFile, System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("NewKart");
            writer.WriteEndElement();
            writer.Close();
            for (var i = 0; i < NewKart.Count; i++)
            {
                XmlDocument xmlDoc = new();
                xmlDoc.Load(FileName.NewKart_LoadFile);
                XmlNode root = xmlDoc.SelectSingleNode("NewKart");
                XmlElement xe1 = xmlDoc.CreateElement("Kart");
                xe1.SetAttribute("id", NewKart[i][0].ToString());
                xe1.SetAttribute("sn", NewKart[i][1].ToString());
                root.AppendChild(xe1);
                xmlDoc.Save(FileName.NewKart_LoadFile);
            }
        }

        private void ItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemType.SelectedItem != null)
            {
                Item_Type = (short)ItemType.SelectedItem;
                ItemID.Items.Clear();
                ItemID.Text = "";

                if (KartExcData.items.TryGetValue(Item_Type, out Dictionary<short, string> innerDictionary))
                {
                    foreach (var innerValue in innerDictionary.Values)
                    {
                        ItemID.Items.Add(innerValue);
                    }
                }
            }
        }

        private void ItemID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemType.SelectedItem != null && ItemID.SelectedItem != null)
            {
                short selectedOuterKey = (short)ItemType.SelectedItem;
                string selectedInnerValue = ItemID.SelectedItem.ToString();

                if (KartExcData.items.TryGetValue(selectedOuterKey, out Dictionary<short, string> innerDictionary))
                {
                    Item_Code = innerDictionary.FirstOrDefault(pair => pair.Value == selectedInnerValue).Key;
                    Console.WriteLine($"[Get Item] Select: Type:{ItemType.Text}, ID:{ItemID.Text}");
                }
            }
        }

        private void ItemType_MouseEnter(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(ItemType, "在下拉列表中选择物品的类型, 或者输入类型代号.");
        }

        private void ItemID_MouseEnter(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(ItemID, "在下拉列表中选择物品的ID, 或者输入代号.");
        }
    }
}
