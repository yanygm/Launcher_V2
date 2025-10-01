using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;
using KartRider;
using System.Xml;
using System.Linq;
using RHOParser;
using Profile;

namespace RiderData
{
	public static class FavoriteItem
	{
		public static List<List<short>> FavoriteItemList = new List<List<short>>();
		public static List<List<string>> FavoriteTrackList = new List<List<string>>();
		public static List<string> MissionList = new List<string>();
		public static List<string> Competitive = new List<string>();
		public static Dictionary<uint, TrackData> TrackDictionary = new Dictionary<uint, TrackData>();

		public static void Favorite_Item()
		{
			using (OutPacket outPacket = new OutPacket("PrFavoriteItemGet"))
			{
				if (File.Exists(FileName.Favorite_LoadFile))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(FileName.Favorite_LoadFile);
					FavoriteItemList = new List<List<short>>();
					XmlNodeList lis = doc.GetElementsByTagName("Title");
					if (lis.Count > 0)
					{
						outPacket.WriteInt(lis.Count);
						foreach (XmlNode xn in lis)
						{
							XmlElement xe = (XmlElement)xn;
							short item = short.Parse(xe.GetAttribute("item"));
							short id = short.Parse(xe.GetAttribute("id"));
							short sn = short.Parse(xe.GetAttribute("sn"));
							outPacket.WriteShort(item);
							outPacket.WriteShort(id);
							outPacket.WriteShort(sn);
							outPacket.WriteByte(0);
							List<short> AddList = new List<short>();
							AddList.Add(item);
							AddList.Add(id);
							AddList.Add(sn);
							FavoriteItemList.Add(AddList);
						}
					}
					else
					{
						outPacket.WriteInt(0);
					}
				}
				else
				{
					outPacket.WriteInt(0);
				}
				RouterListener.MySession.Client.Send(outPacket);
			}
		}

		public static void Favorite_Item_Add(short item, short id, short sn)
		{
			var existingItem = FavoriteItemList.FirstOrDefault(list => list[0] == item && list[1] == id && list[2] == sn);
			if (existingItem == null)
			{
				var newList = new List<short> { item, id, sn };
				FavoriteItemList.Add(newList);
				Save_ItemList(FavoriteItemList);
			}
		}

		public static void Favorite_Item_Del(short item, short id, short sn)
		{
			var itemToRemove = FavoriteItemList.FirstOrDefault(list => list[0] == item && list[1] == id && list[2] == sn);
			if (itemToRemove != null)
			{
				FavoriteItemList.Remove(itemToRemove);
				Save_ItemList(FavoriteItemList);
			}
		}

