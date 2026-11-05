using System;
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
    /// 本地管理后台（挂在 LocalAdminApi 的 127.0.0.1:39314 上）。
    /// 只绑回环、无外网暴露；页面对应 /，接口在 /api/*。
    ///
    /// 页面：概览 / 玩家存档 / 封禁（IP 封禁 + 历史玩家 + 昵称封禁）
    /// 数据：AdminHistory（ipbans.json / players_history.json）+ bans.json（昵称封禁）
    /// </summary>
    public static class AdminWeb
    {
        public sealed class Result
        {
            public int Code;
            public string ContentType;
            public string Body;
        }

        private static readonly object BanLock = new object();
        private static readonly string[] SaveFileWhitelist =
        {
            "Bingo.json", "Competitive.json", "Favorite.json", "FavoriteTrack.json",
            "ItemPresetsConfig.json", "Launcher.json", "Level12Data.json", "NewItem.json",
            "NewKart.json", "Parts12Data.json", "PartsData.json", "PlantData.json",
            "RewardBox.json", "TrainingMission.json", "TuneData.json", "Locked.json"
        };

        private static string BanFile => Path.Combine(FileName.ProfileDir, "bans.json");

        // ---------- 入口 ----------
        public static Result Handle(string method, string path, string query, string body)
        {
            try
            {
                string p = (path ?? "/").TrimEnd('/');
                if (p.Length == 0) p = "/";
                bool post = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);

                if (p == "/" || p.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
                    return new Result { Code = 200, ContentType = "text/html", Body = Html };

                if (p == "/api/overview") return Json(BuildOverview());
                if (p == "/api/rooms") return Json(BuildRooms());
                if (p == "/api/players") return Json(BuildPlayers());

                // 发放货币
                if (p == "/api/grant/currency" && post) return GrantCurrency(GetQuery(query, "nick"), GetQuery(query, "rp"), GetQuery(query, "lucci"), GetQuery(query, "koin"), GetQuery(query, "cash"), GetQuery(query, "tccash"));

                // 发放道具/车辆（写进奖励箱）
                if (p == "/api/grant/item" && post) return GrantRewardBox(GetQuery(query, "nick"), GetQuery(query, "stock"), GetQuery(query, "count"), GetQuery(query, "type"));
                if (p == "/api/stock") return LookupStock(GetQuery(query, "id"));
                if (p == "/api/stock/search") return SearchStock(GetQuery(query, "q"), GetQuery(query, "limit"));

                // 兑换码
                if (p == "/api/coupons") return Json(ReadCoupons());
                if (p == "/api/coupon" && post) return AddCoupon(GetQuery(query, "code"), GetQuery(query, "stock"), GetQuery(query, "count"));
                if (p == "/api/coupon/del" && post) return DeleteCoupon(GetQuery(query, "code"));
                if (p == "/api/coupon/check") return CheckCoupon(GetQuery(query, "code"));

                // 服务器配置
                if (p == "/api/settings") return post ? SaveSettings(body) : ReadSettings();

                // 赛道记录
                if (p == "/api/trackrank") return Json(ListTrackRank());
                if (p == "/api/trackrank/file") return ReadTrackRank(GetQuery(query, "name"));

                if (p == "/api/ipbans") return Json(AdminHistory.ListIpBans());
                if (p == "/api/ipban" && post)
                {
                    string ip = GetQuery(query, "ip");
                    string reason = GetQuery(query, "reason");
                    if (!AdminHistory.AddIpBan(ip, reason)) return Err(400, "IP 格式不正确");
                    int kicked = AdminHistory.KickIp(ip);
                    return Json(new { ok = true, kicked });
                }
                if (p == "/api/ipunban" && post)
                {
                    bool ok = AdminHistory.RemoveIpBan(GetQuery(query, "ip"));
                    return Json(new { ok });
                }
                if (p == "/api/history")
                {
                    int page = int.TryParse(GetQuery(query, "page"), out int pg) ? pg : 1;
                    int size = int.TryParse(GetQuery(query, "size"), out int sz) ? sz : 20;
                    var (total, items) = AdminHistory.QueryHistory(page, size, GetQuery(query, "q"));
                    return Json(new { total, page, size, pages = (total + size - 1) / Math.Max(1, size), items });
                }

                // 昵称封禁（保留旧功能）
                if (p == "/api/bans") return Json(ReadBans());
                if (p == "/api/ban" && post) return AddBan(GetQuery(query, "nick"), GetQuery(query, "reason"), GetQuery(query, "type"));
                if (p == "/api/unban" && post) return RemoveBan(GetQuery(query, "nick"));

                if (p.StartsWith("/api/players/", StringComparison.OrdinalIgnoreCase))
                {
                    string rest = p.Substring("/api/players/".Length);
                    int slash = rest.IndexOf('/');
                    if (slash <= 0) return Err(400, "bad player path");
                    string nick = Uri.UnescapeDataString(rest.Substring(0, slash));
                    string what = rest.Substring(slash + 1);
                    if (what.Equals("files", StringComparison.OrdinalIgnoreCase)) return Json(ListFiles(nick));
                    if (what.Equals("file", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = GetQuery(query, "name");
                        if (post) return SaveFile(nick, name, body);
                        return ReadFile(nick, name);
                    }
                    return Err(404, "not found");
                }

                return Err(404, "not found");
            }
            catch (Exception ex)
            {
                Log.Warn($"[AdminWeb] {path} 处理失败: {ex.Message}");
                return Err(500, ex.Message);
            }
        }

        // ---------- 数据 ----------
        private static object BuildOverview()
        {
            var metrics = Metrics.Snapshot();
            var players = new List<object>();
            foreach (string nick in ClientManager.GetOnlinePlayers())
            {
                players.Add(new { nick, ip = GetIp(nick) });
            }
            return new
            {
                online = ClientManager.OnlineCount,
                rooms = RoomManager._rooms.Count,
                uptimeSeconds = metrics["uptimeSeconds"],
                tcpIn = metrics["tcpIn"],
                tcpOut = metrics["tcpOut"],
                udpIn = metrics["udpIn"],
                udpOut = metrics["udpOut"],
                handlerErrors = metrics["handlerErrors"],
                protocolErrors = metrics["protocolErrors"],
                disconnects = metrics["disconnects"],
                memoryMB = metrics["workingSetMB"],
                players
            };
        }

        private static object BuildRooms()
        {
            var list = new List<object>();
            foreach (var pair in RoomManager._rooms.OrderBy(k => k.Key))
            {
                var room = pair.Value;
                var members = new List<object>();
                foreach (RoomMember m in room._slots)
                {
                    if (m is Player p) members.Add(new { nick = p.Nickname, slot = p.SlotId, team = p.Team, ip = GetIp(p.Nickname) });
                    else if (m is Ai a) members.Add(new { nick = "AI", slot = a.SlotId, team = a.Team, ip = "" });
                }
                list.Add(new
                {
                    id = pair.Key,
                    name = room.RoomName,
                    players = room.GetPlayerCount(),
                    ai = room.GetAiCount(),
                    started = room.Started,
                    locked = room.Lock,
                    members
                });
            }
            return list;
        }

        private static object BuildPlayers()
        {
            var list = new List<object>();
            string dir = FileName.ProfileDir;
            if (!Directory.Exists(dir)) return list;

            var online = new HashSet<string>(ClientManager.GetOnlinePlayers(), StringComparer.OrdinalIgnoreCase);
            var ipBans = new HashSet<string>(AdminHistory.ListIpBans().Select(b => b.ip ?? ""), StringComparer.OrdinalIgnoreCase);
            foreach (string playerDir in Directory.GetDirectories(dir))
            {
                string nick = Path.GetFileName(playerDir);
                int files = 0;
                long bytes = 0;
                DateTime last = DateTime.MinValue;
                try
                {
                    foreach (string f in Directory.GetFiles(playerDir, "*.json"))
                    {
                        var fi = new FileInfo(f);
                        files++;
                        bytes += fi.Length;
                        if (fi.LastWriteTime > last) last = fi.LastWriteTime;
                    }
                }
                catch { }
                bool isOnline = online.Contains(nick);
                string ip = isOnline ? GetIp(nick) : "";
                list.Add(new
                {
                    nick,
                    online = isOnline,
                    ip,
                    files,
                    bytes,
                    lastWrite = last == DateTime.MinValue ? "" : last.ToString("yyyy-MM-dd HH:mm:ss"),
                    banned = GetBanType(nick) != 0,
                    ipBanned = isOnline && ipBans.Contains(ip)
                });
            }
            return list.OrderByDescending(o => ((dynamic)o).online).ThenBy(o => ((dynamic)o).nick).ToList();
        }

        private static List<object> ListFiles(string nick)
        {
            var list = new List<object>();
            string dir = PlayerDir(nick);
            if (!Directory.Exists(dir)) return list;
            foreach (string f in Directory.GetFiles(dir, "*.json").OrderBy(x => x))
            {
                var fi = new FileInfo(f);
                list.Add(new { name = fi.Name, size = fi.Length, modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") });
            }
            return list;
        }

        private static Result ReadFile(string nick, string name)
        {
            if (!IsSafeFile(nick, name)) return Err(400, "文件名不合法");
            string path = Path.Combine(PlayerDir(nick), name);
            if (!File.Exists(path)) return Err(404, "文件不存在");
            string text;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                text = reader.ReadToEnd();
            }
            return Json(new { name, content = text });
        }

        private static Result SaveFile(string nick, string name, string content)
        {
            if (!IsSafeFile(nick, name)) return Err(400, "文件名不合法");
            if (content == null) return Err(400, "内容为空");
            try { using var _ = JsonDocument.Parse(content); }
            catch (Exception ex) { return Err(400, "JSON 格式错误：" + ex.Message); }

            string dir = PlayerDir(nick);
            if (!Directory.Exists(dir)) return Err(404, "玩家目录不存在");
            string path = Path.Combine(dir, name);

            if (File.Exists(path))
            {
                try { File.Copy(path, path + ".bak", true); } catch { }
            }
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);

            bool online = ClientManager.GetParent(nick) != null;
            if (!online) { try { ProfileService.Forget(nick); } catch { } }
            Log.Debug($"[AdminWeb] 写入存档 {nick}/{name}（{content.Length} 字节，在线={online}）");
            return Json(new { ok = true, online, note = online ? "已写入文件，玩家下次登录生效" : "已写入并刷新缓存" });
        }

        // ---------- 昵称封禁（保留旧功能） ----------
        public sealed class BanEntry
        {
            public string nick { get; set; }
            public string ip { get; set; }
            public string reason { get; set; }
            public ushort banType { get; set; }
            public string time { get; set; }
        }

        private static List<BanEntry> ReadBans()
        {
            lock (BanLock)
            {
                try
                {
                    if (!File.Exists(BanFile)) return new List<BanEntry>();
                    return JsonSerializer.Deserialize<List<BanEntry>>(File.ReadAllText(BanFile, Encoding.UTF8)) ?? new List<BanEntry>();
                }
                catch { return new List<BanEntry>(); }
            }
        }

        private static void WriteBans(List<BanEntry> bans)
        {
            lock (BanLock)
            {
                string tmp = BanFile + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(bans, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                if (File.Exists(BanFile)) File.Delete(BanFile);
                File.Move(tmp, BanFile);
            }
        }

        private static Result AddBan(string nick, string reason, string type)
        {
            if (string.IsNullOrWhiteSpace(nick)) return Err(400, "缺少昵称");
            ushort banType = (!string.IsNullOrEmpty(type) && type == "5") ? (ushort)5 : (ushort)2;
            var bans = ReadBans();
            bans.RemoveAll(b => string.Equals(b.nick, nick, StringComparison.OrdinalIgnoreCase));
            bans.Add(new BanEntry { nick = nick, ip = GetIp(nick), reason = reason ?? "", banType = banType, time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            WriteBans(bans);
            try
            {
                var cfg = ProfileService.GetProfileConfig(nick);
                if (cfg?.Rider != null) { cfg.Rider.BanType = banType; ProfileService.Save(nick, cfg); }
            }
            catch (Exception ex) { Log.Warn($"[AdminWeb] 封禁写档失败 {nick}: {ex.Message}"); }
            Log.Debug($"[AdminWeb] 昵称封禁 {nick}（type={banType}）");
            return Json(new { ok = true });
        }

        private static Result RemoveBan(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick)) return Err(400, "缺少昵称");
            var bans = ReadBans();
            bans.RemoveAll(b => string.Equals(b.nick, nick, StringComparison.OrdinalIgnoreCase));
            WriteBans(bans);
            try
            {
                var cfg = ProfileService.GetProfileConfig(nick);
                if (cfg?.Rider != null && cfg.Rider.BanType != 0) { cfg.Rider.BanType = 0; ProfileService.Save(nick, cfg); }
            }
            catch { }
            Log.Debug($"[AdminWeb] 解除昵称封禁 {nick}");
            return Json(new { ok = true });
        }

        private static ushort GetBanType(string nick)
        {
            var hit = ReadBans().FirstOrDefault(b => string.Equals(b.nick, nick, StringComparison.OrdinalIgnoreCase));
            return hit == null ? (ushort)0 : hit.banType;
        }

        /// <summary>登录时调用：先查 IP 封禁，再查昵称封禁；命中就把 BanType 写进档案。</summary>
        public static bool ApplyBanToProfile(string nick, string ip)
        {
            if (string.IsNullOrEmpty(nick)) return false;
            ip = AdminHistory.SplitIp(ip);
            if (AdminHistory.IsIpBanned(ip))
            {
                try
                {
                    var cfg = ProfileService.GetProfileConfig(nick);
                    if (cfg?.Rider != null) { cfg.Rider.BanType = 5; ProfileService.Save(nick, cfg); }
                }
                catch { }
                Log.Debug($"[Ban] {nick}@{ip} 命中 IP 封禁");
                return true;
            }
            var hit = ReadBans().FirstOrDefault(b =>
                string.Equals(b.nick, nick, StringComparison.OrdinalIgnoreCase)
                || (b.banType == 5 && !string.IsNullOrEmpty(b.ip) && b.ip == ip));
            if (hit == null) return false;
            try
            {
                var cfg = ProfileService.GetProfileConfig(nick);
                if (cfg?.Rider != null && cfg.Rider.BanType != hit.banType) { cfg.Rider.BanType = hit.banType; ProfileService.Save(nick, cfg); }
            }
            catch { }
            Log.Debug($"[Ban] {nick}@{ip} 命中昵称封禁（type={hit.banType}）");
            return true;
        }

        // ---------- 发放货币 ----------
        private static Result GrantCurrency(string nick, string rp, string lucci, string koin, string cash, string tccash)
        {
            if (string.IsNullOrWhiteSpace(nick)) return Err(400, "缺少昵称");
            try
            {
                var cfg = ProfileService.GetProfileConfig(nick);
                if (cfg?.Rider == null) return Err(404, "玩家不存在");
                if (uint.TryParse(rp, out uint v1)) cfg.Rider.RP = v1;
                if (uint.TryParse(lucci, out uint v2)) cfg.Rider.Lucci = v2;
                if (uint.TryParse(koin, out uint v3)) cfg.Rider.Koin = v3;
                if (uint.TryParse(cash, out uint v4)) cfg.Rider.Cash = v4;
                if (uint.TryParse(tccash, out uint v5)) cfg.Rider.TcCash = v5;
                ProfileService.Save(nick, cfg);
                Log.Debug($"[AdminWeb] 发放货币 {nick}: RP={cfg.Rider.RP} Lucci={cfg.Rider.Lucci} Koin={cfg.Rider.Koin} Cash={cfg.Rider.Cash} TcCash={cfg.Rider.TcCash}");
                return Json(new { ok = true, rp = cfg.Rider.RP, lucci = cfg.Rider.Lucci, koin = cfg.Rider.Koin, cash = cfg.Rider.Cash, tccash = cfg.Rider.TcCash });
            }
            catch (Exception ex)
            {
                return Err(500, ex.Message);
            }
        }

        // ---------- 发放道具/车辆（奖励箱） ----------
        private static string RewardBoxPath(string nick)
        {
            string dir = PlayerDir(nick);
            return dir == null ? null : Path.Combine(dir, "RewardBox.json");
        }

        private static Result GrantRewardBox(string nick, string stock, string count, string type)
        {
            if (string.IsNullOrWhiteSpace(nick)) return Err(400, "缺少昵称");
            if (!uint.TryParse(stock, out uint stockId) || stockId == 0) return Err(400, "stockId 必须是正整数");
            int cnt = int.TryParse(count, out int c) ? Math.Clamp(c, 1, 99) : 1;
            byte boxType = byte.TryParse(type, out byte bt) ? bt : (byte)0;

            string dir = PlayerDir(nick);
            if (dir == null || !Directory.Exists(dir)) return Err(404, "玩家目录不存在");

            var list = new List<RewardBox>();
            string path = RewardBoxPath(nick);
            if (File.Exists(path))
            {
                try { list = JsonHelper.DeserializeNoBom<List<RewardBox>>(path) ?? new List<RewardBox>(); }
                catch { list = new List<RewardBox>(); }
            }

            long baseId = DateTime.Now.Ticks / 10;   // 微秒级，足够避免重复
            for (int i = 0; i < cnt; i++)
            {
                list.Add(new RewardBox { ID = baseId + i, Type = boxType, stockId = stockId });
            }
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonHelper.Serialize(list), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);

            Log.Debug($"[AdminWeb] 发放奖励箱 {nick}：stockId={stockId} x{cnt}（type={boxType}），当前 {list.Count} 条");
            return Json(new { ok = true, total = list.Count, note = "已放入奖励箱，玩家进游戏到「奖励箱」领取（在线玩家需重新进入大厅/重登）" });
        }

        private static Result LookupStock(string id)
        {
            if (!uint.TryParse(id, out uint stockId)) return Err(400, "stockId 必须是数字");
            Stock.StockName.TryGetValue(stockId, out string stockName);
            if (!Stock.StockList.TryGetValue(stockId, out List<item> items) || items == null)
                return Json(new { ok = false, error = "客户端数据里没有这个 stockId", stockId, name = stockName ?? "" });
            return Json(new
            {
                ok = true,
                stockId,
                name = stockName ?? "",
                count = items.Count,
                items = items.Select(x => new { cat = x.itemCatId, id = x.itemId, num = x.itemCount, days = x.expireDay })
            });
        }

        /// <summary>按商品名搜索 stockId（q 为空时按 stockId 升序返回前 N 条）。</summary>
        private static Result SearchStock(string q, string limitText)
        {
            int limit = int.TryParse(limitText, out int l) ? Math.Clamp(l, 1, 200) : 30;
            string key = (q ?? "").Trim();
            var query = Stock.StockList.Keys.AsEnumerable();
            if (key.Length > 0)
            {
                query = query.Where(id =>
                {
                    if (Stock.StockName.TryGetValue(id, out string name) && name != null
                        && name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    return id.ToString() == key;
                });
            }
            var list = query.OrderBy(id => id).Take(limit).Select(id =>
            {
                Stock.StockName.TryGetValue(id, out string name);
                Stock.StockList.TryGetValue(id, out List<item> items);
                return new { stockId = id, name = name ?? "", items = items?.Count ?? 0 };
            }).ToList();
            return Json(new { total = Stock.StockList.Count, items = list });
        }

        // ---------- 兑换码 ----------
        private static List<Coupon> ReadCoupons()
        {
            lock (BanLock)
            {
                try
                {
                    if (!File.Exists(FileName.Coupon)) return new List<Coupon>();
                    return JsonSerializer.Deserialize<List<Coupon>>(File.ReadAllText(FileName.Coupon, Encoding.UTF8)) ?? new List<Coupon>();
                }
                catch { return new List<Coupon>(); }
            }
        }

        private static void WriteCoupons(List<Coupon> list)
        {
            lock (BanLock)
            {
                string tmp = FileName.Coupon + ".tmp";
                File.WriteAllText(tmp, JsonHelper.Serialize(list), new UTF8Encoding(false));
                if (File.Exists(FileName.Coupon)) File.Delete(FileName.Coupon);
                File.Move(tmp, FileName.Coupon);
            }
        }

        private static Result AddCoupon(string code, string stock, string count)
        {
            if (string.IsNullOrWhiteSpace(code)) return Err(400, "缺少兑换码");
            if (!uint.TryParse(stock, out uint stockId)) return Err(400, "stockId 必须是数字");
            int cnt = int.TryParse(count, out int c) ? c : 1;
            var list = ReadCoupons();
            list.RemoveAll(x => string.Equals(x.CouponNO, code, StringComparison.OrdinalIgnoreCase));
            list.Add(new Coupon { CouponNO = code.Trim(), stockId = stockId, Count = cnt });
            WriteCoupons(list);
            Log.Debug($"[AdminWeb] 生成兑换码 {code}（stock={stockId}, count={cnt}）");
            return Json(new { ok = true });
        }

        private static Result DeleteCoupon(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Err(400, "缺少兑换码");
            var list = ReadCoupons();
            int removed = list.RemoveAll(x => string.Equals(x.CouponNO, code, StringComparison.OrdinalIgnoreCase));
            if (removed > 0) WriteCoupons(list);
            return Json(new { ok = removed > 0 });
        }

        /// <summary>用服务端同一条读取逻辑自检：兑换码到底能不能被读到。</summary>
        private static Result CheckCoupon(string code)
        {
            string key = (code ?? "").Trim();
            var list = ReadCoupons();
            var hit = list.FirstOrDefault(x => string.Equals((x.CouponNO ?? "").Trim(), key, StringComparison.OrdinalIgnoreCase));
            return Json(new
            {
                code = key,
                found = hit != null,
                total = list.Count,
                stockId = hit?.stockId ?? 0,
                count = hit?.Count ?? 0,
                path = FileName.Coupon,
                exists = File.Exists(FileName.Coupon)
            });
        }

        // ---------- 服务器配置 ----------
        private static Result ReadSettings()
        {
            try
            {
                if (!File.Exists(FileName.Load_Settings)) return Err(404, "Settings.json 不存在");
                return Json(new { content = File.ReadAllText(FileName.Load_Settings, Encoding.UTF8), path = FileName.Load_Settings });
            }
            catch (Exception ex) { return Err(500, ex.Message); }
        }

        private static Result SaveSettings(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return Err(400, "内容为空");
            try { using var _ = JsonDocument.Parse(body); }
            catch (Exception ex) { return Err(400, "JSON 格式错误：" + ex.Message); }
            try
            {
                if (File.Exists(FileName.Load_Settings))
                {
                    try { File.Copy(FileName.Load_Settings, FileName.Load_Settings + ".bak", true); } catch { }
                }
                string tmp = FileName.Load_Settings + ".tmp";
                File.WriteAllText(tmp, body, new UTF8Encoding(false));
                if (File.Exists(FileName.Load_Settings)) File.Delete(FileName.Load_Settings);
                File.Move(tmp, FileName.Load_Settings);
                Log.Debug("[AdminWeb] 写入 Settings.json（重启服务端后生效）");
                return Json(new { ok = true, note = "已保存，重启服务端后生效" });
            }
            catch (Exception ex) { return Err(500, ex.Message); }
        }

        // ---------- 赛道记录 ----------
        private static List<object> ListTrackRank()
        {
            var list = new List<object>();
            try
            {
                string dir = FileName.Load_TrackRank;
                if (!Directory.Exists(dir)) return list;
                foreach (string f in Directory.GetFiles(dir, "*.json").OrderByDescending(x => new FileInfo(x).LastWriteTime))
                {
                    var fi = new FileInfo(f);
                    list.Add(new { name = fi.Name, size = fi.Length, modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") });
                }
            }
            catch { }
            return list;
        }

        private static Result ReadTrackRank(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return Err(400, "文件名不合法");
            string path = Path.Combine(FileName.Load_TrackRank, name);
            if (!File.Exists(path)) return Err(404, "文件不存在");
            return Json(new { name, content = File.ReadAllText(path, Encoding.UTF8) });
        }

        // ---------- 工具 ----------
        private static string PlayerDir(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick)) return null;
            if (nick.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) >= 0) return null;
            return Path.Combine(FileName.ProfileDir, nick);
        }

        private static bool IsSafeFile(string nick, string name)
        {
            if (string.IsNullOrWhiteSpace(nick) || string.IsNullOrWhiteSpace(name)) return false;
            if (name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0) return false;
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return false;
            return SaveFileWhitelist.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetIp(string nick)
        {
            if (string.IsNullOrEmpty(nick)) return "";
            try
            {
                var group = ClientManager.GetParent(nick);
                if (group?.Client != null) return AdminHistory.SplitIp(group.Client.GetRemoteAddress());
            }
            catch { }
            return "";
        }

        private static string GetQuery(string query, string key)
        {
            if (string.IsNullOrEmpty(query)) return null;
            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0 && pair.Substring(0, eq) == key) return Uri.UnescapeDataString(pair.Substring(eq + 1));
            }
            return null;
        }

        private static Result Json(object value) => new Result
        {
            Code = 200,
            ContentType = "application/json",
            Body = JsonSerializer.Serialize(value)
        };

        private static Result Err(int code, string message) => new Result
        {
            Code = code,
            ContentType = "application/json",
            Body = JsonSerializer.Serialize(new { ok = false, error = message })
        };

        // ---------- 页面 ----------
        private const string Html = """
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>跑跑卡丁车 · 本地管理后台</title>
<style>
  * { box-sizing: border-box; }
  body { margin:0; font-family:"Microsoft YaHei",system-ui,sans-serif; background:#f5f7fa; color:#243140; }
  header { background:#fff; border-bottom:1px solid #e3e8ef; padding:14px 22px; display:flex; align-items:center; gap:14px; }
  header h1 { font-size:18px; margin:0; font-weight:600; }
  .badge { font-size:12px; color:#fff; background:#2f80ed; border-radius:10px; padding:2px 8px; }
  nav { display:flex; gap:6px; padding:10px 22px 0; background:#fff; border-bottom:1px solid #e3e8ef; }
  nav button { border:none; background:transparent; padding:10px 14px; cursor:pointer; font-size:14px; color:#5b6b7f; border-bottom:2px solid transparent; }
  nav button.active { color:#2f80ed; border-bottom-color:#2f80ed; font-weight:600; }
  main { padding:18px 22px 40px; }
  .cards { display:flex; gap:12px; flex-wrap:wrap; }
  .card { background:#fff; border:1px solid #e3e8ef; border-radius:10px; padding:14px 16px; min-width:150px; flex:1; }
  .card .k { font-size:12px; color:#7b8794; }
  .card .v { font-size:22px; font-weight:600; margin-top:4px; }
  table { width:100%; border-collapse:collapse; background:#fff; border:1px solid #e3e8ef; border-radius:10px; overflow:hidden; }
  th,td { text-align:left; padding:9px 12px; font-size:13px; border-bottom:1px solid #eef2f7; }
  th { background:#fafbfd; color:#5b6b7f; font-weight:600; }
  tr:hover td { background:#f7fafd; }
  .row { display:flex; gap:14px; align-items:flex-start; }
  .col { min-width:0; }
  .panel { background:#fff; border:1px solid #e3e8ef; border-radius:10px; padding:14px; }
  .list button { display:block; width:100%; text-align:left; border:none; background:transparent; padding:8px 10px; border-radius:6px; cursor:pointer; font-size:13px; overflow:hidden; text-overflow:ellipsis; }
  .list button:hover { background:#f0f5fb; }
  .list button.sel { background:#e8f1ff; color:#2f80ed; font-weight:600; }
  textarea { width:100%; height:420px; font-family:Consolas,monospace; font-size:12px; border:1px solid #d8dfe8; border-radius:8px; padding:10px; }
  input[type=text] { padding:7px 10px; border:1px solid #d8dfe8; border-radius:6px; font-size:13px; width:100%; max-width:320px; }
  select { padding:6px 8px; border:1px solid #d8dfe8; border-radius:6px; font-size:13px; }
  .btn { border:none; border-radius:6px; padding:8px 14px; cursor:pointer; font-size:13px; background:#2f80ed; color:#fff; }
  .btn.gray { background:#eef2f7; color:#425466; }
  .btn.red { background:#eb5757; }
  .btn.sm { padding:4px 10px; font-size:12px; }
  .muted { color:#8a97a6; font-size:12px; }
  .ok { color:#219653; } .err { color:#eb5757; }
  .hide { display:none; }
  .pager { display:flex; gap:6px; align-items:center; margin-top:10px; flex-wrap:wrap; }
  .pager button { border:1px solid #d8dfe8; background:#fff; border-radius:6px; padding:4px 10px; cursor:pointer; font-size:12px; }
  .pager button.cur { background:#2f80ed; color:#fff; border-color:#2f80ed; }
  .pager button:disabled { opacity:.45; cursor:default; }
  h3 { font-size:14px; margin:0 0 10px; }
</style>
</head>
<body>
<header>
  <h1>跑跑卡丁车 · 本地管理后台</h1>
  <span class="badge">127.0.0.1 only</span>
</header>
<nav>
  <button data-tab="overview" class="active">概览</button>
  <button data-tab="players">玩家存档</button>
  <button data-tab="bans">封禁</button>
  <button data-tab="grant">发放</button>
  <button data-tab="coupon">兑换码</button>
  <button data-tab="settings">配置</button>
  <button data-tab="rank">赛道记录</button>
</nav>
<main>
  <section id="tab-overview">
    <div class="cards" id="cards"></div>
    <h3 style="margin:18px 0 8px;">在线玩家</h3>
    <table><thead><tr><th>昵称</th><th>IP</th></tr></thead><tbody id="onlineBody"></tbody></table>
    <h3 style="margin:18px 0 8px;">房间</h3>
    <table><thead><tr><th>ID</th><th>房间名</th><th>人数</th><th>AI</th><th>状态</th><th>成员</th></tr></thead><tbody id="roomBody"></tbody></table>
  </section>

  <section id="tab-players" class="hide">
    <div class="row">
      <div class="col" style="flex:0 0 260px;">
        <div class="panel">
          <input type="text" id="search" placeholder="搜索昵称" oninput="renderPlayers()">
          <div class="list" id="playerList" style="margin-top:10px;max-height:560px;overflow:auto;"></div>
        </div>
      </div>
      <div class="col" style="flex:1;">
        <div class="panel">
          <div style="display:flex;align-items:center;gap:12px;">
            <b id="curNick">未选择玩家</b>
            <span class="muted" id="curInfo"></span>
          </div>
          <div class="list" id="fileList" style="margin-top:10px;"></div>
          <div style="margin-top:10px;">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;">
              <span class="muted" id="fileHint">选择一个存档文件</span>
              <span><button class="btn" onclick="saveFile()">保存</button>
                    <button class="btn gray" onclick="loadFile(curFile)">重新加载</button></span>
            </div>
            <textarea id="editor" spellcheck="false" placeholder="（选择左侧文件后显示内容）"></textarea>
            <div class="muted" id="saveMsg"></div>
          </div>
        </div>
      </div>
    </div>
  </section>

  <section id="tab-bans" class="hide">
    <div class="panel">
      <h3>封禁 IP</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="banIp" placeholder="IP 地址，例如 192.168.10.127">
        <input type="text" id="banIpReason" placeholder="原因（可选）">
        <button class="btn red" onclick="addIpBan()">添加封禁</button>
        <span class="muted">该 IP 的在线玩家会被立即踢下线，之后无法登录</span>
      </div>
      <table style="margin-top:12px;"><thead><tr><th>IP</th><th>原因</th><th>时间</th><th style="width:90px;"></th></tr></thead><tbody id="ipBanBody"></tbody></table>
      <div class="muted" id="ipBanMsg" style="margin-top:6px;"></div>
    </div>

    <div class="panel" style="margin-top:14px;">
      <h3>历史玩家</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="histQ" placeholder="搜索昵称或 IP" oninput="histPage=1;loadHistory()">
        每页
        <select id="histSize" onchange="histPage=1;loadHistory()">
          <option>20</option><option>50</option><option>100</option>
        </select>
        <span class="muted" id="histTotal"></span>
      </div>
      <table style="margin-top:12px;">
        <thead><tr><th>昵称</th><th>IP</th><th>最后上线</th><th>最后下线</th><th>登录次数</th><th>累计在线</th><th style="width:90px;"></th></tr></thead>
        <tbody id="histBody"></tbody>
      </table>
      <div class="pager" id="histPager"></div>
    </div>

    <div class="panel" style="margin-top:14px;">
      <h3>昵称封禁</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="banNick" placeholder="昵称">
        <input type="text" id="banReason" placeholder="原因（可选）">
        <button class="btn red" onclick="addBan()">封禁</button>
        <span class="muted">玩家下次登录会看到"账号封停"</span>
      </div>
      <table style="margin-top:12px;"><thead><tr><th>昵称</th><th>IP</th><th>类型</th><th>原因</th><th>时间</th><th style="width:90px;"></th></tr></thead><tbody id="banBody"></tbody></table>
    </div>
  </section>
  <section id="tab-grant" class="hide">
    <div class="panel">
      <h3>发放货币</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="grantNick" placeholder="玩家昵称">
        <input type="text" id="gRp" placeholder="RP">
        <input type="text" id="gLucci" placeholder="Lucci">
        <input type="text" id="gKoin" placeholder="Koin">
        <input type="text" id="gCash" placeholder="Cash">
        <input type="text" id="gTcCash" placeholder="TcCash">
        <button class="btn" onclick="grantCurrency()">保存</button>
      </div>
      <div class="muted" style="margin-top:8px;">留空的项不会被修改（直接改档，在线玩家下次登录生效）。</div>
      <div class="muted" id="grantMsg" style="margin-top:6px;"></div>
    </div>

    <div class="panel" style="margin-top:14px;">
      <h3>发放道具 / 车辆（奖励箱）</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="giStock" placeholder="stockId（道具箱编号）">
        <input type="text" id="giCount" placeholder="数量（默认 1，最大 99）">
        <input type="text" id="giType" placeholder="Type（默认 0）">
        <button class="btn" onclick="grantItem()">发放</button>
        <button class="btn gray" onclick="lookupStock()">查询 stockId</button>
        <input type="text" id="stockQ" placeholder="按名字搜道具（如 葱头）" onkeydown="if(event.key==='Enter')searchStock()">
        <button class="btn gray" onclick="searchStock()">搜索</button>
      </div>
      <div class="muted" style="margin-top:8px;">会写进玩家的「奖励箱」，玩家进游戏后在奖励箱里领取；车辆和道具都走同一个 stockId（客户端车库不区分拥有列表）。</div>
      <div class="muted" id="grantItemMsg" style="margin-top:6px;"></div>
      <div class="list" id="stockResults" style="margin-top:8px;max-height:260px;overflow:auto;"></div>
    </div>

    <div class="panel" style="margin-top:14px;">
      <h3>历史玩家（点一行填入昵称）</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="grantQ" placeholder="搜索昵称或 IP" oninput="grantPage=1;loadGrantHistory()">
        每页
        <select id="grantSize" onchange="grantPage=1;loadGrantHistory()">
          <option>20</option><option>50</option><option>100</option>
        </select>
        <span class="muted" id="grantTotal"></span>
      </div>
      <table style="margin-top:12px;">
        <thead><tr><th>昵称</th><th>IP</th><th>最后上线</th><th>最后下线</th><th>登录次数</th><th>累计在线</th></tr></thead>
        <tbody id="grantHistBody"></tbody>
      </table>
      <div class="pager" id="grantPager"></div>
    </div>
  </section>

  <section id="tab-coupon" class="hide">
    <div class="panel">
      <h3>兑换码</h3>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
        <input type="text" id="cpCode" placeholder="兑换码（25 位大写字母+数字）">
        <button class="btn gray" onclick="genCoupon()">随机 25 位</button>
        <input type="text" id="cpStock" placeholder="stockId（数字）">
        <input type="text" id="cpCount" placeholder="数量（默认 1）">
        <button class="btn" onclick="addCoupon()">生成</button>
        <span class="muted">stockId 是道具箱编号，需与客户端数据对应</span>
        <button class="btn gray" onclick="checkCoupon()">校验</button>
      </div>
      <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-top:10px;">
        <input type="text" id="cpStockQ" placeholder="按名字搜道具（如 葱头 / 红宝石）" onkeydown="if(event.key==='Enter')searchStockForCoupon()">
        <button class="btn gray" onclick="searchStockForCoupon()">搜索 stockId</button>
        <span class="muted" id="cpStockInfo"></span>
      </div>
      <div class="list" id="cpStockResults" style="margin-top:8px;max-height:220px;overflow:auto;"></div>
      <table style="margin-top:12px;"><thead><tr><th>兑换码</th><th>stockId</th><th>数量</th><th style="width:90px;"></th></tr></thead><tbody id="couponBody"></tbody></table>
      <div class="muted" id="couponMsg" style="margin-top:6px;"></div>
    </div>
  </section>

  <section id="tab-settings" class="hide">
    <div class="panel">
      <h3>服务器配置（Settings.json）</h3>
      <div style="margin-bottom:8px;">
        <button class="btn" onclick="saveSettings()">保存</button>
        <button class="btn gray" onclick="loadSettings()">重新加载</button>
        <span class="muted">保存后需要重启服务端才生效</span>
      </div>
      <textarea id="settingsEditor" spellcheck="false" style="height:480px;"></textarea>
      <div class="muted" id="settingsMsg"></div>
    </div>
  </section>

  <section id="tab-rank" class="hide">
    <div class="panel">
      <div class="row">
        <div class="col" style="flex:0 0 320px;">
          <h3>赛道记录文件</h3>
          <div class="list" id="rankList" style="max-height:560px;overflow:auto;"></div>
        </div>
        <div class="col" style="flex:1;">
          <h3 id="rankTitle">选择一个文件</h3>
          <textarea id="rankEditor" readonly spellcheck="false" style="height:480px;"></textarea>
        </div>
      </div>
    </div>
  </section>
</main>
<script>
let players = [], curNick = null, curFile = null;
let histPage = 1, histPages = 1;

document.querySelectorAll('nav button').forEach(b => b.onclick = () => {
  document.querySelectorAll('nav button').forEach(x => x.classList.remove('active'));
  b.classList.add('active');
  ['overview','players','bans','grant','coupon','settings','rank'].forEach(t => document.getElementById('tab-'+t).classList.toggle('hide', t !== b.dataset.tab));
  if (b.dataset.tab === 'overview') loadOverview();
  if (b.dataset.tab === 'players') loadPlayers();
  if (b.dataset.tab === 'bans') { loadIpBans(); loadHistory(); loadNickBans(); }
  if (b.dataset.tab === 'grant') loadGrantHistory();
  if (b.dataset.tab === 'coupon') loadCoupons();
  if (b.dataset.tab === 'settings') loadSettings();
  if (b.dataset.tab === 'rank') loadRankList();
});

async function jget(u) { const r = await fetch(u); return await r.json(); }
async function jpost(u) { const r = await fetch(u, { method:'POST' }); return await r.json(); }
function fmtSec(s) { s = s||0; const h=Math.floor(s/3600), m=Math.floor(s%3600/60); return h>0 ? h+'h'+m+'m' : m+'m'; }

async function loadOverview() {
  const d = await jget('/api/overview');
  const cards = [
    ['在线玩家', d.online], ['房间数', d.rooms], ['运行', d.uptimeSeconds + ' 秒'],
    ['TCP 收/发', d.tcpIn + ' / ' + d.tcpOut], ['UDP 收/发', d.udpIn + ' / ' + d.udpOut],
    ['内存', d.memoryMB + ' MB'], ['处理器异常', d.handlerErrors], ['协议错误', d.protocolErrors]
  ];
  document.getElementById('cards').innerHTML = cards.map(([k,v]) => `<div class="card"><div class="k">${k}</div><div class="v">${v}</div></div>`).join('');
  document.getElementById('onlineBody').innerHTML = (d.players||[]).map(p => `<tr><td>${p.nick}</td><td>${p.ip||'-'}</td></tr>`).join('') || '<tr><td colspan=2 class="muted">无人在线</td></tr>';
  const rooms = await jget('/api/rooms');
  document.getElementById('roomBody').innerHTML = (rooms||[]).map(r => {
    const ms = (r.members||[]).map(m => m.nick + '(' + m.slot + '/' + m.team + ')').join(', ');
    return `<tr><td>${r.id}</td><td>${r.name}</td><td>${r.players}</td><td>${r.ai}</td><td>${r.started?'进行中':'等待'}</td><td>${ms}</td></tr>`;
  }).join('') || '<tr><td colspan=6 class="muted">无房间</td></tr>';
}

async function loadPlayers() { players = await jget('/api/players') || []; renderPlayers(); }

function renderPlayers() {
  const q = (document.getElementById('search').value||'').toLowerCase();
  document.getElementById('playerList').innerHTML = players.filter(p => !q || p.nick.toLowerCase().includes(q)).map(p =>
    `<button class="${p.nick===curNick?'sel':''}" onclick="pickPlayer('${p.nick}')">
       ${p.online?'🟢':'⚪'} ${p.nick} ${p.banned?'<span class="err">[封]</span>':''}
       <div class="muted">${p.online?(p.ip||''):'离线'} · ${p.files} 个文件</div>
     </button>`).join('');
}

async function pickPlayer(nick) {
  curNick = nick; curFile = null; renderPlayers();
  const p = players.find(x => x.nick === nick) || {};
  document.getElementById('curNick').textContent = nick;
  document.getElementById('curInfo').textContent = (p.online ? '在线 ' + (p.ip||'') : '离线') + ' · ' + (p.bytes||0) + ' 字节';
  const files = await jget(`/api/players/${encodeURIComponent(nick)}/files`) || [];
  document.getElementById('fileList').innerHTML = files.map(f =>
    `<button onclick="loadFile('${f.name}')">${f.name} <span class="muted">${f.size} B · ${f.modified}</span></button>`).join('');
  document.getElementById('editor').value = '';
  document.getElementById('fileHint').textContent = '选择一个存档文件';
}

async function loadFile(name) {
  if (!curNick || !name) return;
  curFile = name;
  document.getElementById('fileHint').textContent = name;
  document.getElementById('saveMsg').textContent = '';
  const d = await jget(`/api/players/${encodeURIComponent(curNick)}/file?name=${encodeURIComponent(name)}`);
  document.getElementById('editor').value = d.content || '';
}

async function saveFile() {
  if (!curNick || !curFile) { alert('先选玩家和文件'); return; }
  const content = document.getElementById('editor').value;
  const r = await fetch(`/api/players/${encodeURIComponent(curNick)}/file?name=${encodeURIComponent(curFile)}`,
    { method:'POST', headers:{'Content-Type':'application/json'}, body: content });
  const d = await r.json();
  const el = document.getElementById('saveMsg');
  if (d.ok) { el.className = 'ok'; el.textContent = '已保存：' + (d.note||''); }
  else { el.className = 'err'; el.textContent = '保存失败：' + (d.error||''); }
}

// ---- IP 封禁 ----
async function loadIpBans() {
  const bans = await jget('/api/ipbans') || [];
  document.getElementById('ipBanBody').innerHTML = bans.map(b =>
    `<tr><td>${b.ip}</td><td>${b.reason||''}</td><td>${b.time||''}</td>
     <td><button class="btn gray sm" onclick="removeIpBan('${b.ip}')">解除</button></td></tr>`).join('')
    || '<tr><td colspan=4 class="muted">暂无封禁 IP</td></tr>';
}

async function addIpBan() {
  const ip = document.getElementById('banIp').value.trim();
  if (!ip) { alert('请输入 IP'); return; }
  const reason = document.getElementById('banIpReason').value.trim();
  const r = await fetch(`/api/ipban?ip=${encodeURIComponent(ip)}&reason=${encodeURIComponent(reason)}`, { method:'POST' });
  const d = await r.json();
  const el = document.getElementById('ipBanMsg');
  if (d.ok) { el.className='ok'; el.textContent = `已封禁 ${ip}` + (d.kicked ? `，踢下线 ${d.kicked} 人` : ''); }
  else { el.className='err'; el.textContent = '失败：' + (d.error||''); }
  document.getElementById('banIp').value=''; document.getElementById('banIpReason').value='';
  loadIpBans();
}

async function removeIpBan(ip) {
  await fetch(`/api/ipunban?ip=${encodeURIComponent(ip)}`, { method:'POST' });
  loadIpBans();
}

// ---- 历史玩家 ----
async function loadHistory() {
  const q = document.getElementById('histQ').value.trim();
  const size = document.getElementById('histSize').value;
  const d = await jget(`/api/history?page=${histPage}&size=${size}&q=${encodeURIComponent(q)}`);
  histPages = d.pages || 1;
  document.getElementById('histTotal').textContent = `共 ${d.total||0} 人`;
  document.getElementById('histBody').innerHTML = (d.items||[]).map(it =>
    `<tr><td>${it.nick}</td><td>${it.lastIp||'-'}</td><td>${it.lastLogin||'-'}</td>
      <td>${it.online ? '<span class="muted">在线中</span>' : (it.lastLogout||'-')}</td>
      <td>${it.loginCount||0}</td><td>${fmtSec(it.totalSeconds)}</td>
      <td><button class="btn red sm" onclick="banThisIp('${it.lastIp||''}')">禁IP</button></td></tr>`).join('')
    || '<tr><td colspan=7 class="muted">暂无记录</td></tr>';
  renderPager(d.total||0, histPages);
}

function renderPager(total, pages) {
  const box = document.getElementById('histPager');
  if (pages <= 1) { box.innerHTML = ''; return; }
  let html = `<button onclick="gotoPage(1)" ${histPage===1?'disabled':''}>«</button>`;
  html += `<button onclick="gotoPage(${histPage-1})" ${histPage===1?'disabled':''}>‹</button>`;
  const start = Math.max(1, histPage-2), end = Math.min(pages, start+4);
  for (let i=start; i<=end; i++) html += `<button class="${i===histPage?'cur':''}" onclick="gotoPage(${i})">${i}</button>`;
  html += `<button onclick="gotoPage(${histPage+1})" ${histPage===pages?'disabled':''}>›</button>`;
  html += `<button onclick="gotoPage(${pages})" ${histPage===pages?'disabled':''}>»</button>`;
  box.innerHTML = html;
}

function gotoPage(p) { histPage = p; loadHistory(); }

async function banThisIp(ip) {
  if (!ip) { alert('这条记录没有 IP'); return; }
  if (!confirm('封禁 IP ' + ip + ' ？该 IP 的在线玩家会被踢下线')) return;
  await fetch(`/api/ipban?ip=${encodeURIComponent(ip)}`, { method:'POST' });
  loadIpBans(); loadHistory();
}

// ---- 昵称封禁 ----
async function loadNickBans() {
  const bans = await jget('/api/bans') || [];
  document.getElementById('banBody').innerHTML = bans.map(b =>
    `<tr><td>${b.nick}</td><td>${b.ip||'-'}</td><td>${b.banType===5?'禁止IP':'账号封停'}</td><td>${b.reason||''}</td><td>${b.time||''}</td>
     <td><button class="btn gray sm" onclick="unbanNick('${b.nick}')">解封</button></td></tr>`).join('')
    || '<tr><td colspan=6 class="muted">暂无封禁</td></tr>';
}

async function addBan() {
  const nick = document.getElementById('banNick').value.trim();
  if (!nick) { alert('输入昵称'); return; }
  const reason = document.getElementById('banReason').value.trim();
  await fetch(`/api/ban?nick=${encodeURIComponent(nick)}&reason=${encodeURIComponent(reason)}`, { method:'POST' });
  document.getElementById('banNick').value=''; document.getElementById('banReason').value='';
  loadNickBans(); loadPlayers();
}

async function unbanNick(nick) {
  await fetch(`/api/unban?nick=${encodeURIComponent(nick)}`, { method:'POST' });
  loadNickBans(); loadPlayers();
}

// ---- 发放货币 ----
async function grantCurrency() {
  const nick = document.getElementById('grantNick').value.trim();
  if (!nick) { alert('输入玩家昵称'); return; }
  const q = (k, id) => { const v = document.getElementById(id).value.trim(); return v ? `&${k}=${encodeURIComponent(v)}` : ''; };
  const url = `/api/grant/currency?nick=${encodeURIComponent(nick)}` + q('rp','gRp') + q('lucci','gLucci') + q('koin','gKoin') + q('cash','gCash') + q('tccash','gTcCash');
  const r = await fetch(url, { method:'POST' });
  const d = await r.json();
  const el = document.getElementById('grantMsg');
  if (d.ok) { el.className='ok'; el.textContent = `已保存：RP=${d.rp} Lucci=${d.lucci} Koin=${d.koin} Cash=${d.cash} TcCash=${d.tccash}（在线玩家下次登录生效）`; }
  else { el.className='err'; el.textContent = '失败：' + (d.error||''); }
}

// ---- 发放页历史玩家 ----
let grantPage = 1;

async function loadGrantHistory() {
  const q = document.getElementById('grantQ').value.trim();
  const size = document.getElementById('grantSize').value;
  const d = await jget(`/api/history?page=${grantPage}&size=${size}&q=${encodeURIComponent(q)}`);
  const pages = d.pages || 1;
  document.getElementById('grantTotal').textContent = `共 ${d.total||0} 人`;
  document.getElementById('grantHistBody').innerHTML = (d.items||[]).map(it =>
    `<tr onclick="pickGrantNick('${it.nick}')" style="cursor:pointer">
      <td>${it.nick}</td><td>${it.lastIp||'-'}</td><td>${it.lastLogin||'-'}</td>
      <td>${it.online ? '<span class="muted">在线中</span>' : (it.lastLogout||'-')}</td>
      <td>${it.loginCount||0}</td><td>${fmtSec(it.totalSeconds)}</td></tr>`).join('')
    || '<tr><td colspan=6 class="muted">暂无记录</td></tr>';
  const box = document.getElementById('grantPager');
  if (pages <= 1) { box.innerHTML=''; return; }
  let html = `<button onclick="grantGo(1)" ${grantPage===1?'disabled':''}>«</button>`;
  html += `<button onclick="grantGo(${grantPage-1})" ${grantPage===1?'disabled':''}>‹</button>`;
  const start = Math.max(1, grantPage-2), end = Math.min(pages, start+4);
  for (let i=start;i<=end;i++) html += `<button class="${i===grantPage?'cur':''}" onclick="grantGo(${i})">${i}</button>`;
  html += `<button onclick="grantGo(${grantPage+1})" ${grantPage===pages?'disabled':''}>›</button>`;
  html += `<button onclick="grantGo(${pages})" ${grantPage===pages?'disabled':''}>»</button>`;
  box.innerHTML = html;
}

function grantGo(p) { grantPage = p; loadGrantHistory(); }

function pickGrantNick(nick) {
  document.getElementById('grantNick').value = nick;
  document.getElementById('grantMsg').textContent = '已选择：' + nick;
}

async function grantItem() {
  const nick = document.getElementById('grantNick').value.trim();
  if (!nick) { alert('先在上方选择/填写玩家昵称'); return; }
  const stock = document.getElementById('giStock').value.trim();
  const count = document.getElementById('giCount').value.trim() || '1';
  const type = document.getElementById('giType').value.trim() || '0';
  if (!stock) { alert('填 stockId'); return; }
  const r = await fetch(`/api/grant/item?nick=${encodeURIComponent(nick)}&stock=${encodeURIComponent(stock)}&count=${encodeURIComponent(count)}&type=${encodeURIComponent(type)}`, { method:'POST' });
  const d = await r.json();
  const el = document.getElementById('grantItemMsg');
  if (d.ok) { el.className='ok'; el.textContent = `${d.note}（奖励箱现有 ${d.total} 条）`; }
  else { el.className='err'; el.textContent = '失败：' + (d.error||''); }
}

function genCoupon() {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let s = '';
  const buf = new Uint32Array(25);
  crypto.getRandomValues(buf);
  for (let i = 0; i < 25; i++) s += chars[buf[i] % chars.length];
  document.getElementById('cpCode').value = s;
}

async function searchStock() {
  const q = document.getElementById('stockQ').value.trim();
  const d = await jget(`/api/stock/search?q=${encodeURIComponent(q)}&limit=50`);
  const box = document.getElementById('stockResults');
  box.innerHTML = (d.items||[]).map(it =>
    `<button onclick="document.getElementById('giStock').value='${it.stockId}';lookupStock()">
       ${it.name || '(无名)'} <span class="muted">stockId=${it.stockId} · ${it.items} 件</span>
     </button>`).join('') || `<div class="muted">没搜到（共 ${d.total||0} 个道具）</div>`;
}

async function lookupStock() {
  const stock = document.getElementById('giStock').value.trim();
  const el = document.getElementById('grantItemMsg');
  if (!stock) { alert('填 stockId'); return; }
  const d = await jget(`/api/stock?id=${encodeURIComponent(stock)}`);
  if (!d.ok) { el.className='err'; el.textContent = d.error || '查不到'; return; }
  el.className='ok';
  el.textContent = `stockId=${d.stockId}${d.name ? '「' + d.name + '」' : ''} 含 ${d.count} 件：` + (d.items||[]).map(i => `cat=${i.cat} id=${i.id} x${i.num} (${i.days}天)`).join('，');
}

// ---- 兑换码 ----
async function loadCoupons() {
  const list = await jget('/api/coupons') || [];
  document.getElementById('couponBody').innerHTML = list.map(c =>
    `<tr><td>${c.CouponNO}</td><td>${c.stockId}</td><td>${c.Count}</td>
     <td><button class="btn gray sm" onclick="delCoupon('${c.CouponNO}')">删除</button></td></tr>`).join('')
    || '<tr><td colspan=4 class="muted">暂无兑换码</td></tr>';
}

async function addCoupon() {
  const code = document.getElementById('cpCode').value.trim();
  const stock = document.getElementById('cpStock').value.trim();
  const count = document.getElementById('cpCount').value.trim();
  if (!code || !stock) { alert('兑换码和 stockId 都要填'); return; }
  const r = await fetch(`/api/coupon?code=${encodeURIComponent(code)}&stock=${encodeURIComponent(stock)}&count=${encodeURIComponent(count||'1')}`, { method:'POST' });
  const d = await r.json();
  const el = document.getElementById('couponMsg');
  if (d.ok) { el.className='ok'; el.textContent = '已生成 ' + code; } else { el.className='err'; el.textContent = '失败：' + (d.error||''); }
  document.getElementById('cpCode').value=''; document.getElementById('cpStock').value=''; document.getElementById('cpCount').value='';
  loadCoupons();
}

async function searchStockForCoupon() {
  const q = document.getElementById('cpStockQ').value.trim();
  const d = await jget(`/api/stock/search?q=${encodeURIComponent(q)}&limit=50`);
  const box = document.getElementById('cpStockResults');
  box.innerHTML = (d.items||[]).map(it =>
    `<button onclick="pickCpStock(${it.stockId}, '${(it.name||'').replace(/'/g, "\\'")}')">
       ${it.name || '(无名)'} <span class="muted">stockId=${it.stockId} · ${it.items} 件</span>
     </button>`).join('') || `<div class="muted">没搜到（共 ${d.total||0} 个道具）</div>`;
}

function pickCpStock(id, name) {
  document.getElementById('cpStock').value = id;
  document.getElementById('cpStockInfo').textContent = `已选：${name || '(无名)'}（stockId=${id}）`;
  lookupStockForCoupon(id);
}

async function lookupStockForCoupon(id) {
  const el = document.getElementById('cpStockInfo');
  const d = await jget(`/api/stock?id=${encodeURIComponent(id)}`);
  if (!d.ok) { el.className='err'; el.textContent = d.error || '查不到'; return; }
  el.className='ok';
  el.textContent = `已选：${d.name || '(无名)'}（stockId=${d.stockId}，${d.count} 件：`
    + (d.items||[]).map(i => `cat=${i.cat} id=${i.id} x${i.num}`).join('，') + '）';
}

async function checkCoupon() {
  const code = document.getElementById('cpCode').value.trim();
  if (!code) { alert('先填兑换码'); return; }
  const d = await jget(`/api/coupon/check?code=${encodeURIComponent(code)}`);
  const el = document.getElementById('couponMsg');
  if (d.found) {
    el.className = 'ok';
    el.textContent = `服务端可读到该兑换码：stockId=${d.stockId}，数量=${d.count}（文件 ${d.path}，共 ${d.total} 条）`;
  } else {
    el.className = 'err';
    el.textContent = `服务端读不到该兑换码！文件 ${d.path} 存在=${d.exists}，列表 ${d.total} 条`;
  }
}

async function delCoupon(code) {
  await fetch(`/api/coupon/del?code=${encodeURIComponent(code)}`, { method:'POST' });
  loadCoupons();
}

// ---- 服务器配置 ----
async function loadSettings() {
  const d = await jget('/api/settings');
  document.getElementById('settingsEditor').value = d.content || '';
  document.getElementById('settingsMsg').textContent = d.path || '';
}

async function saveSettings() {
  const content = document.getElementById('settingsEditor').value;
  const r = await fetch('/api/settings', { method:'POST', headers:{'Content-Type':'application/json'}, body: content });
  const d = await r.json();
  const el = document.getElementById('settingsMsg');
  if (d.ok) { el.className='ok'; el.textContent = d.note || '已保存'; } else { el.className='err'; el.textContent = '保存失败：' + (d.error||''); }
}

// ---- 赛道记录 ----
async function loadRankList() {
  const list = await jget('/api/trackrank') || [];
  document.getElementById('rankList').innerHTML = list.map(f =>
    `<button onclick="loadRankFile('${f.name.replace(/'/g, "\\'")}')">${f.name}<div class="muted">${f.size} B · ${f.modified}</div></button>`).join('')
    || '<div class="muted">TrackRank 目录为空</div>';
}

async function loadRankFile(name) {
  document.getElementById('rankTitle').textContent = name;
  const d = await jget(`/api/trackrank/file?name=${encodeURIComponent(name)}`);
  document.getElementById('rankEditor').value = d.content || '';
}

loadOverview();
setInterval(() => { if (!document.getElementById('tab-overview').classList.contains('hide')) loadOverview(); }, 5000);
</script>
</body>
</html>
""";
    }
}
