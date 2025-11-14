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

namespace KartRider
{
    public partial class GetKart : Form
    {
        public static ushort Item_Type = 0;
        public static ushort Item_Code = 0;

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
            ushort tempValue;
            if (ushort.TryParse(ItemID.Text, out tempValue))
            {
                GetKart.Item_Code = tempValue;
                Console.WriteLine($"Add Item:{ItemID.Text} ID:{ItemType.Text}");
            }
            (new Thread(() =>
            {
                if (GetKart.Item_Type == 3)
                {
                    ushort KartSN = 2;

                    var newList = new List<NewKart>();

                    if (File.Exists(FileName.NewKart_LoadFile))
                    {
                        newList = JsonHelper.DeserializeNoBom<List<NewKart>>(FileName.NewKart_LoadFile);
                    }

                    // 查找合适的KartSN
                    ushort currentSN = KartSN;
                    // 循环检查当前SN是否已存在相同KartID的记录
                    while (newList.Any(kart => kart.KartID == GetKart.Item_Code && kart.KartSN == currentSN))
                    {
                        currentSN++; // 存在则SN+1继续检查
                    }

                    using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                    {
                        outPacket.WriteByte(1);
                        outPacket.WriteInt(1);
                        outPacket.WriteUShort(GetKart.Item_Type);
                        outPacket.WriteUShort(GetKart.Item_Code);
                        outPacket.WriteUShort(currentSN);
                        outPacket.WriteShort(1);//수량
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(-1);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(0);
                        RouterListener.MySession.Client.Send(outPacket);
                    }

                    // 添加新记录
                    newList.Add(new NewKart
                    {
                        KartID = GetKart.Item_Code,
                        KartSN = currentSN
                    });

                    Save_NewKartList(newList);
                }
                else
                {
                    var newList = new List<NewItem>();

                    if (File.Exists(FileName.NewItem_LoadFile))
                    {
                        newList = JsonHelper.DeserializeNoBom<List<NewItem>>(FileName.NewItem_LoadFile);
                    }

                    // 检查是否已存在相同的记录
                    var existingItem = newList.FirstOrDefault(item => item.ItemType == GetKart.Item_Type && item.ItemID == GetKart.Item_Code);
                    if (existingItem != null)
                    {
                        // 增加数量
                        existingItem.Count++;
                        Save_NewItemList(newList);
                    }
                    else
                    {
                        using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                        {
                            outPacket.WriteByte(1);
                            outPacket.WriteInt(1);
                            outPacket.WriteUShort(GetKart.Item_Type);
                            outPacket.WriteUShort(GetKart.Item_Code);
                            outPacket.WriteShort(0);
                            outPacket.WriteUShort(1);//수량
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(-1);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            outPacket.WriteShort(0);
                            RouterListener.MySession.Client.Send(outPacket);
                        }
                        // 添加新记录
                        newList.Add(new NewItem
                        {
                            ItemType = GetKart.Item_Type,
                            ItemID = GetKart.Item_Code,
                            Count = 1
                        });
                        Save_NewItemList(newList);
                    }
                }
            })).Start();
        }

        public static void Save_NewKartList(List<NewKart> NewKart)
        {
            File.WriteAllText(FileName.NewKart_LoadFile, JsonHelper.Serialize(NewKart));
        }

        public static void Save_NewItemList(List<NewItem> NewItem)
        {
            File.WriteAllText(FileName.NewItem_LoadFile, JsonHelper.Serialize(NewItem));
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemType.SelectedItem != null)
            {
                GetKart.Item_Type = (ushort)ItemType.SelectedItem;
                ItemID.Items.Clear();

                if (NewRider.items.TryGetValue(GetKart.Item_Type, out Dictionary<ushort, string> innerDictionary))
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
                ushort selectedOuterKey = (ushort)ItemType.SelectedItem;
                string selectedInnerValue = ItemID.SelectedItem.ToString();

                if (NewRider.items.TryGetValue(selectedOuterKey, out Dictionary<ushort, string> innerDictionary))
                {
                    GetKart.Item_Code = innerDictionary.FirstOrDefault(pair => pair.Value == selectedInnerValue).Key;
                    Console.WriteLine($"Add Item:{selectedInnerValue} ID:{GetKart.Item_Code}");
                }
            }
        }
    }

    public class NewKart
    {
        public ushort KartID { get; set; } = 0;
        public ushort KartSN { get; set; } = 0;
    }

    public class NewItem
    {
        public ushort ItemType { get; set; } = 0;
        public ushort ItemID { get; set; } = 0;
        public ushort Count { get; set; } = 0;
    }
}
