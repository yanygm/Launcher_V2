using KartRider;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Profile
{
    public class ProfileService
    {
        public static Setting SettingConfig { get; set; } = new Setting();

        public static ProfileConfig GetProfileConfig(string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            if (File.Exists(filename.config_path))
            {
                return JsonHelper.DeserializeNoBom<ProfileConfig>(filename.config_path) ?? new ProfileConfig();
            }
            return new ProfileConfig();
        }

        public static void SaveSettings()
        {
            var settingsDir = Path.GetDirectoryName(FileName.Load_Settings);
            if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }
            File.WriteAllText(FileName.Load_Settings, JsonHelper.Serialize(SettingConfig));
        }

        public static void LoadSettings()
        {
            if (File.Exists(FileName.Load_Settings))
            {
                SettingConfig = JsonHelper.DeserializeNoBom<Setting>(FileName.Load_Settings) ?? new Setting();
            }
            else
            {
                SettingConfig = new Setting();
                SaveSettings();
            }
        }

        public static void Save(string Nickname, ProfileConfig config)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            File.WriteAllText(filename.config_path, JsonHelper.Serialize(config));
        }

        public static void Load(string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            if (!File.Exists(filename.config_path))
            {
                ProfileConfig newConfig = new ProfileConfig();
                Save(Nickname, newConfig);
            }
            Loaded(Nickname);
        }

        private static void Loaded(string Nickname)
        {
            var config = GetProfileConfig(Nickname);
            if (config?.ServerSetting == null)
            {
                Console.WriteLine("[Loaded] Warning: ProfileConfig or ServerSetting is null for {0}", Nickname);
                return;
            }
            if (config.ServerSetting.PreventItem_Use == 0)
            {
                Program.PreventItem = false;
            }
            else
            {
                Program.PreventItem = true;
            }

            if (config.ServerSetting.SpeedPatch_Use == 0)
            {
                Program.SpeedPatch = false;
                Program.LauncherDlg.Text = "Launcher";
            }
            else
            {
                Program.SpeedPatch = true;
                Program.LauncherDlg.Text = "Launcher (속도 패치)";
            }
        }
    }
}