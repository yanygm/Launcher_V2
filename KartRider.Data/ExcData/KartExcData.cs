using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using KartRider;
using KartRider.IO.Packet;

namespace ExcData
{
	public static class KartExcData
	{
		public static string TuneData_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\TuneData.xml";
		public static string PlantData_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\PlantData.xml";
		public static string LevelData_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\LevelData.xml";
		public static string PartsData_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\PartsData.xml";
		public static string Parts12Data_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\Parts12Data.xml";
		public static string Level12Data_LoadFile = AppDomain.CurrentDomain.BaseDirectory + @"Profile\Level12Data.xml";
		public static List<List<short>> TuneList = new List<List<short>>();
		public static List<List<short>> PlantList = new List<List<short>>();
		public static List<List<short>> LevelList = new List<List<short>>();
		public static List<List<short>> PartsList = new List<List<short>>();
		public static List<List<short>> Parts12List = new List<List<short>>();
		public static List<List<short>> Level12List = new List<List<short>>();
		public static List<List<short>> NewKart = new List<List<short>>();
		public static List<List<short>> Dictionary = new List<List<short>>();

		public static Dictionary<int, string> KartName = new Dictionary<int, string>();
		public static Dictionary<string, XmlDocument> KartSpec = new Dictionary<string, XmlDocument>();
		public static Dictionary<int, string> flyingName = new Dictionary<int, string>();
		public static Dictionary<string, XmlDocument> flyingSpec = new Dictionary<string, XmlDocument>();

		public static XDocument randomTrack = new XDocument();
		public static List<short> emblem = new List<short>();
		public static List<short> dictionary = new List<short>();
		public static Dictionary<uint, string> track = new Dictionary<uint, string>();
		public static List<int> scenario = new List<int>();
		public static List<int> quest = new List<int>();
		public static int seasonId = 0;
		public static List<short> itemProb_indi = new List<short>();
		public static List<short> itemProb_team = new List<short>();

		public static Dictionary<short, Dictionary<short, string>> items = new Dictionary<short, Dictionary<short, string>>();

