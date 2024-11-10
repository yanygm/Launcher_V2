using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Xml;
using System.Xml.Linq;
using KartRider;
using KartRider.IO.Packet;

namespace ExcData
{
	public static class KartExcData
	{
		public static List<List<short>> TuneList = new List<List<short>>();
		public static List<List<short>> PlantList = new List<List<short>>();
		public static List<List<short>> LevelList = new List<List<short>>();
		public static List<List<short>> PartsList = new List<List<short>>();
		public static List<List<short>> Parts12List = new List<List<short>>();
		public static List<List<short>> Level12List = new List<List<short>>();
		public static List<List<short>> NewKart = new List<List<short>>();

		public static Dictionary<int, string> KartName = new Dictionary<int, string>();
		public static Dictionary<string, XmlDocument> KartSpec = new Dictionary<string, XmlDocument>();
		public static Dictionary<int, string> flyingName = new Dictionary<int, string>();
		public static Dictionary<string, XmlDocument> flyingSpec = new Dictionary<string, XmlDocument>();

		public static XDocument randomTrack = new XDocument();
		public static List<short> emblem = new List<short>();
		public static List<short> dictionary = new List<short>();
		public static Dictionary<uint, string> track = new Dictionary<uint, string>();

		public static List<short> character = new List<short>();
		public static List<short> color = new List<short>();
		public static List<short> kart = new List<short>();
		public static List<short> kartXV1 = new List<short>();
		public static List<short> kartV2 = new List<short>();
		public static List<short> plate = new List<short>();
		public static List<short> slotChanger = new List<short>();
		public static List<short> goggle = new List<short>();
		public static List<short> balloon = new List<short>();
		public static List<short> headBand = new List<short>();
		public static List<short> headPhone = new List<short>();
		public static List<short> ticket = new List<short>();
		public static List<short> upgradeKit = new List<short>();
		public static List<short> handGearL = new List<short>();
		public static List<short> uniform = new List<short>();
		public static List<short> decal = new List<short>();
		public static List<short> pet = new List<short>();
		public static List<short> initialCard = new List<short>();
		public static List<short> card = new List<short>();
		public static List<short> aura = new List<short>();
		public static List<short> skidMark = new List<short>();
		public static List<short> roomCard = new List<short>();
		public static List<short> ridColor = new List<short>();
		public static List<short> rpLucciBonus = new List<short>();
		public static List<short> socket = new List<short>();
		public static List<short> tune = new List<short>();
		public static List<short> resetSocket = new List<short>();
		public static List<short> tuneEnginePatch = new List<short>();
		public static List<short> tuneHandle = new List<short>();
		public static List<short> tuneWheel = new List<short>();
		public static List<short> tuneSupportKit = new List<short>();
		public static List<short> enchantProtect = new List<short>();
		public static List<short> flyingPet = new List<short>();
		public static List<short> enchantProtect2 = new List<short>();
		public static List<short> tachometer = new List<short>();
		public static List<short> partsCoating = new List<short>();
		public static List<short> partsTailLamp = new List<short>();
		public static List<short> dye = new List<short>();
		public static List<short> slotBg = new List<short>();
		public static List<short> partsPiece = new List<short>();
		public static List<short> partsEngine12 = new List<short>();
		public static List<short> partsHandle12 = new List<short>();
		public static List<short> partsWheel12 = new List<short>();
		public static List<short> partsBooster12 = new List<short>();
		public static List<short> ethisItem = new List<short>();

