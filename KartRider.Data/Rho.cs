using ExcData;
using KartLibrary.File;
using KartLibrary.File.Lpk;
using KartRider;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
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
                    Log.Debug("开始读取游戏Data内文件(lpk)...");
                    Log.Debug("==============================");
                    packFolderManager.OpenDataFolder(input);
                }
                catch (Exception ex)
                {
                    Log.Debug($"Error opening lpk data: {ex.Message}");
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
                                Log.Debug(fullName);
                                string name = fullName.Substring(10, fullName.Length - 23);
                                if (!(FlyingPet.flyingSpec.ContainsKey(name)))
                                {
                                    byte[] data = packFileInfo.GetData();
                                    using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                    {
                                        XmlDocument flying = new XmlDocument();
                                        flying.Load(stream);
                                        FlyingPet.flyingSpec.Add(name, flying);
                                    }
                                }
                            }
                            if (IsTrackDataFile(fullName, "randomTrack@"))
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    RandomTrack.randomTrack = XDocument.Load(stream);
                                }
                            }
                            // 不再死写 track/ 与 zz：补丁包可能是 track_/ 或其它区服后缀，只要文件名对得上就读取
                            if (IsTrackDataFile(fullName, "track@"))
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    XmlDocument trackDocument = new XmlDocument();
                                    trackDocument.Load(stream);
                                    ProcessTrackList(trackDocument, fullName);
                                }
                            }
                            if (IsTrackDataFile(fullName, "trackLocale@"))
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    XmlDocument localeDocument = new XmlDocument();
                                    localeDocument.Load(stream);
                                    ProcessTrackLocale(localeDocument, fullName);
                                }
                            }
                            if (fullName == "etc_/itemTable.kml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
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
                                        using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(newBytes)))
                                        {
                                            XmlDocument kart1 = new XmlDocument();
                                            kart1.Load(stream);
                                            Kart.kartSpec.Add(name, kart1);
                                        }
                                    }
                                    else
                                    {
                                        using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                    Log.Debug(fullName);
                                    if (!(Kart.kartSpec.ContainsKey(name)))
                                    {
                                        byte[] data = ReplaceBytes(packFileInfo.GetData());
                                        if (data[2] == 13 && data[3] == 0 && data[4] == 10 && data[5] == 0)
                                        {
                                            byte[] newBytes = new byte[data.Length - 4];
                                            newBytes[0] = 255;
                                            newBytes[1] = 254;
                                            Array.Copy(data, 6, newBytes, 2, data.Length - 6);
                                            using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(newBytes)))
                                            {
                                                XmlDocument kart1 = new XmlDocument();
                                                kart1.Load(stream);
                                                Kart.kartSpec.Add(name, kart1);
                                            }
                                        }
                                        else
                                        {
                                            using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                    Log.Debug(fullName);
                                    if (!(Kart.kartSpec.ContainsKey(name)))
                                    {
                                        byte[] data = ReplaceBytes(packFileInfo.GetData());
                                        if (data[2] == 13 && data[3] == 0 && data[4] == 10 && data[5] == 0)
                                        {
                                            byte[] newBytes = new byte[data.Length - 4];
                                            newBytes[0] = 255;
                                            newBytes[1] = 254;
                                            Array.Copy(data, 6, newBytes, 2, data.Length - 6);
                                            using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(newBytes)))
                                            {
                                                XmlDocument kart1 = new XmlDocument();
                                                kart1.Load(stream);
                                                Kart.kartSpec.Add(name, kart1);
                                            }
                                        }
                                        else
                                        {
                                            using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    XmlDocument Quest = new XmlDocument();
                                    Quest.Load(stream);
                                    GameSupport.QuestParams = Quest.GetElementsByTagName("QuestItem");
                                }
                            }
                            if (fullName == $"zeta/{regionCode}/quest/kartPassQuestAutomation.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    GameSupport.questInfo = doc.Descendants("kartPassQuestInfo").First();
                                }
                            }
                            if (fullName == $"zeta/{regionCode}/scenario/scenario.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                            // 道具车词条（变更/使用后/被攻击）：顺带解析，不能 continue——
                            // 下面的 shop/data/item.kml 分支还要加载商店道具表，跳过它客户端进大厅会闪退
                            if (fullName == $"zeta_/{regionCode}/shop/data/item.kml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                ClientItemRules.Load(data);
                                ClientItemRules.LogSummary();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                            if (!NewRider.items[itemCatId].ContainsKey(itemId))
                                            {
                                                NewRider.items[itemCatId][itemId] = itemName;
                                            }
                                        }
                                    }
                                }
                            }
                            if (fullName == "item/slot/transformByKart.xml")
                            {
                                Log.Debug(fullName);
                                byte[] tb = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(tb)))
                                {
                                    ClientItemRules.ApplyTransformByKart(XDocument.Load(ms), false);
                                }
                            }
                            if (fullName == "item/slot/transformByKart@cn.xml")
                            {
                                Log.Debug(fullName);
                                byte[] td = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(td)))
                                {
                                    ClientItemRules.ApplyTransformByKart(XDocument.Load(ms), true);
                                }
                            }
                            if (fullName == "item/slot/fired2Gain.xml")
                            {
                                Log.Debug(fullName);
                                byte[] bd = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(bd)))
                                {
                                    ClientItemRules.ApplyFired2Gain(XDocument.Load(ms), false);
                                }
                            }
                            if (fullName == "item/slot/fired2Gain@cn.xml")
                            {
                                Log.Debug(fullName);
                                byte[] gd = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(gd)))
                                {
                                    ClientItemRules.ApplyFired2Gain(XDocument.Load(ms), true);
                                }
                            }
                            if (fullName == "item/slot/firing2Gain.xml")
                            {
                                Log.Debug(fullName);
                                byte[] bd = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(bd)))
                                {
                                    ClientItemRules.ApplyFiring2Gain(XDocument.Load(ms), false);
                                }
                            }
                            if (fullName == "item/slot/firing2Gain@cn.xml")
                            {
                                Log.Debug(fullName);
                                byte[] gd = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(gd)))
                                {
                                    ClientItemRules.ApplyFiring2Gain(XDocument.Load(ms), true);
                                }
                            }
                            if (fullName == "zeta_/cn/content/itemGameRestrictionItemCount.xml")
                            {
                                Log.Debug(fullName);
                                byte[] rd = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(rd)))
                                {
                                    ItemRule.LoadRestrictions(XDocument.Load(ms), name => ClientItemRules.ResolveName(name));
                                }
                            }
                            if (fullName == "item/slot/itemProb_indi@zz.xml")
                            {
                                Log.Debug(fullName);
                                byte[] probData = packFileInfo.GetData();
                                using (MemoryStream probStream = new MemoryStream(GameSupport.NormalizeXmlEncoding(probData)))
                                {
                                    ItemRule.ApplyFromClient(2, XDocument.Load(probStream));
                                }
                            }
                            if (fullName == "item/slot/itemProb_team2@cn.xml")
                            {
                                Log.Debug(fullName);
                                byte[] probData = packFileInfo.GetData();
                                using (MemoryStream probStream = new MemoryStream(GameSupport.NormalizeXmlEncoding(probData)))
                                {
                                    ItemRule.ApplyFromClient(4, XDocument.Load(probStream));
                                }
                            }
                            // 道具概率表改为内置规则（Room/ItemRule.cs），不再读取客户端的 itemProb BML
                            if (fullName == $"zeta_/{regionCode}/content/basicAI.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    // 加载文档并解析任务
                                    TimeAttack.timeAttackMission = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackCompetitive.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    // 加载文档并解析任务
                                    TimeAttack.timeAttackCompetitive = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackCompetitiveData.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    // 加载文档并解析任务
                                    TimeAttack.timeAttackCompetitiveData = XDocument.Load(stream);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/lottery/lottery.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                            if (fullName == $"zeta_/{regionCode}/content/channel.xml")
                            {
                                Log.Debug(fullName);
                                DateTime now = DateTime.Now;
                                byte[] data = packFileInfo.GetData();
                                byte i = 1;
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
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
                                            Log.Debug($"警告：{channelValue.Name} 的openPeriod格式错误，跳过该节点：{openPeriod}");
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
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    Mix.ParseDocument(doc);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug($"[Data] 跳过无法解析的文件：{packFileInfo.FileName}（{ex.Message}）");
                        }
                    }
                    foreach (LpkFolderInfo packFolderInfo2 in packFolderInfo1.GetFoldersInfo())
                        packFolderInfoQueue.Enqueue(packFolderInfo2);
                }
                // 所有 track/locale 都读完后再收尾：缺名称的用 ID 兜底，避免新增赛道被随机池静默过滤
                RandomTrack.FinalizeTrackList();
                return packFolderManager;
            }
            catch (Exception ex)
            {
                Log.Debug("Error: " + ex.Message);
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
        /// <summary>
        /// trackLocale@{regionCode}.xml：内容与 track@zz.xml 重复（这里带本地化名称与 basicAi），
        /// 统一交给 RandomTrack.AddTrack 合并，同一张地图在 TrackList 里只会保存一份。
        /// </summary>
        private static void ProcessTrackLocale(XmlDocument trackLocale, string sourceName)
        {
            if (trackLocale == null) return;
            int before = RandomTrack.TrackList.Count;
            int total = 0;
            int skipped = 0;
            var localeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement xe in GetTrackElements(trackLocale, "track"))
            {
                total++;
                string id = GetAttributeText(xe, "id");
                if (!IsValidTrackId(id))
                {
                    skipped++;
                    Log.Debug($"[Track] {sourceName}: 跳过赛道 id=\"{id}\"（为空或包含 _S）");
                    continue;
                }
                bool blocked = GetBoolAttribute(xe, "blocked");
                string name = GetAttributeText(xe, "name");
                if (!blocked && !string.IsNullOrWhiteSpace(name))
                {
                    localeNames[id] = name;
                }
                RandomTrack.AddTrack(id, name, null,
                    GetBoolAttribute(xe, "basicAi"),
                    GetBoolAttribute(xe, "isOnlyItemTrack"),
                    blocked);
            }
            void processVariant(string elementName, string suffix)
            {
                foreach (XmlElement xe in GetTrackElements(trackLocale, elementName))
                {
                    total++;
                    string refId = GetAttributeText(xe, "refId");
                    if (!IsValidTrackId(refId))
                    {
                        skipped++;
                        Log.Debug($"[Track] {sourceName}: 跳过 {elementName} 的 refId=\"{refId}\"（为空或包含 _S）");
                        continue;
                    }
                    string variantId = $"{refId}_{suffix}";
                    bool hasBaseName = localeNames.TryGetValue(refId, out string baseName) && !string.IsNullOrWhiteSpace(baseName);
                    string name;
                    if (elementName == "track_rvs")
                    {
                        name = hasBaseName ? $"[反]{baseName}" : string.Empty;
                    }
                    else
                    {
                        name = GetAttributeText(xe, "name");
                        if (string.IsNullOrWhiteSpace(name) && hasBaseName)
                        {
                            name = baseName;
                        }
                    }
                    RandomTrack.AddTrack(variantId, name, RandomTrack.GetTrackGameType(refId),
                        GetBoolAttribute(xe, "basicAi"),
                        GetBoolAttribute(xe, "isOnlyItemTrack"),
                        GetBoolAttribute(xe, "blocked"));
                }
            }
            processVariant("track_crz", "crz");
            processVariant("track_rvs", "rvs");
            Log.Info($"[Track] {sourceName}: 共 {total} 条赛道节点，新增 {RandomTrack.TrackList.Count - before} 条，跳过 {skipped} 条，TrackList 现有 {RandomTrack.TrackList.Count} 条");
        }
        /// <summary>
        /// 判断 lpk 内文件是否是赛道数据（track@xx.xml / trackLocale@xx.xml / randomTrack@xx.xml）。
        /// 只比对文件名，允许任意前置目录与任意区服后缀（track/、track_/、补丁包里的新目录都能命中）。
        /// </summary>
        private static bool IsTrackDataFile(string filePath, string fileNamePrefix)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string normalized = filePath.Replace('\\', '/');
            int slash = normalized.LastIndexOf('/');
            string fileName = slash >= 0 ? normalized.Substring(slash + 1) : normalized;
            if (!fileName.StartsWith(fileNamePrefix, StringComparison.OrdinalIgnoreCase)) return false;
            if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) return false;
            return normalized.IndexOf("/common/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>地图 ID 是否有效（客户端 _S 变体不进地图列表）。</summary>
        private static bool IsValidTrackId(string trackIdentifier)
        {
            return !string.IsNullOrWhiteSpace(trackIdentifier)
                && !trackIdentifier.Contains("_S", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>取指定名字的元素，忽略大小写（手改的 XML 常见 &lt;track&gt; / &lt;Track&gt; 混用）。</summary>
        private static List<XmlElement> GetTrackElements(XmlDocument document, string elementName)
        {
            var elements = new List<XmlElement>();
            if (document == null) return elements;
            foreach (XmlNode node in document.GetElementsByTagName("*"))
            {
                if (node is XmlElement element && string.Equals(element.Name, elementName, StringComparison.OrdinalIgnoreCase))
                {
                    elements.Add(element);
                }
            }
            return elements;
        }

        /// <summary>读取属性值：优先精确匹配，取不到再忽略大小写匹配（手改的 XML 常见 ID/Id 混用）。</summary>
        private static string GetAttributeText(XmlElement element, string attributeName)
        {
            if (element == null) return string.Empty;
            string value = element.GetAttribute(attributeName);
            if (!string.IsNullOrEmpty(value)) return value.Trim();
            if (element.Attributes == null) return string.Empty;
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))
                {
                    return (attribute.Value ?? string.Empty).Trim();
                }
            }
            return string.Empty;
        }

        private static bool GetBoolAttribute(XmlElement element, string attributeName)
        {
            string value = GetAttributeText(element, attributeName);
            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// track@zz.xml：内容与 trackLocale@{regionCode}.xml 重复（这里带 gameType 与 isOnlyItemTrack），
        /// 统一交给 RandomTrack.AddTrack 合并，同一张地图在 TrackList 里只会保存一份。
        /// </summary>
        private static void ProcessTrackList(XmlDocument trackList, string sourceName)
        {
            if (trackList == null) return;
            int before = RandomTrack.TrackList.Count;
            int total = 0;
            int skipped = 0;
            var baseGameTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement xe in GetTrackElements(trackList, "track"))
            {
                total++;
                string id = GetAttributeText(xe, "id");
                if (!IsValidTrackId(id))
                {
                    skipped++;
                    Log.Debug($"[Track] {sourceName}: 跳过赛道 id=\"{id}\"（为空或包含 _S）");
                    continue;
                }
                string gameType = GetAttributeText(xe, "gameType");
                RandomTrack.AddTrack(id, GetAttributeText(xe, "name"), gameType,
                    GetBoolAttribute(xe, "basicAi"),
                    GetBoolAttribute(xe, "isOnlyItemTrack"),
                    GetBoolAttribute(xe, "blocked"));
                if (!baseGameTypes.ContainsKey(id))
                {
                    baseGameTypes.Add(id, gameType);
                }
            }
            void processVariant(string elementName, string suffix)
            {
                foreach (XmlElement xe in GetTrackElements(trackList, elementName))
                {
                    total++;
                    string refId = GetAttributeText(xe, "refId");
                    if (!IsValidTrackId(refId))
                    {
                        skipped++;
                        Log.Debug($"[Track] {sourceName}: 跳过 {elementName} 的 refId=\"{refId}\"（为空或包含 _S）");
                        continue;
                    }
                    string gameType = baseGameTypes.TryGetValue(refId, out string baseGameType) ? baseGameType : string.Empty;
                    if (string.IsNullOrWhiteSpace(gameType))
                    {
                        gameType = RandomTrack.GetTrackGameType(refId);
                    }
                    RandomTrack.AddTrack($"{refId}_{suffix}", GetAttributeText(xe, "name"), gameType,
                        GetBoolAttribute(xe, "basicAi"),
                        GetBoolAttribute(xe, "isOnlyItemTrack"),
                        GetBoolAttribute(xe, "blocked"));
                }
            }
            processVariant("track_crz", "crz");
            processVariant("track_rvs", "rvs");
            Log.Info($"[Track] {sourceName}: 共 {total} 条赛道节点，新增 {RandomTrack.TrackList.Count - before} 条，跳过 {skipped} 条，TrackList 现有 {RandomTrack.TrackList.Count} 条");
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
