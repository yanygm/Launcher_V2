using ExcData;
using KartRider.Common.Utilities;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using KartRider.Logging;

namespace KartRider
{
    public class Track
    {
        public uint hash { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public string gameType { get; set; }
        public bool basicAi { get; set; }

        /// <summary>客户端 track@zz 里的 isOnlyItemTrack：只能用于道具赛。</summary>
        public bool onlyItem { get; set; }

        /// <summary>客户端 trackLocale 里的 blocked：仍然保存进 TrackList，但不会进入随机池。</summary>
        public bool blocked { get; set; }
    }

    public class RandomTrack
    {
        public static Dictionary<uint, Track> TrackList = new Dictionary<uint, Track>();
        public static XDocument randomTrack = new XDocument();
        public static Dictionary<uint, string> RandomName = new Dictionary<uint, string> { { 0, "全部随机" }, { 1, "专业竞速随机" }, { 3, "人气随机（极易）" }, { 4, "人气随机（简单）" }, { 5, "人气随机（普通）" }, { 6, "人气随机（困难）" }, { 7, "人气随机（极难）" }, { 8, "新图随机" }, { 30, "反方向随机" }, { 40, "竞速随机" } };

        public static string GameTrack = "village_R01";

        // track@zz.xml 与 trackLocale@{regionCode}.xml 的内容是重复的（前者带 gameType/仅道具标记，后者带名称/basicAi），
        // 这里用 地图 ID -> hash 的索引做二次去重，保证同一张地图在 TrackList 里只会保存一份。
        private static readonly Dictionary<string, uint> _trackIdIndex = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        // 客户端里 refId 派生的地图 ID 后缀
        private static readonly string[] VariantSuffixes = new[] { "_crz", "_rvs" };

        /// <summary>
        /// 把一张地图（含 refId 派生的变体）合并进 TrackList：
        /// hash 相同或 ID 相同（忽略大小写）的条目不会重复添加，只会补齐缺失的字段。
        /// </summary>
        public static void AddTrack(string trackIdentifier, string name, string gameType = null, bool basicAi = false, bool onlyItem = false, bool blocked = false)
        {
            if (string.IsNullOrWhiteSpace(trackIdentifier)) return;
            string id = trackIdentifier.Trim();
            uint hash = Adler32Helper.GenerateAdler32_UNICODE(id, 0);
            if (hash == 0) return;

            if (_trackIdIndex.TryGetValue(id, out uint indexedHash) && TrackList.TryGetValue(indexedHash, out Track indexed))
            {
                MergeTrack(indexed, name, gameType, basicAi, onlyItem, blocked);
                return;
            }

            if (TrackList.TryGetValue(hash, out Track existing))
            {
                // hash 相同但 ID 不同（Adler32 碰撞）：不新增重复条目，只补齐空字段
                MergeTrack(existing, name, gameType, basicAi, onlyItem, blocked);
                _trackIdIndex[id] = hash;
                return;
            }

            Track track = new Track
            {
                hash = hash,
                ID = id,
                Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim(),
                gameType = gameType == null ? string.Empty : gameType,
                basicAi = basicAi,
                onlyItem = onlyItem,
                blocked = blocked
            };
            TrackList.Add(hash, track);
            _trackIdIndex[id] = hash;

            // 基础地图的 gameType 确定后，把之前已登记但 gameType 为空的 _crz / _rvs 变体补上
            ApplyGameTypeToVariants(track);
        }

        /// <summary>按地图 ID 查询 gameType（查不到返回空字符串）。</summary>
        public static string GetTrackGameType(string trackIdentifier)
        {
            if (string.IsNullOrWhiteSpace(trackIdentifier)) return string.Empty;
            string id = trackIdentifier.Trim();
            if (_trackIdIndex.TryGetValue(id, out uint indexedHash) && TrackList.TryGetValue(indexedHash, out Track indexed))
            {
                return indexed.gameType ?? string.Empty;
            }
            uint hash = Adler32Helper.GenerateAdler32_UNICODE(id, 0);
            return TrackList.TryGetValue(hash, out Track track) ? track.gameType ?? string.Empty : string.Empty;
        }

