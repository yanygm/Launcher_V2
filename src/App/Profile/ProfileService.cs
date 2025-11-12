using Launcher.App.Server;
using Newtonsoft.Json;

namespace Launcher.App.Profile
{
    public class ProfileService
    {
        public static ProfileConfig ProfileConfig { get; set; } = new();

        /// <summary>
        /// Clamp values to valid ranges before saving and after loading.
        /// </summary>
        public static void ClampValue()
        {
            ProfileConfig.Rider.Lucci = Math.Clamp(ProfileConfig.Rider.Lucci, 0, SessionGroup.LucciMax);
            ProfileConfig.GameOption.Set_BGM = Math.Clamp(ProfileConfig.GameOption.Set_BGM, 0f, 1f);
            ProfileConfig.GameOption.Set_Sound = Math.Clamp(ProfileConfig.GameOption.Set_Sound, 0f, 1f);
        }

        /// <summary>
        /// Save the current config to the config file.
        /// </summary>
        public static void Save()
        {
            ClampValue();
            try
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                };

                using (StreamWriter streamWriter = new(FileName.ConfigFile, false))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(ProfileConfig, jsonSettings));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        /// <summary>
        /// Load the config from the config file.
        /// </summary>
        /// <returns>False if fail to load the config</returns>
        public static bool Load()
        {
            bool result = File.Exists(FileName.ConfigFile);
            if (!result)
            {
                ProfileConfig = new ProfileConfig();
                Save();
                return false;
            }
            try
            {
                ProfileConfig = JsonConvert.DeserializeObject<ProfileConfig>(File.ReadAllText(FileName.ConfigFile)) ?? new ProfileConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                ProfileConfig = new ProfileConfig();
                result = false;
            }
            finally
            {
                ClampValue();
                Save();
            }
            return result;
        }
    }
}
