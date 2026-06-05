using ExcData;
using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
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

        public static PackFolderManager Dump(string input)
        {
            try
            {
                regionCode = GetRegionCode(input);

                // Check for sound_bgm_korea.rho and sound_bgm_lotte.rho files
                string inputDir = Path.GetDirectoryName(input);
                string korea = Path.Combine(inputDir, "sound_bgm_korea.rho");
                bool koreaFileExists = File.Exists(korea);
                string lotte = Path.Combine(inputDir, "sound_bgm_lotte.rho");
                bool lotteFileExists = File.Exists(lotte);

                BinaryXmlTag rootTag = GetAAATag(input);

                // Find the sound/bgm folder path
                BinaryXmlTag soundFolder = null;
                BinaryXmlTag bgmFolder = null;

                foreach (BinaryXmlTag subtag in rootTag.Children)
                {
                    if (subtag.Name == "PackFolder" && subtag.GetAttribute("name") == "sound")
                    {
                        soundFolder = subtag;
                        foreach (BinaryXmlTag soundSubtag in soundFolder.Children)
                        {
                            if (soundSubtag.Name == "PackFolder" && soundSubtag.GetAttribute("name") == "bgm")
                            {
                                bgmFolder = soundSubtag;
                                break;
                            }
                        }
                        break;
                    }
                }

                if (bgmFolder != null)
                {
                    // Check existing RhoFolder entries
                    bool koreaEntryExists = false;
                    bool lotteEntryExists = false;
                    bool metadataModified = false;
                    List<BinaryXmlTag> tagsToRemove = new List<BinaryXmlTag>();

                    foreach (BinaryXmlTag tag in bgmFolder.Children)
                    {
                        if (tag.Name == "RhoFolder")
                        {
                            string fileName = tag.GetAttribute("fileName");
                            if (fileName == "sound_bgm_korea.rho")
                            {
                                koreaEntryExists = true;
                                if (!koreaFileExists)
                                {
                                    tagsToRemove.Add(tag);
                                    metadataModified = true;
                                }
                            }
                            else if (fileName == "sound_bgm_lotte.rho")
                            {
                                lotteEntryExists = true;
                                if (!lotteFileExists)
                                {
                                    tagsToRemove.Add(tag);
                                    metadataModified = true;
                                }
                            }
                        }
                    }

                    // Remove tags for files that don't exist
                    foreach (BinaryXmlTag tag in tagsToRemove)
                    {
                        bgmFolder.Children.Remove(tag);
                    }

                    // Add missing entries for files that exist
                    if (koreaFileExists && !koreaEntryExists)
                    {
                        metadataModified = true;
                        using (var Korea = new Rho(korea))
                        {
                            BinaryXmlTag koreaTag = new BinaryXmlTag("RhoFolder");
                            koreaTag.SetAttribute("name", "korea");
                            koreaTag.SetAttribute("fileName", "sound_bgm_korea.rho");
                            koreaTag.SetAttribute("key", Korea.GetFileKey().ToString());
                            koreaTag.SetAttribute("dataHash", Korea.GetDataHash().ToString());
                            koreaTag.SetAttribute("mediaSize", Korea.baseStream.Length.ToString());
                            bgmFolder.Children.Add(koreaTag);
                        }
                    }

                    if (lotteFileExists && !lotteEntryExists)
                    {
                        metadataModified = true;
                        using (var Lotte = new Rho(lotte))
                        {
                            BinaryXmlTag lotteTag = new BinaryXmlTag("RhoFolder");
                            lotteTag.SetAttribute("name", "lotte");
                            lotteTag.SetAttribute("fileName", "sound_bgm_lotte.rho");
                            lotteTag.SetAttribute("key", Lotte.GetFileKey().ToString());
                            lotteTag.SetAttribute("dataHash", Lotte.GetDataHash().ToString());
                            lotteTag.SetAttribute("mediaSize", Lotte.baseStream.Length.ToString());
                            bgmFolder.Children.Add(lotteTag);
                        }
                    }

                    if (metadataModified)
                    {
                        // Save the modified content back to aaa.pk
                        try
                        {
                            string xmlContent = rootTag.ToString();
                            string tempXmlPath = Path.Combine(Path.GetDirectoryName(input), "temp_aaa.xml");
                            File.WriteAllText(tempXmlPath, xmlContent, Encoding.GetEncoding("UTF-16"));

                            // Use the same approach as AAAD to write the aaa.pk file
                            var xdoc = XDocument.Load(tempXmlPath);
                            if (xdoc.Root == null)
                            {
                                Console.WriteLine("Error: Root element is null");
                                return null;
                            }
                            var childCounts = CountChildren(xdoc.Root, 0, new List<int>());
                            byte[] byteArray;
                            using (var reader = XmlReader.Create(tempXmlPath))
                            {
                                using (var outPacket = new OutPacket())
                                {
                                    var Count = 0;
                                    while (reader.Read())
                                        if (reader.NodeType == XmlNodeType.Element)
                                        {
                                            var elementName = reader.Name;
                                            var attCount = reader.AttributeCount;
                                            outPacket.WriteString(elementName);
                                            outPacket.WriteInt();
                                            outPacket.WriteInt(attCount);
                                            for (var i = 0; i < attCount; i++)
                                            {
                                                reader.MoveToAttribute(i);
                                                var attName = reader.Name;
                                                outPacket.WriteString(attName);
                                                var attValue = reader.Value;
                                                outPacket.WriteString(attValue);
                                            }

                                            outPacket.WriteInt(childCounts[Count]);
                                            Count++;
                                            reader.MoveToElement();
                                        }

                                    byteArray = outPacket.ToArray();
                                }
                            }

                            using (var fileStream = new FileStream(input, FileMode.Create))
                            {
                                var binaryWriter = new BinaryWriter(fileStream);
                                binaryWriter.Write(0);
                                var KRDataLength = binaryWriter.WriteKRData(byteArray, false, true);
                                binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                                binaryWriter.Write(KRDataLength);
                            }

                            if (File.Exists(tempXmlPath))
                            {
                                File.Delete(tempXmlPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error writing aaa.pk: {ex.Message}");
                            return null;
                        }
                    }
                }

                // Now open the modified aaa.pk with PackFolderManager
                PackFolderManager packFolderManager = new PackFolderManager();
                try
                {
                    Console.WriteLine("开始读取游戏Data内文件...");
                    Console.WriteLine("==============================");
                    packFolderManager.OpenDataFolder(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening modified aaa.pk: {ex.Message}");
                    return null;
                }

                Queue<PackFolderInfo> packFolderInfoQueue = new Queue<PackFolderInfo>();
                packFolderInfoQueue.Enqueue(packFolderManager.GetRootFolder());
                while (packFolderInfoQueue.Count > 0)
                {
                    PackFolderInfo packFolderInfo1 = packFolderInfoQueue.Dequeue();
                    foreach (PackFileInfo packFileInfo in packFolderInfo1.GetFilesInfo())
                    {
                        string fullName = ReplacePath(packFileInfo.FullName);
                        if (fullName.Contains("flyingPet") && fullName.Contains($"param@{regionCode}.bml"))
                        {
                            Console.WriteLine(fullName);
                            string name = fullName.Substring(10, fullName.Length - 23);
                            if (!(FlyingPet.flyingSpec.ContainsKey(name)))
                            {
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                                {
                                    XmlDocument flying = new XmlDocument();
                                    flying.Load(stream);
                                    FlyingPet.flyingSpec.Add(name, flying);
                                }
                            }
                        }
                        if (fullName == $"track/common/randomTrack@{regionCode}.bml" ||
                            fullName == $"track/common/randomTrack@{regionCode}.xml" ||
                            fullName == $"track_/common/randomTrack@{regionCode}.bml" ||
                            fullName == $"track_/common/randomTrack@{regionCode}.xml"
                            )
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                            {
                                RandomTrack.randomTrack = XDocument.Load(stream);
                            }
                        }
                        if (fullName == $"track/common/track@zz.bml" ||
                            fullName == $"track/common/track@zz.xml" ||
                            fullName == $"track_/common/track@zz.bml" ||
                            fullName == $"track_/common/track@zz.xml"
                            )
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                            {
                                XmlDocument trackLocale = new XmlDocument();
                                trackLocale.Load(stream);
                                ProcessTrackList(trackLocale);
                            }
                        }
                        if (fullName == $"track/common/trackLocale@{regionCode}.bml" ||
                            fullName == $"track/common/trackLocale@{regionCode}.xml" ||
                            fullName == $"track_/common/trackLocale@{regionCode}.bml" ||
                            fullName == $"track_/common/trackLocale@{regionCode}.xml"
                            )
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
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
                                        if (!(Kart.kartName.ContainsKey(id)))
                                        {
                                            Kart.kartName.Add(id, name);
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
                                        if (Kart.kartName.ContainsKey(id))
                                        {
                                            Kart.kartName[id] = name;
                                        }
                                        else
                                        {
                                            Kart.kartName.Add(id, name);
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
                            bool containsTarget = packFolderInfo1.GetFilesInfo().Any(PackFileInfo => ReplacePath(PackFileInfo.FullName) == $"kart_/{name}/param@{regionCode}.xml");
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
                            bool containsTarget = packFolderInfo1.GetFilesInfo().Any(PackFileInfo => ReplacePath(PackFileInfo.FullName) == $"kart_/{name}/param@{regionCode}.xml" || ReplacePath(PackFileInfo.FullName) == $"kart_/{name}/param@{regionCode}.kml");
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
                        if (fullName == $"zeta/{regionCode}/quest/QuestAutomation.bml")
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                            {
                                XmlDocument Quest = new XmlDocument();
                                Quest.Load(stream);
                                XmlNodeList QuestParams = Quest.GetElementsByTagName("QuestItem");
                                if (QuestParams.Count > 0)
                                {
                                    foreach (XmlNode xn in QuestParams)
                                    {
                                        XmlElement xe = (XmlElement)xn;
                                        uint id = uint.Parse(xe.GetAttribute("id"));
                                        if (!(GameSupport.quest.Contains(id)))
                                        {
                                            GameSupport.quest.Add(id);
                                        }
                                    }
                                }
                            }
                        }
                        if (fullName == $"zeta/{regionCode}/quest/kartPassQuestAutomation.bml")
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                            {
                                XDocument doc = XDocument.Load(stream);
                                XElement questInfo = doc.Descendants("kartPassQuestInfo").First();

                                string period = questInfo.Attribute("seasonPeriod").Value;
                                var isInTime = IsCurrentTimeInPeriod(period);
                                if (isInTime != null)
                                {
                                    string seasonId = questInfo.Attribute("seasonId").Value;
                                    List<uint> ids = new List<uint>();
                                    foreach (int group in isInTime)
                                    {
                                        for (int index = 1; index <= 3; index++)
                                        {
                                            string groupStr = group.ToString("D2");  // 1 → 01
                                            string indexStr = index.ToString("D2");  // 1 → 01
                                            // 拼接：14500 + 组号 + 序号
                                            uint id = uint.Parse($"1{seasonId}00{groupStr}{indexStr}");
                                            ids.Add(id);
                                        }
                                    }
                                    GameSupport.QuestEncodeList = ids;
                                }
                            }
                        }
                        if (fullName == $"zeta/{regionCode}/scenario/scenario.bml")
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
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
                        if (fullName == "item/slot/itemProb_indi@zz.bml")
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                            {
                                XDocument doc = XDocument.Load(stream);
                                foreach (var item in doc.Descendants("item"))
                                {
                                    // 获取 idx 属性值
                                    string idxValue = item.Attribute("idx")?.Value;

                                    // 验证并转换为 short 类型
                                    if (short.TryParse(idxValue, out short idx))
                                    {
                                        MultyPlayer.itemProb_indi.Add(idx);
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
                            using (MemoryStream stream = new MemoryStream(BmlToXml(fullName, data)))
                            {
                                XDocument doc = XDocument.Load(stream);
                                foreach (var item in doc.Descendants("item"))
                                {
                                    // 获取 idx 属性值
                                    string idxValue = item.Attribute("idx")?.Value;

                                    // 验证并转换为 short 类型
                                    if (short.TryParse(idxValue, out short idx))
                                    {
                                        MultyPlayer.itemProb_team.Add(idx);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"无法将 '{idxValue}' 转换为 short 类型");
                                    }
                                }
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

                                if (TimeAttack.MissionList.Count == 0)
                                {
                                    TimeAttack.MissionList = currentMissionList;
                                }
                                if (TimeAttack.MissionList.Count > 0)
                                {
                                    Console.WriteLine(string.Join(", ", TimeAttack.MissionList));
                                }
                            }
                        }
                        if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackCompetitive.xml")
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(data))
                            {
                                // 加载文档并解析任务
                                XDocument doc = XDocument.Load(stream);
                                var extractor = new TrackIdExtractor();
                                TimeAttack.Competitive = extractor.GetCurrentWeekTrackIds(doc);
                            }
                        }
                        if (fullName == $"zeta_/{regionCode}/content/timeAttack/timeAttackCompetitiveData.xml")
                        {
                            Console.WriteLine(fullName);
                            byte[] data = packFileInfo.GetData();
                            using (MemoryStream stream = new MemoryStream(data))
                            {
                                // 加载文档并解析任务
                                XDocument doc = XDocument.Load(stream);
                                CompleteTrackScoreCalculator calculator = new CompleteTrackScoreCalculator();
                                TimeAttack.TrackDictionary = calculator.LoadFromXml(doc);
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

                                // 获取所有rewardSet节点
                                XmlNodeList rewardSetNodes = doc.GetElementsByTagName("lottery");
                                XmlNode targetRewardSet = null;

                                var BingoLotteryID = Bingo.BingoLotteryIDs[Bingo.BingoLotteryIDs.Length - 1].ToString();

                                // 查找指定id的rewardSet
                                foreach (XmlNode node in rewardSetNodes)
                                {
                                    XmlElement rewardSetElement = node as XmlElement;

                                    if (rewardSetElement != null && rewardSetElement.GetAttribute("id") == BingoLotteryID)
                                    {
                                        targetRewardSet = node;
                                        break;
                                    }
                                }
                                if (targetRewardSet == null)
                                {
                                    Console.WriteLine($"未找到ID为{BingoLotteryID}的lottery节点");
                                }
                                else
                                {
                                    // 获取该rewardSet下的所有reward节点
                                    XmlNodeList rewardNodes = targetRewardSet.SelectNodes("./rewardSet/reward");
                                    LotteryManager.Initialize(rewardNodes);
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
                            using (MemoryStream stream = new MemoryStream(data))
                            {
                                XDocument doc = XDocument.Load(stream);

                                // 遍历所有Channel节点
                                foreach (var channel in doc.Descendants("Channel"))
                                {
                                    XAttribute openPeriodAttr = channel.Attribute("openPeriod");

                                    var channelValue = new Channel
                                    {
                                        Name = channel.Attribute("name").Value,
                                        CreateSpeed = byte.Parse(channel.Attribute("createSpeed").Value),
                                        GameType = byte.Parse(channel.Attribute("gameType").Value),
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
                    }
                    foreach (PackFolderInfo packFolderInfo2 in packFolderInfo1.GetFoldersInfo())
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

        private static byte[] BmlToXml(string path, byte[] bmlData)
        {
            if (Path.GetExtension(path).ToLower() == ".bml")
            {
                BinaryXmlDocument bxd = new BinaryXmlDocument();
                bxd.Read(Encoding.GetEncoding("UTF-16"), bmlData);
                string output_bml = bxd.RootTag.ToString();
                byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
                return output_data;
            }
            else
            {
                return bmlData;
            }
        }

        private static List<int> CountChildren(XElement element, int level, List<int> childCounts)
        {
            var childCount = element.Elements().Count();
            childCounts.Add(childCount);
            foreach (var child in element.Elements()) CountChildren(child, level + 1, childCounts);
            return childCounts;
        }

        public static BinaryXmlTag GetAAATag(string input)
        {
            // Read the current aaa.pk content
            byte[] aaaPkData;
            try
            {
                using (FileStream fileStream = new FileStream(input, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(fileStream);
                    int dataLen = br.ReadInt32();
                    aaaPkData = br.ReadKRData(dataLen);
                    fileStream.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading aaa.pk: {ex.Message}");
                // If reading fails, return null
                return null;
            }

            BinaryXmlDocument bxmlDoc = new BinaryXmlDocument();
            bxmlDoc.Read(Encoding.GetEncoding("UTF-16"), aaaPkData);
            BinaryXmlTag rootTag = bxmlDoc.RootTag;
            return rootTag;
        }

        public static string GetRegionCode(string input)
        {
            // Extract regionCode from the XML
            string regionCode = "cn"; // Default to CN

            BinaryXmlTag rootTag = GetAAATag(input);
            if (rootTag == null)
            {
                return regionCode;
            }

            try
            {
                // Try to find the region code by looking at zeta folders
                BinaryXmlTag zetaFolder = null;
                foreach (BinaryXmlTag subtag in rootTag.Children)
                {
                    if (subtag.Name == "PackFolder" && subtag.GetAttribute("name") == "zeta")
                    {
                        zetaFolder = subtag;
                        break;
                    }
                }

                if (zetaFolder != null)
                {
                    foreach (BinaryXmlTag subtag in zetaFolder.Children)
                    {
                        if (subtag.Name == "PackFolder")
                        {
                            string folderName = subtag.GetAttribute("name");
                            if (folderName == "kr")
                            {
                                regionCode = "kr";
                                break;
                            }
                            else if (folderName == "cn")
                            {
                                regionCode = "cn";
                                break;
                            }
                            else if (folderName == "tw")
                            {
                                regionCode = "tw";
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting regionCode: {ex.Message}");
            }
            return regionCode;
        }

        private static List<int> IsCurrentTimeInPeriod(string period)
        {
            try
            {
                var times = period.Split('~');
                string startStr = times[0];
                string endStr = times[1];

                DateTime startTime = DateTime.Parse(startStr);
                DateTime endTime = DateTime.Parse(endStr);
                DateTime now = DateTime.Now;

                if (now < startTime || now > endTime)
                    return null;

                int days = (now.Date - startTime.Date).Days;

                // 判断属于哪一段
                if (days < 7)
                    return new List<int>(){ 1, 2, 3, 4, 5, 6, 7 };
                else if (days < 13)
                    return new List<int>(){ 7, 8, 9, 10, 11, 12, 13 };
                else if (days < 19)
                    return new List<int>() { 13, 14, 15, 16, 17, 18, 19 };
                else
                    return new List<int>() { 19, 20, 21 };
            }
            catch
            {
                return null;
            }
        }
    }
}
