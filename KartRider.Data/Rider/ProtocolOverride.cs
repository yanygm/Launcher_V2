using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KartRider.Logging;

namespace RiderData
{
    /// <summary>
    /// 协议字段调试覆盖：在服务端目录放一个 override.ini，按 key=value 覆盖个别字节，
    /// 改完不需要重启服务端，重新进对应界面即生效（文件按修改时间自动重载）。
    /// 仅用于核对客户端字段含义，文件不存在时全部走代码里的默认值。
    /// </summary>
    public static class ProtocolOverride
    {
        private const string FileName = "override.ini";

        private static string _path;
        private static DateTime _stamp;
        private static Dictionary<string, byte> _values = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static bool _missingLogged;

        public static byte Get(string key, byte fallback)
        {
            Load();
            if (_values.TryGetValue(key, out byte value))
            {
                return value;
            }
            return fallback;
        }

        private static string Resolve()
        {
            // 发布/清理会把 exe 目录里的文件删掉，所以同时找上级目录和当前目录
            var candidates = new List<string>();
            string baseDir = AppContext.BaseDirectory;
            candidates.Add(Path.Combine(baseDir, FileName));
            try
            {
                var parent = Directory.GetParent(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (parent != null)
                {
                    candidates.Add(Path.Combine(parent.FullName, FileName));
                }
            }
            catch
            {
            }
            try
            {
                candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), FileName));
            }
            catch
            {
            }
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        private static void Load()
        {
            string path = Resolve();
            if (path == null)
            {
                if (_path != null || !_missingLogged)
                {
                    _missingLogged = true;
                    Log.Info($"[Override] 未找到 {FileName}（查找目录: {AppContext.BaseDirectory} 及其上级），全部使用默认值");
                }
                _path = null;
                _values = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
                return;
            }
            DateTime stamp = File.GetLastWriteTimeUtc(path);
            if (path == _path && stamp == _stamp)
            {
                return;
            }

            var values = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("//"))
                    {
                        continue;
                    }
                    int index = line.IndexOf('=');
                    if (index <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, index).Trim();
                    string text = line.Substring(index + 1).Trim();
                    if (byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value))
                    {
                        values[key] = value;
                    }
                }
                Log.Info($"[Override] 已加载 {path}: {string.Join(", ", values.Select(kv => $"{kv.Key}={kv.Value}"))}");
            }
            catch (Exception ex)
            {
                Log.Info($"[Override] 读取 {FileName} 失败: {ex.Message}");
            }

            _values = values;
            _path = path;
            _stamp = stamp;
        }
    }
}
