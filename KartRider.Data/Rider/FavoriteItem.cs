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
        public static Dictionary<string, List<Favorite_Item>> FavoriteItemLists = new Dictionary<string, List<Favorite_Item>>();
        public static Dictionary<string, Favorite_Track> FavoriteTrackLists = new Dictionary<string, Favorite_Track>();

        public static void Favorite_Item(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var FavoriteItemList = new List<Favorite_Item>();
            if (File.Exists(filename.Favorite_LoadFile))
            {
                FavoriteItemList = JsonHelper.DeserializeNoBom<List<Favorite_Item>>(filename.Favorite_LoadFile);
            }
            FavoriteItemLists.TryAdd(Nickname, FavoriteItemList);
            using (OutPacket outPacket = new OutPacket("PrFavoriteItemGet"))
            {
                outPacket.WriteInt(FavoriteItemList.Count);
                foreach (Favorite_Item FavoriteItem in FavoriteItemList)
                {
                    outPacket.WriteShort(FavoriteItem.ItemCatID);
                    outPacket.WriteShort(FavoriteItem.ItemID);
                    outPacket.WriteShort(FavoriteItem.ItemSN);
                    outPacket.WriteByte(0);
                }
                Parent.Client.Send(outPacket);
            }
        }

        public static void Favorite_Item_Add(string Nickname, short itemCatID, short itemID, short itemSN)
        {
            FavoriteItemLists.TryAdd(Nickname, new List<Favorite_Item>());
            var FavoriteItemList = FavoriteItemLists[Nickname];
            var existingItem = FavoriteItemList.FirstOrDefault(item => item.ItemCatID == itemCatID && item.ItemID == itemID && item.ItemSN == itemSN);
            if (existingItem == null)
            {
                var newItem = new Favorite_Item { ItemCatID = itemCatID, ItemID = itemID, ItemSN = itemSN };
                FavoriteItemList.Add(newItem);
                Save_ItemList(Nickname, FavoriteItemList);
            }
        }

        public static void Favorite_Item_Del(string Nickname, short itemCatID, short itemID, short itemSN)
        {
            if (FavoriteItemLists.ContainsKey(Nickname))
            {
                var itemToRemove = FavoriteItemLists[Nickname].FirstOrDefault(item => item.ItemCatID == itemCatID && item.ItemID == itemID && item.ItemSN == itemSN);
                if (itemToRemove != null)
                {
                    FavoriteItemLists[Nickname].Remove(itemToRemove);
                    Save_ItemList(Nickname, FavoriteItemLists[Nickname]);
                }
            }
        }

        public static void Save_ItemList<T>(string Nickname, T obj)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            File.WriteAllText(filename.Favorite_LoadFile, JsonHelper.Serialize(obj));
        }

        public static void Favorite_Track(SessionGroup Parent, string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var FavoriteTrackList = new Favorite_Track();
            if (File.Exists(filename.FavoriteTrack_LoadFile))
            {
                FavoriteTrackList = JsonHelper.DeserializeNoBom<Favorite_Track>(filename.FavoriteTrack_LoadFile);
            }
            FavoriteTrackLists.TryAdd(Nickname, FavoriteTrackList);
            using (OutPacket outPacket = new OutPacket("PrFavoriteTrackMapGet"))
            {
                var ThemesList = FavoriteTrackList.GetTheme();
                outPacket.WriteInt(ThemesList.Count); //主题数量
                foreach (short theme in ThemesList)
                {
                    outPacket.WriteInt(theme); //主题代码
                    var Tracks = FavoriteTrackList.GetTracks(theme);
                    outPacket.WriteInt(Tracks.Count); //赛道数量
                    foreach (uint Track in Tracks)
                    {
                        outPacket.WriteShort(theme); //主题代码
                        outPacket.WriteUInt(Track); //赛道代码
                        outPacket.WriteByte(0);
                    }
                }
                Parent.Client.Send(outPacket);
            }
        }

        public static void Favorite_Track_Add(string Nickname, short theme, uint track)
        {
            FavoriteTrackLists.TryAdd(Nickname, new Favorite_Track());
            var FavoriteTrackList = FavoriteTrackLists[Nickname];
            FavoriteTrackList.AddTrack(theme, track);
            Save_TrackList(Nickname, FavoriteTrackList);
        }

        public static void Favorite_Track_Del(string Nickname, short theme, uint track)
        {
            FavoriteTrackLists.TryAdd(Nickname, new Favorite_Track());
            var FavoriteTrackList = FavoriteTrackLists[Nickname];
            FavoriteTrackList.RemoveTrack(theme, track);
            Save_TrackList(Nickname, FavoriteTrackList);
        }

        public static void Save_TrackList<T>(string Nickname, T obj)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            File.WriteAllText(filename.FavoriteTrack_LoadFile, JsonHelper.Serialize(obj));
        }
    }

    public class Favorite_Item
    {
        public short ItemCatID { get; set; } = 0;
        public short ItemID { get; set; } = 0;
        public short ItemSN { get; set; } = 0;
    }

    public class Favorite_Track
    {
        // 内部存储：键=Theme(short)，值=该Theme对应的Tracks列表(List<short>)
        private Dictionary<short, List<uint>> _themeTracks = new Dictionary<short, List<uint>>();

        // 公共属性（用于序列化/反序列化）
        public Dictionary<short, List<uint>> ThemeTracks
        {
            get => _themeTracks;
            set => _themeTracks = value;
        }

        // 获取所有Theme（所有键）
        public List<short> GetTheme()
        {
            return _themeTracks.Keys.ToList();
        }

        // 根据Theme获取对应的所有Tracks（若Theme不存在，返回空列表）
        public List<uint> GetTracks(short theme)
        {
            return _themeTracks.TryGetValue(theme, out var tracks) ? new List<uint>(tracks) : new List<uint>();
        }

        /// <summary>
        /// 给指定Theme添加单个Track（若Theme不存在则自动创建）
        /// </summary>
        /// <param name="theme">主题</param>
        /// <param name="track">要添加的赛道</param>
        public void AddTrack(short theme, uint track)
        {
            // 若Theme不存在，先创建并初始化空列表
            if (!_themeTracks.ContainsKey(theme))
            {
                _themeTracks[theme] = new List<uint>();
            }

            // 避免重复添加同一个track（如果需要允许重复，可删除此判断）
            if (!_themeTracks[theme].Contains(track))
            {
                _themeTracks[theme].Add(track);
            }
        }

        /// <summary>
        /// 从指定Theme中删除单个Track（若删除后列表为空，则自动删除Theme）
        /// </summary>
        /// <param name="theme">主题</param>
        /// <param name="track">要删除的赛道</param>
        /// <returns>是否删除成功（true：存在并删除；false：Theme或Track不存在）</returns>
        public bool RemoveTrack(short theme, uint track)
        {
            // 若Theme不存在，直接返回失败
            if (!_themeTracks.TryGetValue(theme, out var tracks))
            {
                return false;
            }

            // 从列表中删除指定track
            bool isTrackRemoved = tracks.Remove(track);

            // 若删除后列表为空，则删除整个Theme
            if (isTrackRemoved && tracks.Count == 0)
            {
                _themeTracks.Remove(theme);
            }

            return isTrackRemoved;
        }

        /// <summary>
        /// 获取所有主题下的所有Tracks
        /// </summary>
        /// <returns>包含所有Track的List<uint></returns>
        public List<uint> GetAllTracks()
        {
            var allTracks = new List<uint>();
            foreach (var tracks in _themeTracks.Values)
            {
                allTracks.AddRange(tracks);
            }
            return allTracks;
        }
    }
}
