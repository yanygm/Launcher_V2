using KartRider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Profile
{
    public class ItemPresetConfig
    {
        public ItemPresetConfig()
        {
            ItemPresets = new List<ItemPreset>
            {
                new ItemPreset { ID = 1 },
                new ItemPreset { ID = 2 },
                new ItemPreset { ID = 3 },
                new ItemPreset { ID = 4 },
                new ItemPreset { ID = 5 },
                new ItemPreset { ID = 6 }
            };
        }

        public List<ItemPreset> ItemPresets { get; set; }
    }

    public class ItemPreset
    {
        public short ID { get; set; }

        public byte Badge { get; set; }

        public byte Enable { get; set; }

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
        public static Dictionary<string, ItemPresetConfig> ItemPresetConfigs { get; set; } = new Dictionary<string, ItemPresetConfig>();

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public static void Save(string Nickname)
        {
            try
            {
                if (!FileName.FileNames.ContainsKey(Nickname))
                {
                    FileName.Load(Nickname);
                }
                var filename = FileName.FileNames[Nickname];

                // 确保目录存在
                string directory = Path.GetDirectoryName(filename.ItemPresetsConfig);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化并写入文件
                if (ItemPresetConfigs.ContainsKey(Nickname))
                {
                    File.WriteAllText(filename.ItemPresetsConfig, JsonHelper.Serialize(ItemPresetConfigs[Nickname]));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static void Load(string Nickname)
        {
            try
            {
                if (!FileName.FileNames.ContainsKey(Nickname))
                {
                    FileName.Load(Nickname);
                }
                var filename = FileName.FileNames[Nickname];

                if (File.Exists(filename.ItemPresetsConfig))
                {
                    var loadedConfig = JsonHelper.DeserializeNoBom<ItemPresetConfig>(filename.ItemPresetsConfig);

                    if (loadedConfig?.ItemPresets != null)
                    {
                        int count = loadedConfig.ItemPresets.Count;

                        int defaultCount = new ItemPresetConfig().ItemPresets.Count;

                        if (count < defaultCount)
                        {
                            loadedConfig = new ItemPresetConfig();
                        }

                        if (count > defaultCount)
                        {
                            loadedConfig.ItemPresets = loadedConfig.ItemPresets.Skip(count - defaultCount).ToList();
                        }
                        ItemPresetConfigs.TryAdd(Nickname, loadedConfig);
                    }
                    else
                    {
                        ItemPresetConfigs.TryAdd(Nickname, new Profile.ItemPresetConfig());
                        Save(Nickname);
                    }
                }
                else
                {
                    ItemPresetConfigs.TryAdd(Nickname, new Profile.ItemPresetConfig());
                    Save(Nickname);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败：{ex.Message}");
                ItemPresetConfigs.TryAdd(Nickname, new Profile.ItemPresetConfig());
                Save(Nickname);
            }
        }
    }
}