		public static void Tune_ExcData()
		{
			int range = 26;//分批次数
			int times = TuneList.Count / range + (TuneList.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = TuneList.GetRange(i * range, (i + 1) * range > TuneList.Count ? (TuneList.Count - i * range) : range);
				int TuneCount = tempList.Count;
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
				{
					if (i == 0)
					{
						oPacket.WriteByte(1);
					}
					else
					{
						oPacket.WriteByte(0);
					}
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(TuneCount);
					for (var f = 0; f < TuneCount; f++)
					{
						oPacket.WriteShort(3);
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteShort(tempList[f][3]);
						oPacket.WriteShort(tempList[f][4]);
						oPacket.WriteShort(tempList[f][5]);
						oPacket.WriteShort(tempList[f][6]);
						oPacket.WriteShort(tempList[f][7]);
						oPacket.WriteShort(tempList[f][8]);
					}
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void Plant_ExcData()
		{
			int range = 26;//分批次数
			int times = PlantList.Count / range + (PlantList.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = PlantList.GetRange(i * range, (i + 1) * range > PlantList.Count ? (PlantList.Count - i * range) : range);
				int PlantCount = tempList.Count;
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
				{
					oPacket.WriteByte(0);
					if (i == 0)
					{
						oPacket.WriteByte(1);
					}
					else
					{
						oPacket.WriteByte(0);
					}
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(PlantCount);
					for (var f = 0; f < PlantCount; f++)
					{
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteInt(4);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteShort(tempList[f][3]);
						oPacket.WriteShort(tempList[f][4]);
						oPacket.WriteShort(tempList[f][5]);
						oPacket.WriteShort(tempList[f][6]);
						oPacket.WriteShort(tempList[f][7]);
						oPacket.WriteShort(tempList[f][8]);
						oPacket.WriteShort(tempList[f][9]);
					}
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void Level_ExcData()
		{
			int range = 26;//分批次数
			int times = LevelList.Count / range + (LevelList.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = LevelList.GetRange(i * range, (i + 1) * range > LevelList.Count ? (LevelList.Count - i * range) : range);
				int LevelCount = tempList.Count;
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
				{
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					if (i == 0)
					{
						oPacket.WriteByte(1);
					}
					else
					{
						oPacket.WriteByte(0);
					}
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(LevelCount);
					for (var f = 0; f < LevelCount; f++)
					{
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteShort(tempList[f][3]);
						oPacket.WriteShort(tempList[f][4]);
						oPacket.WriteShort(tempList[f][5]);
						oPacket.WriteShort(tempList[f][6]);
						oPacket.WriteShort(tempList[f][7]);
						oPacket.WriteShort(tempList[f][8]); //코팅
					}
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void Parts_ExcData()
		{
			int range = 26;//分批次数
			int times = PartsList.Count / range + (PartsList.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = PartsList.GetRange(i * range, (i + 1) * range > PartsList.Count ? (PartsList.Count - i * range) : range);
				int parts = tempList.Count;
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
				{
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					if (i == 0)
					{
						oPacket.WriteByte(1);
					}
					else
					{
						oPacket.WriteByte(0);
					}
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(parts);
					for (var f = 0; f < parts; f++)
					{
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(-1);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteByte((byte)tempList[f][3]);
						oPacket.WriteShort(tempList[f][4]);
						oPacket.WriteShort(tempList[f][5]);
						oPacket.WriteByte((byte)tempList[f][6]);
						oPacket.WriteShort(tempList[f][7]);
						oPacket.WriteShort(tempList[f][8]);
						oPacket.WriteByte((byte)tempList[f][9]);
						oPacket.WriteShort(tempList[f][10]);
						oPacket.WriteShort(tempList[f][11]);
						oPacket.WriteByte((byte)tempList[f][12]);
						oPacket.WriteShort(tempList[f][13]);
						oPacket.WriteShort(tempList[f][14]);
						oPacket.WriteByte(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][15]);
						oPacket.WriteByte(0);
						oPacket.WriteShort(0);
					}
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void Level12_ExcData()
		{
			int range = 26;//分批次数
			int times = Level12List.Count / range + (Level12List.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = Level12List.GetRange(i * range, (i + 1) * range > Level12List.Count ? (Level12List.Count - i * range) : range);
				int Parts = tempList.Count;
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
				{
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					if (i == 0)
					{
						oPacket.WriteByte(1);
					}
					else
					{
						oPacket.WriteByte(0);
					}
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(Parts);
					for (var f = 0; f < Parts; f++)
					{
						oPacket.WriteShort(3);
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteShort(tempList[f][9]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(4);
						oPacket.WriteShort(4);
						oPacket.WriteShort(tempList[f][3]);
						oPacket.WriteShort(tempList[f][5]);
						oPacket.WriteShort(tempList[f][7]);
						oPacket.WriteShort(tempList[f][4]);
						oPacket.WriteShort(tempList[f][6]);
						oPacket.WriteShort(tempList[f][8]);
					}
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void Parts12_ExcData()
		{
			int range = 26;//分批次数
			int times = Parts12List.Count / range + (Parts12List.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = Parts12List.GetRange(i * range, (i + 1) * range > Parts12List.Count ? (Parts12List.Count - i * range) : range);
				int parts12 = tempList.Count;
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
				{
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					oPacket.WriteInt(0);
					if (i == 0)
					{
						oPacket.WriteByte(1);
					}
					else
					{
						oPacket.WriteByte(0);
					}
					oPacket.WriteInt(parts12);
					for (var f = 0; f < parts12; f++)
					{
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(-1);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteShort(tempList[f][3]);
						oPacket.WriteShort(tempList[f][5]);
						oPacket.WriteShort(tempList[f][6]);
						oPacket.WriteShort(tempList[f][8]);
						oPacket.WriteShort(tempList[f][9]);
						oPacket.WriteShort(tempList[f][11]);
						oPacket.WriteShort(tempList[f][12]);
						oPacket.WriteShort(tempList[f][14]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][15]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][16]);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(0);
						oPacket.WriteShort(tempList[f][17]);
						oPacket.WriteShort(0);
					}
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void AddTuneList(short id, short sn, short tune1, short tune2, short tune3, short slot1, short count1, short slot2, short count2)
		{
			var existingList = TuneList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingList == null)
			{
				var newList = new List<short> { id, sn, tune1, tune2, tune3, slot1, count1, slot2, count2 };
				TuneList.Add(newList);
				SaveTuneList(TuneList);
			}
			else
			{
				existingList[2] = tune1;
				existingList[3] = tune2;
				existingList[4] = tune3;
				existingList[5] = slot1;
				existingList[6] = count1;
				existingList[7] = slot2;
				existingList[8] = count2;
				SaveTuneList(TuneList);
			}
		}

		public static void SaveTuneList(List<List<short>> List)
		{
			File.Delete(TuneData_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(TuneData_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("TuneData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(TuneData_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("TuneData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("tune1", List[i][2].ToString());
				xe1.SetAttribute("tune2", List[i][3].ToString());
				xe1.SetAttribute("tune3", List[i][4].ToString());
				xe1.SetAttribute("slot1", List[i][5].ToString());
				xe1.SetAttribute("count1", List[i][6].ToString());
				xe1.SetAttribute("slot2", List[i][7].ToString());
				xe1.SetAttribute("count2", List[i][8].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(TuneData_LoadFile);
			}
		}

		public static void AddPlantList(short id, short sn, short item, short item_id)
		{
			var existingList = PlantList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingList == null)
			{
				var newList = new List<short> { id, sn, 0, 0, 0, 0, 0, 0, 0, 0 };
				switch (item)
				{
					case 43:
						newList[2] = item;
						newList[3] = item_id;
						break;
					case 44:
						newList[4] = item;
						newList[5] = item_id;
						break;
					case 45:
						newList[6] = item;
						newList[7] = item_id;
						break;
					case 46:
						newList[8] = item;
						newList[9] = item_id;
						break;
				}
				PlantList.Add(newList);
				SavePlantList(PlantList);
			}
			else
			{
				switch (item)
				{
					case 43:
						existingList[2] = item;
						existingList[3] = item_id;
						break;
					case 44:
						existingList[4] = item;
						existingList[5] = item_id;
						break;
					case 45:
						existingList[6] = item;
						existingList[7] = item_id;
						break;
					case 46:
						existingList[8] = item;
						existingList[9] = item_id;
						break;
				}
				SavePlantList(PlantList);
			}
		}

		public static void SavePlantList(List<List<short>> List)
		{
			File.Delete(PlantData_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(PlantData_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("PlantData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(PlantData_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("PlantData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("Engine", List[i][2].ToString());
				xe1.SetAttribute("Engine_id", List[i][3].ToString());
				xe1.SetAttribute("Handle", List[i][4].ToString());
				xe1.SetAttribute("Handle_id", List[i][5].ToString());
				xe1.SetAttribute("Wheel", List[i][6].ToString());
				xe1.SetAttribute("Wheel_id", List[i][7].ToString());
				xe1.SetAttribute("Kit", List[i][8].ToString());
				xe1.SetAttribute("Kit_id", List[i][9].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(PlantData_LoadFile);
			}
		}

		public static void AddLevelList(short id, short sn, short level, short point, short v1, short v2, short v3, short v4, short Effect)
		{
			var existingList = LevelList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingList == null)
			{
				var newList = new List<short> { id, sn, level, point, v1, v2, v3, v4, Effect };
				LevelList.Add(newList);
				SaveLevelList(LevelList);
			}
			else
			{
				existingList[3] = point;
				existingList[4] = v1;
				existingList[5] = v2;
				existingList[6] = v3;
				existingList[7] = v4;
				existingList[8] = Effect;
				SaveLevelList(LevelList);
			}
		}

		public static void SaveLevelList(List<List<short>> List)
		{
			File.Delete(LevelData_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(LevelData_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("LevelData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(LevelData_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("LevelData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("level", List[i][2].ToString());
				xe1.SetAttribute("point", List[i][3].ToString());
				xe1.SetAttribute("v1", List[i][4].ToString());
				xe1.SetAttribute("v2", List[i][5].ToString());
				xe1.SetAttribute("v3", List[i][6].ToString());
				xe1.SetAttribute("v4", List[i][7].ToString());
				xe1.SetAttribute("Effect", List[i][8].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(LevelData_LoadFile);
			}
		}

		public static void AddPartsList(short id, short sn, short Item_Cat_Id, short Item_Id, byte Grade, short PartsValue)
		{
			if (Item_Cat_Id == 72 || Item_Cat_Id == 73 || Item_Cat_Id == 74 || Item_Cat_Id == 75 || Item_Cat_Id == 76 || Item_Cat_Id == 77 || Item_Cat_Id == 78)
			{
				var existing12List = Parts12List.FirstOrDefault(list => list[0] == id && list[1] == sn);
				if (existing12List == null)
				{
					var newList = new List<short> { id, sn, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
					switch (Item_Cat_Id)
					{
						case 72:
							newList[2] = Item_Id;
							newList[3] = 1;
							newList[4] = PartsValue;
							break;
						case 73:
							newList[5] = Item_Id;
							newList[6] = 1;
							newList[7] = PartsValue;
							break;
						case 74:
							newList[8] = Item_Id;
							newList[9] = 1;
							newList[10] = PartsValue;
							break;
						case 75:
							newList[11] = Item_Id;
							newList[12] = 1;
							newList[13] = PartsValue;
							break;
						case 76:
							newList[14] = Item_Id;
							break;
						case 77:
							newList[15] = Item_Id;
							break;
						case 78:
							newList[16] = Item_Id;
							break;
					}
					Parts12List.Add(newList);
					SaveParts12List(Parts12List);
				}
				else
				{
					switch (Item_Cat_Id)
					{
						case 72:
							existing12List[2] = Item_Id;
							if (existing12List[3] == 0)
							{
								existing12List[3] = 1;
							}
							existing12List[4] = PartsValue;
							break;
						case 73:
							existing12List[5] = Item_Id;
							if (existing12List[6] == 0)
							{
								existing12List[6] = 1;
							}
							existing12List[7] = PartsValue;
							break;
						case 74:
							existing12List[8] = Item_Id;
							if (existing12List[9] == 0)
							{
								existing12List[9] = 1;
							}
							existing12List[10] = PartsValue;
							break;
						case 75:
							existing12List[11] = Item_Id;
							if (existing12List[12] == 0)
							{
								existing12List[12] = 1;
							}
							existing12List[13] = PartsValue;
							break;
						case 76:
							existing12List[14] = Item_Id;
							break;
						case 77:
							existing12List[15] = Item_Id;
							break;
						case 78:
							existing12List[16] = Item_Id;
							break;
					}
					SaveParts12List(Parts12List);
				}
				return;
			}
			var existingList = PartsList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingList == null)
			{
				var newList = new List<short> { id, sn, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				switch (Item_Cat_Id)
				{
					case 63:
						newList[2] = Item_Id;
						newList[3] = Grade;
						newList[4] = PartsValue;
						break;
					case 64:
						newList[5] = Item_Id;
						newList[6] = Grade;
						newList[7] = PartsValue;
						break;
					case 65:
						newList[8] = Item_Id;
						newList[9] = Grade;
						newList[10] = PartsValue;
						break;
					case 66:
						newList[11] = Item_Id;
						newList[12] = Grade;
						newList[13] = PartsValue;
						break;
					case 68:
						newList[14] = Item_Id;
						break;
					case 69:
						newList[15] = Item_Id;
						break;
				}
				PartsList.Add(newList);
				SavePartsList(PartsList);
			}
			else
			{
				switch (Item_Cat_Id)
				{
					case 63:
						existingList[2] = Item_Id;
						existingList[3] = Grade;
						existingList[4] = PartsValue;
						break;
					case 64:
						existingList[5] = Item_Id;
						existingList[6] = Grade;
						existingList[7] = PartsValue;
						break;
					case 65:
						existingList[8] = Item_Id;
						existingList[9] = Grade;
						existingList[10] = PartsValue;
						break;
					case 66:
						existingList[11] = Item_Id;
						existingList[12] = Grade;
						existingList[13] = PartsValue;
						break;
					case 68:
						existingList[14] = Item_Id;
						break;
					case 69:
						existingList[15] = Item_Id;
						break;
				}
				SavePartsList(PartsList);
			}
		}

		public static void SavePartsList(List<List<short>> List)
		{
			File.Delete(PartsData_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(PartsData_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("PartsData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(PartsData_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("PartsData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("Engine", List[i][2].ToString());
				xe1.SetAttribute("EngineGrade", List[i][3].ToString());
				xe1.SetAttribute("EngineValue", List[i][4].ToString());
				xe1.SetAttribute("Handle", List[i][5].ToString());
				xe1.SetAttribute("HandleGrade", List[i][6].ToString());
				xe1.SetAttribute("HandleValue", List[i][7].ToString());
				xe1.SetAttribute("Wheel", List[i][8].ToString());
				xe1.SetAttribute("WheelGrade", List[i][9].ToString());
				xe1.SetAttribute("WheelValue", List[i][10].ToString());
				xe1.SetAttribute("Booster", List[i][11].ToString());
				xe1.SetAttribute("BoosterGrade", List[i][12].ToString());
				xe1.SetAttribute("BoosterValue", List[i][13].ToString());
				xe1.SetAttribute("Coating", List[i][14].ToString());
				xe1.SetAttribute("TailLamp", List[i][15].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(PartsData_LoadFile);
			}
		}

		public static void SaveParts12List(List<List<short>> List)
		{
			File.Delete(Parts12Data_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(Parts12Data_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("PartsData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(Parts12Data_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("PartsData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("Engine", List[i][2].ToString());
				xe1.SetAttribute("defaultEngine", List[i][3].ToString());
				xe1.SetAttribute("EngineValue", List[i][4].ToString());
				xe1.SetAttribute("Handle", List[i][5].ToString());
				xe1.SetAttribute("defaultHandle", List[i][6].ToString());
				xe1.SetAttribute("HandleValue", List[i][7].ToString());
				xe1.SetAttribute("Wheel", List[i][8].ToString());
				xe1.SetAttribute("defaultWheel", List[i][9].ToString());
				xe1.SetAttribute("WheelValue", List[i][10].ToString());
				xe1.SetAttribute("Booster", List[i][11].ToString());
				xe1.SetAttribute("defaultBooster", List[i][12].ToString());
				xe1.SetAttribute("BoosterValue", List[i][13].ToString());
				xe1.SetAttribute("Coating", List[i][14].ToString());
				xe1.SetAttribute("TailLamp", List[i][15].ToString());
				xe1.SetAttribute("BoosterEffect", List[i][16].ToString());
				xe1.SetAttribute("ExceedType", List[i][17].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(Parts12Data_LoadFile);
			}
		}

		public static void AddLevel12List(short id, short sn, short level, short field, short skill, short skilllevel, short point)
		{
			var existingList = Level12List.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingList == null)
			{
				var newList = new List<short> { id, sn, level, 0, 0, 0, 0, 0, 0, point };
				switch (field)
				{
					case 1:
						newList[3] = skill;
						newList[4] = skilllevel;
						break;
					case 2:
						newList[5] = skill;
						newList[6] = skilllevel;
						break;
					case 3:
						newList[7] = skill;
						newList[8] = skilllevel;
						break;
				}
				Level12List.Add(newList);
				SaveTuning12List(Level12List);
			}
			else
			{
				if (level != -1)
					existingList[2] = level;
				if (point != -1)
					existingList[9] = point;
				switch (field)
				{
					case 1:
						if (skill != -1)
							existingList[3] = skill;
						if (skilllevel != -1)
							existingList[4] = skilllevel;
						break;
					case 2:
						if (skill != -1)
							existingList[5] = skill;
						if (skilllevel != -1)
							existingList[6] = skilllevel;
						break;
					case 3:
						if (skill != -1)
							existingList[7] = skill;
						if (skilllevel != -1)
							existingList[8] = skilllevel;
						break;
				}
				SaveTuning12List(Level12List);
			}
		}

		public static void SaveTuning12List(List<List<short>> List)
		{
			File.Delete(Level12Data_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(Level12Data_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("LevelData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(Level12Data_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("LevelData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("Level", List[i][2].ToString());
				xe1.SetAttribute("Skill1", List[i][3].ToString());
				xe1.SetAttribute("SkillLevel1", List[i][4].ToString());
				xe1.SetAttribute("Skill2", List[i][5].ToString());
				xe1.SetAttribute("SkillLevel2", List[i][6].ToString());
				xe1.SetAttribute("Skill3", List[i][7].ToString());
				xe1.SetAttribute("SkillLevel3", List[i][8].ToString());
				xe1.SetAttribute("Point", List[i][9].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(Level12Data_LoadFile);
			}
		}
	}
}
