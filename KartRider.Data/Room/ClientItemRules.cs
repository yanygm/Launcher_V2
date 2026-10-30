using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KartRider.Logging;
using Profile;

namespace KartRider;

/// <summary>
/// 从客户端 `zeta_/cn/shop/data/item.kml` 的 itemEffect 解析出的道具车词条规则。
///
/// 覆盖三类服务端需要参与的效果（防御/多发/逃脱由客户端自己处理）：
///   - 变更：`将[X]变更为[Y]`、`将[X]，[Z]变更为[Y]`、`将[X]系列道具变更为[Y]`、`一定概率将…`
///   - 使用后：`使用[X]时获得[Y]`、`使用[X]时一定概率获得[Y]`
///   - 被攻击：`被[X]攻击时获得[Y]`、`被[X]，[Z]攻击时一定概率获得[Y]`、`受到[X]攻击时获得[Y]`
///
/// 概率文案没有数值，按以下启发式解释（可在 Profile/V2KartRules.json 里覆盖）：
///   一定概率=100、极高=90、高=80、较低=30、低=20、默认=100
/// </summary>
public static class ClientItemRules
{
    public sealed class KartRule
    {
        public List<short> StartItems { get; } = new();
        public List<Rule> Convert { get; } = new();
        public List<Rule> OnUse { get; } = new();
        public List<Rule> OnAttacked { get; } = new();
    }

    public sealed class Rule
    {
        public short From { get; set; }
        public short To { get; set; }

        /// <summary>该条来自客户端规则表（fired2Gain / firing2Gain / transformByKart），属于权威数据。</summary>
        public bool FromClientTable { get; set; }

        public int Probability { get; set; } = 100;
    }

    private static readonly Dictionary<string, short> NameToId = new Dictionary<string, short>(StringComparer.Ordinal)
    {
        ["大魔王"] = 2,
        ["飞碟"] = 3,
        ["水苍蝇"] = 4,
        ["磁铁"] = 5,
        ["加速器"] = 6,
        ["导弹"] = 7,
        ["香蕉"] = 8, ["香蕉皮"] = 8,
        ["水炸弹"] = 9,
        ["盾牌"] = 10,
        ["天使"] = 11,
        ["电磁波"] = 12, ["解碟"] = 12,
        ["定时水炸弹"] = 13, ["定时炸弹"] = 13,
        ["组队加速器"] = 14,
        ["夺旗模式连发导弹"] = 15,
        ["幽灵"] = 16, ["隐身"] = 16,
        ["地雷"] = 17,
        ["加速盾牌"] = 18,
        ["夺旗模式投射水炸弹"] = 19,
        ["棕色水炸弹"] = 20,
        ["棕色定时水炸弹"] = 21,
        ["夺旗模式棕色投射水炸弹"] = 22,
        ["R博士"] = 23,
        ["警灯"] = 24, ["警笛"] = 24,
        ["弹性陷阱"] = 25,
        ["高级飞碟"] = 26,
        ["毒性水炸弹"] = 27, ["毒水炸弹"] = 27,
        ["毒性定时水炸弹"] = 28,
        ["夺旗模式毒性投射水炸弹"] = 29,
        ["可口可乐"] = 30,
        ["南瓜加速器"] = 31, ["小猪加速器"] = 31, ["心形加速器"] = 31, ["熊猫加速器"] = 31, ["爱心加速器"] = 31, ["狐尾加速器"] = 31, ["舞狮车加速器"] = 31, ["超级加速器"] = 31, ["驯鹿加速器"] = 31, ["黄金加速器"] = 31, ["龙加速器"] = 31, ["龟龟加速器"] = 31,
        ["黄金导弹"] = 32,
        ["追踪导弹"] = 33,
        ["冰冻水炸弹"] = 34, ["冰冻炸弹"] = 34,
        ["定时冰冻炸弹"] = 35,
        ["黄金护盾"] = 36, ["黄金盾牌"] = 36,
        ["水地雷"] = 37,
        ["恶魔阿哥"] = 38,
        ["南瓜水炸弹"] = 44,
        ["小鸡炸弹"] = 45,
        ["墨水"] = 46,
        ["兜网水球"] = 47,
        ["第一恶魔"] = 59,
        ["F加速器"] = 60,
        ["飞行导弹"] = 73,
        ["雷电水炸弹"] = 74,
        ["黄金蛋蛋弹"] = 82,
        ["蛋蛋弹"] = 83,
        ["青色球"] = 84,
        ["巨型香蕉"] = 85,
        ["红色机械导弹"] = 86,
        ["玻璃加速器"] = 87,
        ["黄色加速器"] = 98,
        ["老虎导弹"] = 99, ["黑豹导弹"] = 99,
        ["特殊幽灵"] = 100,
        ["老虎幽灵"] = 101,
        ["糖果导弹"] = 102,
        ["黄金磁铁"] = 103,
        ["电磁导弹"] = 104,
        ["电磁盾牌"] = 106,
        ["恐龙导弹"] = 107,
        ["钩爪导弹"] = 108,
        ["扫描器"] = 109, ["透视镜"] = 109,
        ["道具锁"] = 110, ["锁"] = 110,
        ["闪电"] = 111,
        ["雪精灵"] = 112, ["雪花导弹"] = 112,
        ["路障"] = 113,
        ["乌云"] = 114,
        ["黑云"] = 115,
        ["像素导弹"] = 117,
        ["冰冻水苍蝇"] = 118,
        ["毒性水苍蝇"] = 119,
        ["定时水炸弹苍蝇"] = 120,
        ["自爆定时水炸弹苍蝇"] = 121,
        ["狐尾导弹"] = 126,
        ["随机导弹"] = 127,
        ["发条炸弹"] = 129,
        ["锯齿地雷"] = 130,
        ["特快导弹"] = 131,
        ["蜜蜂"] = 132,
        ["巨无霸"] = 133,
        ["舞狮车导弹"] = 134,
        ["龙卷风"] = 135,
        ["鲨鱼导弹"] = 136,
        ["符咒"] = 137
    };

