using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ExcData;
using KartRider.Logging;

namespace KartRider;

/// <summary>改装效果的类型。</summary>
public enum TuneEffectKind
{
    /// <summary>防御 / 逃脱 / 双导弹 / 金币：客户端或其它系统处理，服务端不参与。</summary>
    ClientOnly = 0,
    /// <summary>获得 X → 换成 Y（开箱转换）。对应 GameSlotPacket 的 type==1。</summary>
    Gain = 1,
    /// <summary>使用 X → 获得 Y（额外发放）。对应 type==10。</summary>
    FiringGain = 2,
    /// <summary>被 X 击中 → 获得 Y（额外发放）。对应 type==11。</summary>
    FiredGain = 3
}

/// <summary>一条「源道具 → 目标道具」映射。一个词条可能有多条（如毒性水炸弹要转 3 种炸弹）。</summary>
public struct TunePair
{
    public short From;
    public short To;

    public TunePair(short from, short to)
    {
        From = from;
        To = to;
    }
}

public sealed class TuneEffect
{
    public int GroupId;
    /// <summary>12 代车 = 投入点数(1~5)；老 enchant = Tune id(1~3)。</summary>
    public int Level;
    public TuneEffectKind Kind;
    public int Prob;
    public string GameType = string.Empty;
    /// <summary>
    /// true = 只在「开箱获得」这一条通道生效。
    /// 老 enchant（1xx）的描述都是"在道具箱中获得X时…"，所以只认吃箱子；
    /// 12 代车（5xx）不区分获得途径，这里是 false。
    /// </summary>
    public bool BoxOnly;
    public List<TunePair> Pairs = new();

