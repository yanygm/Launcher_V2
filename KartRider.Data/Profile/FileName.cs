using ExcData;
using KartRider;
using Launcher.Properties;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string Favorite_LoadFile;
        public string FavoriteTrack_LoadFile;
        public string AI_LoadFile;
        public string TuneData_LoadFile;
        public string PlantData_LoadFile;
        public string LevelData_LoadFile;
        public string PartsData_LoadFile;
        public string Parts12Data_LoadFile;
        public string Level12Data_LoadFile;
        public string Competitive_LoadFile;
        public string TrainingMission_LoadFile;
    }

    public static class FileName
    {
        public static string appDir = AppDomain.CurrentDomain.BaseDirectory; // 应用程序所在目录
        public static string KartRider = Path.GetFullPath(Path.Combine(appDir, @"KartRider.exe"));
        public static string pinFile = Path.GetFullPath(Path.Combine(appDir, @"KartRider.pin"));
        public static string ProfileDir = Path.GetFullPath(Path.Combine(appDir, @"Profile"));
        public static string Load_Console = Path.GetFullPath(Path.Combine(ProfileDir, @"Console.ini"));
        public static string Load_Settings = Path.GetFullPath(Path.Combine(ProfileDir, @"Settings.json"));
        public static string Load_CC = Path.GetFullPath(Path.Combine(ProfileDir, @"CountryCode.ini"));
        public static string NewKart_LoadFile = Path.GetFullPath(Path.Combine(ProfileDir, @"NewKart.json"));
        public static string NewItem_LoadFile = Path.GetFullPath(Path.Combine(ProfileDir, @"NewItem.json"));
        public static string ModelMax_LoadFile = Path.GetFullPath(Path.Combine(ProfileDir, @"ModelMax.xml"));
        public static string SpecialKartConfig = Path.GetFullPath(Path.Combine(ProfileDir, @"SpecialKartConfig.json"));

        public static Dictionary<string, fileName> FileNames = new Dictionary<string, fileName>();

        public static void Load(string nickname)
        {
            var filename = new fileName();
            filename.NicknameDir = Path.GetFullPath(Path.Combine(ProfileDir, nickname));
            filename.config_path = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Launcher.json"));
            filename.ItemPresetsConfig = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"ItemPresetsConfig.json"));
            filename.Favorite_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Favorite.json"));
            filename.FavoriteTrack_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"FavoriteTrack.json"));
            filename.AI_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"AI.xml"));
            filename.TuneData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"TuneData.json"));
            filename.PlantData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"PlantData.json"));
            filename.LevelData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"LevelData.json"));
            filename.PartsData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"PartsData.json"));
            filename.Parts12Data_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Parts12Data.json"));
            filename.Level12Data_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Level12Data.json"));
            filename.Competitive_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Competitive.json"));
            filename.TrainingMission_LoadFile = Path.GetFullPath(Path.Combine(ProfileDir, @"TrainingMission.json"));
            FileNames.TryAdd(nickname, filename);
            if (!Directory.Exists(filename.NicknameDir))
            {
                Directory.CreateDirectory(filename.NicknameDir);
            }
            ProfileService.Load(nickname);
        }
    }
}
