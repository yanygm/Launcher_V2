using System;
using System.IO;
using KartLibrary.File;
using KartLibrary.Consts;
using System.Collections;
using System.Collections.Generic;
using KartLibrary.Xml;
using System.Text;
using ExcData;
using RiderData;
using KartRider;
using KartRider.Common.Utilities;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace RHOParser
{
    public static class KartRhoFile
    {
        public static PackFolderManager packFolderManager = new PackFolderManager();

        public static void Dump(string input)
        {
            packFolderManager.OpenDataFolder(input);
            string regionCode = packFolderManager.regionCode.ToString().ToLower();
            Queue<PackFolderInfo> packFolderInfoQueue = new Queue<PackFolderInfo>();
            packFolderInfoQueue.Enqueue(packFolderManager.GetRootFolder());
            packFolderManager.GetRootFolder();
            while (packFolderInfoQueue.Count > 0)
            {
                PackFolderInfo packFolderInfo1 = packFolderInfoQueue.Dequeue();
                foreach (PackFileInfo packFileInfo in packFolderInfo1.GetFilesInfo())
                {
                    string fullName = ReplacePath(packFileInfo.FullName);
                    if (fullName.Contains("flyingPet") && fullName.Contains("param@" + regionCode + ".bml"))
                    {
                        Console.WriteLine(fullName);
                        string name = fullName.Substring(10, fullName.Length - 23);
                        if (!(KartExcData.flyingSpec.ContainsKey(name)))
                        {
                            byte[] data = packFileInfo.GetData();
                            BinaryXmlDocument bxd = new BinaryXmlDocument();
                            bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                            string output_bml = bxd.RootTag.ToString();
                            byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                            using (MemoryStream stream = new MemoryStream(output_data))
                            {
                                XmlDocument flying = new XmlDocument();
                                flying.Load(stream);
                                KartExcData.flyingSpec.Add(name, flying);
                            }
                        }
                    }
                    if (fullName == "track/common/randomTrack@" + regionCode + ".bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
                        {
                            KartExcData.randomTrack = XDocument.Load(stream);
                        }
                    }
                    if (fullName == "track/common/trackLocale@" + regionCode + ".xml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        using (MemoryStream stream = new MemoryStream(data))
                        {
                            XmlDocument trackLocale = new XmlDocument();
                            trackLocale.Load(stream);
                            XmlNodeList trackParams = trackLocale.GetElementsByTagName("track");
                            if (trackParams.Count > 0)
                            {
                                foreach (XmlNode xn in trackParams)
                                {
                                    XmlElement xe = (XmlElement)xn;
                                    string track = xe.GetAttribute("id");
                                    uint id = Adler32Helper.GenerateAdler32_UNICODE(track, 0);
                                    if (!(KartExcData.track.ContainsKey(id)))
                                    {
                                        KartExcData.track.Add(id, track);
                                    }
                                }
                            }
                        }
                    }
                    if (fullName == "track/common/trackLocale@" + regionCode + ".bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
                        {
                            XmlDocument trackLocale = new XmlDocument();
                            trackLocale.Load(stream);
                            XmlNodeList trackParams = trackLocale.GetElementsByTagName("track");
                            if (trackParams.Count > 0)
                            {
                                foreach (XmlNode xn in trackParams)
                                {
                                    XmlElement xe = (XmlElement)xn;
                                    string track = xe.GetAttribute("id");
                                    uint id = Adler32Helper.GenerateAdler32_UNICODE(track, 0);
                                    if (!(KartExcData.track.ContainsKey(id)))
                                    {
                                        KartExcData.track.Add(id, track);
                                    }
                                }
                            }
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
                                    if (!(KartExcData.KartName.ContainsKey(id)))
                                    {
                                        KartExcData.KartName.Add(id, name);
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
                                    if (!(KartExcData.flyingName.ContainsKey(id)))
                                    {
                                        KartExcData.flyingName.Add(id, name);
                                    }
                                }
                            }
                        }
                    }
                    if (fullName == "etc_/itemTable@" + regionCode + ".xml")
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
                                    if (KartExcData.KartName.ContainsKey(id))
                                    {
                                        KartExcData.KartName[id] = name;
                                    }
                                    else
                                    {
                                        KartExcData.KartName.Add(id, name);
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
                                    if (KartExcData.flyingName.ContainsKey(id))
                                    {
                                        KartExcData.flyingName[id] = name;
                                    }
                                    else
                                    {
                                        KartExcData.flyingName.Add(id, name);
                                    }
                                }
                            }
                        }
                    }
                    if (fullName == "etc_/emblem/emblem@" + regionCode + ".xml")
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
                                        if (!KartExcData.emblem.Contains(id))
                                        {
                                            KartExcData.emblem.Add(id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (fullName.Contains("kart_") && fullName.Contains("/param@" + regionCode + ".xml"))
                    {
                        Console.WriteLine(fullName);
                        byte[] data = ReplaceBytes(packFileInfo.GetData());
                        string name = fullName.Substring(6, fullName.Length - 19);
                        if (!(KartExcData.KartSpec.ContainsKey(name)))
                        {
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
                                    KartExcData.KartSpec.Add(name, kart1);
                                }
                            }
                            else
                            {
                                using (MemoryStream stream = new MemoryStream(data))
                                {
                                    XmlDocument kart2 = new XmlDocument();
                                    kart2.Load(stream);
                                    KartExcData.KartSpec.Add(name, kart2);
                                }
                            }
                        }
                    }
                    if (fullName.Contains("kart_") && fullName.Contains("/param.xml"))
                    {
                        string name = fullName.Substring(6, fullName.Length - 16);
                        byte[] data = ReplaceBytes(packFileInfo.GetData());
                        bool containsTarget = packFolderInfo1.GetFilesInfo().Any(PackFileInfo => ReplacePath(PackFileInfo.FullName) == "kart_/" + name + "/param@" + regionCode + ".xml");
                        if (!containsTarget)
                        {
                            if (!(KartExcData.KartSpec.ContainsKey(name)))
                            {
                                Console.WriteLine(fullName);
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
                                        KartExcData.KartSpec.Add(name, kart1);
                                    }
                                }
                                else
                                {
                                    using (MemoryStream stream = new MemoryStream(data))
                                    {
                                        XmlDocument kart2 = new XmlDocument();
                                        kart2.Load(stream);
                                        KartExcData.KartSpec.Add(name, kart2);
                                    }
                                }
                            }
                        }
                    }
                    if (fullName == "zeta/" + regionCode + "/quest/QuestAutomation.bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
                        {
                            XmlDocument Quest = new XmlDocument();
                            Quest.Load(stream);
                            XmlNodeList QuestParams = Quest.GetElementsByTagName("QuestItem");
                            if (QuestParams.Count > 0)
                            {
                                foreach (XmlNode xn in QuestParams)
                                {
                                    XmlElement xe = (XmlElement)xn;
                                    int id = int.Parse(xe.GetAttribute("id"));
                                    if (!(KartExcData.quest.Contains(id)))
                                    {
                                        KartExcData.quest.Add(id);
                                    }
                                }
                            }
                        }
                    }
                    if (fullName == "zeta/" + regionCode + "/quest/kartPassQuestAutomation.bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
                        {
                            XDocument doc = XDocument.Load(stream);
                            XElement questInfo = doc.Descendants("kartPassQuestInfo").First();
                            KartExcData.seasonId = int.Parse(questInfo.Attribute("seasonId").Value);
                        }
                    }
                    if (fullName == "zeta/" + regionCode + "/scenario/scenario.bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
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
                                    if (!(KartExcData.scenario.Contains(id)))
                                    {
                                        KartExcData.scenario.Add(id);
                                    }
                                }
                            }
                        }
                    }
                    if (fullName == "item/slot/itemProb_indi@zz.bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
                        {
                            XDocument doc = XDocument.Load(stream);
                            foreach (var item in doc.Descendants("item"))
                            {
                                // 获取 idx 属性值
                                string idxValue = item.Attribute("idx")?.Value;

                                // 验证并转换为 short 类型
                                if (short.TryParse(idxValue, out short idx))
                                {
                                    KartExcData.itemProb_indi.Add(idx);
                                }
                                else
                                {
                                    Console.WriteLine($"无法将 '{idxValue}' 转换为 short 类型");
                                }
                            }
                        }
                    }
                    if (fullName == "item/slot/itemProb_team@zz.bml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        BinaryXmlDocument bxd = new BinaryXmlDocument();
                        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
                        string output_bml = bxd.RootTag.ToString();
                        byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                        using (MemoryStream stream = new MemoryStream(output_data))
                        {
                            XDocument doc = XDocument.Load(stream);
                            foreach (var item in doc.Descendants("item"))
                            {
                                // 获取 idx 属性值
                                string idxValue = item.Attribute("idx")?.Value;

                                // 验证并转换为 short 类型
                                if (short.TryParse(idxValue, out short idx))
                                {
                                    KartExcData.itemProb_team.Add(idx);
                                }
                                else
                                {
                                    Console.WriteLine($"无法将 '{idxValue}' 转换为 short 类型");
                                }
                            }
                        }
                    }
                    if (fullName == "zeta_/" + regionCode + "/content/basicAI.xml")
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
                            KartExcData.aiCharacterDict = aiCharacterDict;

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
                            KartExcData.aiKartDict = aiKartDict;
                        }
                    }
                    if (fullName == "zeta_/" + regionCode + "/content/itemDictionary.xml")
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
                                    KartExcData.Dictionary.Add(Add);
                                }
                            }
                        }
                    }
                    if (fullName == "zeta_/" + regionCode + "/content/timeAttack/timeAttackMission.xml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        using (MemoryStream stream = new MemoryStream(data))
                        {
                            // 加载文档并解析任务
                            XDocument doc = XDocument.Load(stream);
                            DateTime now = DateTime.Now;

                            // 解析当前流中的任务
                            var currentMissionList = doc.Descendants("duelMission")
                                .Where(mission =>
                                {
                                    string period = mission.Element("missionSet")?.Attribute("period")?.Value;
                                    if (string.IsNullOrEmpty(period)) return false;

                                    string[] timeRange = period.Split('~');
                                    if (timeRange.Length != 2) return false;

                                    if (!DateTime.TryParse(timeRange[0], out DateTime startTime)) return false;
                                    if (!DateTime.TryParse(timeRange[1], out DateTime endTime)) return false;

                                    return now >= startTime && now <= endTime;
                                })
                                .Select(mission => mission.Attribute("trackId")?.Value)
                                .Where(trackId => !string.IsNullOrEmpty(trackId))
                                .ToList();

                            if (FavoriteItem.MissionList == null)
                            {
                                FavoriteItem.MissionList = currentMissionList;
                            }
                            Console.WriteLine(string.Join(", ", FavoriteItem.MissionList));
                        }
                    }
                    if (fullName == "zeta_/" + regionCode + "/content/timeAttack/timeAttackCompetitive.xml")
                    {
                        Console.WriteLine(fullName);
                        byte[] data = packFileInfo.GetData();
                        using (MemoryStream stream = new MemoryStream(data))
                        {
                            // 加载文档并解析任务
                            XDocument doc = XDocument.Load(stream);
                            var extractor = new TrackIdExtractor();
                            FavoriteItem.Competitive = extractor.GetCurrentWeekTrackIds(doc);
                        }
                    }
                    if (fullName == "zeta_/" + regionCode + "/shop/data/item.kml")
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
                                    short itemCatId = short.Parse(xe.GetAttribute("itemCatId"));
                                    short itemId = short.Parse(xe.GetAttribute("itemId"));
                                    string itemName = xe.GetAttribute("itemName");
                                    if (!KartExcData.items.ContainsKey(itemCatId))
                                    {
                                        KartExcData.items[itemCatId] = new Dictionary<short, string>();
                                    }
                                    KartExcData.items[itemCatId][itemId] = itemName;
                                }
                            }
                        }
                    }
                }
                foreach (PackFolderInfo packFolderInfo2 in packFolderInfo1.GetFoldersInfo())
                    packFolderInfoQueue.Enqueue(packFolderInfo2);
            }
        }

        static byte[] ReplaceBytes(byte[] data)
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
    }
}