		public static void Save_ItemList(List<List<short>> SaveFavorite)
		{
			File.Delete(FileName.Favorite_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(FileName.Favorite_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("Favorite");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < SaveFavorite.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(FileName.Favorite_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("Favorite");
				XmlElement xe1 = xmlDoc.CreateElement("Title");
				xe1.SetAttribute("item", SaveFavorite[i][0].ToString());
				xe1.SetAttribute("id", SaveFavorite[i][1].ToString());
				xe1.SetAttribute("sn", SaveFavorite[i][2].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(FileName.Favorite_LoadFile);
			}
		}

		public static void Favorite_Track()
		{
			if (File.Exists(FileName.FavoriteTrack_LoadFile))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(FileName.FavoriteTrack_LoadFile);
				if (!(doc.DocumentElement == null))
				{
					XmlNode rootNode = doc.DocumentElement;
					HashSet<string> uniqueNames = new HashSet<string>();
					FavoriteTrackList = new List<List<string>>();
					foreach (XmlNode node in rootNode.ChildNodes)
					{
						XmlElement xe = node as XmlElement;
						if (xe != null)
						{
							List<string> addList = new List<string>();
							addList.Add(node.Name);
							addList.Add(xe.GetAttribute("track"));
							FavoriteTrackList.Add(addList);
							if (!uniqueNames.Contains(node.Name))
							{
								uniqueNames.Add(node.Name);
							}
						}
					}
					List<string> Name = uniqueNames.ToList<string>();
					using (OutPacket outPacket = new OutPacket("PrFavoriteTrackMapGet"))
					{
						outPacket.WriteInt(Name.Count); //主题数量
						for (int i = 0; i < Name.Count; i++)
						{
							XmlNodeList lis = doc.GetElementsByTagName(Name[i]);
							if (lis.Count > 0)
							{
								string theme = Name[i].Replace("theme", "");
								outPacket.WriteInt(int.Parse(theme)); //主题代码
								outPacket.WriteInt(lis.Count); //赛道数量
								foreach (XmlNode xn in lis)
								{
									XmlElement xe = (XmlElement)xn;
									uint track = uint.Parse(xe.GetAttribute("track"));
									outPacket.WriteShort(short.Parse(theme)); //主题代码
									outPacket.WriteUInt(track); //赛道代码
									outPacket.WriteByte(0);
								}
							}
							else
							{
								outPacket.WriteInt(0);
							}
						}
						RouterListener.MySession.Client.Send(outPacket);
					}
				}
				else
				{
					using (OutPacket outPacket = new OutPacket("PrFavoriteTrackMapGet"))
					{
						outPacket.WriteInt(0);
						RouterListener.MySession.Client.Send(outPacket);
					}
				}
			}
			else
			{
				using (OutPacket outPacket = new OutPacket("PrFavoriteTrackMapGet"))
				{
					outPacket.WriteInt(0);
					RouterListener.MySession.Client.Send(outPacket);
				}
			}
		}

		public static void Favorite_Track_Add(short theme, uint track)
		{
			var existingTrack = FavoriteTrackList.FirstOrDefault(list => list[0] == "theme" + theme.ToString() && list[1] == track.ToString());
			if (existingTrack == null)
			{
				var newList = new List<string> { "theme" + theme.ToString(), track.ToString() };
				FavoriteTrackList.Add(newList);
				Save_TrackList(FavoriteTrackList);
			}
		}

		public static void Favorite_Track_Del(short theme, uint track)
		{
			var trackToRemove = FavoriteTrackList.FirstOrDefault(list => list[0] == "theme" + theme.ToString() && list[1] == track.ToString());
			if (trackToRemove != null)
			{
				FavoriteTrackList.Remove(trackToRemove);
				Save_TrackList(FavoriteTrackList);
			}
		}

		public static void Save_TrackList(List<List<string>> SaveFavorite)
		{
			File.Delete(FileName.FavoriteTrack_LoadFile);
			XmlTextWriter writer = new XmlTextWriter(FileName.FavoriteTrack_LoadFile, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("FavoriteTrack");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < SaveFavorite.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(FileName.FavoriteTrack_LoadFile);
				XmlNode root = xmlDoc.SelectSingleNode("FavoriteTrack");
				XmlElement xe1 = xmlDoc.CreateElement(SaveFavorite[i][0]);
				xe1.SetAttribute("track", SaveFavorite[i][1]);
				root.AppendChild(xe1);
				xmlDoc.Save(FileName.FavoriteTrack_LoadFile);
			}
		}

    	public static byte GetTrackLevel(uint track)
    	{
        	try
        	{
            	if (!File.Exists(FileName.TrainingMission_LoadFile))
            	{
            	    return 0;
            	}

            	XmlDocument doc = new XmlDocument();
            	doc.Load(FileName.TrainingMission_LoadFile);

            	// 获取根元素
            	XmlElement root = doc.DocumentElement;

            	// 查找指定Track
            	foreach (XmlNode node in root.ChildNodes)
            	{
                	if (node.NodeType == XmlNodeType.Element && 
                    	node.Name == "TrainingMission" && 
                    	uint.TryParse(node.Attributes["Track"]?.Value, out uint existingTrack) &&
                    	existingTrack == track)
                	{
                    	if (byte.TryParse(node.Attributes["Level"]?.Value, out byte level))
                    	{
                        	return level;
                    	}
                    	else
                    	{
                        	return 0;
                    	}
                	}
            	}
            	return 0; // 未找到指定Track
        	}
        	catch
     		{
        	    return 0; // 发生错误时返回0
        	}
    	}

		public static byte TrainingMission(uint Track)
		{
			try
			{
				// 检查文件是否存在
				if (!File.Exists(FileName.TrainingMission_LoadFile))
				{
					CreateXmlFile(Track);
					return 1;
				}
				else
				{
					byte Level = ProcessExistingFile(Track);
					return Level;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"处理文件时发生错误: {ex.Message}");
				return 0; // 返回0表示发生错误
			}
		}

		public static void CreateXmlFile(uint Track)
		{
			XmlDocument doc = new XmlDocument();
			// 创建XML声明
			XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
			doc.AppendChild(xmlDeclaration);

			// 创建根元素
			XmlElement root = doc.CreateElement("TrainingMission");
			doc.AppendChild(root);

			// 添加Track
			XmlElement track1 = doc.CreateElement("TrainingMission");
			track1.SetAttribute("Track", Track.ToString());
			track1.SetAttribute("Level", "1");
			root.AppendChild(track1);

			// 保存文件
			doc.Save(FileName.TrainingMission_LoadFile);
		}

		public static byte ProcessExistingFile(uint Track)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(FileName.TrainingMission_LoadFile);

			// 获取根元素
			XmlElement root = doc.DocumentElement;

			// 查找Track
			XmlNode track1Node = null;
			foreach (XmlNode node in root.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element &&
					node.Name == "TrainingMission" &&
					node.Attributes["Track"]?.Value == Track.ToString())
				{
					track1Node = node;
					break;
				}
			}

			if (track1Node == null)
			{
				// Track不存在，添加它
				XmlElement newTrack1 = doc.CreateElement("TrainingMission");
				newTrack1.SetAttribute("Track", Track.ToString());
				newTrack1.SetAttribute("Level", "1");
				root.AppendChild(newTrack1);
				doc.Save(FileName.TrainingMission_LoadFile);
				return 1;
			}
			else
			{
				// Track存在，增加Level
				if (byte.TryParse(track1Node.Attributes["Level"]?.Value, out byte currentLevel))
				{
					byte newLevel = (byte)(currentLevel + 1);
					track1Node.Attributes["Level"].Value = newLevel.ToString();
					doc.Save(FileName.TrainingMission_LoadFile);
					return newLevel;
				}
				else
				{
					return 0; // 返回0表示发生错误
				}
			}
		}
	}
}
