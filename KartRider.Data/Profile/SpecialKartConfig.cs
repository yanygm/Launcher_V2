using System;
using System.Collections.Generic;
using System.IO;

namespace KartRider;

/// <summary>
/// 特殊道具车配置类
/// </summary>
public class SpecialKartConfig
{
    /// <summary>
    /// 特殊道具车：将指定道具变更为特殊道具
    /// </summary>
    public string SkillChangeDesc { get; set; }
    public Dictionary<short, Dictionary<short, short>> SkillChange { get; set; } = new();

    /// <summary>
    /// 特殊道具车：使用指定道具后获得特殊道具
    /// </summary>
    public string SkillMappingsDesc { get; set; }
    public Dictionary<short, Dictionary<short, short>> SkillMappings { get; set; } = new();

    /// <summary>
    /// 特殊道具车：被指定道具攻击后获得特殊道具
    /// </summary>
    public string SkillAttackedDesc { get; set; }
    public Dictionary<short, Dictionary<short, short>> SkillAttacked { get; set; } = new();

    /// <summary>
    /// 将特殊道具车配置存储到JSON文件（存在时补充缺失内容，保留额外内容）
    /// </summary>
    /// <param name="filePath">文件路径（如：./Config/SpecialKartConfig.json）</param>
    public static void SaveConfigToFile(string filePath)
    {
        // 1. 创建默认配置模板
        var defaultConfig = GetDefaultConfig();

        // 2. 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 3. 处理文件内容
        SpecialKartConfig finalConfig;
        if (File.Exists(filePath))
        {
            // 3.1 读取现有配置
            var existingConfig = JsonHelper.DeserializeNoBom<SpecialKartConfig>(filePath);

            // 3.2 初始化现有配置的字典（避免null引用）
            existingConfig.SkillChange ??= new Dictionary<short, Dictionary<short, short>>();
            existingConfig.SkillMappings ??= new Dictionary<short, Dictionary<short, short>>();
            existingConfig.SkillAttacked ??= new Dictionary<short, Dictionary<short, short>>();

            // 3.3 补充缺失的描述文本
            existingConfig.SkillChangeDesc ??= defaultConfig.SkillChangeDesc;
            existingConfig.SkillMappingsDesc ??= defaultConfig.SkillMappingsDesc;
            existingConfig.SkillAttackedDesc ??= defaultConfig.SkillAttackedDesc;

            // 3.4 补充SkillChange中缺失的配置
            foreach (var (key, valueDict) in defaultConfig.SkillChange)
            {
                if (!existingConfig.SkillChange.ContainsKey(key))
                {
                    existingConfig.SkillChange[key] = new Dictionary<short, short>(valueDict);
                }
                else
                {
                    foreach (var (innerKey, innerValue) in valueDict)
                    {
                        if (!existingConfig.SkillChange[key].ContainsKey(innerKey))
                        {
                            existingConfig.SkillChange[key][innerKey] = innerValue;
                        }
                    }
                }
            }

            // 3.5 补充SkillMappings中缺失的配置
            foreach (var (key, valueDict) in defaultConfig.SkillMappings)
            {
                if (!existingConfig.SkillMappings.ContainsKey(key))
                {
                    existingConfig.SkillMappings[key] = new Dictionary<short, short>(valueDict);
                }
                else
                {
                    foreach (var (innerKey, innerValue) in valueDict)
                    {
                        if (!existingConfig.SkillMappings[key].ContainsKey(innerKey))
                        {
                            existingConfig.SkillMappings[key][innerKey] = innerValue;
                        }
                    }
                }
            }

            // 3.6 补充SkillAttacked中缺失的配置
            foreach (var (key, valueDict) in defaultConfig.SkillAttacked)
            {
                if (!existingConfig.SkillAttacked.ContainsKey(key))
                {
                    existingConfig.SkillAttacked[key] = new Dictionary<short, short>(valueDict);
                }
                else
                {
                    foreach (var (innerKey, innerValue) in valueDict)
                    {
                        if (!existingConfig.SkillAttacked[key].ContainsKey(innerKey))
                        {
                            existingConfig.SkillAttacked[key][innerKey] = innerValue;
                        }
                    }
                }
            }

            finalConfig = existingConfig;
            Console.WriteLine($"配置已更新（补充缺失内容）：{filePath}");
        }
        else
        {
            // 4. 文件不存在时直接使用默认配置
            finalConfig = defaultConfig;
            Console.WriteLine($"配置已创建：{filePath}");
        }

        // 5. 写入最终配置
        File.WriteAllText(filePath, JsonHelper.Serialize(finalConfig));
    }