        private static void MergeTrack(Track track, string name, string gameType, bool basicAi, bool onlyItem, bool blocked)
        {
            if (track == null) return;
            if (!string.IsNullOrWhiteSpace(name)) track.Name = name.Trim();
            if (!string.IsNullOrWhiteSpace(gameType))
            {
                track.gameType = gameType;
                ApplyGameTypeToVariants(track);
            }
            // 两份文件都可能带这些标记，取并集，避免后读到的文件把标记覆盖掉
            track.basicAi |= basicAi;
            track.onlyItem |= onlyItem;
            track.blocked |= blocked;
        }

        /// <summary>
        /// 所有赛道数据加载完成后收尾：缺名称的用 ID 兜底（否则会被随机池过滤掉，表现为“新赛道没读到”）。
        /// </summary>
        public static void FinalizeTrackList()
        {
            int noName = 0;
            int noGameType = 0;
            foreach (Track track in TrackList.Values)
            {
                if (string.IsNullOrWhiteSpace(track.Name))
                {
                    track.Name = track.ID ?? string.Empty;
                    noName++;
                }
                if (string.IsNullOrWhiteSpace(track.gameType))
                {
                    noGameType++;
                }
            }
            Log.Info($"[Track] TrackList 收尾：共 {TrackList.Count} 条，{noName} 条无名称（已用 ID 兜底），{noGameType} 条缺少 gameType");
        }

        private static void ApplyGameTypeToVariants(Track track)
        {
            if (track == null || string.IsNullOrEmpty(track.ID) || string.IsNullOrWhiteSpace(track.gameType)) return;
            foreach (string suffix in VariantSuffixes)
            {
                if (_trackIdIndex.TryGetValue(track.ID + suffix, out uint variantHash) &&
                    TrackList.TryGetValue(variantHash, out Track variant) &&
                    string.IsNullOrWhiteSpace(variant.gameType))
                {
                    variant.gameType = track.gameType;
                }
            }
        }

        // 每个 Nickname 独立记录已使用的随机 track
        private static Dictionary<string, HashSet<uint>> _usedTracks = new Dictionary<string, HashSet<uint>>();
        // 记录每个 Nickname 上一次使用的 Track，用于检测 Track 是否改变
        private static Dictionary<string, uint> _lastTrack = new Dictionary<string, uint>();

        public static string GetTrackName(uint trackId)
        {
            if (RandomName.ContainsKey(trackId))
            {
                return RandomName[trackId];
            }
            else if (TrackList.ContainsKey(trackId))
            {
                return TrackList[trackId].Name;
            }
            else
            {
                return trackId.ToString();
            }
        }

        public static uint GetHash(string trackName)
        {
            var sourceList = TrackList.Values.Select(t => t.Name).ToList();
            if (string.IsNullOrEmpty(trackName) || sourceList == null)
                return 0;

            // 优先精确匹配
            var exactMatch = sourceList.FirstOrDefault(s => s == trackName);
            if (!string.IsNullOrEmpty(exactMatch))
            {
                return TrackList.Keys.FirstOrDefault(k => TrackList[k].Name == exactMatch);
            }

            var scored = sourceList
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => new
                {
                    Str = s,
                    // 子串匹配得分
                    SubstringScore = s.Contains(trackName) ? (double)trackName.Length / s.Length : 0,
                    // 前缀匹配得分
                    PrefixScore = s.StartsWith(trackName) ? 1.0 : (s.StartsWith(trackName.Substring(0, Math.Min(trackName.Length, 2))) ? 0.8 : 0),
                    // 最长公共子序列得分（考虑顺序）
                    LcseqScore = CalculateLCSeqScore(s, trackName),
                    // 字符包含得分（基础分）
                    ContainScore = (double)s.Count(c => trackName.Contains(c)) / s.Length
                })
                .Select(x => new
                {
                    x.Str,
                    // 综合得分：子串/前缀匹配最高优先，其次是顺序匹配，最后是字符包含
                    Similarity = Math.Max(Math.Max(x.SubstringScore, x.PrefixScore), Math.Max(x.LcseqScore, x.ContainScore))
                })
                .Where(x => x.Similarity > 0.3) // 提高阈值，过滤低相似度
                .ToList();

