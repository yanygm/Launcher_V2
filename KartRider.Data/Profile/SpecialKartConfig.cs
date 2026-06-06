using System;
using System.Collections.Generic;
using System.IO;

namespace KartRider;

/// <summary>
/// 技能映射配置项（包含目标道具ID和触发概率）
/// </summary>
public class SkillMappingConfig
{
    /// <summary>
    /// 目标道具ID
    /// </summary>
    public short TargetItemId { get; set; }

    /// <summary>
    /// 触发概率（0-100，100表示100%触发）
    /// </summary>
    public byte Probability { get; set; } = 100;
}

/// <summary>
/// 特殊道具车配置类
/// </summary>
public class SpecialKartConfig
{
    /// <summary>
    /// 特殊道具车：将指定道具变更为特殊道具
    /// </summary>
    public string SkillChangeDesc { get; set; }
    public Dictionary<ushort, Dictionary<short, SkillMappingConfig>> SkillChange { get; set; } = new();

    /// <summary>
    /// 特殊道具车：使用指定道具后获得特殊道具
    /// </summary>
    public string SkillMappingsDesc { get; set; }
    public Dictionary<ushort, Dictionary<short, SkillMappingConfig>> SkillMappings { get; set; } = new();

    /// <summary>
    /// 特殊道具车：被指定道具攻击后获得特殊道具
    /// </summary>
    public string SkillAttackedDesc { get; set; }
    public Dictionary<ushort, Dictionary<short, SkillMappingConfig>> SkillAttacked { get; set; } = new();

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
            var existingConfig = JsonHelper.DeserializeNoBom<SpecialKartConfig>(filePath) ?? new SpecialKartConfig();

            // 3.2 初始化现有配置的字典（避免null引用）
            existingConfig.SkillChange ??= new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>();
            existingConfig.SkillMappings ??= new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>();
            existingConfig.SkillAttacked ??= new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>();

            // 3.3 补充缺失的描述文本
            existingConfig.SkillChangeDesc ??= defaultConfig.SkillChangeDesc;
            existingConfig.SkillMappingsDesc ??= defaultConfig.SkillMappingsDesc;
            existingConfig.SkillAttackedDesc ??= defaultConfig.SkillAttackedDesc;

            // 3.4 补充SkillChange中缺失的配置
            foreach (var (key, valueDict) in defaultConfig.SkillChange)
            {
                if (!existingConfig.SkillChange.ContainsKey(key))
                {
                    existingConfig.SkillChange[key] = new Dictionary<short, SkillMappingConfig>(valueDict);
                }
                else
                {
                    foreach (var (innerKey, innerValue) in valueDict)
                    {
                        if (!existingConfig.SkillChange[key].ContainsKey(innerKey))
                        {
                            existingConfig.SkillChange[key][innerKey] = new SkillMappingConfig
                            {
                                TargetItemId = innerValue.TargetItemId,
                                Probability = innerValue.Probability
                            };
                        }
                    }
                }
            }

            // 3.5 补充SkillMappings中缺失的配置
            foreach (var (key, valueDict) in defaultConfig.SkillMappings)
            {
                if (!existingConfig.SkillMappings.ContainsKey(key))
                {
                    existingConfig.SkillMappings[key] = new Dictionary<short, SkillMappingConfig>(valueDict);
                }
                else
                {
                    foreach (var (innerKey, innerValue) in valueDict)
                    {
                        if (!existingConfig.SkillMappings[key].ContainsKey(innerKey))
                        {
                            existingConfig.SkillMappings[key][innerKey] = new SkillMappingConfig
                            {
                                TargetItemId = innerValue.TargetItemId,
                                Probability = innerValue.Probability
                            };
                        }
                    }
                }
            }

            // 3.6 补充SkillAttacked中缺失的配置
            foreach (var (key, valueDict) in defaultConfig.SkillAttacked)
            {
                if (!existingConfig.SkillAttacked.ContainsKey(key))
                {
                    existingConfig.SkillAttacked[key] = new Dictionary<short, SkillMappingConfig>(valueDict);
                }
                else
                {
                    foreach (var (innerKey, innerValue) in valueDict)
                    {
                        if (!existingConfig.SkillAttacked[key].ContainsKey(innerKey))
                        {
                            existingConfig.SkillAttacked[key][innerKey] = new SkillMappingConfig
                            {
                                TargetItemId = innerValue.TargetItemId,
                                Probability = innerValue.Probability
                            };
                        }
                    }
                }
            }

