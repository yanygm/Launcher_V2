using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KartRider;

public class DataPacket
{
    public string Nickname { get; set; }
    public long TimeTicks { get; set; }
}

public static class Base64Helper
{
    // Base64 加密
    public static string Encode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // 将字符串转换为 UTF8 字节数组
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        // 转换为 Base64 字符串
        return Convert.ToBase64String(bytes);
    }

    // Base64 解密
    public static string Decode(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return string.Empty;

        try
        {
            // 将 Base64 字符串转换为字节数组
            byte[] bytes = Convert.FromBase64String(base64String);
            // 将字节数组转换为 UTF8 字符串
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // 处理无效的 Base64 格式
            return string.Empty;
            Console.WriteLine("输入不是有效的 Base64 字符串", nameof(base64String));
        }
    }
}

// JSON 序列化工具类
public static class JsonHelper
{
    // 静态默认配置
    private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 避免中文转义（可选）
    };

    /// <summary>
    /// 序列化对象为 JSON 字符串（字符串本身无 BOM, BOM 仅存在于字节流中）
    /// </summary>
    public static string Serialize<T>(T obj, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// 反序列化 JSON 字符串为对象
    /// </summary>
    public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        if (!EnsureJsonUtf8NoBom("", out string validJson, json))
            return default;

        if (validJson == null || validJson.Length == 0)
            return default;

        options ??= _defaultOptions;
        return JsonSerializer.Deserialize<T>(validJson, options);
    }

    /// <summary>
    /// 从【无 BOM 的 UTF-8 字节流】反序列化对象
    /// </summary>
    public static T DeserializeNoBom<T>(string FileName, JsonSerializerOptions options = null)
    {
        if (string.IsNullOrEmpty(FileName))
            return default;

        if (!EnsureJsonUtf8NoBom(FileName, out string validJson))
            return default;

        if (validJson == null || validJson.Length == 0)
            return default;

        options ??= _defaultOptions;
        return JsonSerializer.Deserialize<T>(validJson, options);
    }

    public static bool EnsureJsonUtf8NoBom(string filePath, out string jsonData, string jsonContent = null, bool validateJson = true)
    {
        try
        {
            // 1. 确定要写入的内容
            string contentToWrite;
            if (!string.IsNullOrEmpty(jsonContent))
            {
                contentToWrite = jsonContent;
            }
            else
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"错误: 文件 '{filePath}' 不存在, 且未提供 jsonContent.");
                    jsonData = null;
                    return false;
                }
                // 读取时尝试自动检测编码, 以防原文件编码混乱
                contentToWrite = ReadFileWithEncodingDetection(filePath);
            }

            // 2. （可选）验证 JSON 格式
            if (validateJson && !IsValidJson(contentToWrite))
            {
                Console.WriteLine("JSON 格式无效, 已中止保存.");
                jsonData = null;
                return false;
            }

            jsonData = contentToWrite;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EnsureJsonUtf8NoBom: {ex.Message}");
            jsonData = null;
            return false;
        }
    }

    /// <summary>
    /// 尝试检测文件编码并读取内容.
    /// </summary>
    private static string ReadFileWithEncodingDetection(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);

        // 检查 UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        // 检查 UTF-16 LE BOM
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        // 检查 UTF-16 BE BOM
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        // 尝试解码为 UTF-8
        bool isUtf8 = IsValidUtf8(bytes);
        // 尝试解码为 GBK
        bool isGbk = IsValidGbk(bytes);

        if (isUtf8 && isGbk)
        {
            // 如果两种编码都能成功解码，优先选择 UTF-8（更现代、更通用）
            return Encoding.UTF8.GetString(bytes);
        }
        else if (isUtf8)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        else if (isGbk)
        {
            return Encoding.GetEncoding("GBK").GetString(bytes);
        }
        else
        {
            // 如果都失败，回退到系统默认编码，并给出警告
            Console.WriteLine("警告：无法可靠检测编码，将使用系统默认编码。可能导致乱码。");
            return Encoding.Default.GetString(bytes);
        }
    }

    /// <summary>
    /// 检查字节数组是否是有效的 UTF-8 编码。
    /// </summary>
    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            // 如果解码后再编码能还原原始字节，则认为是有效的
            string s = Encoding.UTF8.GetString(bytes);
            return Encoding.UTF8.GetBytes(s).SequenceEqual(bytes);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查字节数组是否是有效的 GBK 编码。
    /// </summary>
    private static bool IsValidGbk(byte[] bytes)
    {
        try
        {
            Encoding gbk = Encoding.GetEncoding("GBK");
            string s = gbk.GetString(bytes);
            return gbk.GetBytes(s).SequenceEqual(bytes);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证 JSON 字符串格式是否有效.
    /// </summary>
    private static bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return false;
        }
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