            if (!scored.Any()) return 0;

            string name = scored.OrderByDescending(x => x.Similarity).First().Str;
            return TrackList.Keys.FirstOrDefault(k => TrackList[k].Name == name);
        }

        /// <summary>
        /// 计算最长公共子序列得分（考虑字符顺序）
        /// </summary>
        private static double CalculateLCSeqScore(string source, string target)
        {
            int m = source.Length;
            int n = target.Length;
            int[,] dp = new int[m + 1, n + 1];

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (source[i - 1] == target[j - 1])
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    else
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }

            int lcsLength = dp[m, n];
            // 得分 = LCS长度 / 较长字符串长度
            return (double)lcsLength / Math.Max(m, n);
        }

        public static uint GetRandomTrack(SessionGroup Parent, string usedTracksName, byte GameType, uint Track, bool ai = false)
        {
            // 如果 TrackList 为空，直接返回默认 Track 的 hash，避免返回无效值
            if (TrackList == null || TrackList.Count == 0)
            {
                Log.Info($"[RandomTrack] Warning: TrackList is empty, returning default track: {GameTrack}");
                return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
            }

            string RandomTrackGameType = "speed";
            string RandomTrackSetRandomTrack = "all";

            // 检测 Track 是否改变，改变则清除该 Nickname 的记录
            if (_lastTrack.TryGetValue(usedTracksName, out uint lastTrack) && lastTrack != Track)
            {
                _usedTracks.Remove(usedTracksName);
            }
            _lastTrack[usedTracksName] = Track;

            // 获取或初始化该 Nickname 的已使用记录
            if (!_usedTracks.ContainsKey(usedTracksName))
            {
                _usedTracks[usedTracksName] = new HashSet<uint>();
            }
            var usedTracks = _usedTracks[usedTracksName];

            if (GameType == 0)
            {
                RandomTrackGameType = "speed";
            }
            else if (GameType == 1)
            {
                RandomTrackGameType = "item";
            }

            if (Track == 0)
            {
                RandomTrackSetRandomTrack = "all";
            }
            else if (Track == 1)
            {
                // 道具赛的"专业"集合叫 clubItem，竞速才叫 clubSpeed
                RandomTrackSetRandomTrack = GameType == 1 ? "clubItem" : "clubSpeed";
            }
            else if (Track == 3)
            {
                RandomTrackSetRandomTrack = "hot1";
            }
            else if (Track == 4)
            {
                RandomTrackSetRandomTrack = "hot2";
            }
            else if (Track == 5)
            {
                RandomTrackSetRandomTrack = "hot3";
            }
            else if (Track == 6)
            {
                RandomTrackSetRandomTrack = "hot4";
            }
            else if (Track == 7)
            {
                RandomTrackSetRandomTrack = "hot5";
            }
            else if (Track == 8)
            {
                RandomTrackSetRandomTrack = "new";
            }
            else if (Track == 23)
            {
                RandomTrackSetRandomTrack = "crazy";
            }
            else if (Track == 30)
            {
                RandomTrackSetRandomTrack = "reverse";
            }
            else if (Track == 40)
            {
                RandomTrackSetRandomTrack = "speedAll";
            }
            else
            {
                // 具体地图（单人选择/计时赛）：Track 本身就是赛道 hash，直接返回，
                // 不能落到"全部随机"——v24 把这里改成随机后，选单图会变成随机图。
                if (TrackList.ContainsKey(Track))
                {
                    return Track;
                }

                Log.Info($"[RandomTrack] Warning: 未识别的随机模式 {Track}，按全部随机处理");
                RandomTrackSetRandomTrack = "all";
            }

            if (RandomTrackSetRandomTrack == "all")
            {
                Random random = new Random();
                List<uint> availableTracks = new List<uint>();
                if (availableTracks == null || availableTracks.Count == 0)
                {
                    // RandomTrackGameType == "speed"：取出 speed + item 的赛道；== "item"：只取出 item 的赛道
                    var allTracks = TrackList.Values
                        .Where(t => t.gameType == RandomTrackGameType || (RandomTrackGameType == "speed" && t.gameType == "item"))
                        .Where(t => !string.IsNullOrEmpty(t.ID) && !string.IsNullOrEmpty(t.Name))
                        .ToList();

                    // 如果 ai 为 true，筛选出 basicAi == true 的 track
                    if (ai)
                    {
                        allTracks = allTracks
                            .Where(t => t.basicAi)
                            .ToList();
                    }

                    // 如果 AI 过滤后没有可用 track，回退到不过滤 AI
                    if (allTracks.Count == 0 && ai)
                    {
                        allTracks = TrackList.Values
                            .Where(t => t.gameType == RandomTrackGameType || (RandomTrackGameType == "speed" && t.gameType == "item"))
                            .Where(t => !string.IsNullOrEmpty(t.ID) && !string.IsNullOrEmpty(t.Name))
                            .ToList();
                    }

                    // 客户端标了 blocked 的赛道不进随机池
                    allTracks = allTracks.Where(t => !t.blocked).ToList();

                    // 竞速房排除"仅道具"赛道（客户端 isOnlyItemTrack）
                    if (RandomTrackGameType == "speed")
                    {
                        allTracks = allTracks.Where(t => !t.onlyItem).ToList();
                    }

                    availableTracks = allTracks.Select(t => t.hash).ToList();
                }

                // 如果仍然没有可用 track（TrackList 中没有对应 gameType 的 track），返回默认
                if (availableTracks == null || availableTracks.Count == 0)
                {
                    Log.Info($"[RandomTrack] Warning: No available tracks for gameType={RandomTrackGameType}, ai={ai}, returning default track: {GameTrack}");
                    return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }

                // 排除已使用的 track
                var unusedTracks = availableTracks.Where(t => !usedTracks.Contains(t)).ToList();

                // 如果所有 track 都已使用完，重置记录
                if (unusedTracks.Count == 0)
                {
                    usedTracks.Clear();
                    unusedTracks = availableTracks;
                }

                if (unusedTracks.Count > 0)
                {
                    uint selectedTrack = unusedTracks[random.Next(unusedTracks.Count)];
                    usedTracks.Add(selectedTrack);
                    return selectedTrack;
                }
                else
                {
                    Log.Info($"[RandomTrack] Warning: No unused tracks available, returning default track: {GameTrack}");
                    return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }
            }
            else if (RandomTrackSetRandomTrack == "speedAll")
            {
                // speedAll（竞速随机）单独处理：直接从 TrackList.Values 中筛选 gameType == "speed" 的赛道
                Random random = new Random();
                List<uint> availableTracks = new List<uint>();

                var allTracks = TrackList.Values
                    .Where(t => t.gameType == "speed")
                    .Where(t => !t.onlyItem && !t.blocked)
                    .Where(t => !string.IsNullOrEmpty(t.ID) && !string.IsNullOrEmpty(t.Name))
                    .ToList();

                // 如果 ai 为 true，筛选出 basicAi == true 的 track
                if (ai)
                {
                    allTracks = allTracks
                        .Where(t => t.basicAi)
                        .ToList();
                }

                // 如果 AI 过滤后没有可用 track，回退到不过滤 AI
                if (allTracks.Count == 0 && ai)
                {
                    allTracks = TrackList.Values
                        .Where(t => t.gameType == "speed")
                        .Where(t => !t.blocked)
                        .Where(t => !string.IsNullOrEmpty(t.ID) && !string.IsNullOrEmpty(t.Name))
                        .ToList();
                }

                availableTracks = allTracks.Select(t => t.hash).ToList();

                // 如果仍然没有可用 track，返回默认
                if (availableTracks == null || availableTracks.Count == 0)
                {
                    Log.Info($"[RandomTrack] Warning: No available tracks for speedAll, ai={ai}, returning default track: {GameTrack}");
                    return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }

                // 排除已使用的 track
                var unusedTracks = availableTracks.Where(t => !usedTracks.Contains(t)).ToList();

                // 如果所有 track 都已使用完，重置记录
                if (unusedTracks.Count == 0)
                {
                    usedTracks.Clear();
                    unusedTracks = availableTracks;
                }

                if (unusedTracks.Count > 0)
                {
                    uint selectedTrack = unusedTracks[random.Next(unusedTracks.Count)];
                    usedTracks.Add(selectedTrack);
                    return selectedTrack;
                }
                else
                {
                    Log.Info($"[RandomTrack] Warning: No unused tracks available, returning default track: {GameTrack}");
                    return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }
            }
            else
            {
                // 从 XML 中获取该类型配置的赛道
                List<uint> availableHashes = GetRandomTracksFromXml(RandomTrackGameType, RandomTrackSetRandomTrack);

                // 如果 XML 中没有配置该类型的 track，返回默认
                if (availableHashes.Count == 0)
                {
                    Log.Info($"[RandomTrack] Warning: No tracks found in XML for gameType={RandomTrackGameType}, randomType={RandomTrackSetRandomTrack}, returning default track: {GameTrack}");
                    return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }

                // 排除已使用的 track
                var unusedHashes = availableHashes.Where(h => !usedTracks.Contains(h)).ToList();

                // 如果所有 track 都已使用完，重置记录
                if (unusedHashes.Count == 0)
                {
                    usedTracks.Clear();
                    unusedHashes = availableHashes;
                }

                if (unusedHashes.Count > 0)
                {
                    Random random = new Random();
                    uint selectedHash = unusedHashes[random.Next(unusedHashes.Count)];
                    usedTracks.Add(selectedHash);
                    return selectedHash;
                }

                Log.Info($"[RandomTrack] Warning: No unused hashes available, returning default track: {GameTrack}");
                return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
            }
        }

