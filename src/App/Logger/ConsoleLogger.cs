using System.Text;

namespace Launcher.App.Logger
{
    /// <summary>
    /// 控制台日志记录器，用于将控制台输出同时保存到日志文件
    /// </summary>
    // 自定义文本编写器，用于缓存控制台输出
    public class CachedConsoleWriter : TextWriter
    {
        #region color

        const string YELLOW = "\u001b[33m";
        const string RED = "\u001b[31m";
        const string RESET = "\u001b[0m";

        #endregion

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

        public void Warn(string message)
        {
            string warningMessage = $"[WARNING] {message}";
            warningMessage = YELLOW + warningMessage + RESET;
            _originalOut.WriteLine(warningMessage);
            _cache.AppendLine(warningMessage);
        }

        public void Error(string message)
        {
            string errorMessage = $"[ERROR] {message}";
            errorMessage = RED + errorMessage + RESET;
            _originalOut.WriteLine(errorMessage);
            _cache.AppendLine(errorMessage);
        }

        public void Info(string message)
        {
            string infoMessage = $"[INFO] {message}";
            _originalOut.WriteLine(infoMessage);
            _cache.AppendLine(infoMessage);
        }

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

        /// <summary>
        /// 将缓存内容保存到文件
        /// </summary>
        /// <returns>日志文件名</returns>
        public static string SaveToFile()
        {
            try
            {
                // 生成带时间戳的文件名
                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}.log";
                File.WriteAllText(fileName, cachedWriter.Cache);
                Console.WriteLine($"控制台输出到文件: {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存日志文件时发生错误: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
