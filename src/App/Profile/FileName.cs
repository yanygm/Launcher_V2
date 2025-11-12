using Microsoft.Win32;

namespace Launcher.App.Profile
{
    /// <summary>
    /// A static class containing file names and paths.
    /// </summary>
    public static class FileName
    {
        #region Constants

        public const string KartRider = "KartRider.exe";
        public const string PinFile = "KartRider.pin";
        public const string PinFileBak = "KartRider-bak.pin";

        public const string TCGKartRegPath = @"HKEY_CURRENT_USER\SOFTWARE\TCGame\kart";

        #endregion

        public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;

        public static readonly string TCGKartGamePath = Path.GetFullPath((string)(Registry.GetValue(TCGKartRegPath, "gamepath", AppDir) ?? AppDir));

        public static readonly string ProfileDir = Path.GetFullPath(Path.Combine(AppDir, @"Profile\"));
        public static readonly string LogDir = Path.GetFullPath(Path.Combine(ProfileDir, @"Logs\"));

        public static readonly string TimeAttackLog = Path.GetFullPath(Path.Combine(LogDir, @"TimeAttack.log"));

        public static readonly string ConfigFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Config.json"));
        public static readonly string SpecialKartConfig = Path.GetFullPath(Path.Combine(AppDir, @"Profile\SpecialKartConfig.json"));

        public static readonly string Update_File = Path.GetFullPath(Path.Combine(AppDir, @"Update.bat"));
        public static readonly string Update_Folder = Path.GetFullPath(Path.Combine(AppDir, @"Update\"));

        public static readonly string Whitelist = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Whitelist.ini"));
        public static readonly string Blacklist = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Blacklist.ini"));

        public static readonly string NewKart_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\NewKart.xml"));
        public static readonly string ModelMax_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\ModelMax.xml"));
        public static readonly string Favorite_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Favorite.xml"));
        public static readonly string FavoriteTrack_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\FavoriteTrack.xml"));
        public static readonly string TrainingMission_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\TrainingMission.xml"));
        public static readonly string Competitive_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Competitive.xml"));
        public static readonly string AI_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\AI.xml"));
        public static readonly string TuneData_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\TuneData.xml"));
        public static readonly string PlantData_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\PlantData.xml"));
        public static readonly string LevelData_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\LevelData.xml"));
        public static readonly string PartsData_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\PartsData.xml"));
        public static readonly string Parts12Data_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Parts12Data.xml"));
        public static readonly string Level12Data_LoadFile = Path.GetFullPath(Path.Combine(AppDir, @"Profile\Level12Data.xml"));
    }
}
