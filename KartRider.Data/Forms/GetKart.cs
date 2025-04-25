using System;
using KartRider.IO.Packet;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using ExcData;
using System.Linq;
using System.Collections.Generic;

namespace KartRider
{
    public partial class GetKart : Form
    {
        public static string NewKart_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\NewKart.xml";
        public static short Item_Type = 0;
        public static short Item_Code = 0;

        public GetKart()
        {
            InitializeComponent();
            foreach (var outerKey in KartExcData.items.Keys)
            {
                ItemType.Items.Add(outerKey);
            }
            ItemType.SelectedIndex = 2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (new Thread(() =>
            {
                if (GetKart.Item_Type == 3)
                {
                    short KartSN = 2;
                    if (KartExcData.NewKart == null)
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(GetKart.Item_Type);
                            outPacket.WriteShort(GetKart.Item_Code);
                            outPacket.WriteShort(KartSN);
                            outPacket.WriteShort(1); // 数量 수량
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(-1);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            RouterListener.MySession.Client.Send(outPacket);
                        }
                        var newList = new List<short> { GetKart.Item_Code, KartSN };
                        KartExcData.NewKart.Add(newList);
                        Save_NewKartList(KartExcData.NewKart);
                    }
                    else
                    {
                        var existingItems = KartExcData.NewKart.Where(list => list[0] == GetKart.Item_Code).ToList();
                        if (existingItems == null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                            {
                                outPacket.WriteByte(1);
                                outPacket.WriteInt(1);
                                outPacket.WriteShort(GetKart.Item_Type);
                                outPacket.WriteShort(GetKart.Item_Code);
                                outPacket.WriteShort(KartSN);
                                outPacket.WriteShort(1); // 数量 수량
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(-1);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                RouterListener.MySession.Client.Send(outPacket);
                            }
                            var newList = new List<short> { GetKart.Item_Code, KartSN };
                            KartExcData.NewKart.Add(newList);
                            Save_NewKartList(KartExcData.NewKart);
                        }
                        else
                        {
                            KartSN = (short)(existingItems.Count + (int)KartSN);
                            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                            {
                                outPacket.WriteByte(1);
                                outPacket.WriteInt(1);
                                outPacket.WriteShort(GetKart.Item_Type);
                                outPacket.WriteShort(GetKart.Item_Code);
                                outPacket.WriteShort(KartSN);
                                outPacket.WriteShort(1); // 数量 수량
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(-1);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                RouterListener.MySession.Client.Send(outPacket);
                            }
                            var newList = new List<short> { GetKart.Item_Code, KartSN };
                            KartExcData.NewKart.Add(newList);
                            Save_NewKartList(KartExcData.NewKart);
                        }
                    }
                }
                else
                {
                    using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                    {
                        outPacket.WriteByte(1);
                        outPacket.WriteInt(1);
                        outPacket.WriteShort(GetKart.Item_Type);
                        outPacket.WriteShort(GetKart.Item_Code);
                        outPacket.WriteUShort(0);
                        outPacket.WriteShort(1); // 数量 수량
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(-1);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(0);
                        RouterListener.MySession.Client.Send(outPacket);
                    }
                }
            })).Start();
        }

        public static void Save_NewKartList(List<List<short>> NewKart)
        {
            File.Delete(NewKart_LoadFile);
            XmlTextWriter writer = new XmlTextWriter(NewKart_LoadFile, System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("NewKart");
            writer.WriteEndElement();
            writer.Close();
            for (var i = 0; i < NewKart.Count; i++)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(NewKart_LoadFile);
                XmlNode root = xmlDoc.SelectSingleNode("NewKart");
                XmlElement xe1 = xmlDoc.CreateElement("Kart");
                xe1.SetAttribute("id", NewKart[i][0].ToString());
                xe1.SetAttribute("sn", NewKart[i][1].ToString());
                root.AppendChild(xe1);
                xmlDoc.Save(NewKart_LoadFile);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemType.SelectedItem != null)
            {
                GetKart.Item_Type = (short)ItemType.SelectedItem;
                ItemID.Items.Clear();

                if (KartExcData.items.TryGetValue(GetKart.Item_Type, out Dictionary<short, string> innerDictionary))
                {
                    foreach (var innerValue in innerDictionary.Values)
                    {
                        ItemID.Items.Add(innerValue);
                    }
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemType.SelectedItem != null && ItemID.SelectedItem != null)
            {
                short selectedOuterKey = (short)ItemType.SelectedItem;
                string selectedInnerValue = ItemID.SelectedItem.ToString();

                if (KartExcData.items.TryGetValue(selectedOuterKey, out Dictionary<short, string> innerDictionary))
                {
                    GetKart.Item_Code = innerDictionary.FirstOrDefault(pair => pair.Value == selectedInnerValue).Key;
                    Console.WriteLine($"Add Item:{selectedInnerValue} ID:{GetKart.Item_Code}");
                }
            }
        }
    }
}
