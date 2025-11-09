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
        public static Dictionary<string, ProfileConfig> ProfileConfigs { get; set; } = new Dictionary<string, ProfileConfig>();
        public static Setting SettingConfig { get; set; } = new Setting();

        public static void SaveSettings()
        {
            File.WriteAllText(FileName.Load_Settings, JsonHelper.Serialize(SettingConfig));
        }

        public static void LoadSettings()
        {
            if (File.Exists(FileName.Load_Settings))
            {
                SettingConfig = JsonHelper.DeserializeNoBom<Setting>(FileName.Load_Settings);
            }
            else
            {
                SettingConfig = new Setting();
                SaveSettings();
            }
        }

        public static void Save(string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            if (ProfileConfigs.ContainsKey(Nickname))
            {
                File.WriteAllText(filename.config_path, JsonHelper.Serialize(ProfileConfigs[Nickname]));
            }
        }

        public static void Load(string Nickname)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            if (File.Exists(filename.config_path))
            {
                ProfileConfigs.TryAdd(Nickname, JsonHelper.DeserializeNoBom<ProfileConfig>(filename.config_path));
                Loaded(Nickname);
            }
            else
            {
                ProfileConfigs.TryAdd(Nickname, new ProfileConfig());
                Save(Nickname);
            }
        }

        private static void Loaded(string Nickname)
        {
            if (ProfileConfigs[Nickname].ServerSetting.PreventItem_Use == 0)
            {
                Program.PreventItem = false;
            }
            else
            {
                Program.PreventItem = true;
            }

            if (ProfileConfigs[Nickname].ServerSetting.SpeedPatch_Use == 0)
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
