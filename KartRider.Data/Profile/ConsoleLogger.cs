using System;
using System.IO;
using System.Text;

namespace LoggerLibrary
{
    /// <summary>
    /// 控制台日志记录器，用于将控制台输出缓存并在崩溃时保存到日志文件。
    /// </summary>
    public class CachedConsoleWriter : TextWriter
    {
        public static CachedConsoleWriter cachedWriter;
        private readonly TextWriter _originalOut;
        private readonly StringBuilder _cache;

        public string Cache => _cache.ToString();

        public void ClearCache() => _cache.Clear();

        public CachedConsoleWriter(TextWriter originalOut)
        {
            _originalOut = originalOut;
            _cache = new StringBuilder();
        }

        public override Encoding Encoding => _originalOut.Encoding;

        public override void Write(char value)
        {
            _originalOut.Write(value);
            _cache.Append(value);
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

        public static void SaveToFile()
        {
            try
            {
                if (cachedWriter == null || string.IsNullOrEmpty(cachedWriter.Cache))
                    return;

                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}.log";
                File.WriteAllText(fileName, cachedWriter.Cache);
            }
            catch (Exception ex)
            {
                // 保存失败时不再触发 Console.WriteLine 避免递归
                System.Diagnostics.Debug.WriteLine($"保存日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 立即同步保存缓存到文件并清空，用于 UnhandledException 等进程崩溃场景。
        /// </summary>
        public static void ForceSaveAndClear()
        {
            if (cachedWriter == null || string.IsNullOrEmpty(cachedWriter.Cache))
                return;

            SaveToFile();
            cachedWriter.ClearCache();
        }
    }
}
