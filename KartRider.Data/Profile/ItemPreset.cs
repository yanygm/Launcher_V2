using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile
{
    public class ItemPresetConfig
    {
        // 初始化时创建 3 个预设项
        public ItemPresetConfig()
        {
            ItemPresets = new List<ItemPreset>
            {
                new ItemPreset { ID = 1 },  // 第一个预设，默认 ID=1
                new ItemPreset { ID = 2 },  // 第二个预设，默认 ID=2
                new ItemPreset { ID = 3 }   // 第三个预设，默认 ID=3
            };
        }

        // 包含 3 个 ItemPreset 的集合
        public List<ItemPreset> ItemPresets { get; set; }
    }

    public class ItemPreset
    {
        public short ID { get; set; }

        public int Badge { get; set; }

        public string Name { get; set; } = "";

        public short Character { get; set; }

        public short Paint { get; set; }

        public short Kart { get; set; }

        public short Plate { get; set; }

        public short Goggle { get; set; }

        public short Balloon { get; set; }

        public short Unknown1 { get; set; }

        public short HeadBand { get; set; }

        public short HeadPhone { get; set; }

        public short HandGearL { get; set; }

        public short Unknown2 { get; set; }

        public short Uniform { get; set; }

        public short Decal { get; set; }

        public short Pet { get; set; }

        public short FlyingPet { get; set; }

        public short Aura { get; set; }

        public short SkidMark { get; set; }

        public short SpecialKit { get; set; }

        public short RidColor { get; set; }

        public short BonusCard { get; set; }

        public short BossModeCard { get; set; }

        public short KartPlant1 { get; set; }

        public short KartPlant2 { get; set; }

        public short KartPlant3 { get; set; }

        public short KartPlant4 { get; set; }

        public short Unknown3 { get; set; }

        public short FishingPole { get; set; }

        public short Tachometer { get; set; }

        public short Dye { get; set; }

        public short KartSN { get; set; }

        public byte Unknown4 { get; set; }

        public short KartCoating { get; set; }

        public short KartTailLamp { get; set; }

        public short slotBg { get; set; }

        public short KartCoating12 { get; set; }

        public short KartTailLamp12 { get; set; }

        public short KartBoosterEffect12 { get; set; }

        public short Unknown5 { get; set; }
    }

    public class ItemPresetsService
    {
        // 静态配置实例，默认包含 3 个预设
        public static ItemPresetConfig ItemPresetConfig { get; set; } = new ItemPresetConfig();

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public static void Save()
        {
            try
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

                // 确保目录存在
                string directory = Path.GetDirectoryName(FileName.ItemPresetsConfig);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化并写入文件（包含 3 个预设）
                using (var streamWriter = new StreamWriter(FileName.ItemPresetsConfig, false, Encoding.UTF8))
                {
                    string json = JsonConvert.SerializeObject(ItemPresetConfig, jsonSettings);
                    streamWriter.Write(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 从文件加载配置（确保加载后仍有 3 个预设）
        /// </summary>
        public static void Load()
        {
            try
            {
                if (File.Exists(FileName.ItemPresetsConfig))
                {
                    string configStr = File.ReadAllText(FileName.ItemPresetsConfig, Encoding.UTF8);
                    var loadedConfig = JsonConvert.DeserializeObject<ItemPresetConfig>(configStr);

                    // 确保加载后集合不为空且有 3 个元素（防止文件损坏导致数据不完整）
                    if (loadedConfig?.ItemPresets != null)
                    {
                        // 不足 3 个则补全
                        while (loadedConfig.ItemPresets.Count < 3)
                        {
                            loadedConfig.ItemPresets.Add(new ItemPreset());
                        }
                        // 超过 3 个则截断
                        if (loadedConfig.ItemPresets.Count > 3)
                        {
                            loadedConfig.ItemPresets = loadedConfig.ItemPresets.Take(3).ToList();
                        }
                        ItemPresetConfig = loadedConfig;
                    }
                    else
                    {
                        // 配置无效时使用默认（3 个预设）
                        ItemPresetConfig = new ItemPresetConfig();
                    }
                }
                else
                {
                    // 文件不存在时创建默认配置（含 3 个预设）
                    Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败：{ex.Message}");
                ItemPresetConfig = new ItemPresetConfig(); // 异常时使用默认
            }
        }
    }
}
