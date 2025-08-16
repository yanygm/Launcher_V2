using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using KartRider.Common.Utilities;

namespace RHOParser
{
    public class CompetitiveData
    {
        public uint Track { get; set; }
        public short Kart { get; set; }
        public uint Time { get; set; }
        public short Boooster { get; set; }
        public uint BooosterPoint { get; set; }
        public short Crash { get; set; }
        public uint CrashPoint { get; set; }
        public uint Point { get; set; }
    }

    public class CompetitiveDataManager
    {
        private string _filePath = AppDomain.CurrentDomain.BaseDirectory + @"Profile\Competitive.xml";

        public CompetitiveDataManager()
        {
            // 如果文件不存在则创建
            if (!File.Exists(_filePath))
            {
                CreateNewFile();
            }
        }

        // 创建新的XML文件
        private void CreateNewFile()
        {
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("CompetitiveDataList")
            );
            doc.Save(_filePath);
        }

        // 保存数据，如果Track重复则比较Time，Time小则替换
        public void SaveData(CompetitiveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            XDocument doc = XDocument.Load(_filePath);
            var root = doc.Root;

            // 查找是否存在相同的Track
            var existingElement = root.Elements("Data")
                .FirstOrDefault(e => (uint)e.Attribute("Track") == data.Track);

            if (existingElement != null)
            {
                // 存在相同Track，比较Time
                uint existingTime = (uint)existingElement.Attribute("Time");
                if (data.Time < existingTime)
                {
                    // Time更小，替换数据
                    existingElement.Attribute("Track").SetValue(data.Track);
                    existingElement.Attribute("Kart").SetValue(data.Kart);
                    existingElement.Attribute("Time").SetValue(data.Time);
                    existingElement.Attribute("Boooster").SetValue(data.Boooster);
                    existingElement.Attribute("BooosterPoint").SetValue(data.BooosterPoint);
                    existingElement.Attribute("Crash").SetValue(data.Crash);
                    existingElement.Attribute("CrashPoint").SetValue(data.CrashPoint);
                    existingElement.Attribute("Point").SetValue(data.Point);
                }
                // 如果Time不更小则不做处理
            }
            else
            {
                // 不存在相同Track，添加新数据
                root.Add(new XElement("Data",
                    new XAttribute("Track", data.Track),
                    new XAttribute("Kart", data.Kart),
                    new XAttribute("Time", data.Time),
                    new XAttribute("Boooster", data.Boooster),
                    new XAttribute("BooosterPoint", data.BooosterPoint),
                    new XAttribute("Crash", data.Crash),
                    new XAttribute("CrashPoint", data.CrashPoint),
                    new XAttribute("Point", data.Point)
                ));
            }

            doc.Save(_filePath);
        }

