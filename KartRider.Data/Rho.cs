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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                // 与 KartRider.exe.c 的资源管理层一致：所有 .lpk 的记录表先合并成一张按路径索引的全局表，
                // 只有真正需要解析的条目才打开分包、定位到数据区并解密（见 LpkResourceManager）。
                // Profile.Setting.ReadDuplicateLpkFiles：true = 同一路径的多份分包副本全部读入后按条目去重，
                // false = 回到旧行为，每个虚拟路径只读先装载的那一份。
                LpkResourceManager.ReadDuplicateSources = ProfileService.SettingConfig.ReadDuplicateLpkFiles;
                var archives = new LpkResourceManager();
                if (!archives.Open(input, out string openError))
                {
                    Log.Debug($"Error opening lpk data: {openError}");
                    return null;
                }
                Log.Debug("开始按需读取游戏Data内文件(lpk)...");
                Log.Debug("==============================");
                regionCode = archives.DetectRegionCode();

                // 只登记名字、不解密：随机池校验要覆盖所有分包（含补丁包）里的赛道资源
                foreach (string rawPath in archives.AllPaths)
                    RandomTrack.NoteTrackFile(rawPath);

                // 目录树里只放「可能需要解析」的条目，GetData() 才真正读盘解密
                archives.BuildRequiredTree(regionCode);
                Log.Debug($"[Data] 索引 {archives.ArchiveCount} 个 LPK / {archives.PathCount} 条记录，待解析 {archives.RequiredCount} 条");
                string duplicateMode = LpkResourceManager.ReadDuplicateSources
                    ? "全部读入并按条目去重"
                    : "只读先装载的一份（旧行为）";
                if (archives.DuplicatePathCount > 0)
                    Log.Warn($"[Data] 索引里有 {archives.DuplicatePathCount} 条虚拟路径在多个分包中重复出现（多余副本 {archives.DuplicateRecordCount} 条），ReadDuplicateLpkFiles={LpkResourceManager.ReadDuplicateSources} → {duplicateMode}：{string.Join("；", archives.DuplicateSamples)}");
                else
                    Log.Debug($"[Data] 索引里没有跨分包重复的虚拟路径（ReadDuplicateLpkFiles={LpkResourceManager.ReadDuplicateSources}）");

                // 这几张是「单例」数据表：track@zz / trackLocale@区服 / etc_/itemTable.kml /
                // etc_/itemTable@区服.xml / zeta_/区服/shop/data/item.kml。
                // 重复可能有两层：① 同一路径在基础包+补丁包里各有一份；② track_/ 与 track/ 这类不同目录写法
                // 让同一张表在目录树里出现两次。两种都「照读」，解析出的条目按各自主键归并到
                // TrackList / kartName / flyingName / NewRider.items 这些 List·Dictionary 里，
                // 已存在的只补齐缺失字段、不重复追加，因此最终集合里同一份内容只有一条。
                // tableFiles 只用于统计「哪张表读了几份文件」，便于日志确认数据里是否真有重复。
                var tableFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                void NoteTableFile(string fullName)
                {
                    string logicalPath = NormalizeSingletonPath(fullName);
                    if (!tableFiles.TryGetValue(logicalPath, out List<string> paths))
                        tableFiles[logicalPath] = paths = new List<string>();
                    int sources = Math.Max(1, archives.SourceCount(fullName));
                    for (int i = 0; i < sources; i++)
                        paths.Add(fullName);
                }

                Queue<LpkFolderInfo> packFolderInfoQueue = new Queue<LpkFolderInfo>();
                packFolderInfoQueue.Enqueue(archives.GetRootFolder());
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
                                string name = GetFlyingPetSpecName(fullName, regionCode);
                                if (!string.IsNullOrEmpty(name) && !FlyingPet.flyingSpec.ContainsKey(name))
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
                            if (IsTrackDataFile(fullName, $"randomTrack@{regionCode}"))
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    RandomTrack.randomTrack = XDocument.Load(stream);
                                }
                            }
                            // 不再死写 track/ 与 zz：补丁包可能是 track_/ 或其它区服后缀，只要文件名对得上就读取
                            if (IsTrackDataFile(fullName, $"track@zz"))
                            {
                                NoteTableFile(fullName);
                                // 同一路径可能在基础包与补丁包里各有一份：逐份读入，条目按赛道 ID 合并不重复
                                foreach ((byte[] data, string source) in archives.ReadAllSources(fullName))
                                {
                                    Log.Debug($"{fullName} ← {source}");
                                    using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                    {
                                        XmlDocument trackDocument = new XmlDocument();
                                        trackDocument.Load(stream);
                                        ProcessTrackList(trackDocument, $"{fullName} ← {source}");
                                    }
                                }
                            }
                            if (IsTrackDataFile(fullName, $"trackLocale@{regionCode}"))
                            {
                                NoteTableFile(fullName);
                                foreach ((byte[] data, string source) in archives.ReadAllSources(fullName))
                                {
                                    Log.Debug($"{fullName} ← {source}");
                                    using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                    {
                                        XmlDocument localeDocument = new XmlDocument();
                                        localeDocument.Load(stream);
                                        ProcessTrackLocale(localeDocument, $"{fullName} ← {source}");
                                    }
                                }
                            }
                            if (fullName == "etc_/itemTable.kml")
                            {
                                NoteTableFile(fullName);
                                foreach ((byte[] data, string source) in archives.ReadAllSources(fullName))
                                {
                                    Log.Debug($"{fullName} ← {source}");
                                    using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                    {
                                        // 与 itemTable@区服.xml 是同一张表的两份：条目按 id 归并，先到的不被覆盖
                                        ProcessItemTable(XDocument.Load(stream), $"{fullName} ← {source}", overrideFlyingName: false);
                                    }
                                }
                            }
                            if (fullName == $"etc_/itemTable@{regionCode}.xml")
                            {
                                NoteTableFile(fullName);
                                foreach ((byte[] data, string source) in archives.ReadAllSources(fullName))
                                {
                                    Log.Debug($"{fullName} ← {source}");
                                    using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                    {
                                        // 区服表是权威数据：同名飞宠用区服表的名字覆盖，车辆只补缺失字段
                                        ProcessItemTable(XDocument.Load(stream), $"{fullName} ← {source}", overrideFlyingName: true);
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
                                    // 驾照等级表：catLevel（1 起）→ 该等级的关卡数，客户端按这张表渲染关卡
                                    var licenseTable = new SortedDictionary<byte, byte>();
                                    foreach (var category in xdoc.Descendants("category"))
                                    {
                                        if (!byte.TryParse(category.Attribute("catLevel")?.Value, out byte licenseLevel) || licenseLevel == 0)
                                        {
                                            continue;
                                        }
                                        int stepCount = category.Descendants("item")
                                            .Count(i => byte.TryParse(i.Attribute("step")?.Value, out byte stepValue) && stepValue > 0);
                                        if (stepCount > 0)
                                        {
                                            licenseTable[licenseLevel] = (byte)System.Math.Min(stepCount, byte.MaxValue);
                                        }
                                    }
                                    if (licenseTable.Count > 0)
                                    {
                                        RiderSchool.LicenseSteps.Clear();
                                        foreach (var license in licenseTable)
                                        {
                                            RiderSchool.LicenseSteps[license.Key] = license.Value;
                                        }
                                        Log.Info($"[RiderSchool] 驾照等级表 {licenseTable.Count} 档: "
                                            + string.Join(", ", licenseTable.Select(kv => $"{kv.Key}={kv.Value}")));
                                    }
                                    // PRO 特殊设定：proCategory 定义徽章编号与 3 条“达成赛道纪录”时限
                                    var proCategory = xdoc.Descendants("proCategory").FirstOrDefault();
                                    if (proCategory != null)
                                    {
                                        if (ushort.TryParse(proCategory.Attribute("emblemId")?.Value, out ushort emblemId))
                                        {
                                            RiderSchool.ProEmblemId = emblemId;
                                        }
                                        RiderSchool.ProTimeLimits.Clear();
                                        RiderSchool.ProMissions.Clear();
                                        foreach (var proItem in proCategory.Descendants("item"))
                                        {
                                            var mission = new RiderSchool.ProEmblemMission
                                            {
                                                Track = proItem.Attribute("track")?.Value,
                                                TimeLimit = int.TryParse(proItem.Attribute("timeLimit")?.Value, out int limit) ? limit : 0
                                            };
                                            if (byte.TryParse(proItem.Attribute("gameSpeed")?.Value, out byte gameSpeed) && gameSpeed > 0)
                                            {
                                                mission.GameSpeed = gameSpeed;
                                            }
                                            RiderSchool.ProMissions.Add(mission);
                                            if (mission.TimeLimit > 0)
                                            {
                                                RiderSchool.ProTimeLimits.Add(mission.TimeLimit);
                                            }
                                        }
                                        Log.Info($"[RiderSchool] PRO 徽章={RiderSchool.ProEmblemId}，纪录赛道={string.Join("/", RiderSchool.ProMissions.Select(m => m.Track))}，时限={string.Join(",", RiderSchool.ProTimeLimits)}");
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
                            if (fullName == $"zeta_/{regionCode}/quest/QuestAutomation.xml" || fullName == $"zeta/{regionCode}/quest/QuestAutomation.xml")
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
                            if (fullName == $"zeta_/{regionCode}/quest/kartPassQuestAutomation.xml" || fullName == $"zeta/{regionCode}/quest/kartPassQuestAutomation.xml")
                            {
                                Log.Debug(fullName);
                                byte[] data = packFileInfo.GetData();
                                using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                {
                                    XDocument doc = XDocument.Load(stream);
                                    // 包内通常同时存在当前赛季与下一赛季，必须按 seasonPeriod 选出生效的那条
                                    GameSupport.questInfo = GameSupport.SelectActiveKartPassQuestInfo(doc.Descendants("kartPassQuestInfo"));
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/scenario/scenario.xml" || fullName == $"zeta/{regionCode}/scenario/scenario.xml")
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
                                NoteTableFile(fullName);
                                // 基础包与补丁包可能各带一份 item.kml：逐份读入，按 (类别, 道具 ID) 合并去重
                                foreach ((byte[] data, string source) in archives.ReadAllSources(fullName))
                                {
                                    Log.Debug($"{fullName} ← {source}");
                                    ClientItemRules.Load(data);
                                    ClientItemRules.LogSummary();
                                    using (MemoryStream stream = new MemoryStream(GameSupport.NormalizeXmlEncoding(data)))
                                    {
                                        XmlDocument doc = new XmlDocument();
                                        doc.Load(stream);
                                        XmlNodeList bodyParams = doc.GetElementsByTagName("item");
                                        int itemTotal = bodyParams.Count;
                                        int itemAdded = 0;
                                        int itemDuplicated = 0;
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
                                                // 以 (道具类别, 道具 ID) 为主键：重复文件/重复条目只保留先读到的那条
                                                if (!NewRider.items[itemCatId].ContainsKey(itemId))
                                                {
                                                    NewRider.items[itemCatId][itemId] = itemName;
                                                    itemAdded++;
                                                }
                                                else
                                                {
                                                    itemDuplicated++;
                                                }
                                            }
                                        }
                                        Log.Debug($"[ItemKml] {fullName}: 条目 {itemTotal} 条（新增 {itemAdded} / 已存在跳过 {itemDuplicated}），NewRider.items 现有 {NewRider.items.Sum(cat => cat.Value.Count)} 条");
                                    }
                                }
                            }
                            // RP 赛（福不福）池子：客户端决定"好运/霉运"画面的唯一依据
                            if (fullName.EndsWith($"/content/bokbulbok/bokbulbok.xml", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Debug(fullName);
                                NoteTableFile(fullName);
                                foreach ((byte[] data, string source) in archives.ReadAllSources(fullName))
                                {
                                    BokbulbokPool.Load(data, source);
                                }
                                BokbulbokPool.LogSummary();
                            }
                            if (fullName == $"zeta_/{regionCode}/engine/kart12TuningData.xml")
                            {
                                Log.Debug(fullName);
                                byte[] tuneMap = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(tuneMap)))
                                {
                                    KartTuneTable.LoadSkillMap(XDocument.Load(ms));
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/enchant/enchant.xml")
                            {
                                Log.Debug(fullName);
                                byte[] tuneData = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(tuneData)))
                                {
                                    KartTuneTable.LoadEnchant(XDocument.Load(ms));
                                }
                                KartTuneTable.LogSummary();
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
                            if (fullName == $"item/slot/transformByKart@{regionCode}.xml")
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
                            if (fullName == $"item/slot/fired2Gain@{regionCode}.xml")
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
                            if (fullName == $"item/slot/firing2Gain@{regionCode}.xml")
                            {
                                Log.Debug(fullName);
                                byte[] gd = packFileInfo.GetData();
                                using (MemoryStream ms = new MemoryStream(GameSupport.NormalizeXmlEncoding(gd)))
                                {
                                    ClientItemRules.ApplyFiring2Gain(XDocument.Load(ms), true);
                                }
                            }
                            if (fullName == $"zeta_/{regionCode}/content/itemGameRestrictionItemCount.xml")
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
                            if (fullName == $"item/slot/itemProb_team2@{regionCode}.xml")
                            {
                                Log.Debug(fullName);
                                byte[] probData = packFileInfo.GetData();
                                using (MemoryStream probStream = new MemoryStream(GameSupport.NormalizeXmlEncoding(probData)))
                                {
                                    ItemRule.ApplyFromClient(4, XDocument.Load(probStream));
                                }
                            }
                            if (fullName == "item/slot/itemProb_flag@zz.xml")
                            {
                                // 夺旗赛专用道具表：客户端只支持这组道具，普通道具会导致吃道具闪退
                                Log.Debug(fullName);
                                byte[] probData = packFileInfo.GetData();
                                using (MemoryStream probStream = new MemoryStream(GameSupport.NormalizeXmlEncoding(probData)))
                                {
                                    ItemRule.ApplyFromClient(GameModes.FlagTeam, XDocument.Load(probStream));
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
                                        // 顺便记住商品名（管理后台按名字搜 stockId 用）
                                        if (stockElement.HasAttribute("stockName"))
                                        {
                                            Stock.StockName[stockId] = stockElement.GetAttribute("stockName");
                                        }
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
                                        string startText = periodParts.Length > 0 ? periodParts[0] : "";
                                        // 解析开始时间：兼容 yyyy-MM-ddTHH:mm:ss 与 yyyyMMddTHHmmss
                                        if (!TryParseChannelPeriod(startText, out DateTime startTime))
                                        {
                                            // 解析不了就按「已开放」处理：旧实现直接跳过，会让整档频道凭空少掉
                                            Log.Debug($"警告：{channelValue.Name} 的openPeriod无法解析，按已开放处理：{openPeriod}");
                                            GameSupport.Channels.TryAdd(i++, channelValue);
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
                int duplicatedTables = 0;
                var duplicatedTableNames = new List<string>();
                foreach (KeyValuePair<string, List<string>> table in tableFiles)
                {
                    if (table.Value.Count <= 1) continue;
                    duplicatedTables++;
                    // 同一路径的多个来源合并显示为「路径 ×N 份」，不同路径写法逐个列出
                    var groups = table.Value
                        .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.Count() > 1 ? $"{g.Key} ×{g.Count()} 份" : g.Key);
                    duplicatedTableNames.Add($"{table.Key} ← {string.Join(" , ", groups)}");
                }
                if (duplicatedTables > 0)
                {
                    string action = LpkResourceManager.ReadDuplicateSources
                        ? "已全部读入并按条目主键合并去重"
                        : "ReadDuplicateLpkFiles=false，每张只读第一份（旧行为）";
                    Log.Warn($"[Data] 单例表：{tableFiles.Count} 张表中 {duplicatedTables} 张存在重复文件（{action}）：{string.Join("；", duplicatedTableNames)}");
                }
                else
                    Log.Debug($"[Data] 单例表：读入 {tableFiles.Count} 张表，每张只有 1 份文件，未发现重复");
                // 所有 track/locale 都读完后再收尾：缺名称的用 ID 兜底，避免新增赛道被随机池静默过滤
                RandomTrack.FinalizeTrackList();
                return archives;
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
            int index = file.IndexOf(".rho", StringComparison.OrdinalIgnoreCase);
            if (index < 0) return file;
            // 单次分配：去掉 ".rho" 的同时把 '.' 前的 '_' 还原成 '/'
            return string.Create(file.Length - 4, (file, index), static (span, state) =>
            {
                string source = state.file;
                int cut = state.index;
                int j = 0;
                for (int i = 0; i < source.Length; i++)
                {
                    if (i == cut) { i += 3; continue; }
                    char c = source[i];
                    span[j++] = (i < cut && c == '_') ? '/' : c;
                }
            });
        }

        /// <summary>
        /// 单例数据表（track@zz / trackLocale@区服 / itemTable.* / shop item.kml）的去重键：
        /// 目录部分把 '_' 视作 '/'、折叠重复斜杠、去掉末尾斜杠，文件名保持原样，
        /// 使「track_/common/track@zz.xml」与「track/common/track@zz.xml」被判为同一份表。
        /// </summary>
        private static string NormalizeSingletonPath(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;
            int slash = fullName.LastIndexOf('/');
            ReadOnlySpan<char> directory = slash >= 0 ? fullName.AsSpan(0, slash) : ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> fileName = slash >= 0 ? fullName.AsSpan(slash + 1) : fullName.AsSpan();

            var builder = new StringBuilder(directory.Length + fileName.Length + 1);
            bool lastSlash = false;
            foreach (char c in directory)
            {
                char ch = c == '_' ? '/' : c;
                if (ch == '/')
                {
                    if (lastSlash) continue;
                    lastSlash = true;
                }
                else
                {
                    lastSlash = false;
                }
                builder.Append(ch);
            }
            if (builder.Length == 0 || builder[builder.Length - 1] != '/') builder.Append('/');
            builder.Append(fileName);
            return builder.ToString();
        }

        /// <summary>
        /// 解析 channel.xml 的 openPeriod 开始时间。
        /// 兼容 "yyyy-MM-ddTHH:mm:ss"、"yyyyMMddTHHmmss" 等写法；"*" 或空表示不限开始时间。
        /// </summary>
        private static bool TryParseChannelPeriod(string text, out DateTime time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(text) || text.Trim() == "*")
            {
                time = DateTime.MinValue;
                return true;
            }
            string value = text.Trim();
            if (DateTime.TryParse(value, out time))
            {
                return true;
            }
            return DateTime.TryParseExact(
                value,
                new[] { "yyyyMMdd'T'HHmmss", "yyyyMMddHHmmss", "yyyy-MM-dd HH:mm:ss" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out time);
        }

        /// <summary>
        /// etc_/itemTable.kml 与 etc_/itemTable@区服.xml：两张表内容重叠，数据里还可能存在重复文件。
        /// 条目按主键读进 Dictionary（车辆/飞宠均以 id 为键），已存在的只补齐缺失字段、不重复追加，
        /// 因此文件读几遍，Kart.kartName / FlyingPet.flyingName 里每个 id 都只有一条。
        /// </summary>
        private static void ProcessItemTable(XDocument doc, string sourceName, bool overrideFlyingName)
        {
            if (doc == null) return;

            int kartTotal = 0, kartAdded = 0, kartMerged = 0, kartInvalid = 0;
            foreach (XElement kart in doc.Descendants("kart").Where(kart => kart.Attribute("name") != null))
            {
                kartTotal++;
                if (!int.TryParse(kart.Attribute("id")?.Value, out int id))
                {
                    kartInvalid++;
                    continue;
                }
                string name = kart.Attribute("name").Value;
                byte grade = byte.Parse(kart.Attribute("grade")?.Value ?? "0");
                List<short> defaultEnchant = (kart.Attribute("defaultEnchant")?.Value ?? "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => short.TryParse(s.Trim(), out var v) ? (short?)v : null)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .ToList();
                if (Kart.kartName.TryGetValue(id, out KartTable kartTable))
                {
                    // 已存在（重复文件或另一份表）：只补缺失字段，不新增条目
                    kartMerged++;
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
                    Kart.kartName.Add(id, new KartTable { Name = name, grade = grade, defaultEnchant = defaultEnchant });
                    kartAdded++;
                }
            }

            int flyingTotal = 0, flyingAdded = 0, flyingDuplicated = 0, flyingInvalid = 0;
            foreach (XElement flyingPet in doc.Descendants("flyingPet").Where(f => f.Attribute("name") != null))
            {
                flyingTotal++;
                if (!int.TryParse(flyingPet.Attribute("id")?.Value, out int id))
                {
                    flyingInvalid++;
                    continue;
                }
                string name = flyingPet.Attribute("name").Value;
                if (!FlyingPet.flyingName.ContainsKey(id))
                {
                    FlyingPet.flyingName.Add(id, name);
                    flyingAdded++;
                }
                else
                {
                    flyingDuplicated++;
                    // 区服表优先；同一份表的重复文件保留先读到的那条
                    if (overrideFlyingName)
                    {
                        FlyingPet.flyingName[id] = name;
                    }
                }
            }

            Log.Debug($"[ItemTable] {sourceName}: 车辆 {kartTotal} 条（新增 {kartAdded} / 已存在合并 {kartMerged} / 无效 {kartInvalid}），" +
                      $"飞宠 {flyingTotal} 条（新增 {flyingAdded} / 重复 {flyingDuplicated} / 无效 {flyingInvalid}），" +
                      $"合计 kartName {Kart.kartName.Count} / flyingName {FlyingPet.flyingName.Count}");
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
                bool blocked = IsTrackBlocked(xe);
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
                        IsTrackBlocked(xe));
                }
            }
            processVariant("track_crz", "crz");
            processVariant("track_rvs", "rvs");
            Log.Info($"[Track] {sourceName}: 共 {total} 条赛道节点，新增 {RandomTrack.TrackList.Count - before} 条，跳过 {skipped} 条，TrackList 现有 {RandomTrack.TrackList.Count} 条");
        }

        /// <summary>
        /// 从 flyingPet/xxx/param@cn.xml 里取出飞宠参数名（取 param@xx.xml 所在目录的最后一级）。
        /// 客户端里还有 flyingPet/xxx/xxx/param@cn.xml 这种多一层目录的写法，
        /// 原来固定 Substring(10, len-23) 会得到 "xxx/" 这种带斜杠的 key，永远匹配不上 flyingName。
        /// </summary>
        private static string GetFlyingPetSpecName(string fullName, string regionCode)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;
            string normalized = fullName.Replace('\\', '/');
            int index = normalized.LastIndexOf($"param@{regionCode}.xml", StringComparison.OrdinalIgnoreCase);
            if (index < 0) index = normalized.LastIndexOf("param@", StringComparison.OrdinalIgnoreCase);
            if (index < 0) return string.Empty;
            string folder = normalized.Substring(0, index).TrimEnd('/');
            int slash = folder.LastIndexOf('/');
            return (slash >= 0 ? folder.Substring(slash + 1) : folder).Trim();
        }

        private static bool IsTrackDataFile(string filePath, string fileNamePrefix)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            // 全程用 Span 判断，不做 Replace / Substring（该函数按记录调用，可达十万次量级）
            ReadOnlySpan<char> path = filePath.AsSpan();
            int slash = path.LastIndexOfAny('/', '\\');
            ReadOnlySpan<char> fileName = slash >= 0 ? path.Slice(slash + 1) : path;
            if (!fileName.StartsWith(fileNamePrefix.AsSpan(), StringComparison.OrdinalIgnoreCase)) return false;
            if (!fileName.EndsWith(".xml".AsSpan(), StringComparison.OrdinalIgnoreCase)) return false;
            return path.IndexOf("/common/".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("\\common\\".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0;
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

        /// <summary>
        /// 赛道是否"不进随机池"：
        ///   blocked='true'        —— 客户端标记禁用
        ///   isOnlyTraining='true' —— 仅练习场（例如 village_I11 城镇 自由练习场）
        ///   choosable='false'     —— 不可选（例如 [奔跑车手] 系列）
        /// 这些赛道仍然保留在 TrackList 里（能显示/能手动选），只是不参与随机。
        /// </summary>
        private static bool IsTrackBlocked(XmlElement element)
        {
            if (GetBoolAttribute(element, "blocked")
                || GetBoolAttribute(element, "isOnlyTraining"))
            {
                return true;
            }
            string choosable = GetAttributeText(element, "choosable");
            return choosable.Equals("false", StringComparison.OrdinalIgnoreCase);
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
                    IsTrackBlocked(xe));
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
                        IsTrackBlocked(xe));
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

        /// <summary>
        /// 目录扫描深度：默认 0 = 只读取 Data 一级目录（根目录）里的 .lpk，不递归子目录。
        /// 调大到 1、2… 才包含对应层级的子目录，可配合 ProbeLpkFolders 做「一级级测试」。
        /// </summary>
        public static int MaxLpkSubdirectoryLevel { get; set; } = 0;

        /// <summary>
        /// 装载时让更深子目录里的分包优先（用于自定义包覆盖官方包）；默认 false。
        /// 只有 MaxLpkSubdirectoryLevel &gt; 0（能扫到子目录）时才有意义。
        /// </summary>
        public static bool PreferDeeperLpkDirectories { get; set; } = false;

        /// <summary>
        /// 排查开关：设 true 时目录树挂全部 LPK 记录（不再按 IsRequiredPath 过滤），
        /// 用于验证「按需读取白名单漏文件」；数据仍只在 GetData() 时惰性解密。
        /// 离线验证全量树与精简树生成的数据表完全一致，故默认保持 false。
        /// </summary>
        public static bool BuildFullLpkTree { get; set; } = false;

        /// <summary>
        /// 一条 LPK 记录是否属于 Dump 需要解析的内容。这里只是「候选名单」：
        /// 名单中的条目只有在 Dump 的规则分支真正命中、调用 GetData() 时才会读盘解密。
        /// 新增解析规则时，记得同步这里或 RequiredExactPaths。
        /// </summary>
        /// <summary>区服相关的匹配串预先拼好，避免按记录（十万级）反复做字符串插值。</summary>
        private sealed class RegionSuffixes
        {
            public readonly string ParamXml;
            public readonly string ParamKml;
            public readonly string RandomTrackXml;
            public readonly string TrackLocaleXml;
            public readonly string FlyingPetParamXml;

            public RegionSuffixes(string region)
            {
                ParamXml = $"/param@{region}.xml";
                ParamKml = $"/param@{region}.kml";
                RandomTrackXml = $"randomTrack@{region}";
                TrackLocaleXml = $"trackLocale@{region}";
                FlyingPetParamXml = $"param@{region}.xml";
            }
        }

        private static bool IsRequiredPath(string fullName, RegionSuffixes rules, HashSet<string> exact)
        {
            if (string.IsNullOrEmpty(fullName)) return false;
            if (exact.Contains(fullName)) return true;
            if (fullName.Contains("flyingPet") && fullName.Contains(rules.FlyingPetParamXml)) return true;
            if (IsTrackDataFile(fullName, rules.RandomTrackXml)
                || IsTrackDataFile(fullName, "track@zz")
                || IsTrackDataFile(fullName, rules.TrackLocaleXml)) return true;
            return fullName.Contains("kart_")
                && (fullName.Contains(rules.ParamXml)
                    || fullName.Contains(rules.ParamKml)
                    || fullName.Contains("/param.xml"));
        }

        /// <summary>Dump 里按全路径精确匹配的那些文件。</summary>
        private static HashSet<string> RequiredExactPaths(string region)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "etc_/itemTable.kml",
                $"etc_/itemTable@{region}.xml",
                $"etc_/emblem/emblem@{region}.xml",
                $"etc_/riderSchool/riderSchoolLocale@{region}.xml",
                $"zeta_/{region}/quest/QuestAutomation.xml",
                $"zeta/{region}/quest/QuestAutomation.xml",
                $"zeta_/{region}/quest/kartPassQuestAutomation.xml",
                $"zeta/{region}/quest/kartPassQuestAutomation.xml",
                $"zeta_/{region}/scenario/scenario.xml",
                $"zeta/{region}/scenario/scenario.xml",
                $"zeta_/{region}/shop/data/item.kml",
                $"zeta_/{region}/engine/kart12TuningData.xml",
                $"zeta_/{region}/enchant/enchant.xml",
                "item/slot/transformByKart.xml",
                $"item/slot/transformByKart@{region}.xml",
                "item/slot/fired2Gain.xml",
                $"item/slot/fired2Gain@{region}.xml",
                "item/slot/firing2Gain.xml",
                $"item/slot/firing2Gain@{region}.xml",
                $"zeta_/{region}/content/itemGameRestrictionItemCount.xml",
                "item/slot/itemProb_indi@zz.xml",
                $"item/slot/itemProb_team2@{region}.xml",
                "item/slot/itemProb_flag@zz.xml",
                $"zeta_/{region}/content/basicAI.xml",
                $"zeta_/{region}/content/bingoGachaSet.xml",
                $"zeta_/{region}/content/itemDictionary.xml",
                $"zeta_/{region}/content/timeAttack/timeAttackMission.xml",
                $"zeta_/{region}/content/timeAttack/timeAttackCompetitive.xml",
                $"zeta_/{region}/content/timeAttack/timeAttackCompetitiveData.xml",
                $"zeta_/{region}/lottery/lottery.xml",
                $"zeta_/{region}/shop/data/stock.kml",
                $"zeta_/{region}/content/channel.xml",
                // RP 赛（福不福）池子：客户端决定"好运/霉运"画面的唯一依据，必须读
                $"zeta_/{region}/content/bokbulbok/bokbulbok.xml",
                $"zeta/{region}/content/bokbulbok/bokbulbok.xml",
                $"zeta_/{region}/content/mix/mixTable.xml",
            };
        }

        /// <summary>
        /// 与 KartRider.exe.c 的 LPK 资源管理层等价的按需读取器：
        ///   sub_1476C3430  把所有分包的记录合并成按路径查找的全局表；
        ///   sub_1476B70D0  按名查记录；
        ///   sub_1476E9510  CreateFileW 打开所属 .lpk → sub_1476D7410 定位到 dataBegin + (blocks &lt;&lt; 10)
        ///                  → sub_1476D7430 限定读窗口 [start, start + csize)；
        ///   sub_1476E97A0  sub_1476E9CF0 / sub_1476ED2B0 用 MASK 派生 32 字节 key，ChaCha20 只解前 1024 字节，
        ///                  flags &amp; 1 时 sub_1476EEFB0 做 zlib inflate，产出明文。
        /// LpkV4.Decode / LpkV4.Extract 就是上述密钥派生、ChaCha 与 inflate 的 C# 移植。
        /// 这里只保存记录表，不缓存整包、不常驻文件句柄；文件数据按条目打开、解密后缓存解密结果。
        /// </summary>
        private sealed class LpkResourceManager : LpkFolderManager
        {
            private sealed class Archive
            {
                public string Path;
                public LpkIndex Index;
                public int[] Digits;
                public byte[] Nonce;
            }

            private sealed class RecordEntry
            {
                public Archive Archive;
                public LpkRecord Record;
            }

            private readonly List<Archive> _archives = new List<Archive>();
            private readonly Dictionary<string, List<RecordEntry>> _records =
                new Dictionary<string, List<RecordEntry>>(StringComparer.OrdinalIgnoreCase);
            private readonly List<string> _allPaths = new List<string>();
            // 并行预解密时多个线程会同时写各自的 key，用并发字典保证线程安全
            private readonly ConcurrentDictionary<string, byte[]> _decoded =
                new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            // 每个来源各自的解密结果（同一路径在多个分包里重复时用）
            private readonly ConcurrentDictionary<(string Key, int Source), byte[]> _decodedBySource = new();
            private int _requiredCount;

            public int ArchiveCount => _archives.Count;
            public int PathCount => _allPaths.Count;
            public int RequiredCount => _requiredCount;
            public IEnumerable<string> AllPaths => _allPaths;

            /// <summary>
            /// 是否读入同一虚拟路径在多个分包里的所有副本；由 Profile.Setting.ReadDuplicateLpkFiles 控制。
            /// true = 每份都读、条目按主键合并去重；false = 回到旧行为，只读先装载的那一份。
            /// </summary>
            public static bool ReadDuplicateSources { get; set; } = true;

            /// <summary>在多个分包里重复出现的虚拟路径数量（同一路径被多个 .lpk 各带一份）。</summary>
            public int DuplicatePathCount { get; private set; }
            /// <summary>重复路径多出来的副本总数（每多一个分包 +1）。</summary>
            public int DuplicateRecordCount { get; private set; }
            /// <summary>重复路径的示例（路径 + 来源分包），用于日志排查。</summary>
            public List<string> DuplicateSamples { get; } = new List<string>();

            /// <summary>该虚拟路径在索引里共有几个来源（跨分包副本数）；不存在返回 0。</summary>
            public int SourceCount(string key)
            {
                return key != null && _records.TryGetValue(key, out List<RecordEntry> entries) ? entries.Count : 0;
            }

            /// <summary>只读每个分包的头部与记录表，合并成全局路径索引；不读取任何文件数据。</summary>
            public bool Open(string input, out string error)
            {
                string root;
                string[] lpkFiles = EnumerateLpkFiles(input, out root, out error);
                if (error != null) return false;
                if (lpkFiles.Length == 0)
                {
                    error = "未找到任何 .lpk 文件";
                    return false;
                }

                // 按文件名（大小写不敏感）顺序装载：0_*.lpk 这类自定义分包排在 DataPack* 前面，
                // 同路径记录保留先装载的那条，自定义分包因此能覆盖官方分包。
                // 开启 PreferDeeper 时先按子目录深度降序，组内仍按文件名排序。
                if (PreferDeeperLpkDirectories)
                {
                    Array.Sort(lpkFiles, (a, b) =>
                    {
                        int depth = SubdirectoryLevel(root, b).CompareTo(SubdirectoryLevel(root, a));
                        return depth != 0 ? depth : CompareByFileName(a, b);
                    });
                }
                else
                {
                    Array.Sort(lpkFiles, CompareByFileName);
                }
                foreach (string lpk in lpkFiles)
                {
                    try
                    {
                        using var fs = new FileStream(lpk, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        LpkIndex index = LpkV4.Decode(fs, System.IO.Path.GetFileName(lpk));
                        var (digits, nonce) = LpkV4.Parameters(index.Mixing);
                        var archive = new Archive
                        {
                            Path = lpk,
                            Index = index,
                            Digits = digits,
                            Nonce = nonce
                        };
                        _archives.Add(archive);
                        foreach (LpkRecord record in index.Records)
                        {
                            string raw = NormalizeRecordPath(record.Path);
                            string key = KartRhoFile.ReplacePath(raw);
                            if (!_records.TryGetValue(key, out var entries))
                                _records[key] = entries = new List<RecordEntry>();
                            entries.Add(new RecordEntry { Archive = archive, Record = record });
                            _allPaths.Add(raw);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 单个分包损坏/版本不认识时只跳过它，不能让整份 Data 加载失败
                        Log.Debug($"[Data] 跳过无法索引的 LPK：{System.IO.Path.GetFileName(lpk)}（{ex.Message}）");
                    }
                }
                if (_archives.Count == 0)
                {
                    error = "没有任何可用的 .lpk 分包";
                    return false;
                }
                CountDuplicatePaths();
                return true;
            }

            /// <summary>
            /// 统计「同一虚拟路径出现在多个分包」的重复记录：基础包 + 补丁包里各带一份是最常见的情况。
            /// 这些副本不会再进目录树（树按路径只挂一份），所以必须在这里统计，否则上层永远看不到重复。
            /// </summary>
            private void CountDuplicatePaths()
            {
                DuplicatePathCount = 0;
                DuplicateRecordCount = 0;
                DuplicateSamples.Clear();
                foreach (KeyValuePair<string, List<RecordEntry>> record in _records)
                {
                    int sources = record.Value.Count;
                    if (sources <= 1) continue;
                    DuplicatePathCount++;
                    DuplicateRecordCount += sources - 1;
                    if (DuplicateSamples.Count < 10)
                    {
                        var names = new List<string>(sources);
                        foreach (RecordEntry entry in record.Value)
                            names.Add(System.IO.Path.GetFileName(entry.Archive.Path));
                        DuplicateSamples.Add($"{record.Key} ×{sources}（{string.Join(" , ", names)}）");
                    }
                }
            }

            /// <summary>统一解析输入并枚举 .lpk，按 MaxLpkSubdirectoryLevel 限制子目录深度。</summary>
            private static string[] EnumerateLpkFiles(string input, out string root, out string error)
            {
                error = null;
                string[] files;
                if (System.IO.File.Exists(input) && input.EndsWith(".lpk", StringComparison.OrdinalIgnoreCase))
                {
                    root = System.IO.Path.GetDirectoryName(input) ?? ".";
                    files = new[] { input };
                }
                else
                {
                    root = System.IO.Directory.Exists(input) ? input : System.IO.Path.GetDirectoryName(input);
                    if (string.IsNullOrEmpty(root)) root = ".";
                    if (!System.IO.Directory.Exists(root))
                    {
                        error = "找不到 .lpk 文件所在目录: " + root;
                        return Array.Empty<string>();
                    }
                    if (MaxLpkSubdirectoryLevel <= 0)
                    {
                        // 只读取 Data 一级目录：Data\Downloaded 等子目录里的分包不进索引
                        files = System.IO.Directory.GetFiles(root, "*.lpk", SearchOption.TopDirectoryOnly);
                    }
                    else
                    {
                        files = System.IO.Directory.GetFiles(root, "*.lpk", SearchOption.AllDirectories);
                        if (MaxLpkSubdirectoryLevel < int.MaxValue)
                        {
                            string baseDir = root;
                            files = files.Where(p => SubdirectoryLevel(baseDir, p) <= MaxLpkSubdirectoryLevel).ToArray();
                        }
                    }
                }
                return files;
            }

            /// <summary>相对根目录的子目录层级：根 = 0，一级子目录 = 1，依此类推。</summary>
            private static int SubdirectoryLevel(string root, string file)
            {
                string relative = System.IO.Path.GetRelativePath(root, file);
                string directory = System.IO.Path.GetDirectoryName(relative) ?? "";
                if (directory.Length == 0 || directory == ".") return 0;
                return directory.Split(
                    new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries).Length;
            }

            /// <summary>只把规则可能用到的条目挂到目录树上，DataLoader 全部是惰性解密。</summary>
            public void BuildRequiredTree(string regionCode)
            {
                HashSet<string> exact = RequiredExactPaths(regionCode);
                var rules = new RegionSuffixes(regionCode);
                var keys = new List<string>();
                foreach (string key in _records.Keys)
                {
                    if (KartRhoFile.BuildFullLpkTree || IsRequiredPath(key, rules, exact))
                        keys.Add(key);
                }
                keys.Sort(StringComparer.OrdinalIgnoreCase);
                _requiredCount = keys.Count;
                Prefetch(keys);
                foreach (string key in keys)
                {
                    string path = key;
                    AddToTree(path, () => Read(path));
                }
            }

            /// <summary>是否并行预解密（默认开）。并行只影响“读盘 + 解密 + 解压”，XML 解析顺序不变。</summary>
            public static bool ParallelPrefetch { get; set; } = true;

            /// <summary>并行预解密线程数；0 = 按 CPU 核心数自动决定。</summary>
            public static int PrefetchThreads { get; set; } = 0;

            /// <summary>
            /// 先把待解析记录并行读进缓存，随后 Dump 的顺序遍历就只剩 XML 解析。
            /// 单个文件失败只记日志，顺序阶段仍会重试并给出同样的错误信息。
            /// </summary>
            private void Prefetch(List<string> keys)
            {
                if (!ParallelPrefetch || keys.Count < 8) return;
                int threads = PrefetchThreads > 0 ? PrefetchThreads : Environment.ProcessorCount;
                if (threads <= 1) return;
                threads = Math.Min(threads, 8);

                long started = Environment.TickCount64;
                Parallel.For(0, keys.Count, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                {
                    try
                    {
                        Read(keys[i]);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"[Data] 预读 {keys[i]} 失败：{ex.Message}");
                    }
                });
                Log.Debug($"[Data] 并行预解密 {keys.Count} 条（{threads} 线程）耗时 {Environment.TickCount64 - started} ms");
            }

            /// <summary>
            /// sub_1476E9510 + sub_1476E97A0：按路径命中记录后打开所属 .lpk，
            /// 定位到数据区并做密钥派生 / ChaCha 解密 / inflate，最后校验长度与 MD5。
            /// </summary>
            private byte[] Read(string key)
            {
                if (_decoded.TryGetValue(key, out byte[] cached)) return cached;
                if (!_records.TryGetValue(key, out var entries))
                    throw new LpkException("记录不存在: " + key);

                Exception lastError = null;
                // 先装载的分包优先；记录损坏/解密失败时，回退到其它分包里的同路径记录
                for (int i = 0; i < entries.Count; i++)
                {
                    try
                    {
                        byte[] data = ReadSource(key, entries, i);
                        _decoded[key] = data;
                        return data;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        Log.Debug($"[Data] {key} 在 {System.IO.Path.GetFileName(entries[i].Archive.Path)} 解密失败：{ex.Message}");
                    }
                }
                throw new LpkException($"{key} 无法解密：{lastError?.Message}");
            }

            private byte[] ReadSource(string key, List<RecordEntry> entries, int index)
            {
                if (_decodedBySource.TryGetValue((key, index), out byte[] cached)) return cached;
                RecordEntry entry = entries[index];
                using var fs = new FileStream(entry.Archive.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] data = LpkV4.Extract(fs, entry.Record, entry.Archive.Index.DataBegin,
                    entry.Archive.Digits, entry.Archive.Nonce);
                _decodedBySource[(key, index)] = data;
                return data;
            }

            /// <summary>
            /// 读出该虚拟路径在**所有**分包里的副本（解密失败的来源记日志后跳过），连同来源分包名。
            /// 同一路径同时存在于基础包与补丁包时，只看第一份会漏掉补丁里的条目；
            /// 上层拿到多份后按条目主键合并去重即可。
            /// </summary>
            public List<(byte[] Data, string Archive)> ReadAllSources(string key)
            {
                var result = new List<(byte[] Data, string Archive)>();
                if (key == null || !_records.TryGetValue(key, out List<RecordEntry> entries)) return result;
                // 设置关掉时回到旧行为：每个虚拟路径只读第一份
                int count = ReadDuplicateSources ? entries.Count : 1;
                for (int i = 0; i < count; i++)
                {
                    string archiveName = System.IO.Path.GetFileName(entries[i].Archive.Path);
                    try
                    {
                        // 只读一份时走 Read()，直接复用预解密缓存
                        result.Add((count == 1 ? Read(key) : ReadSource(key, entries, i), archiveName));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"[Data] {key} 在 {archiveName} 解密失败：{ex.Message}");
                    }
                }
                return result;
            }

            private void AddToTree(string key, Func<byte[]> loader)
            {
                string[] parts = key.Split('/');
                LpkFolderInfo current = GetRootFolder();
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (!current.SubFolders.TryGetValue(parts[i], out var sub))
                    {
                        sub = new LpkFolderInfo
                        {
                            FolderName = parts[i],
                            FullName = current.FullName == "" ? parts[i] : current.FullName + "/" + parts[i]
                        };
                        current.SubFolders[parts[i]] = sub;
                    }
                    current = sub;
                }
                string fileName = parts[parts.Length - 1];
                if (current.Files.ContainsKey(fileName)) return;
                current.Files[fileName] = new LpkFileInfo
                {
                    FileName = fileName,
                    FullName = key,
                    DataLoader = loader
                };
            }

            // LPK 里的路径有几种写法：zeta/cn/...、zeta_/cn/...、zeta_cn.rho/...
            // 前缀一次拼好，扫描十万级记录时不再每条 ToLowerInvariant + 12 次字符串拼接。
            private static readonly string[] RegionNames = { "kr", "cn", "tw" };
            private static readonly string[][] RegionPrefixes =
            {
                new[] { "zeta/kr/", "zeta_/kr/", "zeta_kr.rho", "zeta_kr/" },
                new[] { "zeta/cn/", "zeta_/cn/", "zeta_cn.rho", "zeta_cn/" },
                new[] { "zeta/tw/", "zeta_/tw/", "zeta_tw.rho", "zeta_tw/" }
            };

            /// <summary>依据全局索引里的记录路径推断区服代码（kr / cn / tw），默认 cn。</summary>
            public string DetectRegionCode()
            {
                foreach (string path in _allPaths)
                {
                    for (int r = 0; r < RegionPrefixes.Length; r++)
                    {
                        string[] prefixes = RegionPrefixes[r];
                        for (int p = 0; p < prefixes.Length; p++)
                        {
                            if (path.StartsWith(prefixes[p], StringComparison.OrdinalIgnoreCase))
                                return RegionNames[r];
                        }
                    }
                }
                return "cn";
            }

            private static string NormalizeRecordPath(string path)
            {
                if (string.IsNullOrEmpty(path)) return string.Empty;
                return path.Replace('\\', '/').TrimStart('/');
            }

            /// <summary>按文件名排序；同名时用完整路径作次级键，保证跨子目录时顺序确定。</summary>
            private static int CompareByFileName(string a, string b)
            {
                int result = string.Compare(System.IO.Path.GetFileName(a), System.IO.Path.GetFileName(b),
                    StringComparison.OrdinalIgnoreCase);
                return result != 0 ? result : string.CompareOrdinal(a, b);
            }
        }
    }
}