		public static void Tune_ExcData()
		{
			int TuneCount = TuneList.Count;
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
			{
				oPacket.WriteByte(1);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteInt(TuneCount);
				for (var i = 0; i < TuneCount; i++)
				{
					oPacket.WriteShort(3);
					oPacket.WriteShort(TuneList[i][0]);
					oPacket.WriteShort(TuneList[i][1]);
					oPacket.WriteShort(0);
					oPacket.WriteShort(0);
					oPacket.WriteShort(TuneList[i][2]);
					oPacket.WriteShort(TuneList[i][3]);
					oPacket.WriteShort(TuneList[i][4]);
					oPacket.WriteShort(0);
					oPacket.WriteShort(0);
					oPacket.WriteShort(0);
					oPacket.WriteShort(0);
				}
				oPacket.WriteBytes(new byte[29]);
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void Plant_ExcData()
		{
			int PlantCount = PlantList.Count;
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
			{
				oPacket.WriteByte(0);
				oPacket.WriteByte(1);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteInt(0);
				oPacket.WriteInt(PlantCount);
				for (var i = 0; i < PlantCount; i++)
				{
					oPacket.WriteShort(PlantList[i][0]);
					oPacket.WriteShort(PlantList[i][1]);
					oPacket.WriteInt(4);
					oPacket.WriteShort(PlantList[i][2]);
					oPacket.WriteShort(PlantList[i][3]);
					oPacket.WriteShort(PlantList[i][4]);
					oPacket.WriteShort(PlantList[i][5]);
					oPacket.WriteShort(PlantList[i][6]);
					oPacket.WriteShort(PlantList[i][7]);
					oPacket.WriteShort(PlantList[i][8]);
					oPacket.WriteShort(PlantList[i][9]);
				}
				oPacket.WriteBytes(new byte[25]);
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void Level_ExcData()
		{
			int LevelCount = LevelList.Count;
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderExcDataPacket"))
			{
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(1);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteByte(0);
				oPacket.WriteInt(0);
				oPacket.WriteInt(0);
				oPacket.WriteInt(LevelCount);
				for (var i = 0; i < LevelCount; i++)
				{
					oPacket.WriteShort(LevelList[i][0]);
					oPacket.WriteShort(LevelList[i][1]);
					oPacket.WriteShort(LevelList[i][2]);
					oPacket.WriteShort(LevelList[i][3]);
					oPacket.WriteShort(LevelList[i][4]);
					oPacket.WriteShort(LevelList[i][5]);
					oPacket.WriteShort(LevelList[i][6]);
					oPacket.WriteShort(LevelList[i][7]);
					oPacket.WriteShort(LevelList[i][8]); //코팅
				}
				oPacket.WriteBytes(new byte[21]);
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void Parts_ExcData()
		{
			int range = 100;//分批次数
			int times = kartXV1.Count / range + (kartXV1.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = kartXV1.GetRange(i * range, (i + 1) * range > kartXV1.Count ? (kartXV1.Count - i * range) : range);
				int Parts = tempList.Count;
				short sn = 1;
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
					oPacket.WriteInt(Parts);
					for (var f = 0; f < Parts; f++)
					{
						short id = tempList[f];
						var partsKartAndSN = new { Kart = id, SN = sn };
						var partsList = PartsList;
						var existingParts = partsList.FirstOrDefault(list => list[0] == partsKartAndSN.Kart && list[1] == partsKartAndSN.SN);
						if (existingParts != null)
						{
							oPacket.WriteShort(existingParts[0]);
							oPacket.WriteShort(existingParts[1]);
							oPacket.WriteHexString("00 00 FF FF 00 00");
							oPacket.WriteShort(existingParts[2]);
							oPacket.WriteByte((byte)existingParts[3]);
							oPacket.WriteShort(existingParts[4]);
							oPacket.WriteShort(existingParts[5]);
							oPacket.WriteByte((byte)existingParts[6]);
							oPacket.WriteShort(existingParts[7]);
							oPacket.WriteShort(existingParts[8]);
							oPacket.WriteByte((byte)existingParts[9]);
							oPacket.WriteShort(existingParts[10]);
							oPacket.WriteShort(existingParts[11]);
							oPacket.WriteByte((byte)existingParts[12]);
							oPacket.WriteShort(existingParts[13]);
							oPacket.WriteShort(existingParts[14]);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(existingParts[15]);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
						}
						else
						{
							oPacket.WriteShort(id);
							oPacket.WriteShort(sn);
							oPacket.WriteHexString("00 00 FF FF 00 00");
							oPacket.WriteShort(0);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
						}
					}
					oPacket.WriteBytes(new byte[17]);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}

			List<List<short>> result = new List<List<short>>();
			foreach (var innerList in PartsList)
			{
				if (innerList.Count > 1 && innerList[1] > 1)
				{
					result.Add(innerList);
				}
			}

			if (result.Count > 0)
			{
				foreach (var list in result)
				{
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
						oPacket.WriteInt(list.Count);
						for (var j = 0; j < list.Count; j++)
						{
							oPacket.WriteShort(list[0]);
							oPacket.WriteShort(list[1]);
							oPacket.WriteHexString("00 00 FF FF 00 00");
							oPacket.WriteShort(list[2]);
							oPacket.WriteByte((byte)list[3]);
							oPacket.WriteShort(list[4]);
							oPacket.WriteShort(list[5]);
							oPacket.WriteByte((byte)list[6]);
							oPacket.WriteShort(list[7]);
							oPacket.WriteShort(list[8]);
							oPacket.WriteByte((byte)list[9]);
							oPacket.WriteShort(list[10]);
							oPacket.WriteShort(list[11]);
							oPacket.WriteByte((byte)list[12]);
							oPacket.WriteShort(list[13]);
							oPacket.WriteShort(list[14]);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(list[15]);
							oPacket.WriteByte(0);
							oPacket.WriteShort(0);
						}
						oPacket.WriteBytes(new byte[17]);
						RouterListener.MySession.Client.Send(oPacket);
					}
				}
			}
		}

		public static void Parts12_ExcData()
		{
			int range = 100;//分批次数
			int times = kartV2.Count / range + (kartV2.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = kartV2.GetRange(i * range, (i + 1) * range > kartV2.Count ? (kartV2.Count - i * range) : range);
				int Parts = tempList.Count;
				short sn = 1;
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
					oPacket.WriteByte(1);
					oPacket.WriteInt(Parts);
					for (var f = 0; f < Parts; f++)
					{
						short id = tempList[f];
						var partsKartAndSN = new { Kart = id, SN = sn };
						var partsList = Parts12List;
						var existingParts = partsList.FirstOrDefault(list => list[0] == partsKartAndSN.Kart && list[1] == partsKartAndSN.SN);
						if (existingParts != null)
						{
							oPacket.WriteShort(existingParts[0]);
							oPacket.WriteShort(existingParts[1]);
							oPacket.WriteHexString("00 00 FF FF 00 00");
							oPacket.WriteShort(existingParts[2]);
							oPacket.WriteShort(existingParts[3]);
							oPacket.WriteShort(existingParts[4]);
							oPacket.WriteShort(existingParts[5]);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteInt(1);
						}
						else
						{
							oPacket.WriteShort(id);
							oPacket.WriteShort(sn);
							oPacket.WriteHexString("00 00 FF FF 00 00");
							oPacket.WriteShort(1);
							oPacket.WriteShort(1);
							oPacket.WriteShort(1);
							oPacket.WriteShort(1);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteInt(1);
						}
					}
					oPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}

			List<List<short>> result = new List<List<short>>();
			foreach (var innerList in Parts12List)
			{
				if (innerList.Count > 1 && innerList[1] > 1)
				{
					result.Add(innerList);
				}
			}

			if (result.Count > 0)
			{
				foreach (var list in result)
				{
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
						oPacket.WriteByte(1);
						oPacket.WriteInt(list.Count);
						for (var j = 0; j < list.Count; j++)
						{
							oPacket.WriteShort(list[0]);
							oPacket.WriteShort(list[1]);
							oPacket.WriteHexString("00 00 FF FF 00 00");
							oPacket.WriteShort(list[2]);
							oPacket.WriteShort(list[3]);
							oPacket.WriteShort(list[4]);
							oPacket.WriteShort(list[5]);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteShort(0);
							oPacket.WriteInt(1);
						}
						oPacket.WriteInt(0);
						RouterListener.MySession.Client.Send(oPacket);
					}
				}
			}
		}

		public static void Level12_ExcData()
		{
			int range = 100;//分批次数
			int times = Level12List.Count / range + (Level12List.Count % range > 0 ? 1 : 0);
			Console.WriteLine("Level12List Count: " + Level12List.Count);
			Console.WriteLine("times: " + times);
			for (int i = 0; i < times; i++)
			{
				var tempList = Level12List.GetRange(i * range, (i + 1) * range > Level12List.Count ? (Level12List.Count - i * range) : range);
				int Parts = tempList.Count;
				Console.WriteLine("tempList Count: " + tempList.Count);
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
					oPacket.WriteBytes(new byte[17]);
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}

		public static void AddTuneList(short id, short sn, short tune1, short tune2, short tune3)
		{
			var existingList = TuneList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingList == null)
			{
				var newList = new List<short> { id, sn, tune1, tune2, tune3 };
				TuneList.Add(newList);
				SaveTuneList(TuneList);
			}
			else
			{
				existingList[2] = tune1;
				existingList[3] = tune2;
				existingList[4] = tune3;
				SaveTuneList(TuneList);
			}
		}

		public static void DelTuneList(short id, short sn)
		{
			var itemToRemove = TuneList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (itemToRemove != null)
			{
				TuneList.Remove(itemToRemove);
				SaveTuneList(TuneList);
			}
		}

		public static void SaveTuneList(List<List<short>> List)
		{
			File.Delete(@"Profile\TuneData.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\TuneData.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("TuneData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\TuneData.xml");
				XmlNode root = xmlDoc.SelectSingleNode("TuneData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("tune1", List[i][2].ToString());
				xe1.SetAttribute("tune2", List[i][3].ToString());
				xe1.SetAttribute("tune3", List[i][4].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\TuneData.xml");
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
			File.Delete(@"Profile\PlantData.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\PlantData.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("PlantData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\PlantData.xml");
				XmlNode root = xmlDoc.SelectSingleNode("PlantData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("item1", List[i][2].ToString());
				xe1.SetAttribute("item_id1", List[i][3].ToString());
				xe1.SetAttribute("item2", List[i][4].ToString());
				xe1.SetAttribute("item_id2", List[i][5].ToString());
				xe1.SetAttribute("item3", List[i][6].ToString());
				xe1.SetAttribute("item_id3", List[i][7].ToString());
				xe1.SetAttribute("item4", List[i][8].ToString());
				xe1.SetAttribute("item_id4", List[i][9].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\PlantData.xml");
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
			File.Delete(@"Profile\LevelData.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\LevelData.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("LevelData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\LevelData.xml");
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
				xmlDoc.Save(@"Profile\LevelData.xml");
			}
		}

		public static void AddPartsList(short id, short sn, short Item_Cat_Id, short Item_Id, byte Grade, short PartsValue)
		{
			if (Item_Cat_Id == 72 || Item_Cat_Id == 73 || Item_Cat_Id == 74 || Item_Cat_Id == 75 || Item_Cat_Id == 76 || Item_Cat_Id == 77 || Item_Cat_Id == 78)
			{
				var existing12List = Parts12List.FirstOrDefault(list => list[0] == id && list[1] == sn);
				if (existing12List == null)
				{
					var newList = new List<short> { id, sn, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
					switch (Item_Cat_Id)
					{
						case 72:
							newList[2] = Item_Id;
							break;
						case 73:
							newList[3] = Item_Id;
							break;
						case 74:
							newList[4] = Item_Id;
							break;
						case 75:
							newList[5] = Item_Id;
							break;
						case 76:
							newList[6] = Item_Id;
							break;
						case 77:
							newList[7] = Item_Id;
							break;
						case 78:
							newList[8] = Item_Id;
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
							break;
						case 73:
							existing12List[3] = Item_Id;
							break;
						case 74:
							existing12List[4] = Item_Id;
							break;
						case 75:
							existing12List[5] = Item_Id;
							break;
						case 76:
							existing12List[6] = Item_Id;
							break;
						case 77:
							existing12List[7] = Item_Id;
							break;
						case 78:
							existing12List[8] = Item_Id;
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
			File.Delete(@"Profile\PartsData.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\PartsData.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("PartsData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\PartsData.xml");
				XmlNode root = xmlDoc.SelectSingleNode("PartsData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("Item_Id1", List[i][2].ToString());
				xe1.SetAttribute("Grade1", List[i][3].ToString());
				xe1.SetAttribute("PartsValue1", List[i][4].ToString());
				xe1.SetAttribute("Item_Id2", List[i][5].ToString());
				xe1.SetAttribute("Grade2", List[i][6].ToString());
				xe1.SetAttribute("PartsValue2", List[i][7].ToString());
				xe1.SetAttribute("Item_Id3", List[i][8].ToString());
				xe1.SetAttribute("Grade3", List[i][9].ToString());
				xe1.SetAttribute("PartsValue3", List[i][10].ToString());
				xe1.SetAttribute("Item_Id4", List[i][11].ToString());
				xe1.SetAttribute("Grade4", List[i][12].ToString());
				xe1.SetAttribute("PartsValue4", List[i][13].ToString());
				xe1.SetAttribute("partsCoating", List[i][14].ToString());
				xe1.SetAttribute("partsTailLamp", List[i][15].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\PartsData.xml");
			}
		}

		public static void SaveParts12List(List<List<short>> List)
		{
			File.Delete(@"Profile\Parts12Data.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\Parts12Data.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("PartsData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\Parts12Data.xml");
				XmlNode root = xmlDoc.SelectSingleNode("PartsData");
				XmlElement xe1 = xmlDoc.CreateElement("Kart");
				xe1.SetAttribute("id", List[i][0].ToString());
				xe1.SetAttribute("sn", List[i][1].ToString());
				xe1.SetAttribute("Engine", List[i][2].ToString());
				xe1.SetAttribute("Handle", List[i][3].ToString());
				xe1.SetAttribute("Wheel", List[i][4].ToString());
				xe1.SetAttribute("Booster", List[i][5].ToString());
				xe1.SetAttribute("Coating", List[i][6].ToString());
				xe1.SetAttribute("TailLamp", List[i][7].ToString());
				xe1.SetAttribute("BoosterWave", List[i][8].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\Parts12Data.xml");
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
			File.Delete(@"Profile\Level12Data.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\Level12Data.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("LevelData");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < List.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\Level12Data.xml");
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
				xmlDoc.Save(@"Profile\Level12Data.xml");
			}
		}
	}
}
