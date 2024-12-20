﻿using System;
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
		public static short Item_Type = 0;
		public static short Item_Code = 0;

		public GetKart()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			GetKart.Item_Type = short.Parse(this.tx_ItemType.Text);
			GetKart.Item_Code = short.Parse(this.tx_ItemCode.Text);
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
							outPacket.WriteShort(1);//수량
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
								outPacket.WriteShort(1);//수량
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
								outPacket.WriteShort(1);//수량
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
						outPacket.WriteShort(1);//수량
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

		private void FormItem_Load(object sender, EventArgs e)
		{
			this.tx_ItemType.Text = string.Concat(GetKart.Item_Type);
			this.tx_ItemCode.Text = string.Concat(GetKart.Item_Code);
		}

		private void tx_ItemType_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))          
			{
				e.Handled = true;
			}
		}

		private void tx_ItemCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))         
			{
				e.Handled = true;
			}
		}

		public static void Save_NewKartList(List<List<short>> NewKart)
		{
			File.Delete(@"Profile\NewKart.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\NewKart.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("NewKart");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < NewKart.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\NewKart.xml");
				XmlNode root = xmlDoc.SelectSingleNode("NewKart");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", NewKart[i][0].ToString());
				xe1.SetAttribute("sn", NewKart[i][1].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\NewKart.xml");
			}
		}
	}
}