    private static readonly Dictionary<int, KartRule> Rules = new Dictionary<int, KartRule>();
    /// <summary>读取 transformByKart：各车的“变更/转换”规则（含开箱转换），覆盖文本解析结果。</summary>
    private static readonly Dictionary<(int Kart, short From), Rule> TransformMap = new();
    private static readonly Dictionary<(int Kart, short From), Rule> FiredMap = new();
    private static readonly Dictionary<(int Kart, short From), Rule> FiringMap = new();

    public static int ApplyTransformByKart(System.Xml.Linq.XDocument doc, bool isOverride = false)
    {
        if (doc == null)
        {
            return 0;
        }
        if (!isOverride)
        {
            TransformMap.Clear();
        }
        int skipped = 0;
        int ignored = 0;
        foreach (System.Xml.Linq.XElement el in doc.Descendants("item"))
        {
            if (!int.TryParse((string)el.Attribute("kartId"), out int kartId))
            {
                continue;
            }
            string srcName = (string)el.Attribute("srcIdx") ?? "";
            string dstName = (string)el.Attribute("dstIdx") ?? "";
            if (IgnoredEnglish.Contains(srcName) || IgnoredEnglish.Contains(dstName))
            {
                ignored++;
                continue;
            }
            short from = ResolveEnglish(srcName);
            short to = ResolveEnglish(dstName);
            int prob = int.TryParse((string)el.Attribute("probability"), out int pr) ? pr : 100;
            if (from <= 0 || to <= 0 || from == to || prob <= 0)
            {
                skipped++;
                continue;
            }
            TransformMap[(kartId, from)] = new Rule { From = from, To = to, Probability = prob };
        }

        // 按车重建 Convert 列表（@cn 覆盖 base 的同一 (车, 原道具)）
        var byKart = TransformMap.GroupBy(kv => kv.Key.Kart)
                                 .ToDictionary(g => g.Key, g => g.Select(kv => kv.Value).ToList());
        foreach (KeyValuePair<int, List<Rule>> kv in byKart)
        {
            if (!Rules.TryGetValue(kv.Key, out KartRule rule))
            {
                rule = new KartRule();
                Rules[kv.Key] = rule;
            }
            rule.Convert.Clear();
            rule.Convert.AddRange(kv.Value);
        }
        return byKart.Count;
    }

