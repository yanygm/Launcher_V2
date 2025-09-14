using System;
using System.IO;
using System.Text;

namespace LoggerLibrary
{
    /// <summary>
    /// 控制台日志记录器，用于将控制台输出同时保存到日志文件
    /// </summary>
    // 自定义文本编写器，用于缓存控制台输出
    public class CachedConsoleWriter : TextWriter
    {
        public static CachedConsoleWriter cachedWriter;
        private readonly TextWriter _originalOut;
        private readonly StringBuilder _cache;
    
        // 提供对缓存的访问
        public string Cache => _cache.ToString();
    
        // 清空缓存
        public void ClearCache() => _cache.Clear();

        public CachedConsoleWriter(TextWriter originalOut)
        {
            _originalOut = originalOut;
            _cache = new StringBuilder();
        }

        public override Encoding Encoding => _originalOut.Encoding;

        public override void Write(char value)
        {
            _originalOut.Write(value);  // 输出到控制台
            _cache.Append(value);       // 同时写入缓存
        }

        public override void Write(string value)
        {
            _originalOut.Write(value);
            _cache.Append(value);
        }

        public override void WriteLine(string value)
        {
            _originalOut.WriteLine(value);
            _cache.AppendLine(value);
        }

        // 将缓存内容保存到文件
        public static void SaveToFile()
        {
            try
            {
                // 生成带时间戳的文件名
                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}.log";
                File.WriteAllText(fileName, cachedWriter.Cache);
                Console.WriteLine($"控制台输出到文件: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存文件失败: {ex.Message}");
            }
        }
    }
}
