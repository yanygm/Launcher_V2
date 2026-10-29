using ExcData;
using KartLibrary.File;
using KartLibrary.File.Lpk;
using KartRider;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using Profile;
using RiderData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace KartRider
{
    public static class KartRhoFile
    {
        public static string regionCode = "cn";

        public static LpkFolderManager Dump(string input)
        {
            try
            {
                // 使用 lpk-unpacker 读取游戏 Data 分包（.lpk）
                LpkFolderManager packFolderManager = new LpkFolderManager();
                try
                {
                    Console.WriteLine("开始读取游戏Data内文件(lpk)...");
                    Console.WriteLine("==============================");
                    packFolderManager.OpenDataFolder(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening lpk data: {ex.Message}");
                    return null;
                }

                regionCode = packFolderManager.GetRegionCode();

                Queue<LpkFolderInfo> packFolderInfoQueue = new Queue<LpkFolderInfo>();
                packFolderInfoQueue.Enqueue(packFolderManager.GetRootFolder());
                while (packFolderInfoQueue.Count > 0)
                {
                    LpkFolderInfo packFolderInfo1 = packFolderInfoQueue.Dequeue();
                    foreach (LpkFileInfo packFileInfo in packFolderInfo1.GetFilesInfo())
                    {
                        // 单个文件解析失败不再中断整份数据加载（补丁包里出现过畸形 XML，直接把整个 Data 加载打断）
                        try
                        {
                            string fullName = ReplacePath(packFileInfo.FullName);
                            if (fullName.Contains("flyingPet") && fullName.Contains($"param@{regionCode}.xml"))
                            {
                                Console.WriteLine(fullName);
                                string name = fullName.Substring(10, fullName.Length - 23);
                                if (!(FlyingPet.flyingSpec.ContainsKey(name)))
                                {
                                    byte[] data = packFileInfo.GetData();
                                    using (MemoryStream stream = new MemoryStream(data))
                                    {
                                        XmlDocument flying = new XmlDocument();
                                        flying.Load(stream);
                                        FlyingPet.flyingSpec.Add(name, flying);
                                    }
                                }
                            }
                            if (fullName == $"track/common/randomTrack@{regionCode}.xml" ||
                                fullName == $"track_/common/randomTrack@{regionCode}.xml"
                                )
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    RandomTrack.randomTrack = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"track/common/track@zz.xml" ||
                                fullName == $"track_/common/track@zz.xml"
                                )
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument trackLocale = new XmlDocument();
                                    trackLocale.Load(stream);
                                    ProcessTrackList(trackLocale);
                                }
                            }
                            if (fullName == $"track/common/trackLocale@{regionCode}.xml" ||
                                fullName == $"track_/common/trackLocale@{regionCode}.xml"
                                )
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument trackLocale = new XmlDocument();
                                    trackLocale.Load(stream);
                                    ProcessTrackLocale(trackLocale);
                                }
                            }
                            if (fullName == "etc_/itemTable.kml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);

                                    var kartsWithName = doc.Descendants("kart").Where(kart => kart.Attribute("name") != null);
                                    if (kartsWithName.Count() > 0)
                                    {
                                        foreach (var kart in kartsWithName)
                                        {
                                            int id = int.Parse(kart.Attribute("id").Value);
                                            string name = kart.Attribute("name").Value;

                                            byte grade = byte.Parse(kart.Attribute("grade")?.Value ?? "0");

                                            List<short> defaultEnchant = (kart.Attribute("defaultEnchant")?.Value ?? "")
                                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => short.TryParse(s.Trim(), out var v) ? (short?)v : null)
                                                .Where(v => v.HasValue)
                                                .Select(v => v.Value)
                                                .ToList();

                                            if (Kart.kartName.ContainsKey(id))
                                            {
                                                var kartTable = Kart.kartName[id];
                                                if (kartTable.grade == 0 && grade != 0)
                                                {
                                                    kartTable.grade = grade;
                                                }
                                                if (kartTable.defaultEnchant.Count < 1 && defaultEnchant.Count > 0)
                                                {
                                                    kartTable.defaultEnchant = defaultEnchant;
                                                }
                                            }
                                            else
                                            {
                                                var kartTable = new KartTable { Name = name, grade = grade, defaultEnchant = defaultEnchant };
                                                Kart.kartName.Add(id, kartTable);
                                            }
                                        }
                                    }

                                    var flyingWithName = doc.Descendants("flyingPet").Where(kart => kart.Attribute("name") != null);
                                    if (flyingWithName.Count() > 0)
                                    {
                                        foreach (var flyingPet in flyingWithName)
                                        {
                                            int id = int.Parse(flyingPet.Attribute("id").Value);
                                            string name = flyingPet.Attribute("name").Value;
                                            if (!(FlyingPet.flyingName.ContainsKey(id)))
                                            {
                                                FlyingPet.flyingName.Add(id, name);
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName == $"etc_/itemTable@{regionCode}.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    var kartsWithName = doc.Descendants("kart").Where(kart => kart.Attribute("name") != null);
                                    if (kartsWithName.Count() > 0)
                                    {
                                        foreach (var kart in kartsWithName)
                                        {
                                            int id = int.Parse(kart.Attribute("id").Value);
                                            string name = kart.Attribute("name").Value;

                                            byte grade = byte.Parse(kart.Attribute("grade")?.Value ?? "0");

                                            List<short> defaultEnchant = (kart.Attribute("defaultEnchant")?.Value ?? "")
                                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => short.TryParse(s.Trim(), out var v) ? (short?)v : null)
                                                .Where(v => v.HasValue)
                                                .Select(v => v.Value)
                                                .ToList();

                                            if (Kart.kartName.ContainsKey(id))
                                            {
                                                var kartTable = Kart.kartName[id];
                                                if (kartTable.grade == 0 && grade != 0)
                                                {
                                                    kartTable.grade = grade;
                                                }
                                                if (kartTable.defaultEnchant.Count < 1 && defaultEnchant.Count > 0)
                                                {
                                                    kartTable.defaultEnchant = defaultEnchant;
                                                }
                                            }
                                            else
                                            {
                                                var kartTable = new KartTable { Name = name, grade = grade, defaultEnchant = defaultEnchant };
                                                Kart.kartName.Add(id, kartTable);
                                            }
                                        }
                                    }
                                    var flyingsWithName = doc.Descendants("flyingPet").Where(flyingPet => flyingPet.Attribute("name") != null);
                                    if (flyingsWithName.Count() > 0)
                                    {
                                        foreach (var flyingPet in flyingsWithName)
                                        {
                                            int id = int.Parse(flyingPet.Attribute("id").Value);
                                            string name = flyingPet.Attribute("name").Value;
                                            if (FlyingPet.flyingName.ContainsKey(id))
                                            {
                                                FlyingPet.flyingName[id] = name;
                                            }
                                            else
                                            {
                                                FlyingPet.flyingName.Add(id, name);
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName == $"etc_/emblem/emblem@{regionCode}.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(stream);
                                    XmlNodeList bodyParams = doc.GetElementsByTagName("emblem");
                                    if (bodyParams.Count > 0)
                                    {
                                        foreach (XmlNode xn in bodyParams)
                                        {
                                            XmlElement xe = (XmlElement)xn;
                                            short id;
                                            if (short.TryParse(xe.GetAttribute("id"), out id))
                                            {
                                                if (!Emblem.emblem.Contains(id))
                                                {
                                                    Emblem.emblem.Add(id);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName == $"etc_/riderSchool/riderSchoolLocale@{regionCode}.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument xdoc = XDocument.Load(stream);

                                    var validCatLevels = xdoc.Descendants("category")
                                        // 获取 catLevel 属性值，过滤掉 null 或空值
                                        .Select(c => c.Attribute("catLevel")?.Value)
                                        .Where(levelStr => !string.IsNullOrEmpty(levelStr))
                                        // 尝试转换为整数，过滤掉转换失败的（如非数字格式）
                                        .Select(levelStr =>
                                        {
                                            byte.TryParse(levelStr, out byte level);
                                            return level;
                                        })
                                        // 过滤掉转换后为 0 的无效值（默认转换失败返回 0）
                                        .Where(level => level > 0);

                                    if (validCatLevels.Any())
                                    {
                                        RiderSchool.catLevel = validCatLevels.Max();
                                    }

                                    // 筛选 catLevel='6' 的 category 节点
                                    var targetCategory = xdoc.Descendants("category")
                                        .FirstOrDefault(c => c.Attribute("catLevel")?.Value == RiderSchool.catLevel.ToString());

                                    if (targetCategory != null)
                                    {
                                        List<byte> validSteps = targetCategory.Descendants("item")
                                           .Select(item => item.Attribute("step")?.Value)
                                           .Where(stepStr => !string.IsNullOrEmpty(stepStr))
                                           .Select(stepStr =>
                                           {
                                               byte.TryParse(stepStr, out byte step);
                                               return step;
                                           })
                                           .Where(step => step != 0) // 过滤转换失败的无效值
                                           .OrderBy(step => step)   // 升序排序
                                           .ToList();

                                        if (validSteps.Any())
                                        {
                                            RiderSchool.maxStep = validSteps.Max();

                                            // 按索引拆分：偶数列（索引 0、2、4...）
                                            RiderSchool.evenProStep = validSteps
                                                .Where((step, index) => index % 2 == 0) // 索引%2==0 → 偶索引
                                                .ToList();

                                            // 按索引拆分：奇数列（索引 1、3、5...）
                                            RiderSchool.oddProStep = validSteps
                                                .Where((step, index) => index % 2 != 0) // 索引%2!=0 → 奇索引
                                                .ToList();
                                        }
                                    }
                                }
                            }
                            if (fullName.Contains("kart_") && fullName.Contains($"/param@{regionCode}.xml"))
                            {
                                Console.WriteLine(fullName);
                                string name = fullName.Substring(6, fullName.Length - 19);
                                if (!(Kart.kartSpec.ContainsKey(name)))
                                {
                                    byte[] data = ReplaceBytes(packFileInfo.GetData());
                                    if (data[2] == 13 && data[3] == 0 && data[4] == 10 && data[5] == 0)
                                    {
                                        byte[] newBytes = new byte[data.Length - 4];
                                        newBytes[0] = 255;
                                        newBytes[1] = 254;
                                        Array.Copy(data, 6, newBytes, 2, data.Length - 6);
                                        using (MemoryStream stream = new MemoryStream(newBytes))
                                        {
                                            XmlDocument kart1 = new XmlDocument();
                                            kart1.Load(stream);
                                            Kart.kartSpec.Add(name, kart1);
                                        }
                                    }
                                    else
                                    {
                                        using (MemoryStream stream = new MemoryStream(data))
                                        {
                                            XmlDocument kart2 = new XmlDocument();
                                            kart2.Load(stream);
                                            Kart.kartSpec.Add(name, kart2);
                                        }
                                    }
                                }
                            }
                            if (fullName.Contains("kart_") && fullName.Contains($"/param@{regionCode}.kml"))
                            {
                                string name = fullName.Substring(6, fullName.Length - 19);
                                bool containsTarget = packFolderInfo1.GetFilesInfo().Any(pf => ReplacePath(pf.FullName) == $"kart_/{name}/param@{regionCode}.xml");
                                if (!containsTarget)
                                {
                                    Console.WriteLine(fullName);
                                    if (!(Kart.kartSpec.ContainsKey(name)))
                                    {
                                        byte[] data = ReplaceBytes(packFileInfo.GetData());
                                        if (data[2] == 13 && data[3] == 0 && data[4] == 10 && data[5] == 0)
                                        {
                                            byte[] newBytes = new byte[data.Length - 4];
                                            newBytes[0] = 255;
                                            newBytes[1] = 254;
                                            Array.Copy(data, 6, newBytes, 2, data.Length - 6);
                                            using (MemoryStream stream = new MemoryStream(newBytes))
                                            {
                                                XmlDocument kart1 = new XmlDocument();
                                                kart1.Load(stream);
                                                Kart.kartSpec.Add(name, kart1);
                                            }
                                        }
                                        else
                                        {
                                            using (MemoryStream stream = new MemoryStream(data))
                                            {
                                                XmlDocument kart2 = new XmlDocument();
                                                kart2.Load(stream);
                                                Kart.kartSpec.Add(name, kart2);
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName.Contains("kart_") && fullName.Contains("/param.xml"))
                            {
                                string name = fullName.Substring(6, fullName.Length - 16);
                                bool containsTarget = packFolderInfo1.GetFilesInfo().Any(pf => ReplacePath(pf.FullName) == $"kart_/{name}/param@{regionCode}.xml" || ReplacePath(pf.FullName) == $"kart_/{name}/param@{regionCode}.kml");
                                if (!containsTarget)
                                {
                                    Console.WriteLine(fullName);
                                    if (!(Kart.kartSpec.ContainsKey(name)))
                                    {
                                        byte[] data = ReplaceBytes(packFileInfo.GetData());
                                        if (data[2] == 13 && data[3] == 0 && data[4] == 10 && data[5] == 0)
                                        {
                                            byte[] newBytes = new byte[data.Length - 4];
                                            newBytes[0] = 255;
                                            newBytes[1] = 254;
                                            Array.Copy(data, 6, newBytes, 2, data.Length - 6);
                                            using (MemoryStream stream = new MemoryStream(newBytes))
                                            {
                                                XmlDocument kart1 = new XmlDocument();
                                                kart1.Load(stream);
                                                Kart.kartSpec.Add(name, kart1);
                                            }
                                        }
                                        else
                                        {
                                            using (MemoryStream stream = new MemoryStream(data))
                                            {
                                                XmlDocument kart2 = new XmlDocument();
                                                kart2.Load(stream);
                                                Kart.kartSpec.Add(name, kart2);
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName == $"zeta/{regionCode}/quest/QuestAutomation.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument Quest = new XmlDocument();
                                    Quest.Load(stream);
                                    GameSupport.QuestParams = Quest.GetElementsByTagName("QuestItem");
                                }
                            }
                            if (fullName == $"zeta/{regionCode}/quest/kartPassQuestAutomation.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    GameSupport.questInfo = doc.Descendants("kartPassQuestInfo").First();
                                }
                            }
                            if (fullName == $"zeta/{regionCode}/scenario/scenario.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument Scenario = new XmlDocument();
                                    Scenario.Load(stream);
                                    XmlNodeList ScenarioParams = Scenario.GetElementsByTagName("Chapter");
                                    if (ScenarioParams.Count > 0)
                                    {
                                        foreach (XmlNode xn in ScenarioParams)
                                        {
                                            XmlElement xe = (XmlElement)xn;
                                            int id = int.Parse(xe.GetAttribute("id"));
                                            if (!(GameSupport.scenario.Contains(id)))
                                            {
                                                GameSupport.scenario.Add(id);
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName == "item/slot/itemProb_indi@zz.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    SlotData.itemProb_indi = XDocument.Load(stream);
                                }
                            }
                            if (fullName == "item/slot/itemProb_team@zz.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    SlotData.itemProb_team = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/basicAI.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    XElement aiItem = doc.Descendants("aiItem").First();

                                    // 角色Dictionary：键为short类型的角色ID
                                    var aiCharacterDict = aiItem.Elements("character")
                                        .ToDictionary(
                                            c => short.Parse(c.Attribute("id").Value),  // 键：short类型ID
                                            c => new AICharacter
                                            {
                                                Id = short.Parse(c.Attribute("id").Value),
                                                Rids = c.Elements("rid").Select(rid => rid.Attribute("name").Value).ToList(),
                                                Balloons = c.Elements("balloon").Select(b => new AIAccessory
                                                {
                                                    Id = short.Parse(b.Attribute("id").Value),
                                                    Speed = int.Parse(b.Attribute("speed").Value),
                                                    Item = int.Parse(b.Attribute("item").Value)
                                                }).ToList(),
                                                Headbands = c.Elements("headband").Select(h => new AIAccessory
                                                {
                                                    Id = short.Parse(h.Attribute("id").Value),
                                                    Speed = int.Parse(h.Attribute("speed").Value),
                                                    Item = int.Parse(h.Attribute("item").Value)
                                                }).ToList(),
                                                Goggles = c.Elements("goggle").Select(g => new AIAccessory
                                                {
                                                    Id = short.Parse(g.Attribute("id").Value),
                                                    Speed = int.Parse(g.Attribute("speed").Value),
                                                    Item = int.Parse(g.Attribute("item").Value)
                                                }).ToList()
                                            }
                                        );
                                    MultyPlayer.aiCharacterDict = aiCharacterDict;

                                    // 卡丁车Dictionary：键为short类型的卡丁车ID
                                    var aiKartDict = aiItem.Elements("kart")
                                        .ToDictionary(
                                            k => short.Parse(k.Attribute("id").Value),  // 键：short类型ID
                                            k => new AIKart
                                            {
                                                Id = short.Parse(k.Attribute("id").Value),
                                                Speed = int.Parse(k.Attribute("speed").Value),
                                                Item = int.Parse(k.Attribute("item").Value),
                                            }
                                        );
                                    MultyPlayer.aiKartDict = aiKartDict;
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/bingoGachaSet.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    Bingo.bingoGachaSet = doc.Root
                                        .Element("setRewardList")
                                        .Elements("set")
                                        .ToDictionary(
                                            set => (byte)(int)set.Attribute("id"),
                                            set => (uint)set.Element("reward")
                                                          .Element("completeReward")
                                                          .Element("stock")
                                                          .Attribute("id")
                                        );
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/itemDictionary.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    var items = doc.Descendants("item");
                                    foreach (var item in items)
                                    {
                                        short catId = short.Parse(item.Attribute("catId")?.Value ?? "0");
                                        string valuesStr = item.Attribute("values")?.Value;
                                        string[] valuesArray = valuesStr?.Split(',');
                                        for (int i = 0; i < valuesArray.Count(); i++)
                                        {
                                            List<short> Add = new List<short> { catId, short.Parse(valuesArray[i]) };
                                            GameSupport.Dictionary.Add(Add);
                                        }
                                    }
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackMission.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    // 加载文档并解析任务
                                    TimeAttack.timeAttackMission = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackCompetitive.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    // 加载文档并解析任务
                                    TimeAttack.timeAttackCompetitive = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackCompetitiveData.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    // 加载文档并解析任务
                                    TimeAttack.timeAttackCompetitiveData = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/lottery/lottery.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    // 创建XML文档对象
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(stream);

                                    // 获取所有lottery节点
                                    XmlNodeList lotteryNodes = doc.GetElementsByTagName("lottery");

                                    // 遍历所有lottery节点，将各自的reward保存到LotteryList
                                    foreach (XmlNode node in lotteryNodes)
                                    {
                                        XmlElement lotteryElement = node as XmlElement;
                                        if (lotteryElement == null) continue;

                                        // loggingOption下的lottery节点只有itemId没有id，跳过
                                        if (!ushort.TryParse(lotteryElement.GetAttribute("id"), out ushort lotteryId)) continue;

                                        // 获取该lottery下的所有reward节点
                                        XmlNodeList rewardNodes = lotteryElement.SelectNodes("./rewardSet/reward");
                                        if (rewardNodes == null || rewardNodes.Count == 0) continue;

                                        // 按lotteryId缓存对应的奖励数据
                                        Lottery.GetOrCreate(lotteryId).Initialize(rewardNodes, false);
                                    }
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/shop/data/stock.kml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    // 创建XML文档对象
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(stream);

                                    // 获取所有stock节点
                                    XmlNodeList stockNodes = doc.GetElementsByTagName("stock");

                                    // 遍历所有stock节点，将各自的item列表保存到StockList
                                    foreach (XmlNode node in stockNodes)
                                    {
                                        XmlElement stockElement = node as XmlElement;
                                        if (stockElement == null) continue;

                                        // 没有stockId的节点跳过
                                        if (!uint.TryParse(stockElement.GetAttribute("stockId"), out uint stockId)) continue;

                                        // 存在salePrice时，按stockId缓存对应的价格数据
                                        if (stockElement.HasAttribute("salePrice") &&
                                            uint.TryParse(stockElement.GetAttribute("salePrice"), out uint salePrice))
                                        {
                                            // priceType为可选项，解析失败时取默认值
                                            byte.TryParse(stockElement.GetAttribute("priceType"), out byte priceType);

                                            if (!Stock.PriceList.ContainsKey(stockId))
                                            {
                                                Stock.PriceList[stockId] = new price(priceType, salePrice);
                                            }
                                        }

                                        // 获取该stock下的所有item节点
                                        XmlNodeList itemNodes = stockElement.GetElementsByTagName("item");
                                        if (itemNodes == null || itemNodes.Count == 0) continue;

                                        // 解析该stock下的所有item
                                        List<item> itemList = new List<item>();
                                        foreach (XmlNode itemNode in itemNodes)
                                        {
                                            XmlElement itemElement = itemNode as XmlElement;
                                            if (itemElement == null) continue;

                                            // 道具分类Id与道具Id为必填项，解析失败则跳过该item
                                            if (!ushort.TryParse(itemElement.GetAttribute("itemCatId"), out ushort itemCatId)) continue;
                                            if (!ushort.TryParse(itemElement.GetAttribute("itemId"), out ushort itemId)) continue;

                                            // 数量与天数为可选项，解析失败时取默认值
                                            ushort.TryParse(itemElement.GetAttribute("itemCount"), out ushort itemCount);
                                            double.TryParse(itemElement.GetAttribute("expireDay"), out double expireDay);

                                            itemList.Add(new item(itemCatId, itemId, itemCount, expireDay));
                                        }

                                        if (itemList.Count == 0) continue;

                                        // 按stockId缓存对应的道具数据
                                        if (!Stock.StockList.ContainsKey(stockId))
                                        {
                                            Stock.StockList[stockId] = itemList;
                                        }
                                    }
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/shop/data/item.kml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(stream);
                                    XmlNodeList bodyParams = doc.GetElementsByTagName("item");
                                    if (bodyParams.Count > 0)
                                    {
                                        foreach (XmlNode xn in bodyParams)
                                        {
                                            XmlElement xe = (XmlElement)xn;
                                            ushort itemCatId = ushort.Parse(xe.GetAttribute("itemCatId"));
                                            ushort itemId = ushort.Parse(xe.GetAttribute("itemId"));
                                            string itemName = xe.GetAttribute("itemName");
                                            if (!NewRider.items.ContainsKey(itemCatId))
                                            {
                                                NewRider.items[itemCatId] = new Dictionary<ushort, string>();
                                            }
                                            NewRider.items[itemCatId][itemId] = itemName;
                                        }
                                    }
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/channel.xml")
                            {
                                Console.WriteLine(fullName);
                                DateTime now = DateTime.Now;
                                byte[] data = packFileInfo.GetData();
                                byte i = 1;
                                using (MemoryStream stream = new MemoryStream(GameSupport.StripXmlBom(data)))
                                {
                                    XDocument doc = XDocument.Load(stream);

                                    // 遍历所有Channel节点
                                    foreach (var channel in doc.Descendants("Channel"))
                                    {
                                        XAttribute openPeriodAttr = channel.Attribute("openPeriod");

                                        // gameType 68 是双人组队模式（官方抓包值，兼容早期自造的 5/6）：
                                        // 内部归一化成组队模式的 3/4，仅用 DuoTeam 标记区分，保证后续分包/结算逻辑与组队模式一致；
                                        // WireGameType 保留原始值，建房/房间信息里按它下发给客户端
                                        byte channelGameType = byte.Parse(channel.Attribute("gameType").Value);
                                        var channelValue = new Channel
                                        {
                                            Name = channel.Attribute("name").Value,
                                            CreateSpeed = byte.Parse(channel.Attribute("createSpeed").Value),
                                            DuoTeam = DuoTeamMode.IsDuoGameType(channelGameType),
                                            GameType = DuoTeamMode.ToBaseGameType(channelGameType),
                                            WireGameType = channelGameType,
                                        };

                                        // 情况1：无openPeriod属性 → 直接加入结果
                                        if (openPeriodAttr == null)
                                        {
                                            GameSupport.Channels.TryAdd(i++, channelValue);
                                            continue;
                                        }

                                        // 情况2：有openPeriod属性 → 判断时间范围
                                        string openPeriod = openPeriodAttr.Value;
                                        string[] periodParts = openPeriod.Split('~');
                                        // 解析开始时间（格式：yyyy-MM-ddTHH:mm:ss）
                                        if (periodParts.Length != 2 || !DateTime.TryParse(periodParts[0], out DateTime startTime))
                                        {
                                            Console.WriteLine($"警告：{channelValue.Name} 的openPeriod格式错误，跳过该节点：{openPeriod}");
                                            continue;
                                        }

                                        // 当前时间 >= 开始时间 → 加入结果（*表示无结束时间）
                                        if (now >= startTime)
                                        {
                                            GameSupport.Channels.TryAdd(i++, channelValue);
                                        }
                                    }
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/mix/mixTable.xml")
                            {
                                Console.WriteLine(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    Mix.ParseDocument(doc);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Data] 跳过无法解析的文件：{packFileInfo.FileName}（{ex.Message}）");
                        }
                    }
                    foreach (LpkFolderInfo packFolderInfo2 in packFolderInfo1.GetFoldersInfo())
                        packFolderInfoQueue.Enqueue(packFolderInfo2);
                }
                return packFolderManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }

        private static byte[] ReplaceBytes(byte[] data)
        {
            byte[] oldBytes = new byte[] {
            0x3C, 0x00, 0x3F, 0x00, 0x78, 0x00, 0x6D, 0x00, 0x6C, 0x00, 0x20, 0x00,
            0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x73, 0x00, 0x69, 0x00, 0x6F, 0x00, 0x6E, 0x00,
            0x3D, 0x00, 0x27, 0x00, 0x31, 0x00, 0x2E, 0x00, 0x30, 0x00, 0x27, 0x00, 0x20, 0x00,
            0x65, 0x00, 0x6E, 0x00, 0x63, 0x00, 0x6F, 0x00, 0x64, 0x00, 0x69, 0x00, 0x6E, 0x00,
            0x67, 0x00, 0x3D, 0x00, 0x27, 0x00, 0x55, 0x00, 0x54, 0x00, 0x46, 0x00, 0x2D, 0x00,
            0x31, 0x00, 0x36, 0x00, 0x27, 0x00, 0x3F, 0x00, 0x3E, 0x00, 0x0D, 0x00, 0x0A, 0x00,
            0x3C, 0x00, 0x3F, 0x00, 0x78, 0x00, 0x6D, 0x00, 0x6C, 0x00, 0x20, 0x00,
            0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x73, 0x00, 0x69, 0x00, 0x6F, 0x00, 0x6E, 0x00,
            0x3D, 0x00, 0x27, 0x00, 0x31, 0x00, 0x2E, 0x00, 0x30, 0x00, 0x27, 0x00, 0x20, 0x00,
            0x65, 0x00, 0x6E, 0x00, 0x63, 0x00, 0x6F, 0x00, 0x64, 0x00, 0x69, 0x00, 0x6E, 0x00,
            0x67, 0x00, 0x3D, 0x00, 0x27, 0x00, 0x55, 0x00, 0x54, 0x00, 0x46, 0x00, 0x2D, 0x00,
            0x31, 0x00, 0x36, 0x00, 0x27, 0x00, 0x3F, 0x00, 0x3E, 0x00, 0x0D, 0x00, 0x0A, 0x00
        };

            byte[] newBytes = new byte[] {
            0x3C, 0x00, 0x3F, 0x00, 0x78, 0x00, 0x6D, 0x00, 0x6C, 0x00, 0x20, 0x00,
            0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x73, 0x00, 0x69, 0x00, 0x6F, 0x00, 0x6E, 0x00,
            0x3D, 0x00, 0x27, 0x00, 0x31, 0x00, 0x2E, 0x00, 0x30, 0x00, 0x27, 0x00, 0x20, 0x00,
            0x65, 0x00, 0x6E, 0x00, 0x63, 0x00, 0x6F, 0x00, 0x64, 0x00, 0x69, 0x00, 0x6E, 0x00,
            0x67, 0x00, 0x3D, 0x00, 0x27, 0x00, 0x55, 0x00, 0x54, 0x00, 0x46, 0x00, 0x2D, 0x00,
            0x31, 0x00, 0x36, 0x00, 0x27, 0x00, 0x3F, 0x00, 0x3E, 0x00, 0x0D, 0x00, 0x0A, 0x00
        };
            int oldLength = oldBytes.Length;
            int newLength = newBytes.Length;
            int dataLength = data.Length;

            byte[] result = new byte[dataLength];
            int resultIndex = 0;
            int i = 0;

            while (i < dataLength)
            {
                bool found = true;
                for (int j = 0; j < oldLength; j++)
                {
                    if (i + j >= dataLength || data[i + j] != oldBytes[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    for (int k = 0; k < newLength; k++)
                    {
                        result[resultIndex++] = newBytes[k];
                    }
                    i += oldLength;
                }
                else
                {
                    result[resultIndex++] = data[i++];
                }
            }

            Array.Resize(ref result, resultIndex);
            return result;
        }

        private static string ReplacePath(string file)
        {
            return file.IndexOf(".rho") > -1 ? file.Substring(0, file.IndexOf(".rho")).Replace("_", "/") + file.Substring(file.IndexOf(".rho") + 4) : file;
        }

        private static bool StreamsAreEqual(Stream stream1, Stream stream2)
        {
            const int bufferSize = 4096;
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];

            try
            {
                int bytesRead1;
                int bytesRead2;

                do
                {
                    bytesRead1 = stream1.Read(buffer1, 0, bufferSize);
                    bytesRead2 = stream2.Read(buffer2, 0, bufferSize);

                    if (bytesRead1 != bytesRead2)
                        return false;

                    for (int i = 0; i < bytesRead1; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                            return false;
                    }
                } while (bytesRead1 > 0);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ProcessTrackLocale(XmlDocument trackLocale)
        {
            if (trackLocale == null) return;

            var localeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode xn in trackLocale.GetElementsByTagName("track"))
            {
                XmlElement xe = xn as XmlElement;
                if (xe == null) continue;
                string blocked = xe.GetAttribute("blocked");
                if (blocked.Equals("true", StringComparison.OrdinalIgnoreCase)) continue;
                string id = xe.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id) || id.Contains("_S", StringComparison.OrdinalIgnoreCase)) continue;
                string name = xe.GetAttribute("name");
                bool basicAi = xe.GetAttribute("basicAi").Equals("true", StringComparison.OrdinalIgnoreCase);
                AddOrUpdateTrackListEntry(id, name, basicAi);
                localeNames[id] = name;
            }

            void processVariant(string elementName, string suffix)
            {
                foreach (XmlNode xn in trackLocale.GetElementsByTagName(elementName))
                {
                    XmlElement xe = xn as XmlElement;
                    if (xe == null) continue;
                    string blocked = xe.GetAttribute("blocked");
                    if (blocked.Equals("true", StringComparison.OrdinalIgnoreCase)) continue;
                    string refId = xe.GetAttribute("refId");
                    if (string.IsNullOrWhiteSpace(refId) || refId.Contains("_S", StringComparison.OrdinalIgnoreCase)) continue;
                    string variantId = $"{refId}_{suffix}";
                    bool basicAi = xe.GetAttribute("basicAi").Equals("true", StringComparison.OrdinalIgnoreCase);

                    string name;
                    if (elementName == "track_rvs")
                    {
                        if (localeNames.TryGetValue(refId, out var baseName) && !string.IsNullOrWhiteSpace(baseName))
                        {
                            name = $"[反]{baseName}";
                        }
                        else
                        {
                            name = string.Empty;
                        }
                    }
                    else
                    {
                        name = xe.GetAttribute("name");
                        if (string.IsNullOrWhiteSpace(name) && localeNames.TryGetValue(refId, out var baseName))
                        {
                            name = baseName;
                        }
                        else if (string.IsNullOrWhiteSpace(name))
                        {
                            name = string.Empty;
                        }
                    }

                    AddOrUpdateTrackListEntry(variantId, name, basicAi);
                }
            }

            processVariant("track_crz", "crz");
            processVariant("track_rvs", "rvs");
        }

        private static string ResolveVariantGameType(string trackIdentifier)
        {
            uint adler32Id = Adler32Helper.GenerateAdler32_UNICODE(trackIdentifier, 0);
            if (string.IsNullOrWhiteSpace(trackIdentifier)) return string.Empty;
            int suffixIndex = trackIdentifier.LastIndexOf('_');
            if (suffixIndex <= 0) return string.Empty;
            string baseId = trackIdentifier.Substring(0, suffixIndex);
            if (RandomTrack.TrackList.TryGetValue(adler32Id, out var baseTrack))
            {
                return baseTrack.gameType ?? string.Empty;
            }
            return string.Empty;
        }

        private static void AddOrUpdateTrackListEntry(string trackIdentifier, string name, bool basicAi = false)
        {
            if (string.IsNullOrWhiteSpace(trackIdentifier)) return;
            uint adler32Id = Adler32Helper.GenerateAdler32_UNICODE(trackIdentifier, 0);
            string realName = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
            if (RandomTrack.TrackList.ContainsKey(adler32Id))
            {
                Track existingTrack = RandomTrack.TrackList[adler32Id];
                if (!string.IsNullOrWhiteSpace(realName))
                {
                    existingTrack.Name = realName;
                }
                existingTrack.basicAi = basicAi;
                if (string.IsNullOrWhiteSpace(existingTrack.gameType))
                {
                    existingTrack.gameType = ResolveVariantGameType(trackIdentifier);
                }
            }
            else
            {
                RandomTrack.TrackList.Add(adler32Id, new Track
                {
                    hash = adler32Id,
                    ID = trackIdentifier,
                    Name = realName,
                    gameType = ResolveVariantGameType(trackIdentifier),
                    basicAi = basicAi
                });
            }
        }

        private static void ProcessTrackList(XmlDocument trackLocale)
        {
            if (trackLocale == null) return;

            var baseGameTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode xn in trackLocale.GetElementsByTagName("track"))
            {
                XmlElement xe = xn as XmlElement;
                if (xe == null) continue;
                string id = xe.GetAttribute("id");
                if (string.IsNullOrWhiteSpace(id) || id.Contains("_S", StringComparison.OrdinalIgnoreCase)) continue;
                string gameType = xe.GetAttribute("gameType");
                string name = xe.GetAttribute("name");
                AddRandomTrackEntry(id, gameType, name);
                if (!baseGameTypes.ContainsKey(id))
                {
                    baseGameTypes.Add(id, gameType);
                }
            }

            void processVariant(string elementName, string suffix)
            {
                foreach (XmlNode xn in trackLocale.GetElementsByTagName(elementName))
                {
                    XmlElement xe = xn as XmlElement;
                    if (xe == null) continue;
                    string refId = xe.GetAttribute("refId");
                    if (string.IsNullOrWhiteSpace(refId) || refId.Contains("_S", StringComparison.OrdinalIgnoreCase)) continue;
                    string variantId = $"{refId}_{suffix}";
                    string gameType = baseGameTypes.TryGetValue(refId, out var gt) ? gt : string.Empty;
                    string name = xe.GetAttribute("name");
                    AddRandomTrackEntry(variantId, gameType, name);
                }
            }

            processVariant("track_crz", "crz");
            processVariant("track_rvs", "rvs");
        }

        private static void AddRandomTrackEntry(string trackIdentifier, string gameType, string name)
        {
            if (string.IsNullOrWhiteSpace(trackIdentifier)) return;
            uint adler32Id = Adler32Helper.GenerateAdler32_UNICODE(trackIdentifier, 0);
            string realName = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
            if (!RandomTrack.TrackList.ContainsKey(adler32Id))
            {
                if (string.IsNullOrWhiteSpace(gameType))
                {
                    gameType = ResolveVariantGameType(trackIdentifier);
                }
                RandomTrack.TrackList.Add(adler32Id, new Track
                {
                    hash = adler32Id,
                    ID = trackIdentifier,
                    Name = realName,
                    gameType = gameType
                });
            }
            else
            {
                Track existingTrack = RandomTrack.TrackList[adler32Id];
                if (!string.IsNullOrWhiteSpace(gameType))
                {
                    existingTrack.gameType = gameType;
                }
                else if (string.IsNullOrWhiteSpace(existingTrack.gameType))
                {
                    existingTrack.gameType = ResolveVariantGameType(trackIdentifier);
                }
                if (!string.IsNullOrWhiteSpace(realName))
                {
                    existingTrack.Name = realName;
                }
            }
        }

        private static List<int> CountChildren(XElement element, int level, List<int> childCounts)
        {
            var childCount = element.Elements().Count();
            childCounts.Add(childCount);
            foreach (var child in element.Elements()) CountChildren(child, level + 1, childCounts);
            return childCounts;
        }
    }
}