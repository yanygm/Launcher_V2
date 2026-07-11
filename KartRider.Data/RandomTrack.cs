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

namespace KartRider
{
    public class Track
    {
        public uint hash { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public string gameType { get; set; }
        public bool basicAi { get; set; }
    }

    public class RandomTrack
    {
        public static Dictionary<uint, Track> TrackList = new Dictionary<uint, Track>();
        public static XDocument randomTrack = new XDocument();
        public static Dictionary<uint, string> RandomName = new Dictionary<uint, string> { { 0, "全部随机" }, { 1, "专业竞速随机" }, { 3, "人气随机（极易）" }, { 4, "人气随机（简单）" }, { 5, "人气随机（普通）" }, { 6, "人气随机（困难）" }, { 7, "人气随机（极难）" }, { 8, "新图随机" }, { 30, "反方向随机" }, { 40, "竞速随机" } };

        public static string GameTrack = "village_R01";

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
                Console.WriteLine("[RandomTrack] Warning: TrackList is empty, returning default track: {0}", GameTrack);
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
                RandomTrackSetRandomTrack = "clubSpeed";
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
                RandomTrackSetRandomTrack = "Unknown";
            }

            if (RandomTrackSetRandomTrack == "all" || RandomTrackSetRandomTrack == "speedAll")
            {
                Random random = new Random();
                List<uint> availableTracks = new List<uint>();
                if (FileName.FileNames.ContainsKey(Parent.Client.Nickname))
                {
                    var filename = FileName.FileNames[Parent.Client.Nickname];
                    var FavoriteTrackList = new Favorite_Track();
                    if (File.Exists(filename.FavoriteTrack_LoadFile))
                    {
                        FavoriteTrackList = JsonHelper.DeserializeNoBom<Favorite_Track>(filename.FavoriteTrack_LoadFile) ?? new Favorite_Track();
                    }
                    availableTracks = FavoriteTrackList.GetAllTracks();
                }

                if (availableTracks == null || availableTracks.Count == 0)
                {
                    var allTracks = TrackList.Values.Where(t => t.gameType == RandomTrackGameType).ToList();

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
                        allTracks = TrackList.Values.Where(t => t.gameType == RandomTrackGameType).ToList();
                    }

                    availableTracks = allTracks.Select(t => t.hash).ToList();
                }

                // 如果仍然没有可用 track（TrackList 中没有对应 gameType 的 track），返回默认
                if (availableTracks == null || availableTracks.Count == 0)
                {
                    Console.WriteLine("[RandomTrack] Warning: No available tracks for gameType={0}, ai={1}, returning default track: {2}", RandomTrackGameType, ai, GameTrack);
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
                    Console.WriteLine("[RandomTrack] Warning: No unused tracks available, returning default track: {0}", GameTrack);
                    return Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }
            }
            else if (RandomTrackSetRandomTrack == "Unknown")
            {
                if (TrackList.ContainsKey(Track))
                {
                    return Track;
                }
                else
                {
                    return Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
                }
                Console.WriteLine("RandomTrack: {0} / {1} / {2}", RandomTrackGameType, RandomTrackSetRandomTrack, RandomTrack.GameTrack);
            }
            else
            {
                XDocument doc = randomTrack;
                var TrackSet = doc.Descendants("RandomTrackSet")
                    .FirstOrDefault(rts => (string)rts.Attribute("gameType") == RandomTrackGameType && (string)rts.Attribute("randomType") == RandomTrackSetRandomTrack);

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
                        .FirstOrDefault(rts => (string)rts.Attribute("randomType") == RandomTrackSetRandomTrack);
                    if (TrackList != null)
                    {
                        availableTrackIds = TrackList.Descendants("track")
                            .Select(t => (string)t.Attribute("id"))
                            .Where(id => !string.IsNullOrEmpty(id))
                            .ToList();
                    }
                }

                // 获取这些 track 对应的 hash
                var availableHashes = availableTrackIds
                    .Select(id => Adler32Helper.GenerateAdler32_UNICODE(id, 0))
                    .Where(hash => hash != 0)
                    .ToList();

                // 如果 XML 中没有配置该类型的 track，返回默认
                if (availableHashes.Count == 0)
                {
                    Console.WriteLine("[RandomTrack] Warning: No tracks found in XML for gameType={0}, randomType={1}, returning default track: {2}", RandomTrackGameType, RandomTrackSetRandomTrack, GameTrack);
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

                Console.WriteLine("[RandomTrack] Warning: No unused hashes available, returning default track: {0}", GameTrack);
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
    }
}