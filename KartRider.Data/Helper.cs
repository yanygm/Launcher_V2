using System;
using System.IO;
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
    /// 序列化对象为 JSON 字符串（字符串本身无 BOM，BOM 仅存在于字节流中）
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

        if (!IsValidJsonFile("", json))
            return default;

        options ??= _defaultOptions;
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// 从【无 BOM 的 UTF-8 字节流】反序列化对象
    /// </summary>
    public static T DeserializeNoBom<T>(string FileName, JsonSerializerOptions options = null)
    {
        if (string.IsNullOrEmpty(FileName))
            return default;

        if (!IsValidJsonFile(FileName))
            return default;

        byte[] utf8Bytes = File.ReadAllBytes(FileName);
        if (utf8Bytes == null || utf8Bytes.Length == 0)
            return default;

        options ??= _defaultOptions;
        // 直接反序列化字节流（无需先转字符串，更高效）
        return JsonSerializer.Deserialize<T>(utf8Bytes, options);
    }

    public static bool IsValidJsonFile(string filePath, string jsonContent = null)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonContent))
                jsonContent = File.ReadAllText(filePath);
            // 尝试解析（仅验证格式，不反序列化具体对象）
            using (var doc = JsonDocument.Parse(jsonContent))
            {
                return true; // 解析成功
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON 格式错误：{ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"文件异常：{ex.Message}");
            return false;
        }
    }
}
