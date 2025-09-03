using Newtonsoft.Json;
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
    public Dictionary<short, Dictionary<short, short>> SkillChange { get; set; }

    /// <summary>
    /// 特殊道具车：使用指定道具后获得特殊道具
    /// </summary>
    public string SkillMappingsDesc { get; set; }
    public Dictionary<short, Dictionary<short, short>> SkillMappings { get; set; }

    /// <summary>
    /// 特殊道具车：被指定道具攻击后获得特殊道具
    /// </summary>
    public string SkillAttackedDesc { get; set; }
    public Dictionary<short, Dictionary<short, short>> SkillAttacked { get; set; }

    /// <summary>
    /// 将特殊道具车配置存储到JSON文件
    /// </summary>
    /// <param name="filePath">文件路径（如：./Config/SpecialKartConfig.json）</param>
    public static void SaveConfigToFile(string filePath)
    {
        // 1. 初始化配置对象，填充原代码中的数据
        var config = new SpecialKartConfig
        {
            SkillChangeDesc = "特殊道具车：将指定道具变更为特殊道具",
            SkillChange = new Dictionary<short, Dictionary<short, short>>
            {
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

        // 2. 确保目录存在（若不存在则创建）
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 3. 若文件不存在，则将配置对象序列化为JSON并写入文件
        if (!File.Exists(filePath))
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented); // Formatting.Indented：格式化JSON（易读）
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

            Console.WriteLine($"配置已成功保存到：{filePath}");
        }
    }

    /// <summary>
    /// 从JSON文件读取特殊道具车配置
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <returns>特殊道具车配置对象（SpecialKartConfig）</returns>
    public static SpecialKartConfig LoadConfigFromFile(string filePath)
    {
        // 1. 检查文件是否存在
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("特殊道具车配置文件不存在", filePath);
        }

        // 2. 读取JSON文本
        string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        // 3. 反序列化为配置对象
        var config = JsonConvert.DeserializeObject<SpecialKartConfig>(json);
        if (config == null)
        {
            throw new Exception("配置文件解析失败，可能是JSON格式错误");
        }

        Console.WriteLine($"配置已成功从 {filePath} 读取");
        return config;
    }
}