            finalConfig = existingConfig;
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

        SpecialKartConfig config;
        try
        {
            config = JsonHelper.DeserializeNoBom<SpecialKartConfig>(filePath);
            if (config == null)
            {
                throw new Exception("配置文件解析结果为null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置文件格式不正确或解析失败: {ex.Message}");
            Console.WriteLine("将使用默认配置覆盖本地文件...");

            // 使用默认配置覆盖本地文件
            config = GetDefaultConfig();
            File.WriteAllText(filePath, JsonHelper.Serialize(config));
            Console.WriteLine($"本地文件已用默认配置覆盖: {filePath}");

            return config;
        }

        // 确保字典不为null（避免后续使用时的null引用异常）
        config.SkillChange ??= new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>();
        config.SkillMappings ??= new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>();
        config.SkillAttacked ??= new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>();

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
            SkillChange = new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>
            {
                { 1620, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 117, Probability = 100 }} } },
                { 1615, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }}, {7, new SkillMappingConfig { TargetItemId = 99, Probability = 100 }} } },
                { 1612, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }} } },
                { 1610, new Dictionary<short, SkillMappingConfig> { {9, new SkillMappingConfig { TargetItemId = 34, Probability = 100 }} } },
                { 1605, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 118, Probability = 100 }} } },
                { 1601, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 131, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1600, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1597, new Dictionary<short, SkillMappingConfig> { {9, new SkillMappingConfig { TargetItemId = 34, Probability = 100 }} } },
                { 1594, new Dictionary<short, SkillMappingConfig> { {3, new SkillMappingConfig { TargetItemId = 112, Probability = 100 }} } },
                { 1593, new Dictionary<short, SkillMappingConfig> { {3, new SkillMappingConfig { TargetItemId = 112, Probability = 100 }} } },
                { 1592, new Dictionary<short, SkillMappingConfig> { {3, new SkillMappingConfig { TargetItemId = 112, Probability = 100 }} } },
                { 1591, new Dictionary<short, SkillMappingConfig> { {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }}, {5, new SkillMappingConfig { TargetItemId = 24, Probability = 100 }} } },
                { 1590, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 17, Probability = 100 }} } },
                { 1588, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }}, {127, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }} } },
                { 1585, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 118, Probability = 100 }} } },
                { 1579, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1575, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 119, Probability = 100 }}, {9, new SkillMappingConfig { TargetItemId = 27, Probability = 100 }} } },
                { 1571, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }} } },
                { 1569, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 7, Probability = 100 }} } },
                { 1567, new Dictionary<short, SkillMappingConfig> { {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1565, new Dictionary<short, SkillMappingConfig> { {33, new SkillMappingConfig { TargetItemId = 137, Probability = 100 }}, {3, new SkillMappingConfig { TargetItemId = 137, Probability = 100 }} } },
                { 1563, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 136, Probability = 100 }}, {114, new SkillMappingConfig { TargetItemId = 16, Probability = 100 }} } },
                { 1561, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 37, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1551, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 25, Probability = 100 }} } },
                { 1543, new Dictionary<short, SkillMappingConfig> { {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1548, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 132, Probability = 100 }} } },
                { 1536, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 17, Probability = 100 }}, {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }} } },
                { 1526, new Dictionary<short, SkillMappingConfig> { {9, new SkillMappingConfig { TargetItemId = 27, Probability = 100 }} } },
                { 1522, new Dictionary<short, SkillMappingConfig> { {9, new SkillMappingConfig { TargetItemId = 34, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1511, new Dictionary<short, SkillMappingConfig> { {2, new SkillMappingConfig { TargetItemId = 38, Probability = 100 }} } },
                { 1510, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }} } },
                { 1509, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }} } },
                { 1507, new Dictionary<short, SkillMappingConfig> { {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1506, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }} } },
                { 1505, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 129, Probability = 100 }}, {4, new SkillMappingConfig { TargetItemId = 120, Probability = 100 }} } },
                { 1502, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 4, Probability = 100 }} } },
                { 1500, new Dictionary<short, SkillMappingConfig> { {9, new SkillMappingConfig { TargetItemId = 34, Probability = 100 }}, {113, new SkillMappingConfig { TargetItemId = 135, Probability = 100 }}, {33, new SkillMappingConfig { TargetItemId = 135, Probability = 100 }} } },
                { 1496, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 134, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1494, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 132, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1491, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 82, Probability = 100 }}, {9, new SkillMappingConfig { TargetItemId = 27, Probability = 100 }}, {13, new SkillMappingConfig { TargetItemId = 28, Probability = 100 }} } },
                { 1489, new Dictionary<short, SkillMappingConfig> { {9, new SkillMappingConfig { TargetItemId = 111, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1487, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }}, {10, new SkillMappingConfig { TargetItemId = 36, Probability = 100 }} } },
                { 1484, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }}, {6, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1482, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1481, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 102, Probability = 100 }}, {9, new SkillMappingConfig { TargetItemId = 34, Probability = 100 }} } },
                { 1479, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 131, Probability = 100 }} } }
            },

            SkillMappingsDesc = "特殊道具车：使用指定道具后获得特殊道具",
            SkillMappings = new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>
            {
                { 1607, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1601, new Dictionary<short, SkillMappingConfig> { {131, new SkillMappingConfig { TargetItemId = 5, Probability = 100 }} } },
                { 1597, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1590, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 10, Probability = 100 }} } },
                { 1569, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 7, Probability = 100 }} } },
                { 1567, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1450, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 5, Probability = 100 }}, {5, new SkillMappingConfig { TargetItemId = 24, Probability = 100 }} } },
                { 1563, new Dictionary<short, SkillMappingConfig> { {136, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1548, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1510, new Dictionary<short, SkillMappingConfig> { {32, new SkillMappingConfig { TargetItemId = 32, Probability = 60 }} } },
                { 1507, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1496, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 24, Probability = 100 }} } },
                { 1489, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 10, Probability = 100 }} } },
                { 1479, new Dictionary<short, SkillMappingConfig> { {131, new SkillMappingConfig { TargetItemId = 5, Probability = 100 }} } }
            },

            SkillAttackedDesc = "特殊道具车：被指定道具攻击后获得特殊道具",
            SkillAttacked = new Dictionary<ushort, Dictionary<short, SkillMappingConfig>>
            {
                { 1613, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 10, Probability = 100 }} } },
                { 1610, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1607, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 5, Probability = 100 }} } },
                { 1605, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 111, Probability = 100 }} } },
                { 1600, new Dictionary<short, SkillMappingConfig> { {32, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }}, {99, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1588, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }} } },
                { 1581, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }}, {7, new SkillMappingConfig { TargetItemId = 31, Probability = 100 }} } },
                { 1571, new Dictionary<short, SkillMappingConfig> { {8, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1561, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 111, Probability = 40 }} } },
                { 1557, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 32, Probability = 100 }}, {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }} } },
                { 1555, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }}, {9, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1551, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1524, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 103, Probability = 100 }} } },
                { 1511, new Dictionary<short, SkillMappingConfig> { {7, new SkillMappingConfig { TargetItemId = 5, Probability = 100 }} } },
                { 1510, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 10, Probability = 100 }} } },
                { 1509, new Dictionary<short, SkillMappingConfig> { {5, new SkillMappingConfig { TargetItemId = 10, Probability = 100 }} } },
                { 1506, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }}, {9, new SkillMappingConfig { TargetItemId = 6, Probability = 100 }} } },
                { 1502, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 9, Probability = 100 }} } },
                { 1482, new Dictionary<short, SkillMappingConfig> { {4, new SkillMappingConfig { TargetItemId = 119, Probability = 100 }}, {9, new SkillMappingConfig { TargetItemId = 119, Probability = 100 }} } }
            }
        };
    }
}