using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KartRider.Logging;

namespace LoggerLibrary
{
    /// <summary>
    /// 控制台重定向 + 崩溃转储。
    ///
    /// 原实现把所有 Console 输出无界追加进 StringBuilder（且非线程安全），
    /// 每包一行 hex 时一小时能涨到 GB 级。
    /// 现在：固定容量环形缓冲（只保留最近 N 行，供崩溃时落盘），写入加锁。
    /// </summary>
    public class CachedConsoleWriter : TextWriter
    {
        private const int MaxLines = 50000;
        private const int MaxLineLength = 4096;

        public static CachedConsoleWriter cachedWriter;

        private readonly TextWriter _originalOut;
        private readonly Queue<string> _lines = new Queue<string>(1024);
        private readonly object _gate = new object();

        public CachedConsoleWriter(TextWriter originalOut)
        {
            _originalOut = originalOut;
        }

        public override Encoding Encoding => _originalOut.Encoding;

        public string Cache
        {
            get
            {
                lock (_gate)
                {
                    var sb = new StringBuilder();
                    foreach (string line in _lines)
                    {
                        sb.AppendLine(line);
                    }
                    return sb.ToString();
                }
            }
        }

        public void ClearCache()
        {
            lock (_gate)
            {
                _lines.Clear();
            }
        }

        public override void Write(char value)
        {
            try
            {
                _originalOut.Write(value);
            }
            catch
            {
            }
            Append(value.ToString());
        }

        public override void Write(string value)
        {
            try
            {
                _originalOut.Write(value);
            }
            catch
            {
            }
            Append(value);
        }

        public override void WriteLine(string value)
        {
            try
            {
                _originalOut.WriteLine(value);
            }
            catch
            {
            }
            Append(value);
        }

        private void Append(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (text.Length > MaxLineLength)
            {
                text = text.Substring(0, MaxLineLength) + "…(截断)";
            }
            lock (_gate)
            {
                if (_lines.Count >= MaxLines)
                {
                    _lines.Dequeue();
                }
                _lines.Enqueue(text.TrimEnd('\r', '\n'));
            }
        }

        public static void SaveToFile()
        {
            try
            {
                if (cachedWriter == null)
                {
                    return;
                }
                string text = cachedWriter.Cache;
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
                string fileName = Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yyyyMMddHHmmss}.log");
                // 带 BOM：避免 Notepad / PowerShell 5.1 把 UTF-8 当 ANSI 显示成乱码
                File.WriteAllText(fileName, text, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存日志失败: {ex.Message}");
            }
        }

        public static void ForceSaveAndClear()
        {
            if (cachedWriter == null)
            {
                return;
            }
            SaveToFile();
            cachedWriter.ClearCache();
        }
    }
}
