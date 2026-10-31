using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ExcData;
using KartRider.Logging;

namespace KartRider;

/// <summary>12 代车（kart12）改装效果的类型。</summary>
public enum TuneEffectKind
{
    /// <summary>防御 / 逃脱：客户端自己判定，服务端不参与。</summary>
    ClientOnly = 0,
    /// <summary>获得 X → 换成 Y（开箱转换）。对应 GameSlotPacket 的 type==1。</summary>
    Gain = 1,
    /// <summary>使用 X → 获得 Y（额外发放）。对应 type==10。</summary>
    FiringGain = 2,
    /// <summary>被 X 击中 → 获得 Y（额外发放）。对应 type==11。</summary>
    FiredGain = 3
}

public sealed class TuneEffect
{
    public int GroupId;
    public int Level;
    public TuneEffectKind Kind;
    /// <summary>触发用的源道具 id（来自 TuneGroup 的 transform / firing / fired 属性）。</summary>
    public short[] From = Array.Empty<short>();
    public short To;
    public int Prob;
    public string GameType = string.Empty;
}

/// <summary>
/// 12 代车改装（kart12 tuning）效果表。
///
/// 数据全部来自客户端，不在代码里写任何概率：
///   zeta_/{region}/engine/kart12TuningData.xml —— Skill idx(1~21) → tuneGroupId(411~534)
///   zeta_/{region}/enchant/enchant.xml        —— TuneGroup → 每个等级(1~5)的效果 + prob
///
/// 玩家自己选了哪几个技能、各投了几点，在 Profile/&lt;昵称&gt;/Level12Data.json 的
/// Level12.Skill1..3 / SkillGrade1..3（也就是服务端已经存好的那份数据）。
/// </summary>
public static class KartTuneTable
{
    /// <summary>Skill idx(1~21) → tuneGroupId。</summary>
    private static readonly Dictionary<int, int> SkillToGroup = new();

    /// <summary>tuneGroupId → 下标 1~5 的效果（下标 0 不用）。</summary>
    private static readonly Dictionary<int, TuneEffect[]> GroupLevels = new();

    public static bool Loaded => GroupLevels.Count > 0;

    /// <summary>读取 kart12TuningData.xml：建立 Skill idx → tuneGroupId 的映射。</summary>
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

    /// <summary>读取 enchant.xml：解析道具向（5xx）改装组的每级效果与概率。</summary>
    public static int LoadEnchant(XDocument doc)
    {
        if (doc == null) return 0;
        int groups = 0, effects = 0;
        foreach (XElement group in doc.Descendants("TuneGroup"))
        {
            if (!int.TryParse((string)group.Attribute("id"), out int gid)) continue;
            if (gid < 500 || gid > 599) continue;   // 5xx = 道具向；4xx 是速度向

            // 源道具列表优先取组属性（transform='6;31' / firing='5' / fired='3'），这是客户端自己的写法
            short[] from = ParseIdList(group.Attribute("transform"))
                           ?? ParseIdList(group.Attribute("firing"))
                           ?? ParseIdList(group.Attribute("fired"));

            var levels = new TuneEffect[6];
            foreach (XElement tune in group.Elements("Tune"))
            {
                if (!int.TryParse((string)tune.Attribute("id"), out int level)) continue;
                if (level < 1 || level > 5) continue;
                XElement tag = tune.Elements().FirstOrDefault();
                if (tag == null) continue;

                TuneEffectKind kind;
                switch (tag.Name.LocalName)
                {
                    case "EnchanterGain": kind = TuneEffectKind.Gain; break;
                    case "EnchanterFiringGain": kind = TuneEffectKind.FiringGain; break;
                    case "EnchanterFiredGain": kind = TuneEffectKind.FiredGain; break;
                    default: kind = TuneEffectKind.ClientOnly; break;   // Shield / WaterEscape …
                }

                short to = ClientItemRules.ResolveName((string)tag.Attribute("gainItemId"));
                short[] sources = from;
                if (sources == null || sources.Length == 0)
                {
                    short one = ClientItemRules.ResolveName((string)tag.Attribute("itemId"));
                    sources = one > 0 ? new[] { one } : Array.Empty<short>();
                }
                // 521「用磁铁获得加速器」：客户端 firing 属性只写了主源 5（磁铁），
                // 但技能描述是"使用[磁铁]，[黄金磁铁]时"，实测客户端使用黄金磁铁上报的是 103，
                // 这里按描述补上，否则黄金磁铁永远不触发。
                if (gid == 521)
                {
                    sources = MergeIds(sources, 103);
                }
                int prob = int.TryParse((string)tag.Attribute("prob"), out int pr) ? pr : 0;

                levels[level] = new TuneEffect
                {
                    GroupId = gid,
                    Level = level,
                    Kind = kind,
                    From = sources,
                    To = to,
                    Prob = prob,
                    GameType = (string)tag.Attribute("gameType") ?? string.Empty
                };
                effects++;
            }
            GroupLevels[gid] = levels;
            groups++;
        }
        Log.Debug($"[Tune] enchant.xml: 道具向改装组 {groups} 个，效果 {effects} 条");
        return groups;
    }

