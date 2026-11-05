using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 管理后台的封禁与历史数据。
    ///
    ///   Profile\ipbans.json            IP 封禁表（按 IP 精确匹配）
    ///   Profile\players_history.json   历史玩家（按昵称去重，登录/下线时更新）
    ///
    /// 两个文件都是"读-改-写 + 临时文件原子替换"，用一把静态锁串行化。
    /// </summary>
    public static class AdminHistory
    {
        public sealed class IpBan
        {
            public string ip { get; set; }
            public string reason { get; set; }
            public string time { get; set; }
        }

        public sealed class PlayerRecord
        {
            public string nick { get; set; }
            public string lastIp { get; set; }
            public List<string> ipHistory { get; set; } = new List<string>();
            public string lastLogin { get; set; }
            public string lastLogout { get; set; }
            public int loginCount { get; set; }
            public long totalSeconds { get; set; }
            /// <summary>仅内存：本次登录时间，用于算在线时长（不写盘）</summary>
            [System.Text.Json.Serialization.JsonIgnore]
            public DateTime sessionStart { get; set; }
        }

        private static readonly object Gate = new();
        private static Dictionary<string, PlayerRecord> _history;
        private static List<IpBan> _ipBans;
        private static readonly HashSet<string> _online = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static string HistoryFile => Path.Combine(FileName.ProfileDir, "players_history.json");
        private static string IpBanFile => Path.Combine(FileName.ProfileDir, "ipbans.json");

        // ---------- 加载 / 保存 ----------
        private static Dictionary<string, PlayerRecord> LoadHistory()
        {
            if (_history != null) return _history;
            lock (Gate)
            {
                if (_history != null) return _history;
                try
                {
                    if (File.Exists(HistoryFile))
                    {
                        string text = File.ReadAllText(HistoryFile, Encoding.UTF8);
                        _history = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(text)
                                   ?? new Dictionary<string, PlayerRecord>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[AdminHistory] 读取历史玩家失败: {ex.Message}");
                }
                _history ??= new Dictionary<string, PlayerRecord>(StringComparer.OrdinalIgnoreCase);
                return _history;
            }
        }

        private static void SaveHistory()
        {
            try
            {
                Directory.CreateDirectory(FileName.ProfileDir);
                string tmp = HistoryFile + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                if (File.Exists(HistoryFile)) File.Delete(HistoryFile);
                File.Move(tmp, HistoryFile);
            }
            catch (Exception ex)
            {
                Log.Warn($"[AdminHistory] 保存历史玩家失败: {ex.Message}");
            }
        }

        private static List<IpBan> LoadIpBans()
        {
            if (_ipBans != null) return _ipBans;
            lock (Gate)
            {
                if (_ipBans != null) return _ipBans;
                try
                {
                    if (File.Exists(IpBanFile))
                    {
                        string text = File.ReadAllText(IpBanFile, Encoding.UTF8);
                        _ipBans = JsonSerializer.Deserialize<List<IpBan>>(text) ?? new List<IpBan>();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[AdminHistory] 读取 IP 封禁失败: {ex.Message}");
                }
                _ipBans ??= new List<IpBan>();
                return _ipBans;
            }
        }

        private static void SaveIpBans()
        {
            try
            {
                Directory.CreateDirectory(FileName.ProfileDir);
                string tmp = IpBanFile + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(_ipBans, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                if (File.Exists(IpBanFile)) File.Delete(IpBanFile);
                File.Move(tmp, IpBanFile);
            }
            catch (Exception ex)
            {
                Log.Warn($"[AdminHistory] 保存 IP 封禁失败: {ex.Message}");
            }
        }

        // ---------- 生命周期 ----------
        /// <summary>服务端启动时调用：上次进程可能崩过，把所有"在线"标记清掉。</summary>
        public static void ResetOnlineFlags()
        {
            lock (Gate)
            {
                _online.Clear();
                Log.Debug($"[AdminHistory] 历史玩家 {LoadHistory().Count} 条，IP 封禁 {LoadIpBans().Count} 条");
            }
        }

        public static void OnLogin(string nick, string ip)
        {
            if (string.IsNullOrEmpty(nick)) return;
            lock (Gate)
            {
                var map = LoadHistory();
                if (!map.TryGetValue(nick, out PlayerRecord rec))
                {
                    rec = new PlayerRecord { nick = nick };
                    map[nick] = rec;
                }
                rec.nick = nick;
                rec.lastIp = ip ?? "";
                rec.ipHistory ??= new List<string>();
                if (!string.IsNullOrEmpty(ip) && !rec.ipHistory.Contains(ip))
                {
                    rec.ipHistory.Insert(0, ip);
                    if (rec.ipHistory.Count > 3) rec.ipHistory.RemoveRange(3, rec.ipHistory.Count - 3);
                }
                rec.lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                rec.sessionStart = DateTime.Now;
                rec.loginCount++;
                _online.Add(nick);
                SaveHistory();
            }
        }

        public static void OnLogout(string nick)
        {
            if (string.IsNullOrEmpty(nick)) return;
            lock (Gate)
            {
                var map = LoadHistory();
                _online.Remove(nick);
                if (map.TryGetValue(nick, out PlayerRecord rec))
                {
                    rec.lastLogout = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (rec.sessionStart != default)
                    {
                        long sec = (long)(DateTime.Now - rec.sessionStart).TotalSeconds;
                        if (sec > 0) rec.totalSeconds += sec;
                        rec.sessionStart = default;
                    }
                    SaveHistory();
                }
            }
        }

        // ---------- IP 封禁 ----------
        public static bool IsIpBanned(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            lock (Gate)
            {
                return LoadIpBans().Any(b => string.Equals(b.ip, ip, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static List<IpBan> ListIpBans()
        {
            lock (Gate) { return LoadIpBans().OrderByDescending(b => b.time).ToList(); }
        }

        public static bool AddIpBan(string ip, string reason)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            if (!IPAddress.TryParse(ip.Trim(), out _)) return false;
            lock (Gate)
            {
                var list = LoadIpBans();
                list.RemoveAll(b => string.Equals(b.ip, ip.Trim(), StringComparison.OrdinalIgnoreCase));
                list.Add(new IpBan { ip = ip.Trim(), reason = reason ?? "", time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
                SaveIpBans();
            }
            Log.Debug($"[AdminHistory] 新增 IP 封禁 {ip}（原因={reason}）");
            return true;
        }

        public static bool RemoveIpBan(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            lock (Gate)
            {
                var list = LoadIpBans();
                int removed = list.RemoveAll(b => string.Equals(b.ip, ip.Trim(), StringComparison.OrdinalIgnoreCase));
                if (removed > 0) SaveIpBans();
                return removed > 0;
            }
        }

        // ---------- 历史查询 ----------
        public sealed class HistoryItem
        {
            public string nick { get; set; }
            public string lastIp { get; set; }
            public string lastLogin { get; set; }
            public string lastLogout { get; set; }
            public int loginCount { get; set; }
            public long totalSeconds { get; set; }
            public bool online { get; set; }
        }

        public static (int total, List<HistoryItem> items) QueryHistory(int page, int size, string q)
        {
            page = page < 1 ? 1 : page;
            size = size < 1 ? 20 : Math.Min(size, 200);
            lock (Gate)
            {
                var map = LoadHistory();
                IEnumerable<PlayerRecord> query = map.Values;
                if (!string.IsNullOrWhiteSpace(q))
                {
                    string key = q.Trim();
                    query = query.Where(r =>
                        (r.nick ?? "").IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0
                        || (r.lastIp ?? "").IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                var ordered = query
                    .OrderByDescending(r => ParseTime(r.lastLogout) ?? ParseTime(r.lastLogin) ?? DateTime.MinValue)
                    .ToList();
                int total = ordered.Count;
                var items = ordered.Skip((page - 1) * size).Take(size).Select(r => new HistoryItem
                {
                    nick = r.nick,
                    lastIp = r.lastIp,
                    lastLogin = r.lastLogin,
                    lastLogout = r.lastLogout,
                    loginCount = r.loginCount,
                    totalSeconds = r.totalSeconds,
                    online = _online.Contains(r.nick ?? "")
                }).ToList();
                return (total, items);
            }
        }

        /// <summary>踢掉某个 IP 上的所有在线玩家（封禁 IP 后立即生效）。</summary>
        public static int KickIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return 0;
            int kicked = 0;
            foreach (SessionGroup group in ClientManager.GetClients().ToList())
            {
                try
                {
                    string remote = group?.Client?.GetRemoteAddress() ?? "";
                    if (SplitIp(remote) != ip.Trim()) continue;
                    group.Client.Disconnect();
                    kicked++;
                }
                catch { }
            }
            if (kicked > 0) Log.Debug($"[AdminHistory] 封禁 IP {ip}：踢下线 {kicked} 人");
            return kicked;
        }

        private static DateTime? ParseTime(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            return DateTime.TryParse(text, out DateTime dt) ? dt : (DateTime?)null;
        }

        /// <summary>从 "ip:port" 里取出 IP（兼容 IPv6 的 [::1]:port）。</summary>
        public static string SplitIp(string remote)
        {
            if (string.IsNullOrEmpty(remote)) return "";
            remote = remote.Trim();
            if (remote.StartsWith("["))
            {
                int end = remote.IndexOf(']');
                return end > 0 ? remote.Substring(1, end - 1) : remote;
            }
            int colon = remote.LastIndexOf(':');
            // 纯 IPv6（不含端口）里冒号很多，且 IPv6 没有点号
            if (colon > 0 && remote.IndexOf('.') >= 0) return remote.Substring(0, colon);
            if (colon > 0 && remote.IndexOf(':') != colon) return remote; // 多个冒号 → 当 IPv6 整体
            return colon > 0 ? remote.Substring(0, colon) : remote;
        }
    }
}