        /// <summary>
        /// 清除指定 Nickname 的已使用记录
        /// </summary>
        public static void ClearUsedTracks(string usedTracksName)
        {
            _usedTracks.Remove(usedTracksName);
            _lastTrack.Remove(usedTracksName);
        }

        /// <summary>
        /// 清除所有已使用记录
        /// </summary>
        public static void ClearAllUsedTracks()
        {
            _usedTracks.Clear();
            _lastTrack.Clear();
        }

        /// <summary>
        /// 从 randomTrack XML 中获取指定 gameType / randomType 配置的赛道 hash 列表
        /// </summary>
        private static List<uint> GetRandomTracksFromXml(string gameType, string randomType)
        {
            XDocument doc = randomTrack;
            var TrackSet = doc.Descendants("RandomTrackSet")
                .FirstOrDefault(rts => (string)rts.Attribute("gameType") == gameType && (string)rts.Attribute("randomType") == randomType);

            List<string> availableTrackIds = new List<string>();

            if (TrackSet != null)
            {
                availableTrackIds = TrackSet.Descendants("track")
                    .Select(t => (string)t.Attribute("id"))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();
            }
            else
            {
                var TrackList = doc.Descendants("RandomTrackList")
                    .FirstOrDefault(rts => (string)rts.Attribute("randomType") == randomType);
                if (TrackList != null)
                {
                    availableTrackIds = TrackList.Descendants("track")
                        .Select(t => (string)t.Attribute("id"))
                        .Where(id => !string.IsNullOrEmpty(id))
                        .ToList();
                }
            }

            // 获取这些 track 对应的 hash（集合里可能有重复项，先去掉）
            List<uint> hashes = availableTrackIds
                .Select(id => Adler32Helper.GenerateAdler32_UNICODE(id, 0))
                .Where(hash => hash != 0)
                .Distinct()
                .ToList();

            // 客户端集合里可能混着竞速/道具赛道：按房间模式过滤（过滤后为空则保留原集合）
            List<uint> filtered = hashes
                .Where(h => TrackList.TryGetValue(h, out Track track)
                            && !track.onlyItem
                            && !track.blocked
                            && (track.gameType == gameType || gameType == "speed" && track.gameType == "item"))
                .ToList();
            return filtered.Count > 0 ? filtered : hashes;
        }
    }
}