    /// <summary>启动时打一行"表已就绪"，不逐组刷屏（正式版）。</summary>
    public static void LogSummary()
    {
        if (!Loaded)
        {
            Log.Warn("[Tune] 改装表为空（没有读到 kart12TuningData/enchant），12 代车改装不会生效");
            return;
        }
        Log.Info($"[Tune] 改装表就绪：12 代车改装组 {GroupLevels.Count} 个，技能映射 {SkillToGroup.Count} 条");
    }

    /// <summary>技能 idx + 投入点数 → 该等级的效果。</summary>
    public static bool TryGetEffect(int skillIdx, int grade, out TuneEffect effect)
    {
        effect = null;
        if (skillIdx <= 0 || grade <= 0) return false;
        if (!SkillToGroup.TryGetValue(skillIdx, out int group)) return false;
        if (!GroupLevels.TryGetValue(group, out TuneEffect[] levels)) return false;
        int level = Math.Clamp(grade, 1, 5);
        effect = levels[level];
        return effect != null;
    }

    /// <summary>取玩家当前装备车辆的改装数据（Level12）。</summary>
    public static Level12 GetPlayerTune(string nickname, ushort kartId, ushort kartSn)
    {
        if (string.IsNullOrEmpty(nickname) || kartId == 0) return null;
        return KartExcData.TryGetLevel12(nickname, (short)kartId, (short)kartSn, out Level12 tune) ? tune : null;
    }

    /// <summary>三个槽里是否有任一槽对 item 命中，并按表里的 prob 掷一次概率。</summary>
    public static bool TryRoll(Level12 tune, short item, TuneEffectKind kind, out TuneEffect hit)
    {
        hit = null;
        if (tune == null || item <= 0) return false;
        for (int slot = 1; slot <= 3; slot++)
        {
            short skill = slot == 1 ? tune.Skill1 : slot == 2 ? tune.Skill2 : tune.Skill3;
            short grade = slot == 1 ? tune.SkillGrade1 : slot == 2 ? tune.SkillGrade2 : tune.SkillGrade3;
            if (skill <= 0 || grade <= 0) continue;
            if (!TryGetEffect(skill, grade, out TuneEffect eff) || eff == null) continue;
            if (eff.Kind != kind || eff.Prob <= 0 || eff.To <= 0) continue;
            if (!eff.From.Contains(item)) continue;
            // gameType='withoutBoss' / 'withoutBossFlag'：Boss 模式外的普通对局，当前实现只在普通对局生效
            if (Random.Shared.Next(100) >= eff.Prob) continue;
            hit = eff;
            return true;
        }
        return false;
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

    /// <summary>把 extra 合并进 ids（去重，保持顺序）。</summary>
    private static short[] MergeIds(short[] ids, params short[] extra)
    {
        var list = new List<short>(ids ?? Array.Empty<short>());
        foreach (short id in extra)
        {
            if (id > 0 && !list.Contains(id))
            {
                list.Add(id);
            }
        }
        return list.ToArray();
    }
}
