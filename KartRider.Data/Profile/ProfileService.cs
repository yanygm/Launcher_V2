using KartRider;
using Newtonsoft.Json;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Profile
{
    public class ProfileService
    {
        public static ProfileConfig ProfileConfig { get; set; } = new ProfileConfig();

        public static void Save()
        {
            var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
            };

            using (StreamWriter streamWriter = new StreamWriter(FileName.config_path, false))
            {
                streamWriter.Write(Newtonsoft.Json.JsonConvert.SerializeObject(ProfileConfig, jsonSettings));
            }
        }

        public static void Load()
        {
            if (File.Exists(FileName.config_path))
            {
                string config_str = System.IO.File.ReadAllText(FileName.config_path);
                ProfileConfig = JsonConvert.DeserializeObject<ProfileConfig>(config_str);

                Loaded();
            }
            else
            {
                using (StreamWriter streamWriter = new StreamWriter(FileName.config_path, false))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(ProfileConfig));
                }
            }
        }

        private static void Loaded()
        {
            if (ProfileConfig.ServerSetting.PreventItem_Use == 0)
            {
                Program.PreventItem = false;
            }
            else
            {
                Program.PreventItem = true;
            }

            if (ProfileConfig.ServerSetting.SpeedPatch_Use == 0)
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