    /// <summary>
    /// 从JSON文件读取特殊道具车配置
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <returns>特殊道具车配置对象（SpecialKartConfig）</returns>
    public static SpecialKartConfig LoadConfigFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("特殊道具车配置文件不存在", filePath);
        }

        var config = JsonHelper.DeserializeNoBom<SpecialKartConfig>(filePath);
        if (config == null)
        {
            throw new Exception("配置文件解析失败，可能是JSON格式错误");
        }

        // 确保字典不为null（避免后续使用时的null引用异常）
        config.SkillChange ??= new Dictionary<short, Dictionary<short, short>>();
        config.SkillMappings ??= new Dictionary<short, Dictionary<short, short>>();
        config.SkillAttacked ??= new Dictionary<short, Dictionary<short, short>>();

        Console.WriteLine($"道具车特性配置已成功从 {filePath} 读取");
        return config;
    }

    /// <summary>
    /// 创建默认配置模板（提取为单独方法便于维护）
    /// </summary>
    private static SpecialKartConfig GetDefaultConfig()
    {
        return new SpecialKartConfig
        {
            SkillChangeDesc = "特殊道具车：将指定道具变更为特殊道具",
            SkillChange = new Dictionary<short, Dictionary<short, short>>
            {
                { 1594, new Dictionary<short, short> { {3, 112} } },
                { 1593, new Dictionary<short, short> { {3, 112} } },
                { 1592, new Dictionary<short, short> { {3, 112} } },
                { 1591, new Dictionary<short, short> { {6, 31}, {5, 24} } },
                { 1590, new Dictionary<short, short> { {8, 17} } },
                { 1588, new Dictionary<short, short> { {7, 32}, {127, 32} } },
                { 1585, new Dictionary<short, short> { {4, 118} } },
                { 1579, new Dictionary<short, short> { {5, 103}, {6, 31} } },
                { 1575, new Dictionary<short, short> { {4, 119}, {9, 27} } },
                { 1571, new Dictionary<short, short> { {7, 32} } },
                { 1569, new Dictionary<short, short> { {4, 7} } },
                { 1567, new Dictionary<short, short> { {6, 31} } },
                { 1565, new Dictionary<short, short> { {33, 137}, {3, 137} } },
                { 1563, new Dictionary<short, short> { {7, 136}, {114, 16} } },
                { 1561, new Dictionary<short, short> { {8, 37}, {6, 31} } },
                { 1551, new Dictionary<short, short> { {8, 25} } },
                { 1543, new Dictionary<short, short> { {6, 31} } },
                { 1548, new Dictionary<short, short> { {4, 132} } },
                { 1536, new Dictionary<short, short> { {8, 17}, {5, 103} } },
                { 1526, new Dictionary<short, short> { {9, 27} } },
                { 1522, new Dictionary<short, short> { {9, 34}, {6, 31} } },
                { 1511, new Dictionary<short, short> { {2, 38} } },
                { 1510, new Dictionary<short, short> { {7, 32} } },
                { 1509, new Dictionary<short, short> { {7, 32} } },
                { 1507, new Dictionary<short, short> { {6, 31} } },
                { 1506, new Dictionary<short, short> { {5, 103} } },
                { 1505, new Dictionary<short, short> { {8, 129}, {4, 120} } },
                { 1502, new Dictionary<short, short> { {7, 4} } },
                { 1500, new Dictionary<short, short> { {9, 34}, {113, 135}, {33, 135} } },
                { 1496, new Dictionary<short, short> { {7, 134}, {6, 31} } },
                { 1494, new Dictionary<short, short> { {4, 132}, {6, 31} } },
                { 1491, new Dictionary<short, short> { {8, 82}, {9, 27}, {13, 28} } },
                { 1489, new Dictionary<short, short> { {9, 111}, {6, 31} } },
                { 1487, new Dictionary<short, short> { {5, 103}, {10, 36} } },
                { 1484, new Dictionary<short, short> { {7, 32}, {6, 31} } },
                { 1482, new Dictionary<short, short> { {5, 6} } },
                { 1481, new Dictionary<short, short> { {7, 102}, {9, 34} } },
                { 1479, new Dictionary<short, short> { {7, 131} } }
            },

            SkillMappingsDesc = "特殊道具车：使用指定道具后获得特殊道具",
            SkillMappings = new Dictionary<short, Dictionary<short, short>>
            {
                { 1590, new Dictionary<short, short> { {5, 10} } },
                { 1569, new Dictionary<short, short> { {5, 7} } },
                { 1567, new Dictionary<short, short> { {5, 31} } },
                { 1450, new Dictionary<short, short> { {7, 5}, {5, 24} } },
                { 1563, new Dictionary<short, short> { {136, 6} } },
                { 1548, new Dictionary<short, short> { {5, 6} } },
                { 1510, new Dictionary<short, short> { {32, 32} } },
                { 1507, new Dictionary<short, short> { {5, 31} } },
                { 1496, new Dictionary<short, short> { {5, 24} } },
                { 1489, new Dictionary<short, short> { {5, 10} } },
                { 1479, new Dictionary<short, short> { {131, 5} } }
            },

            SkillAttackedDesc = "特殊道具车：被指定道具攻击后获得特殊道具",
            SkillAttacked = new Dictionary<short, Dictionary<short, short>>
            {
                { 1588, new Dictionary<short, short> { {7, 32} } },
                { 1581, new Dictionary<short, short> { {5, 31}, {7, 31} } },
                { 1571, new Dictionary<short, short> { {8, 6} } },
                { 1561, new Dictionary<short, short> { {7, 111} } },
                { 1557, new Dictionary<short, short> { {7, 32}, {5, 103} } },
                { 1555, new Dictionary<short, short> { {4, 6}, {9, 6} } },
                { 1551, new Dictionary<short, short> { {7, 6} } },
                { 1524, new Dictionary<short, short> { {5, 103} } },
                { 1511, new Dictionary<short, short> { {7, 5} } },
                { 1510, new Dictionary<short, short> { {5, 10} } },
                { 1509, new Dictionary<short, short> { {5, 10} } },
                { 1506, new Dictionary<short, short> { {4, 6}, {9, 6} } },
                { 1502, new Dictionary<short, short> { {4, 9} } },
                { 1482, new Dictionary<short, short> { {4, 119}, {9, 119} } }
            }
        };
    }
}