    /// <summary>源道具里是否包含 item，命中则给出目标道具。</summary>
    public bool TryGetTarget(short item, out short target)
    {
        target = 0;
        foreach (TunePair pair in Pairs)
        {
            if (pair.From == item && pair.To > 0)
            {
                target = pair.To;
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// 改装效果表（两套系统共用一张表）：
///
///   12 代车 kart12 tuning ：zeta_/{region}/engine/kart12TuningData.xml（技能 idx → 组 5xx）
///                           zeta_/{region}/enchant/enchant.xml（组 → 每级效果 + prob）
///                           玩家数据：Profile/&lt;昵称&gt;/Level12Data.json（Skill1..3 + SkillGrade1..3）
///
///   老 enchant（魔方）    ：组 1xx，编码 Tune 值 = 组 × 100 + Tune id
///                           玩家数据：Profile/&lt;昵称&gt;/TuneData.json（Tune1..3）
///
/// 概率全部来自客户端表，代码里不写任何数值。
/// </summary>
public static class KartTuneTable
{
    /// <summary>Skill idx(1~21) → tuneGroupId。</summary>
    private static readonly Dictionary<int, int> SkillToGroup = new();

    /// <summary>tuneGroupId → 下标（12 代车 1~5 / 老 enchant 1~3）的效果。</summary>
    private static readonly Dictionary<int, TuneEffect[]> GroupLevels = new();

    public static bool Loaded => GroupLevels.Count > 0;

    /// <summary>读取 kart12TuningData.xml：建立 Skill idx → tuneGroupId 的映射（只对 12 代车有用）。</summary>
    public static int LoadSkillMap(XDocument doc)
    {
        if (doc == null) return 0;
        int count = 0;
        foreach (XElement el in doc.Descendants("Skill"))
        {
            if (!int.TryParse((string)el.Attribute("idx"), out int idx)) continue;
            if (!int.TryParse((string)el.Attribute("tuneGroupId"), out int group)) continue;
            if (idx <= 0 || group <= 0) continue;
            SkillToGroup[idx] = group;
            count++;
        }
        Log.Debug($"[Tune] kart12TuningData: 技能映射 {SkillToGroup.Count} 条");
        return count;
    }

    /// <summary>读取 enchant.xml：解析 1xx（老 enchant）与 5xx（12 代车）的每级效果与概率。</summary>
    public static int LoadEnchant(XDocument doc)
    {
        if (doc == null) return 0;
        int groups = 0, effects = 0, skippedMode = 0;
        foreach (XElement group in doc.Descendants("TuneGroup"))
        {
            if (!int.TryParse((string)group.Attribute("id"), out int gid)) continue;
            bool isOld = gid >= 100 && gid <= 199;    // 老 enchant（魔方 / 粒子激活器）
            bool isKart12 = gid >= 500 && gid <= 599; // 12 代车
            if (!isOld && !isKart12) continue;

            var levels = new TuneEffect[10];
            foreach (XElement tune in group.Elements("Tune"))
            {
                if (!int.TryParse((string)tune.Attribute("id"), out int level)) continue;
                if (level < 1 || level > 9) continue;

                XElement[] tags = tune.Elements().ToArray();
                if (tags.Length == 0) continue;

                TuneEffectKind kind = ParseKind(tags[0].Name.LocalName);
                int prob = int.TryParse((string)tags[0].Attribute("prob"), out int p) ? p : 0;
                string gameType = (string)tags[0].Attribute("gameType") ?? string.Empty;

                // BOSS 战 / AI 挑战赛等特殊模式：本版不做，留待以后（表里 gameType 已经区分得很清楚）
                if (IsOtherMode(gameType))
                {
                    skippedMode++;
                    continue;
                }

                var effect = new TuneEffect
                {
                    GroupId = gid,
                    Level = level,
                    Kind = kind,
                    Prob = prob,
                    GameType = gameType,
                    Pairs = BuildPairs(kind, group, tags),
                    // 老 enchant 的「获得即转换」按客户端描述只认道具箱；12 代车不区分途径
                    BoxOnly = isOld && kind == TuneEffectKind.Gain
                };
                // 521（12 代车）/ 108（老 enchant）都是「用磁铁获得加速器」：
                // 客户端 firing 只写了主源 5（磁铁），但技能描述是"使用[磁铁]，[黄金磁铁]时"，
                // 实测客户端使用黄金磁铁上报的是 103，这里补上。
                if ((gid == 521 || gid == 108) && effect.Kind == TuneEffectKind.FiringGain)
                {
                    foreach (TunePair pair in effect.Pairs.ToArray())
                    {
                        AddPair(effect.Pairs, 103, pair.To);
                    }
                }
                levels[level] = effect;
                effects++;
            }
            GroupLevels[gid] = levels;
            groups++;
        }
        Log.Debug($"[Tune] enchant.xml: 改装组 {groups} 个，效果 {effects} 条，跳过其他模式 {skippedMode} 条");
        return groups;
    }

    /// <summary>两个数据源都加载完后打一行，不逐组刷屏。</summary>
    public static void LogSummary()
    {
        if (!Loaded)
        {
            Log.Warn("[Tune] 改装表为空（没有读到 enchant），改装词条不会生效");
            return;
        }
        int oldCount = GroupLevels.Keys.Count(g => g >= 100 && g <= 199);
        int newCount = GroupLevels.Keys.Count(g => g >= 500 && g <= 599);
        Log.Info($"[Tune] 改装表就绪：老 enchant {oldCount} 组 + 12 代车 {newCount} 组，技能映射 {SkillToGroup.Count} 条");
    }

    /// <summary>12 代车：技能 idx + 投入点数 → 该等级的效果。</summary>
    public static bool TryGetEffect(int skillIdx, int grade, out TuneEffect effect)
    {
        effect = null;
        if (skillIdx <= 0 || grade <= 0) return false;
        if (!SkillToGroup.TryGetValue(skillIdx, out int group)) return false;
        return TryGetLevel(group, Math.Clamp(grade, 1, 5), out effect);
    }

    /// <summary>老 enchant：Tune 值（组×100 + Tune id）→ 该词条的效果。</summary>
    public static bool TryGetOldEffect(int tuneValue, out TuneEffect effect)
    {
        effect = null;
        if (tuneValue <= 0) return false;
        int group = tuneValue / 100;   // 10803 → 108
        int id = tuneValue % 100;      // 10803 → 3
        if (group < 100 || group > 199 || id <= 0) return false;
        return TryGetLevel(group, id, out effect);
    }

    private static bool TryGetLevel(int group, int level, out TuneEffect effect)
    {
        effect = null;
        if (!GroupLevels.TryGetValue(group, out TuneEffect[] levels)) return false;
        if (level < 0 || level >= levels.Length) return false;
        effect = levels[level];
        return effect != null;
    }

    /// <summary>
    /// 取玩家当前装备车辆的全部改装效果（12 代车三槽 + 老 enchant 三条词条），
    /// 供开箱 / 使用 / 被击三条通道统一使用。
    /// </summary>
    public static List<TuneEffect> GetPlayerEffects(string nickname, ushort kartId, ushort kartSn)
    {
        var list = new List<TuneEffect>(6);
        if (string.IsNullOrEmpty(nickname) || kartId == 0) return list;

        // 12 代车（kart12）
        if (KartExcData.TryGetLevel12(nickname, (short)kartId, (short)kartSn, out Level12 level12) && level12 != null)
        {
            AddSlot(list, level12.Skill1, level12.SkillGrade1);
            AddSlot(list, level12.Skill2, level12.SkillGrade2);
            AddSlot(list, level12.Skill3, level12.SkillGrade3);
        }

        // 老 enchant（魔方 / 粒子激活器）
        if (KartExcData.TryGetTune(nickname, (short)kartId, (short)kartSn, out Tune tune) && tune != null)
        {
            AddOld(list, tune.Tune1);
            AddOld(list, tune.Tune2);
            AddOld(list, tune.Tune3);
        }
        return list;

        void AddSlot(List<TuneEffect> target, short skill, short grade)
        {
            if (TryGetEffect(skill, grade, out TuneEffect effect) && effect != null) target.Add(effect);
        }

        void AddOld(List<TuneEffect> target, short value)
        {
            if (TryGetOldEffect(value, out TuneEffect effect) && effect != null) target.Add(effect);
        }
    }

    /// <summary>
    /// 在玩家的所有改装效果里查：拿到 item 时是否命中某个 kind 的效果（按表里的 prob 掷一次）。
    /// <paramref name="fromBox"/> 表示这次是「吃道具箱」获得的；
    /// 老 enchant 的转换词条（BoxOnly）只在 fromBox 时才允许触发。
    /// </summary>
    public static bool TryRoll(List<TuneEffect> effects, short item, TuneEffectKind kind, bool fromBox, out TuneEffect hit, out short target)
    {
        hit = null;
        target = 0;
        if (effects == null || effects.Count == 0 || item <= 0) return false;
        foreach (TuneEffect effect in effects)
        {
            if (effect == null || effect.Kind != kind || effect.Prob <= 0) continue;
            if (effect.BoxOnly && !fromBox) continue;
            if (!effect.TryGetTarget(item, out short to) || to <= 0) continue;
            if (Random.Shared.Next(100) >= effect.Prob) continue;
            hit = effect;
            target = to;
            return true;
        }
        return false;
    }

    private static TuneEffectKind ParseKind(string tagName)
    {
        switch (tagName)
        {
            case "EnchanterGain": return TuneEffectKind.Gain;
            case "EnchanterFiringGain": return TuneEffectKind.FiringGain;
            case "EnchanterFiredGain": return TuneEffectKind.FiredGain;
            default: return TuneEffectKind.ClientOnly;   // Shield / WaterEscape / TwoRocket / GetLucci …
        }
    }

    /// <summary>
    /// BOSS 战 / AI 挑战赛等特殊模式：本版不做，留待以后。
    /// 注意只认 "BossGame" / "aiGame" 这两个值 ——
    /// "withoutBoss" / "withoutBossFlag" 是"BOSS 战之外都生效"，属于普通对局，必须保留。
    /// </summary>
    private static bool IsOtherMode(string gameType)
    {
        if (string.IsNullOrEmpty(gameType)) return false;
        return gameType.Equals("BossGame", StringComparison.OrdinalIgnoreCase)
               || gameType.Equals("aiGame", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 组装「源 → 目标」映射：
    ///   源列表优先取组属性（Gain 用 transform、FiringGain 用 firing、FiredGain 用 fired）——
    ///   那正是客户端自己标的"这个改装对哪些道具生效"（如 6;31 = 加速器 + 黄金加速器）。
    ///   一个词条多个目标时（毒性/冰冻水炸弹），按位置与 transform 列表一一对应。
    /// </summary>
    private static List<TunePair> BuildPairs(TuneEffectKind kind, XElement group, XElement[] tags)
    {
        var pairs = new List<TunePair>();
        if (kind == TuneEffectKind.ClientOnly) return pairs;

        var tagPairs = new List<TunePair>();
        foreach (XElement tag in tags)
        {
            short from = ClientItemRules.ResolveName((string)tag.Attribute("itemId"));
            short to = ClientItemRules.ResolveName((string)tag.Attribute("gainItemId"));
            if (to > 0)
            {
                tagPairs.Add(new TunePair(from, to));
            }
        }
        if (tagPairs.Count == 0) return pairs;

        string attrName = kind == TuneEffectKind.FiringGain ? "firing"
                        : kind == TuneEffectKind.FiredGain ? "fired"
                        : "transform";
        short[] sources = ParseIdList(group.Attribute(attrName));

        if (tagPairs.Count == 1)
        {
            short to = tagPairs[0].To;
            bool any = false;
            if (sources != null)
            {
                foreach (short src in sources)
                {
                    AddPair(pairs, src, to);
                    any = true;
                }
            }
            if (!any) AddPair(pairs, tagPairs[0].From, to);
            return pairs;
        }

        if (sources != null && sources.Length == tagPairs.Count)
        {
            for (int i = 0; i < tagPairs.Count; i++)
            {
                AddPair(pairs, sources[i], tagPairs[i].To);
            }
            return pairs;
        }

        foreach (TunePair pair in tagPairs)
        {
            AddPair(pairs, pair.From, pair.To);
        }
        return pairs;
    }

    private static void AddPair(List<TunePair> pairs, short from, short to)
    {
        if (from <= 0 || to <= 0) return;
        if (pairs.Any(p => p.From == from && p.To == to)) return;
        pairs.Add(new TunePair(from, to));
    }

    private static short[] ParseIdList(XAttribute attribute)
    {
        if (attribute == null) return null;
        string text = attribute.Value;
        if (string.IsNullOrWhiteSpace(text)) return null;
        var list = new List<short>();
        foreach (string part in text.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (short.TryParse(part.Trim(), out short id) && id > 0 && !list.Contains(id))
            {
                list.Add(id);
            }
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

}
