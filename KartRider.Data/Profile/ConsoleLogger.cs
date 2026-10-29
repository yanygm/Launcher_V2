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

        // ---- 自动落盘：把控制台每一行同时写进 AutoLogs\Server_时间.log ----
        // 目的：联机时出问题（结算包异常、掉线等）不再依赖手动点「保存日志」，
        // 事后直接看文件里的十六进制包就能定位。
        private static StreamWriter _autoLog;
        private static readonly object _autoLogLock = new object();
        private static bool _autoLogOpened;
        private static long _autoLogBytes;
        private const long AutoLogMaxBytes = 40L * 1024 * 1024; // 单文件上限 40MB

        private static void AutoLogLine(string line)
        {
            if (_autoLogBytes > AutoLogMaxBytes)
            {
                return;
            }
            try
            {
                lock (_autoLogLock)
                {
                    if (!_autoLogOpened)
                    {
                        _autoLogOpened = true;
                        try
                        {
                            string dir = Path.Combine(AppContext.BaseDirectory, "AutoLogs");
                            Directory.CreateDirectory(dir);
                            string file = Path.Combine(dir, $"Server_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                            _autoLog = new StreamWriter(new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false))
                            {
                                AutoFlush = true
                            };
                        }
                        catch
                        {
                            _autoLog = null;
                        }
                    }
                    if (_autoLog != null)
                    {
                        _autoLog.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
                        _autoLogBytes += line.Length + 12;
                    }
                }
            }
            catch
            {
                // 落盘失败不能影响主流程
            }
        }

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
            // AutoLogLine(value);
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
