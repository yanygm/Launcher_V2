using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// ModelMax.xml 的只读缓存表（id → modelMaxX/modelMaxY）。
    ///
    /// 原实现的问题（也是"同车不同值"的根源）：
    ///   1. 每个 KartSpec 实例都 File.OpenRead + XDocument.Load 一次整份 XML，
    ///      8 人房一场比赛要解析十几遍；文件被临时占用时该玩家会退化到 0；
    ///   2. 查找用 `(short)kart.Attribute("id")`，id &gt; 32767 时永远匹配不上；
    ///   3. 缺失 id 时静默返回 0，没有任何"哪些车没配"的诊断。
    ///
    /// 现在：按文件时间戳缓存、int 主键、缺失 id 只告警一次，并提供覆盖率校验。
    /// </summary>
    public static class ModelMaxTable
    {
        private static readonly object Gate = new object();
        private static Dictionary<int, (float X, float Y)> _map = new Dictionary<int, (float X, float Y)>();
        private static DateTime _fileStamp = DateTime.MinValue;
        private static string _loadedPath;
        private static bool _loaded;

        private static readonly HashSet<int> WarnedMissing = new HashSet<int>();
        private static bool _validated;
        private static readonly object WarnGate = new object();

        public static int Count
        {
            get
            {
                lock (Gate)
                {
                    return _map.Count;
                }
            }
        }

        /// <summary>文件被改写后调用，强制下次重新加载。</summary>
        public static void Invalidate()
        {
            lock (Gate)
            {
                _loaded = false;
                _fileStamp = DateTime.MinValue;
                _loadedPath = null;
            }
        }

        public static bool TryGet(int kartId, out float modelMaxX, out float modelMaxY)
        {
            modelMaxX = 0f;
            modelMaxY = 0f;

            Dictionary<int, (float X, float Y)> map = GetMap();
            if (map != null && map.TryGetValue(kartId, out var value))
            {
                modelMaxX = value.X;
                modelMaxY = value.Y;
                return true;
            }
            return false;
        }

        /// <summary>记录一次"该 id 没有 modelMax"（每个 id 只提示一次，避免刷屏）。</summary>
        public static void ReportMissing(int kartId)
        {
            bool first = false;
            lock (WarnGate)
            {
                if (WarnedMissing.Count < 500 && WarnedMissing.Add(kartId))
                {
                    first = true;
                }
            }
            if (first)
            {
                Log.Debug($"[ModelMax] 卡丁车 {kartId} 无模型尺寸配置，发送 0（客户端将使用自身默认值）");
            }
        }

        /// <summary>
        /// 一次性覆盖率校验：把"已知卡丁车表"与 ModelMax.xml 对比，
        /// 直接告诉运维哪些车会退化成 0（这就是"部分车辆有数据、其他车辆没有"的可见证据）。
        /// </summary>
        public static void ValidateOnce(IEnumerable<int> knownKartIds)
        {
            if (_validated || knownKartIds == null)
            {
                return;
            }

            List<int> known = knownKartIds as List<int> ?? knownKartIds.ToList();
            if (known.Count == 0)
            {
                // 卡丁车表还没加载好，等下一次
                return;
            }

            _validated = true;
            Dictionary<int, (float X, float Y)> map = GetMap();
            if (map == null || map.Count == 0)
            {
                Log.Warn("[ModelMax] 未加载到任何模型尺寸（ModelMax.xml 缺失或为空），所有卡丁车都会发送 0");
                return;
            }

            List<int> missing = known.Where(id => !map.ContainsKey(id)).OrderBy(id => id).ToList();
            Log.Info($"[ModelMax] 已加载 {map.Count} 条；卡丁车 {known.Count} 辆，其中 {missing.Count} 辆未配置"
                     + (missing.Count > 0 ? "（开局发送 0，客户端回退默认）" : ""));
            if (missing.Count > 0)
            {
                Log.Debug("[ModelMax] 未配置的 id: " + string.Join(", ", missing.Take(60))
                          + (missing.Count > 60 ? $" …共 {missing.Count} 个" : ""));
            }
        }

        private static Dictionary<int, (float X, float Y)> GetMap()
        {
            string path = FileName.ModelMax_LoadFile;

            lock (Gate)
            {
                DateTime stamp = DateTime.MinValue;
                try
                {
                    if (File.Exists(path))
                    {
                        stamp = File.GetLastWriteTimeUtc(path);
                    }
                }
                catch
                {
                }

                if (_loaded
                    && string.Equals(_loadedPath, path, StringComparison.OrdinalIgnoreCase)
                    && stamp == _fileStamp)
                {
                    return _map;
                }

                _loaded = true;
                _loadedPath = path;
                _fileStamp = stamp;
                _map = Load(path);
                return _map;
            }
        }

        private static Dictionary<int, (float X, float Y)> Load(string path)
        {
            var map = new Dictionary<int, (float X, float Y)>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return map;
            }

            try
            {
                using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                XDocument doc = XDocument.Load(stream);
                int duplicates = 0;
                foreach (XElement kart in doc.Descendants("kart"))
                {
                    string idText = (string)kart.Attribute("id");
                    if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    {
                        continue;
                    }
                    if (!float.TryParse((string)kart.Attribute("modelMaxX"), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                        || !float.TryParse((string)kart.Attribute("modelMaxY"), NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    {
                        continue;
                    }
                    if (map.ContainsKey(id))
                    {
                        duplicates++;
                    }
                    map[id] = (x, y);
                }
                if (duplicates > 0)
                {
                    Log.Warn($"[ModelMax] 文件中存在 {duplicates} 个重复 id（后者覆盖前者）");
                }
            }
            catch (Exception ex) when (ex is XmlException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Error($"[ModelMax] 读取 {path} 失败，本次运行所有车辆模型尺寸按 0 处理", ex);
            }

            return map;
        }
    }
}
