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

    // 无 BOM 的 UTF-8 编码（全局复用，避免重复创建）
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);

    /// <summary>
    /// 序列化对象为 JSON 字符串（字符串本身无 BOM，BOM 仅存在于字节流中）
    /// </summary>
    public static string Serialize<T>(T obj, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// 序列化对象为【无 BOM 的 UTF-8 字节流】（适合直接写入文件/网络）
    /// </summary>
    public static byte[] SerializeNoBom<T>(T obj, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        // 先序列化为字符串，再用无 BOM 编码转换为字节（推荐）
        string json = JsonSerializer.Serialize(obj, options);
        return _utf8NoBom.GetBytes(json);

        // 若直接用 SerializeNoBom，需注意：
        // 该方法默认生成的字节流无 BOM（内部使用无 BOM 的 UTF-8），但文档未明确保证，因此上述方法更稳妥
    }

    /// <summary>
    /// 反序列化 JSON 字符串为对象
    /// </summary>
    public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        options ??= _defaultOptions;
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// 从【无 BOM 的 UTF-8 字节流】反序列化对象
    /// </summary>
    public static T DeserializeNoBom<T>(string FileName, JsonSerializerOptions options = null)
    {
        byte[] utf8Bytes = File.ReadAllBytes(FileName);
        if (utf8Bytes == null || utf8Bytes.Length == 0)
            return default;

        options ??= _defaultOptions;
        // 直接反序列化字节流（无需先转字符串，更高效）
        return JsonSerializer.Deserialize<T>(utf8Bytes, options);
    }
}
