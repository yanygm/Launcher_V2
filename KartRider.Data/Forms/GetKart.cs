using ExcData;
using KartRider.IO.Packet;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace KartRider
{
    public partial class GetKart : Form
    {
        public static short Item_Type = 0;
        public static short Item_Code = 0;

        public GetKart()
        {
            InitializeComponent();
            foreach (var outerKey in NewRider.items.Keys)
            {
                ItemType.Items.Add(outerKey);
            }
            ItemType.SelectedIndex = 2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            short tempValue;
            if (short.TryParse(ItemID.Text, out tempValue))
            {
                GetKart.Item_Code = tempValue;
                Console.WriteLine($"Add Item:{ItemID.Text} ID:{ItemType.Text}");
            }
            (new Thread(() =>
            {
                if (GetKart.Item_Type == 3)
                {
                    short KartSN = 2;
                    if (NewRider.NewKart == null)
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteInt(1);
                            outPacket.WriteShort(GetKart.Item_Type);
                            outPacket.WriteShort(GetKart.Item_Code);
                            outPacket.WriteShort(KartSN);
                            outPacket.WriteShort(1);//수량
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(-1);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            RouterListener.MySession.Client.Send(outPacket);
                        }
                        var newList = new List<short> { GetKart.Item_Code, KartSN };
                        NewRider.NewKart.Add(newList);
                        Save_NewKartList(NewRider.NewKart);
                    }
                    else
                    {
                        var existingItems = NewRider.NewKart.Where(list => list[0] == GetKart.Item_Code).ToList();
                        if (existingItems == null)
                        {
                            using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                            {
                                outPacket.WriteByte(1);
                                outPacket.WriteInt(1);
                                outPacket.WriteShort(GetKart.Item_Type);
                                outPacket.WriteShort(GetKart.Item_Code);
                                outPacket.WriteShort(KartSN);
                                outPacket.WriteShort(1);//수량
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(-1);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                RouterListener.MySession.Client.Send(outPacket);
                            }
                            var newList = new List<short> { GetKart.Item_Code, KartSN };
                            NewRider.NewKart.Add(newList);
                            Save_NewKartList(NewRider.NewKart);
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
                                outPacket.WriteShort(1);//수량
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(-1);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                outPacket.WriteShort(0);
                                RouterListener.MySession.Client.Send(outPacket);
                            }
                            var newList = new List<short> { GetKart.Item_Code, KartSN };
                            NewRider.NewKart.Add(newList);
                            Save_NewKartList(NewRider.NewKart);
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
                        outPacket.WriteShort(0);
                        outPacket.WriteUShort(1);//수량
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
            File.Delete(FileName.NewKart_LoadFile);
            XmlTextWriter writer = new XmlTextWriter(FileName.NewKart_LoadFile, System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("NewKart");
            writer.WriteEndElement();
            writer.Close();
            for (var i = 0; i < NewKart.Count; i++)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(FileName.NewKart_LoadFile);
                XmlNode root = xmlDoc.SelectSingleNode("NewKart");
                XmlElement xe1 = xmlDoc.CreateElement("Kart");
                xe1.SetAttribute("id", NewKart[i][0].ToString());
                xe1.SetAttribute("sn", NewKart[i][1].ToString());
                root.AppendChild(xe1);
                xmlDoc.Save(FileName.NewKart_LoadFile);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemType.SelectedItem != null)
            {
                GetKart.Item_Type = (short)ItemType.SelectedItem;
                ItemID.Items.Clear();

                if (NewRider.items.TryGetValue(GetKart.Item_Type, out Dictionary<short, string> innerDictionary))
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

                if (NewRider.items.TryGetValue(selectedOuterKey, out Dictionary<short, string> innerDictionary))
                {
                    GetKart.Item_Code = innerDictionary.FirstOrDefault(pair => pair.Value == selectedInnerValue).Key;
                    Console.WriteLine($"Add Item:{selectedInnerValue} ID:{GetKart.Item_Code}");
                }
            }
        }
    }
}
