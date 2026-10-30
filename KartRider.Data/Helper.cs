using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using KartRider.Logging;

namespace KartRider;

public class DataPacket
{
    public string Nickname { get; set; }
    public ushort ClientVersion { get; set; }
    public string CompileTime { get; set; }
}

public static class Base64Helper
{
    // Base64 加密
    public static string Encode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes);
    }

    // Base64 解密
    public static string Decode(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return string.Empty;

        try
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// 安全的 uint 反序列化转换器，处理越界数值和异常格式
/// </summary>
public class UInt32SafeConverter : System.Text.Json.Serialization.JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt32(out uint value))
                return value;
            if (reader.TryGetInt64(out long longValue))
            {
                if (longValue < 0)
                    return 0;
                if (longValue > uint.MaxValue)
                    return uint.MaxValue;
                return (uint)longValue;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (uint.TryParse(str, out uint parsedValue))
                return parsedValue;
            if (long.TryParse(str, out long longValue))
            {
                if (longValue < 0) return 0;
                if (longValue > uint.MaxValue) return uint.MaxValue;
                return (uint)longValue;
            }
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

// JSON 序列化工具类
public static class JsonHelper
{
    private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static JsonHelper()
    {
        _defaultOptions.Converters.Add(new UInt32SafeConverter());
    }

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static string Serialize<T>(T obj, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        options ??= _defaultOptions;
        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException ex)
        {
            Log.Warn($"[Json] 反序列化失败: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 从文件反序列化。
    /// 与旧实现的差异：不再为了"验证格式"额外跑一遍 JsonDocument.Parse，
    /// 也不再对整份文本做 UTF-8/GBK 的双向往返比对（那是 O(2n) 的纯浪费）。
    /// 失败时返回 null 并把损坏文件备份为 .bad，绝不返回"看起来正常的空对象"。
    /// </summary>
    public static T DeserializeNoBom<T>(string FileName, JsonSerializerOptions options = null)
    {
        if (string.IsNullOrEmpty(FileName))
            return default;

        try
        {
            if (!File.Exists(FileName))
                return default;

            string json = ReadAllTextDetect(FileName);
            if (string.IsNullOrWhiteSpace(json))
                return default;

            options ??= _defaultOptions;
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException ex)
        {
            Log.Warn($"[Json] {FileName} 解析失败: {ex.Message}（已备份为 .bad）");
            TryBackup(FileName);
            return default;
        }
        catch (Exception ex)
        {
            Log.Warn($"[Json] {FileName} 读取失败: {ex.Message}");
            return default;
        }
    }

    private static void TryBackup(string path)
    {
        try
        {
            string backup = path + ".bad";
            File.Copy(path, backup, true);
        }
        catch
        {
        }
    }

    /// <summary>
    /// 原子写：先写 .tmp，再替换目标文件（.NET 会保留原文件的原子性语义），
    /// 避免写一半崩溃导致存档损坏。
    /// </summary>
    public static void WriteAllTextAtomic(string path, string content)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(false));

        if (!File.Exists(path))
        {
            File.Move(tmp, path);
            return;
        }

        try
        {
            File.Move(tmp, path, true);
        }
        catch (PlatformNotSupportedException)
        {
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
        catch (IOException)
        {
            // 目标被占用（杀软/编辑器）时退化为覆盖写
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
    }

    public static bool EnsureJsonUtf8NoBom(string filePath, out string jsonData, string jsonContent = null, bool validateJson = true)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonContent))
            {
                jsonData = jsonContent;
                return true;
            }
            if (!File.Exists(filePath))
            {
                jsonData = null;
                return false;
            }
            jsonData = ReadAllTextDetect(filePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"[Json] EnsureJsonUtf8NoBom: {filePath} {ex.Message}");
            jsonData = null;
            return false;
        }
    }

    /// <summary>
    /// 读文本并处理 BOM/GBK。旧实现用"解码后再编码比对"判断编码，
    /// 这里改用严格 UTF-8 解码：失败即视为 GBK，单次 O(n)。
    /// </summary>
    public static string ReadAllTextDetect(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            try
            {
                return Encoding.GetEncoding("GBK").GetString(bytes);
            }
            catch
            {
                return Encoding.Default.GetString(bytes);
            }
        }
    }

    public static string GetFilePath()
    {
        string filePath = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            filePath = Process.GetCurrentProcess().MainModule.FileName;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Log.Warn("[Json] 无法获取有效的程序集路径");
                return null;
            }
        }
        return filePath;
    }
}
