using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;
using KartRider;
using System.Xml;
using System.Linq;

namespace RiderData
{
	public static class FavoriteItem
	{
		public static List<List<short>> FavoriteItemList = new List<List<short>>();
		public static List<List<string>> FavoriteTrackList = new List<List<string>>();

		public static void Favorite_Item()
		{
			using (OutPacket outPacket = new OutPacket("PrFavoriteItemGet"))
			{
				if (File.Exists(@"Profile\Favorite.xml"))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(@"Profile\Favorite.xml");
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
			File.Delete(@"Profile\Favorite.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\Favorite.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("Favorite");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < SaveFavorite.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\Favorite.xml");
				XmlNode root = xmlDoc.SelectSingleNode("Favorite");
				XmlElement xe1 = xmlDoc.CreateElement("Title");
				xe1.SetAttribute("item", SaveFavorite[i][0].ToString());
				xe1.SetAttribute("id", SaveFavorite[i][1].ToString());
				xe1.SetAttribute("sn", SaveFavorite[i][2].ToString());
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\Favorite.xml");
			}
		}

		public static void Favorite_Track()
		{
			if (File.Exists(@"Profile\FavoriteTrack.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\FavoriteTrack.xml");
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
									int track = int.Parse(xe.GetAttribute("track"));
									outPacket.WriteShort(short.Parse(theme)); //主题代码
									outPacket.WriteInt(track); //赛道代码
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

		public static void Favorite_Track_Add(short theme, int track)
		{
			var existingTrack = FavoriteTrackList.FirstOrDefault(list => list[0] == "theme" + theme.ToString() && list[1] == track.ToString());
			if (existingTrack == null)
			{
				var newList = new List<string> { "theme" + theme.ToString(), track.ToString() };
				FavoriteTrackList.Add(newList);
				Save_TrackList(FavoriteTrackList);
			}
		}

		public static void Favorite_Track_Del(short theme, int track)
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
			File.Delete(@"Profile\FavoriteTrack.xml");
			XmlTextWriter writer = new XmlTextWriter(@"Profile\FavoriteTrack.xml", System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument();
			writer.WriteStartElement("FavoriteTrack");
			writer.WriteEndElement();
			writer.Close();
			for (var i = 0; i < SaveFavorite.Count; i++)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(@"Profile\FavoriteTrack.xml");
				XmlNode root = xmlDoc.SelectSingleNode("FavoriteTrack");
				XmlElement xe1 = xmlDoc.CreateElement(SaveFavorite[i][0]);
				xe1.SetAttribute("track", SaveFavorite[i][1]);
				root.AppendChild(xe1);
				xmlDoc.Save(@"Profile\FavoriteTrack.xml");
			}
		}
	}
}