        // 读取所有数据
        public List<CompetitiveData> LoadAllData()
        {
            if (!File.Exists(_filePath))
                return new List<CompetitiveData>();

            XDocument doc = XDocument.Load(_filePath);

            return doc.Root.Elements("Data")
                .Select(e => new CompetitiveData
                {
                    Track = (uint)e.Attribute("Track"),
                    Kart = (short)e.Attribute("Kart"),
                    Time = (uint)e.Attribute("Time"),
                    Boooster = (short)e.Attribute("Boooster"),
                    BooosterPoint = (uint)e.Attribute("BooosterPoint"),
                    Crash = (short)e.Attribute("Crash"),
                    CrashPoint = (uint)e.Attribute("CrashPoint"),
                    Point = (uint)e.Attribute("Point")
                })
                .ToList();
        }
    }

    public class TrackIdExtractor
    {
        public List<string> GetCurrentWeekTrackIds(XDocument doc)
        {
            try
            {
                DateTime now = DateTime.Now; // 获取当前时间

                // 查找当前时间处于其开放周期内的Competitive节点
                var currentCompetitive = doc.Descendants("Competitive")
                    .FirstOrDefault(competitive =>
                        IsTimeInPeriod(now, competitive.Attribute("openPeriod").Value));

                if (currentCompetitive == null)
                {
                    Console.WriteLine("未找到当前时间所在的Competitive周期");
                    return new List<string>();
                }

                // 解析当前Competitive的开始时间
                string openPeriod = currentCompetitive.Attribute("openPeriod").Value;
                DateTime startTime = ParsePeriodStart(openPeriod);

                // 计算当前时间属于第几周（1-4）
                int weekNumber = CalculateWeekNumber(startTime, now);

                // 查找对应的Set节点
                var targetSet = currentCompetitive.Descendants("Set")
                    .FirstOrDefault(set =>
                        set.Attribute("setId").Value == weekNumber.ToString());

                if (targetSet == null)
                {
                    Console.WriteLine($"未找到setId为{weekNumber}的Set节点");
                    return new List<string>();
                }

                // 提取该Set下的所有trackId
                var trackIds = targetSet.Descendants("slot")
                    .Select(slot => slot.Attribute("trackId").Value)
                    .ToList();

                return trackIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理过程中发生错误: {ex.Message}");
                return new List<string>();
            }
        }

        // 判断时间是否在周期内
        private bool IsTimeInPeriod(DateTime time, string period)
        {
            string[] periodParts = period.Split('~');
            if (periodParts.Length != 2)
                return false;

            if (DateTime.TryParseExact(periodParts[0], "yyyy-M-dTHH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) &&
                DateTime.TryParseExact(periodParts[1], "yyyy-M-dTHH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
            {
                return time >= start && time <= end;
            }

            return false;
        }

        // 解析周期开始时间
        private DateTime ParsePeriodStart(string period)
        {
            string startPart = period.Split('~')[0];
            return DateTime.ParseExact(startPart, "yyyy-M-dTHH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        // 计算当前时间属于第几周（1-4）
        private int CalculateWeekNumber(DateTime startTime, DateTime currentTime)
        {
            TimeSpan span = currentTime - startTime;
            int totalDays = (int)span.TotalDays;
            int weekNumber = (totalDays / 7) + 1;

            // 确保周数在1-4范围内
            return Math.Clamp(weekNumber, 1, 4);
        }
    }

    public class CompleteTrackScoreCalculator
    {
        /// <summary>
        /// 从XML文件加载赛道数据
        /// </summary>
        public Dictionary<uint, TrackData> LoadFromXml(XDocument doc)
        {
            Dictionary<uint, TrackData> TrackDictionary = new Dictionary<uint, TrackData>();

            foreach (XElement trackElement in doc.Descendants("TrackInfo"))
            {
                uint trackId = Adler32Helper.GenerateAdler32_UNICODE(trackElement.Attribute("trackId").Value, 0);
                uint standardTime = uint.Parse(trackElement.Attribute("time").Value);

                TrackData trackData = new TrackData
                {
                    TrackId = trackId,
                    StandardTime = standardTime
                };

                foreach (XElement bonusElement in trackElement.Elements("driveBonus"))
                {
                    string type = bonusElement.Attribute("type").Value;
                    uint count = uint.Parse(bonusElement.Attribute("count").Value);
                    uint point = uint.Parse(bonusElement.Attribute("point").Value);

                    trackData.Bonuses.Add(new DriveBonus
                    {
                        Type = type,
                        Count = count,
                        Point = point
                    });
                }

                TrackDictionary[trackId] = trackData;
            }
            return TrackDictionary;
        }

        /// <summary>
        /// 计算指定赛道的详细得分（碰撞得分只取最高优先级）
        /// </summary>
        public TrackScoreDetails CalculateTrackScoreDetails(
            uint trackId,
            uint actualTime,
            short actualBoostCount,
            short actualCrashCount,
            Dictionary<uint, TrackData> TrackDictionary)
        {
            if (!TrackDictionary.ContainsKey(trackId))
            {
                Console.WriteLine($"找不到赛道ID: {trackId}");
                return null;
            }

            TrackData track = TrackDictionary[trackId];
            uint timeScore = CalculateTimeScore(track.StandardTime, actualTime);
            uint boostScore = CalculateBoostScore(track.Bonuses, actualBoostCount);
            uint crashScore = CalculateCrashScoreWithPriority(track.Bonuses, actualCrashCount);

            return new TrackScoreDetails
            {
                TrackId = trackId,
                TotalScore = timeScore + boostScore + crashScore,
                TimeScore = timeScore,
                BoostScore = boostScore,
                CrashScore = crashScore,
            };
        }

        /// <summary>
        /// 计算加速得分（累加所有符合条件的分数）
        /// </summary>
        private uint CalculateBoostScore(List<DriveBonus> bonuses, short actualBoostCount)
        {
            uint score = 0;
            foreach (var bonus in bonuses.Where(b => b.Type == "boooster"))
            {
                if (actualBoostCount > bonus.Count)
                {
                    score += bonus.Point;
                }
            }
            return score;
        }

        /// <summary>
        /// 计算碰撞得分（只取最高优先级的符合条件分数）
        /// 优先级：count值越小，优先级越高（条件越严格）
        /// </summary>
        private uint CalculateCrashScoreWithPriority(List<DriveBonus> bonuses, short actualCrashCount)
        {
            // 获取所有crash类型的奖励，并按count升序排序（优先级从高到低）
            var crashBonuses = bonuses
                .Where(b => b.Type == "crash")
                .OrderBy(b => b.Count)
                .ToList();

            // 依次检查，返回第一个符合条件的分数（最高优先级）
            foreach (var bonus in crashBonuses)
            {
                if (actualCrashCount < bonus.Count)
                {
                    return bonus.Point;
                }
            }

            // 没有符合条件的情况
            return 0;
        }

        /// <summary>
        /// 根据标准时间和实际用时计算时间成绩得分
        /// </summary>
        /// <param name="standardTime">标准时间</param>
        /// <param name="actualTime">实际用时</param>
        /// <returns>时间成绩得分</returns>
        private uint CalculateTimeScore(uint standardTime, uint actualTime)
        {
            try
            {
                // 输出计算过程，方便调试
                Console.WriteLine($"\n时间得分计算过程:");
                Console.WriteLine($"标准时间: {standardTime}, 实际时间: {actualTime}");

                // 计算偏差值
                long deviation = (long)actualTime - standardTime; // 使用long避免溢出
                Console.WriteLine($"偏差值: {deviation}");

                double score;
                if (deviation < 0)
                {
                    // 实际时间更少，加分
                    double bonus = Math.Abs(deviation) * 0.5;
                    score = 10000 + bonus;
                    Console.WriteLine($"加分计算: 10000 + {Math.Abs(deviation)} × 0.5 = {10000} + {bonus} = {score}");
                }
                else
                {
                    // 实际时间更多，扣分
                    double penalty = deviation * 0.2;
                    score = 10000 - penalty;
                    Console.WriteLine($"扣分计算: 10000 - {deviation} × 0.2 = {10000} - {penalty} = {score}");
                }

                uint integerPart = (uint)score;
                return integerPart;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算错误: {ex.Message}");
                return 0; // 出错时返回0
            }
        }
    }

    // 赛道得分明细类（包含时间成绩）
    public class TrackScoreDetails
    {
        public uint TrackId { get; set; }
        public uint TotalScore { get; set; }      // 总得分
        public uint TimeScore { get; set; }       // 时间成绩得分
        public uint BoostScore { get; set; }      // 加速得分
        public uint CrashScore { get; set; }      // 碰撞得分
    }

    // 赛道数据类
    public class TrackData
    {
        public uint TrackId { get; set; }
        public uint StandardTime { get; set; }
        public List<DriveBonus> Bonuses { get; set; }

        public TrackData()
        {
            Bonuses = new List<DriveBonus>();
        }
    }

    // 奖励信息类
    public class DriveBonus
    {
        public string Type { get; set; }
        public uint Count { get; set; }
        public uint Point { get; set; }
    }
}