    /// <summary>
    /// 读取 fired2Gain：**被**指定道具击中后按概率获得另一个道具（覆盖文本解析结果）。
    /// 命名与语义经客户端数据核对：fired2Gain 的键是"被击中的道具"
    /// （例：纯白雷兽V1 firedItemIdx=magnet → gainItemIdx=thunderbolt，对应"被吸得雷10"）。
    /// </summary>
    public static int ApplyFired2Gain(System.Xml.Linq.XDocument doc, bool isOverride = false)
    {
        if (doc == null)
        {
            return 0;
        }
        if (!isOverride)
        {
            FiredMap.Clear();
        }
        foreach (System.Xml.Linq.XElement el in doc.Descendants("item"))
        {
            if (!int.TryParse((string)el.Attribute("kartId"), out int kartId))
            {
                continue;
            }
            short from = ResolveEnglish((string)el.Attribute("firedItemIdx"));
            short to = ResolveEnglish((string)el.Attribute("gainItemIdx"));
            int prob = int.TryParse((string)el.Attribute("probability"), out int pr) ? pr : 100;
            if (from <= 0 || to <= 0 || from == to || prob <= 0)
            {
                continue;
            }
            FiredMap[(kartId, from)] = new Rule { From = from, To = to, Probability = prob, FromClientTable = true };
        }
        var byKart = FiredMap.GroupBy(kv => kv.Key.Kart).ToDictionary(g => g.Key, g => g.Select(kv => kv.Value).ToList());
        foreach (KeyValuePair<int, List<Rule>> kv in byKart)
        {
            if (!Rules.TryGetValue(kv.Key, out KartRule rule))
            {
                rule = new KartRule();
                Rules[kv.Key] = rule;
            }
            rule.OnAttacked.Clear();
            rule.OnAttacked.AddRange(kv.Value);
        }
        return byKart.Count;
    }

    /// <summary>
    /// 读取 firing2Gain：**使用**（发射）指定道具后按概率获得道具（覆盖文本解析结果）。
    /// 命名与语义经客户端数据核对：firing2Gain 的键是"自己用掉的道具"
    /// （例：黯泣V1 firingItemIdx=rocket → gainItemIdx=magnet，对应"用白炸得吸33"）。
    /// </summary>
    public static int ApplyFiring2Gain(System.Xml.Linq.XDocument doc, bool isOverride = false)
    {
        if (doc == null)
        {
            return 0;
        }
        if (!isOverride)
        {
            FiringMap.Clear();
        }
        foreach (System.Xml.Linq.XElement el in doc.Descendants("item"))
        {
            if (!int.TryParse((string)el.Attribute("kartId"), out int kartId))
            {
                continue;
            }
            short from = ResolveEnglish((string)el.Attribute("firingItemIdx"));
            short to = ResolveEnglish((string)el.Attribute("gainItemIdx"));
            int prob = int.TryParse((string)el.Attribute("probability"), out int pr) ? pr : 100;
            if (from <= 0 || to <= 0 || prob <= 0)
            {
                continue;
            }
            FiringMap[(kartId, from)] = new Rule { From = from, To = to, Probability = prob, FromClientTable = true };
        }
        var byKart = FiringMap.GroupBy(kv => kv.Key.Kart).ToDictionary(g => g.Key, g => g.Select(kv => kv.Value).ToList());
        foreach (KeyValuePair<int, List<Rule>> kv in byKart)
        {
            if (!Rules.TryGetValue(kv.Key, out KartRule rule))
            {
                rule = new KartRule();
                Rules[kv.Key] = rule;
            }
            rule.OnUse.Clear();
            rule.OnUse.AddRange(kv.Value);
        }
        return byKart.Count;
    }

    /// <summary>查询指定车的“获得 from → 变成 to”规则。</summary>
    public static bool TryGetTransform(int kartId, short from, out Rule rule, out int probability)
    {
        rule = null;
        probability = 0;
        if (TransformMap.TryGetValue((kartId, from), out Rule hit))
        {
            rule = hit;
            probability = hit.Probability;
            return true;
        }
        return false;
    }

    /// <summary>供外部（限制表）把英文名解析成 id。</summary>
    public static short ResolveName(string name)
    {
        return ResolveEnglish(name);
    }

