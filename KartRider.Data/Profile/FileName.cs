using System;
using System.IO;

namespace Profile
{
	public static class FileName
	{
		public static string appDir = AppDomain.CurrentDomain.BaseDirectory; // 应用程序所在目录
		public static string KartRider = Path.GetFullPath(Path.Combine(appDir, @"KartRider.exe"));
		public static string pinFile = Path.GetFullPath(Path.Combine(appDir, @"KartRider.pin"));
		public static string ProfileDir = Path.GetFullPath(Path.Combine(appDir, @"Profile\"));
		public static string config_path = Path.GetFullPath(Path.Combine(appDir, @"Profile\Launcher.json"));
		public static string SpecialKartConfig = Path.GetFullPath(Path.Combine(appDir, @"Profile\SpecialKartConfig.json"));

		public static string Load_Console = Path.GetFullPath(Path.Combine(appDir, @"Profile\Console.ini"));
		public static string Load_CC = Path.GetFullPath(Path.Combine(appDir, @"Profile\CountryCode.ini"));

		public static string NewKart_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\NewKart.xml"));
		public static string ModelMax_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\ModelMax.xml"));
		public static string Favorite_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\Favorite.xml"));
		public static string FavoriteTrack_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\FavoriteTrack.xml"));
		public static string TrainingMission_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\TrainingMission.xml"));
		public static string Competitive_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\Competitive.xml"));
		public static string AI_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\AI.xml"));
		public static string TuneData_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\TuneData.xml"));
		public static string PlantData_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\PlantData.xml"));
		public static string LevelData_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\LevelData.xml"));
		public static string PartsData_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\PartsData.xml"));
		public static string Parts12Data_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\Parts12Data.xml"));
		public static string Level12Data_LoadFile = Path.GetFullPath(Path.Combine(appDir, @"Profile\Level12Data.xml"));
	}
}
