using ExcData;
using KartRider;
using KartRider.Logging;
using RiderData;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Profile
{
    public class fileName
    {
        public string NicknameDir;
        public string config_path;
        public string ItemPresetsConfig;
        public string NewKart_LoadFile;
        public string NewItem_LoadFile;
        public string Favorite_LoadFile;
        public string Locked_LoadFile;
        public string FavoriteTrack_LoadFile;
        public string TuneData_LoadFile;
        public string PlantData_LoadFile;
        public string LevelData_LoadFile;
        public string PartsData_LoadFile;
        public string Parts12Data_LoadFile;
        public string Level12Data_LoadFile;
        public string Competitive_LoadFile;
        public string TrainingMission_LoadFile;
        public string GiveGift_LoadFile;
        public string RewardBox_LoadFile;
        public string Bingo_LoadFile;
    }

    public static class FileName
    {
        public static string appDir = AppDomain.CurrentDomain.BaseDirectory; // 应用程序所在目录
        public static string KartRider = Path.GetFullPath(Path.Combine(appDir, @"KartRider.exe"));
        public static string pinFile = Path.GetFullPath(Path.Combine(appDir, @"KartRider.pin"));
        public static string ProfileDir = Path.GetFullPath(Path.Combine(appDir, @"Profile"));
        public static string Load_Settings = Path.GetFullPath(Path.Combine(ProfileDir, @"Settings.json"));
        public static string Load_TrackRank = Path.GetFullPath(Path.Combine(appDir, @"TrackRank"));
        public static string ModelMax_LoadFile = Path.GetFullPath(Path.Combine(ProfileDir, @"ModelMax.xml"));
        public static string Coupon = Path.GetFullPath(Path.Combine(ProfileDir, @"Coupon.json"));

        /// <summary>
        /// 昵称 → 路径集合。原实现是普通 Dictionary，被多个会话线程并发 Load/Remove/索引，
        /// 会撕裂或抛 InvalidOperationException；改为并发字典后 GetOrAdd 是原子的。
        /// </summary>
        public static ConcurrentDictionary<string, fileName> FileNames { get; } =
            new ConcurrentDictionary<string, fileName>(StringComparer.Ordinal);

        public static fileName Load(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return null;
            }
            return FileNames.GetOrAdd(nickname, Create);
        }

        private static fileName Create(string nickname)
        {
            var filename = new fileName();
            filename.NicknameDir = Path.GetFullPath(Path.Combine(ProfileDir, nickname));
            filename.config_path = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Launcher.json"));
            filename.ItemPresetsConfig = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"ItemPresetsConfig.json"));
            filename.Favorite_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Favorite.json"));
            filename.Locked_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Locked.json"));
            filename.FavoriteTrack_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"FavoriteTrack.json"));
            filename.NewKart_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"NewKart.json"));
            filename.NewItem_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"NewItem.json"));
            filename.TuneData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"TuneData.json"));
            filename.PlantData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"PlantData.json"));
            filename.LevelData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"LevelData.json"));
            filename.PartsData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"PartsData.json"));
            filename.Parts12Data_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Parts12Data.json"));
            filename.Level12Data_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Level12Data.json"));
            filename.Competitive_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Competitive.json"));
            filename.TrainingMission_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"TrainingMission.json"));
            filename.GiveGift_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"GiveGift.json"));
            filename.RewardBox_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"RewardBox.json"));
            filename.Bingo_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Bingo.json"));

            try
            {
                if (!Directory.Exists(filename.NicknameDir))
                {
                    Directory.CreateDirectory(filename.NicknameDir);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FileName] 创建目录失败 {nickname}: {ex.Message}");
            }

            return filename;
        }

        /// <summary>
        /// 清理会话结束后不再需要的昵称缓存（避免静态字典无界增长）。
        /// </summary>
        public static void Forget(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            FileNames.TryRemove(nickname, out _);
        }
    }
}
