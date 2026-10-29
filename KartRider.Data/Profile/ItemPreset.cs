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
        public ushort ID { get; set; }

        public byte Badge { get; set; }

        public byte Enable { get; set; }

        public string Name { get; set; } = "";

        public ushort Character { get; set; }

        public ushort Paint { get; set; }

        public ushort Kart { get; set; }

        public ushort Plate { get; set; }

        public ushort Goggle { get; set; }

        public ushort Balloon { get; set; }

        public ushort Unknown1 { get; set; }

        public ushort HeadBand { get; set; }

        public ushort HeadPhone { get; set; }

        public ushort HandGearL { get; set; }

        public ushort Unknown2 { get; set; }

        public ushort Uniform { get; set; }

        public ushort Decal { get; set; }

        public ushort Pet { get; set; }

        public ushort FlyingPet { get; set; }

        public ushort Aura { get; set; }

        public ushort SkidMark { get; set; }

        public ushort SpecialKit { get; set; }

        public ushort RidColor { get; set; }

        public ushort BonusCard { get; set; }

        public ushort BossModeCard { get; set; }

        public ushort KartPlant1 { get; set; }

        public ushort KartPlant2 { get; set; }

        public ushort KartPlant3 { get; set; }

        public ushort KartPlant4 { get; set; }

        public ushort Unknown3 { get; set; }

        public ushort FishingPole { get; set; }

        public ushort Tachometer { get; set; }

        public ushort Dye { get; set; }

        public ushort KartSN { get; set; }

        public byte Unknown4 { get; set; }

        public ushort KartCoating { get; set; }

        public ushort KartTailLamp { get; set; }

        public ushort slotBg { get; set; }

        public ushort KartCoating12 { get; set; }

        public ushort KartTailLamp12 { get; set; }

        public ushort KartBoosterEffect12 { get; set; }

        public ushort Unknown5 { get; set; }
    }

    public class ItemPresetsService
    {
        /// <summary>
        /// 从文件获取配置
        /// </summary>
        public static ItemPresetConfig GetItemPresetConfig(string Nickname)
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
                    var loadedConfig = JsonHelper.DeserializeNoBom<ItemPresetConfig>(filename.ItemPresetsConfig) ?? new ItemPresetConfig();

                    if (loadedConfig?.ItemPresets != null)
                    {
                        int count = loadedConfig.ItemPresets.Count;
                        int defaultCount = new ItemPresetConfig().ItemPresets.Count;

                        if (count < defaultCount)
                        {
                            return new ItemPresetConfig();
                        }

                        if (count > defaultCount)
                        {
                            loadedConfig.ItemPresets = loadedConfig.ItemPresets.Skip(count - defaultCount).ToList();
                        }
                        return loadedConfig;
                    }
                }
                return new ItemPresetConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败：{ex.Message}");
                return new ItemPresetConfig();
            }
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public static void Save(string Nickname, ItemPresetConfig config)
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
                File.WriteAllText(filename.ItemPresetsConfig, JsonHelper.Serialize(config));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败：{ex.Message}");
            }
        }
    }
}