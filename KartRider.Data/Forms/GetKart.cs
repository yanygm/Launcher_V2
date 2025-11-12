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

                    var newList = new List<NewKart>();

                    if (File.Exists(FileName.NewKart_LoadFile))
                    {
                        newList = JsonHelper.DeserializeNoBom<List<NewKart>>(FileName.NewKart_LoadFile);
                    }

                    // 查找合适的KartSN
                    short currentSN = KartSN;
                    // 循环检查当前SN是否已存在相同KartID的记录
                    while (newList.Any(kart => kart.KartID == GetKart.Item_Code && kart.KartSN == currentSN))
                    {
                        currentSN++; // 存在则SN+1继续检查
                    }

                    using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
                    {
                        outPacket.WriteByte(1);
                        outPacket.WriteInt(1);
                        outPacket.WriteShort(GetKart.Item_Type);
                        outPacket.WriteShort(GetKart.Item_Code);
                        outPacket.WriteShort(currentSN);
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

        public static void Save_NewKartList(List<NewKart> NewKart)
        {
            File.WriteAllText(FileName.NewKart_LoadFile, JsonHelper.Serialize(NewKart));
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

    public class NewKart
    {
        public short KartID { get; set; } = 0;
        public short KartSN { get; set; } = 0;
    }
}
