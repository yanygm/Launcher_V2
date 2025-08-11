using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RHOParser
{
    public class CompetitiveData
    {
        public uint Track { get; set; }
        public short Kart { get; set; }
        public uint Time { get; set; }
        public short Auf { get; set; }
        public short Cf { get; set; }
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
                    existingElement.Attribute("Auf").SetValue(data.Auf);
                    existingElement.Attribute("Cf").SetValue(data.Cf);
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
                    new XAttribute("Auf", data.Auf),
                    new XAttribute("Cf", data.Cf)
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
                    Auf = (short)e.Attribute("Auf"),
                    Cf = (short)e.Attribute("Cf")
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
}