    private static short ResolveEnglish(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return 0;
        }
        return EnglishToId.TryGetValue(name.Trim(), out short id) ? id : (short)0;
    }

    /// <summary>手动指定的道具名 → id（V2KartRules.json 的 itemIds 段），优先于内置表。</summary>
    /// <summary>客户端规则表里使用的英文道具名 → id（只收录有把握的）。</summary>
    private static readonly Dictionary<string, short> EnglishToId = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase)
    {
        ["banana"] = 8, ["banana2"] = 8, ["cloud2"] = 114, ["cloud"] = 114,
        ["shield"] = 10, ["emp"] = 12, ["devil"] = 2, ["guideRocket"] = 33,
        ["ufo"] = 3, ["barricade"] = 113, ["randomRocket"] = 127, ["rocket"] = 7,
        ["waterBomb"] = 9, ["waterFly"] = 4, ["thunderbolt"] = 111, ["booster"] = 6,
        ["magnet"] = 5, ["scanning"] = 109, ["slotLock"] = 110, ["angel"] = 11,
        ["timeBomb"] = 13, ["siren"] = 24, ["goldRocket"] = 32, ["tigerRocket"] = 99,
        ["waterMine"] = 37, ["mine"] = 37, ["cokeBomb"] = 30, ["timeCokeBomb"] = 30,
        ["areaUfo"] = 26,
        ["animalBooster"] = 31, ["oil"] = 46, ["superMagnet"] = 103, ["lockdownRocket"] = 104,
        ["goldShield"] = 36, ["bigBanana"] = 85, ["candyRocket"] = 102,
        ["dinoEggRocket"] = 107, ["foxTailRocket"] = 126, ["eggMine"] = 83,
        ["goldEggMine"] = 82, ["infectedWaterFly"] = 119, ["waterbombFly"] = 120,
        ["infectedBomb"] = 28, ["darkCloud"] = 115, ["snowman"] = 112,
        ["snowBomb"] = 34, ["protectShield"] = 81, ["superShield"] = 18,
        ["tigerGhost"] = 101, ["rainbowCloud"] = 114, ["rainbowCloud2"] = 114,
        ["snowWaterFly"] = 118, ["blockRocket"] = 117, ["duckMine"] = 45,
        ["forceZone"] = 25,
        ["pumpkinBomb"] = 44, ["darkCloud2"] = 115, ["timeInfectedBomb"] = 28,
        ["timeSnowBomb"] = 35, ["honeyBee"] = 132, ["talisman"] = 137,
        ["dinoClawRocket"] = 108, ["lionMaskRocket"] = 134, ["springMine"] = 25,
        ["newDevil"] = 2, ["pantherRocket"] = 99,
        ["cokeRocket"] = 30, ["cokeRocketWorldCup"] = 30, ["prisonBomb"] = 47,
        ["goldRocketForBoss"] = 32, ["deliveryRocket"] = 131, ["abyssBarricade"] = 135,
        ["cogWheelMine"] = 130, ["rollingBomb"] = 9, ["rollingInfectedBomb"] = 27
    };

    /// <summary>客户端表里存在、但实际没有对应道具的名字（不参与统计“跳过”）。</summary>
    private static readonly HashSet<string> IgnoredEnglish = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "rollingCokeBomb"
    };

    private static int _parsedKarts;
    private static int _plainKarts;
    private static int _plainRules;
    private static bool _loaded;
    private static bool _enabled = true;

    public static bool IsLoaded => _loaded;

    public static bool Enabled => _enabled;

    public static void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled)
        {
            Log.Warn("[KartRules] 词条规则已在本次运行关闭（--no-kartrules）");
        }
    }

    private static readonly Regex StartRegex = new Regex(
        @"比赛开始时(?:可)?获得(.*?)中的(\d+)种", RegexOptions.Compiled);

    private static readonly Regex ConvertRegex = new Regex(
        @"(一定概率)?将((?:\[[^\]]+\][，,、]?\s*)+?)(?:系列道具)?(?:变更为|转换为|变为)\s*\[([^\]]+)\]", RegexOptions.Compiled);

    private static readonly Regex AcquireConvertRegex = new Regex(
        @"获得\s*((?:\[[^\]]+\][，,、]?\s*)+?)时转换为\s*\[([^\]]+)\]",
        RegexOptions.Compiled);

    private static readonly Regex OnUseRegex = new Regex(
        @"使用\s*\[([^\]]+)\]\s*时(一定概率|低概率|较低概率|高概率)?(?:获得|转换为|变为|变更为)\s*\[([^\]]+)\]", RegexOptions.Compiled);

    private static readonly Regex OnAttackedRegex = new Regex(
        @"(?:被|受到)\s*((?:\[[^\]]+\][，,、]?\s*)+?)攻击时(一定概率|低概率|较低概率|高概率|极高概率)?获得\s*\[([^\]]+)\]",
        RegexOptions.Compiled);

    /// <summary>解析 item.kml（UTF-16 文本）。启动时调用一次。</summary>
    public static void Load(byte[] kmlData)
    {
        if (kmlData == null || kmlData.Length < 2)
        {
            Log.Warn("[KartRules] item.kml 为空，跳过词条解析");
            return;
        }

        string text;
        try
        {
            text = kmlData[0] == 0xFF && kmlData[1] == 0xFE
                ? Encoding.Unicode.GetString(kmlData, 2, kmlData.Length - 2)
                : Encoding.Unicode.GetString(kmlData);
        }
        catch (Exception ex)
        {
            Log.Warn("[KartRules] item.kml 解码失败", ex);
            return;
        }

        lock (Rules)
        {
            // 不能 Clear：item.kml 位于 rho5，item/slot 的规则表位于 rho，
            // 遍历顺序上表一定先到；清空会把已加载的表规则整体冲掉。
            // （v99/v2 的"黯泣V1 使用导弹不触发、货拉拉 迅 必定获得"根因就在这一行。）

            foreach (Match m in Regex.Matches(text, @"<item\b[^>]*?/>"))
            {
                string node = m.Value;
                // 只解析卡丁车（itemCatId=3）：部件的 itemEffect 也会带方括号，混进来会污染名称表
                if (node.IndexOf("itemCatId='3'", StringComparison.Ordinal) < 0)
                {
                    continue;
                }
                Match idMatch = Regex.Match(node, @"itemId='(\d+)'");
                Match effectMatch = Regex.Match(node, @"itemEffect='([^']*)'");
                if (!idMatch.Success || !effectMatch.Success)
                {
                    continue;
                }
                string effect = effectMatch.Groups[1].Value;
                if (!int.TryParse(idMatch.Groups[1].Value, out int kartId))
                {
                    continue;
                }

                KartRule rule = effect.IndexOf('[') >= 0 ? Parse(kartId, effect) : ParsePlainText(kartId, effect);
                if (rule != null)
                {
                    if (!Rules.TryGetValue(kartId, out KartRule existing))
                    {
                        Rules[kartId] = rule;
                    }
                    else
                    {
                        // 客户端表是权威数据：表里已有的类别一律保留，只补表里没有的类别
                        if (existing.Convert.Count == 0)
                        {
                            existing.Convert.AddRange(rule.Convert);
                        }
                        if (existing.OnUse.Count == 0)
                        {
                            existing.OnUse.AddRange(rule.OnUse);
                        }
                        if (existing.OnAttacked.Count == 0)
                        {
                            existing.OnAttacked.AddRange(rule.OnAttacked);
                        }
                        foreach (short startItem in rule.StartItems)
                        {
                            if (!existing.StartItems.Contains(startItem))
                            {
                                existing.StartItems.Add(startItem);
                            }
                        }
                    }

                    if (effect.IndexOf('[') < 0)
                    {
                        _plainKarts++;
                    }
                }
            }

            _parsedKarts = Rules.Count;
            _loaded = true;
        }

        if (_plainKarts > 0)
        {
        Log.Debug($"[KartRules] 其中纯文本词条 {_plainKarts} 辆，解析出 {_plainRules} 条规则");
        }

        Log.Info($"[KartRules] 已从客户端解析 {_parsedKarts} 辆道具车词条");
    }

    /// <summary>
    /// 纯文本词条解析（老车）：没有 [] 分隔，靠关键词 + 名字最长匹配。
    /// 支持：受到X攻击时获得Y / 使用X时获得|变更为Y / 将X变更为Y / 获得X变更为Y / 发射X / 比赛开始获得X。
    /// </summary>
    private static KartRule ParsePlainText(int kartId, string effect)
    {
        var rule = new KartRule();
        bool any = false;

        string[] clauses = effect.Split(new[] { '|', '。', '；', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawClause in clauses)
        {
            string clause = rawClause.Trim();
            if (clause.Length == 0)
            {
                continue;
            }

            int probability = ExtractProbability(clause);

            // 踩到/碰到 X 时获得 Y（例：踩到香蕉皮 100% 获得加速器）
            if ((clause.Contains("踩到") || clause.Contains("碰到")) && clause.Contains("获得"))
            {
                string trigger = clause.Contains("踩到") ? "踩到" : "碰到";
                string fromPart = Between(clause, trigger, "时");
                if (fromPart.Length == 0)
                {
                    fromPart = Between(clause, trigger, "获得");
                }
                string toPart = After(clause, "获得");
                foreach (short from in ResolveNamesIn(fromPart))
                {
                    foreach (short to in ResolveNamesIn(toPart))
                    {
                        if (from != to && !HasOnAttacked(rule, from, to))
                        {
                            rule.OnAttacked.Add(new Rule { From = from, To = to, Probability = probability });
                            any = true;
                            _plainRules++;
                        }
                    }
                }
                continue;
            }
            // 受到[X]攻击时获得[Y]
            if (clause.Contains("受到") && clause.Contains("攻击") && clause.Contains("获得"))
            {
                string fromPart = Between(clause, "受到", "攻击");
                string toPart = After(clause, "获得");
                foreach (short from in ResolveNamesIn(fromPart))
                {
                    foreach (short to in ResolveNamesIn(toPart))
                    {
                        if (from != to && !HasOnAttacked(rule, from, to))
                        {
                            rule.OnAttacked.Add(new Rule { From = from, To = to, Probability = probability });
                            any = true;
                            _plainRules++;
                        }
                    }
                }
                continue;
            }

            // 使用 X 时 获得/变更为 Y
            if (clause.Contains("使用") && (clause.Contains("获得") || clause.Contains("变更为") || clause.Contains("转换为")))
            {
                string fromPart = Between(clause, "使用", "时");
                if (fromPart.Length == 0)
                {
                    fromPart = Between(clause, "使用", "获得");
                }
                string toPart = clause.Contains("变更为") ? After(clause, "变更为")
                              : clause.Contains("转换为") ? After(clause, "转换为")
                              : After(clause, "获得");
                foreach (short from in ResolveNamesIn(fromPart))
                {
                    foreach (short to in ResolveNamesIn(toPart))
                    {
                        if (from != to && !HasOnUse(rule, from, to))
                        {
                            rule.OnUse.Add(new Rule { From = from, To = to, Probability = probability });
                            any = true;
                            _plainRules++;
                        }
                    }
                }
                continue;
            }

            // 将 X 变更为 Y / 获得 X 变更为 Y
            if (clause.Contains("变更为") || clause.Contains("转换为") || clause.Contains("变为"))
            {
                string marker = clause.Contains("变更为") ? "变更为" : clause.Contains("转换成") ? "转换为" : clause.Contains("转换为") ? "转换为" : "变为";
                int idx = clause.IndexOf(marker, StringComparison.Ordinal);
                string fromPart = LeftOfLastItem(clause.Substring(0, idx));
                string toPart = After(clause, marker);
                foreach (short from in ResolveNamesIn(fromPart))
                {
                    foreach (short to in ResolveNamesIn(toPart))
                    {
                        if (from != to && rule.Convert.All(r => r.From != from || r.To != to))
                        {
                            rule.Convert.Add(new Rule { From = from, To = to, Probability = 100 });
                            any = true;
                            _plainRules++;
                        }
                    }
                }
                continue;
            }

            // 比赛开始获得 X
            if (clause.Contains("比赛开始") && clause.Contains("获得"))
            {
                foreach (short to in ResolveNamesIn(After(clause, "获得")))
                {
                    if (!rule.StartItems.Contains(to))
                    {
                        rule.StartItems.Add(to);
                        any = true;
                        _plainRules++;
                    }
                }
                continue;
            }

            // 发射 X（等于使用该道具）
            if (clause.Contains("发射"))
            {
                foreach (short to in ResolveNamesIn(After(clause, "发射")))
                {
                    if (!rule.StartItems.Contains(to))
                    {
                        rule.StartItems.Add(to);
                        any = true;
                        _plainRules++;
                    }
                }
            }
        }

        return any ? rule : null;
    }

    private static bool HasOnAttacked(KartRule rule, short from, short to)
    {
        return rule.OnAttacked.Any(r => r.From == from && r.To == to);
    }

    private static bool HasOnUse(KartRule rule, short from, short to)
    {
        return rule.OnUse.Any(r => r.From == from && r.To == to);
    }

    /// <summary>把文本里出现的道具名全部解析成 id（最长匹配，避免"水炸弹"吃掉"毒性水炸弹"）。</summary>
    private static List<short> ResolveNamesIn(string text)
    {
        var result = new List<short>();
        if (string.IsNullOrEmpty(text))
        {
            return result;
        }
        foreach (string name in NameToId.Keys.OrderByDescending(n => n.Length))
        {
            if (text.Contains(name))
            {
                short id = NameToId[name];
                if (id > 0 && !result.Contains(id))
                {
                    result.Add(id);
                }
                text = text.Replace(name, "\u0001");
            }
        }
        return result;
    }

    private static int ExtractProbability(string clause)
    {
        Match m = Regex.Match(clause, @"(\d{1,3})\s*%");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int percent))
        {
            return Math.Clamp(percent, 1, 100);
        }
        if (clause.Contains("较高概率"))
        {
            return 50;
        }
        if (clause.Contains("高概率"))
        {
            return 70;
        }
        if (clause.Contains("一定概率") || clause.Contains("有一定概率"))
        {
            return 30;
        }
        return 100;
    }

    private static string Between(string text, string start, string end)
    {
        int i = text.IndexOf(start, StringComparison.Ordinal);
        if (i < 0)
        {
            return "";
        }
        i += start.Length;
        int j = text.IndexOf(end, i, StringComparison.Ordinal);
        return j < 0 ? text.Substring(i) : text.Substring(i, j - i);
    }

    private static string After(string text, string marker)
    {
        int i = text.IndexOf(marker, StringComparison.Ordinal);
        return i < 0 ? "" : text.Substring(i + marker.Length);
    }

    /// <summary>取"变更为"之前最后一个道具名开始的片段。</summary>
    private static string LeftOfLastItem(string text)
    {
        int best = -1;
        foreach (string name in NameToId.Keys)
        {
            int idx = text.LastIndexOf(name, StringComparison.Ordinal);
            if (idx > best)
            {
                best = idx;
            }
        }
        return best < 0 ? text : text.Substring(best);
    }

    private static KartRule Parse(int kartId, string effect)
    {
        KartRule rule = new KartRule();
        bool any = false;

        // 统一分隔符，便于正则
        effect = effect.Replace("&#xA;", "|").Replace("\n", "|");
        foreach (string rawPart in effect.Split('|'))
        {
            string part = rawPart.Trim();
            if (part.Length == 0)
            {
                continue;
            }

            // 防御/免疫类由客户端判定，不参与服务端规则，也不统计其中的道具名
            if (part.Contains("防御"))
            {
                continue;
            }

            // 获得[X]时转换为[Y]：拿到手就变成另一个道具，等价于变更
            Match acquire = AcquireConvertRegex.Match(part);
            if (acquire.Success)
            {
                short acquireTo = Resolve(acquire.Groups[2].Value);
                if (acquireTo > 0)
                {
                    foreach (Match fromName in Regex.Matches(acquire.Groups[1].Value, @"\[([^\]]+)\]"))
                    {
                        foreach (short from in ResolveMany(fromName.Groups[1].Value))
                        {
                            if (from != acquireTo)
                            {
                                rule.Convert.Add(new Rule { From = from, To = acquireTo, Probability = 100 });
                                any = true;
                            }
                        }
                    }
                }
                continue;
            }

            Match convert = ConvertRegex.Match(part);
            if (convert.Success)
            {
                short to = Resolve(convert.Groups[3].Value);
                if (to > 0)
                {
                    int prob = convert.Groups[1].Success ? 100 : 100;
                    foreach (Match fromName in Regex.Matches(convert.Groups[2].Value, @"\[([^\]]+)\]"))
                    {
                        foreach (short from in ResolveMany(fromName.Groups[1].Value))
                        {
                            if (from != to)
                            {
                                rule.Convert.Add(new Rule { From = from, To = to, Probability = prob });
                                any = true;
                            }
                        }
                    }
                }
                continue;
            }

            Match onUse = OnUseRegex.Match(part);
            if (onUse.Success)
            {
                short from = Resolve(onUse.Groups[1].Value);
                short to = Resolve(onUse.Groups[3].Value);
                if (from > 0 && to > 0)
                {
                    rule.OnUse.Add(new Rule { From = from, To = to, Probability = ParseProbability(onUse.Groups[2].Value) });
                    any = true;
                }
                continue;
            }

            Match attacked = OnAttackedRegex.Match(part);
            if (attacked.Success)
            {
                short to = Resolve(attacked.Groups[3].Value);
                if (to > 0)
                {
                    int prob = ParseProbability(attacked.Groups[2].Value);
                    foreach (Match fromName in Regex.Matches(attacked.Groups[1].Value, @"\[([^\]]+)\]"))
                    {
                        foreach (short from in ResolveMany(fromName.Groups[1].Value))
                        {
                            rule.OnAttacked.Add(new Rule { From = from, To = to, Probability = prob });
                            any = true;
                        }
                    }
                }
                continue;
            }

            Match start = StartRegex.Match(part);
            if (start.Success)
            {
                foreach (Match name in Regex.Matches(start.Groups[1].Value, @"\[([^\]]+)\]"))
                {
                    short id = Resolve(name.Groups[1].Value);
                    if (id > 0 && !rule.StartItems.Contains(id))
                    {
                        rule.StartItems.Add(id);
                    }
                }
                if (rule.StartItems.Count > 0)
                {
                    any = true;
                }
            }
        }

        return any ? rule : null;
    }

    private static short Resolve(string name)
    {
        string key = name.Trim();
        if (NameToId.TryGetValue(key, out short id))
        {
            return id;
        }
        return 0;
    }

    /// <summary>把 "水苍蝇/水炸弹" 这类组合名拆成多个 id。</summary>
    private static List<short> ResolveMany(string name)
    {
        var ids = new List<short>(2);
        foreach (string piece in name.Split('/'))
        {
            short id = Resolve(piece);
            if (id > 0 && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }
        return ids;
    }

    private static int ParseProbability(string word)
    {
        switch (word)
        {
            case "一定概率": return 100;
            case "极高概率":
            case "非常高的概率": return 90;
            case "高概率": return 80;
            case "较低概率": return 30;
            case "低概率": return 20;
            default: return 100;
        }
    }

    public static bool TryGet(int kartId, out KartRule rule)
    {
        return Rules.TryGetValue(kartId, out rule);
    }

    /// <summary>随机判定：返回 true 表示本次应该按词条生效。</summary>
    private static bool Roll(int probability)
    {
        return probability >= 100 || Random.Shared.Next(100) < probability;
    }

    public static bool TryConvert(int kartId, short itemId, out short target)
    {
        target = itemId;
        if (!Rules.TryGetValue(kartId, out KartRule rule))
        {
            return false;
        }
        foreach (Rule r in rule.Convert)
        {
            if (r.From == itemId && Roll(r.Probability))
            {
                target = r.To;
                return true;
            }
        }
        return false;
    }

    public static bool TryOnUse(int kartId, short itemId, out short target)
    {
        target = itemId;
        if (!Rules.TryGetValue(kartId, out KartRule rule))
        {
            return false;
        }
        foreach (Rule r in rule.OnUse)
        {
            if (r.From == itemId && Roll(r.Probability))
            {
                target = r.To;
                return true;
            }
        }
        return false;
    }

    public static bool TryOnAttacked(int kartId, short itemId, out short target)
    {
        target = itemId;
        if (!Rules.TryGetValue(kartId, out KartRule rule))
        {
            return false;
        }
        foreach (Rule r in rule.OnAttacked)
        {
            if (r.From == itemId && Roll(r.Probability))
            {
                target = r.To;
                return true;
            }
        }
        return false;
    }

    private static void MergeFromJson(List<Rule> target, List<Rule> source)
    {
        foreach (Rule r in source)
        {
            Rule exist = target.FirstOrDefault(x => x.From == r.From && x.To == r.To);
            if (exist == null)
            {
                target.Add(r);
            }
            else if (!exist.FromClientTable)
            {
                // 文本来源的条目允许用 JSON 改概率；表来源的不动
                exist.Probability = r.Probability;
            }
        }
    }

    /// <summary>启动日志用：统计各类规则数量。</summary>
    public static void LogSummary()
    {
        int convert = 0, onUse = 0, onAttacked = 0, start = 0;
        foreach (KartRule rule in Rules.Values)
        {
            convert += rule.Convert.Count;
            onUse += rule.OnUse.Count;
            onAttacked += rule.OnAttacked.Count;
            if (rule.StartItems.Count > 0)
            {
                start++;
            }
        }
        Log.Debug($"[KartRules] 变更 {convert} 条 / 使用后 {onUse} 条 / 被攻击 {onAttacked} 条 / 开局 {start} 辆");
    }
}
