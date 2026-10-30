using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using KartRider.Logging;

namespace KartRider;

/// <summary>名次档位（对应规则图上的 1位 / 上位 / 中位 / 下位）。</summary>
public enum RankGroup
{
    Top = 0,
    High = 1,
    Mid = 2,
    Low = 3
}

/// <summary>
/// 道具赛获取规则（按官方规则图重建，替代原先"按里程差 300/500 米分档"的旧实现）。
///
/// 数据来源：两张规则图
///   - 个人赛：香蕉 25 / 乌云 20 / 盾牌 40·25 / 电磁波 15 / 大魔王 2·2·2 / 追踪导弹 5·3 /
///             飞碟 5·6 / 路障 5·5 / 导弹 23·20 / 水炸弹 20·11 / 水苍蝇 25·10 /
///             闪电 3·1 / 加速器 24·51 / 磁铁 5·15·32
///   - 组队赛：香蕉皮 25·2 / 云 20 / 盾牌 35·20 / 电磁波 10·5 / 大魔王 2·2·2 /
///             追踪导弹 3·4 / 飞碟 4·5 / 路障 5·5 / 随机导弹 15·5 / 导弹 10·10 /
///             水炸弹 15·10 / 水苍蝇 25·15 / 闪电 3·1 / 加速器 20·60 / 磁铁 5·8·20 /
///             扫描器 10·1 / 锁 1·2·2 / 天使 1·3·1 / 定时炸弹 3·5
/// 每列合计 1000（千分比）。变更卡与道具箱使用同一张表。
///
/// 另外：每局每个玩家通过任何方式获得的「天使 / 道具锁 / 闪电」合计最多 2 个。
/// </summary>
public static class ItemRule
{
    public const short Angel = 11;
    public const short SlotLock = 110;
    public const short Thunderbolt = 111;
    public const short Booster = 6;

    /// <summary>三种稀有道具每局每人的上限。</summary>
    public const int RareCapPerGame = 2;

    // 千分比：[Top, High, Mid, Low]
    private static Dictionary<short, int[]> PersonalTable = new Dictionary<short, int[]>
    {
        [8] = new[] { 250, 0, 0, 0 },        // 香蕉
        [114] = new[] { 200, 0, 0, 0 },      // 乌云
        [10] = new[] { 400, 250, 0, 0 },     // 盾牌
        [12] = new[] { 150, 0, 0, 0 },       // 电磁波
        [2] = new[] { 0, 20, 20, 20 },       // 大魔王
        [33] = new[] { 0, 0, 50, 30 },       // 追踪导弹
        [3] = new[] { 0, 0, 50, 60 },        // 飞碟
        [113] = new[] { 0, 0, 50, 50 },      // 路障
        [7] = new[] { 0, 230, 200, 0 },      // 导弹
        [9] = new[] { 0, 200, 110, 0 },      // 水炸弹
        [4] = new[] { 0, 250, 100, 0 },      // 水苍蝇
        [111] = new[] { 0, 0, 30, 10 },      // 闪电
        [6] = new[] { 0, 0, 240, 510 },      // 加速器
        [5] = new[] { 0, 50, 150, 320 },     // 磁铁
    };

    private static Dictionary<short, int[]> TeamTable = new Dictionary<short, int[]>
    {
        [8] = new[] { 250, 20, 0, 0 },       // 香蕉皮
        [114] = new[] { 200, 0, 0, 0 },      // 云
        [10] = new[] { 350, 200, 0, 0 },     // 盾牌
        [12] = new[] { 100, 0, 50, 0 },      // 电磁波（解碟）
        [2] = new[] { 0, 20, 20, 20 },       // 大魔王
        [33] = new[] { 0, 0, 30, 40 },       // 追踪导弹
        [3] = new[] { 0, 0, 40, 50 },        // 飞碟
        [113] = new[] { 0, 0, 50, 50 },      // 路障
        [127] = new[] { 0, 150, 50, 0 },     // 随机导弹
        [7] = new[] { 0, 100, 100, 0 },      // 导弹
        [9] = new[] { 0, 150, 100, 0 },      // 水炸弹
        [4] = new[] { 0, 250, 150, 0 },      // 水苍蝇
        [111] = new[] { 0, 0, 30, 10 },      // 闪电
        [6] = new[] { 0, 0, 200, 600 },      // 加速器
        [5] = new[] { 0, 50, 80, 200 },      // 磁铁
        [109] = new[] { 100, 10, 0, 0 },     // 扫描器
        [110] = new[] { 0, 10, 20, 20 },     // 锁
        [11] = new[] { 0, 10, 30, 10 },      // 天使
        [13] = new[] { 0, 30, 50, 0 },       // 定时炸弹
    };

    // 房间人数 → 各名次档位（索引 0 = 第 1 名）
    private static readonly RankGroup[][] RankByCount =
    {
        new[] { RankGroup.Top, RankGroup.Mid },                                                                          // 2 人
        new[] { RankGroup.Top, RankGroup.Mid, RankGroup.Low },                                                           // 3 人
        new[] { RankGroup.Top, RankGroup.Mid, RankGroup.Mid, RankGroup.Low },                                            // 4 人
        new[] { RankGroup.Top, RankGroup.High, RankGroup.Mid, RankGroup.Mid, RankGroup.Low },                            // 5 人
        new[] { RankGroup.Top, RankGroup.High, RankGroup.Mid, RankGroup.Mid, RankGroup.Low, RankGroup.Low },             // 6 人
        new[] { RankGroup.Top, RankGroup.High, RankGroup.High, RankGroup.Mid, RankGroup.Mid, RankGroup.Low, RankGroup.Low },  // 7 人
        new[] { RankGroup.Top, RankGroup.High, RankGroup.High, RankGroup.Mid, RankGroup.Mid, RankGroup.Mid, RankGroup.Low, RankGroup.Low }, // 8 人
    };

    /// <summary>
    /// 用客户端 itemProb_*.bml 覆盖概率表，并按列归一化到 1000‰。
    /// 客户端组队表各列合计是 110（旧权重），不归一化会让抽取比例失真。
    /// </summary>
    public static bool ApplyFromClient(byte gameType, XDocument doc)
    {
        if (doc == null)
        {
            return false;
        }

        var parsed = new Dictionary<short, int[]>();
        int[] sums = new int[4];
        foreach (XElement el in doc.Descendants("item"))
        {
            if (!short.TryParse((string)el.Attribute("idx"), out short id))
            {
                continue;
            }
            var weight = new int[4];
            weight[0] = ParseWeight(el, "toprank");
            weight[1] = ParseWeight(el, "highrank");
            weight[2] = ParseWeight(el, "midrank");
            weight[3] = ParseWeight(el, "lowrank");
            if (weight.Sum() <= 0)
            {
                continue;
            }
            for (int i = 0; i < 4; i++)
            {
                sums[i] += weight[i];
            }
            parsed[id] = weight;
        }
        if (parsed.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<short, int[]> kv in parsed)
        {
            for (int i = 0; i < 4; i++)
            {
                kv.Value[i] = sums[i] > 0 ? (int)Math.Round(kv.Value[i] * 1000.0 / sums[i]) : 0;
            }
        }

        if (gameType == 4)
        {
            // 组队改用客户端 itemProb_team2@cn（列合计 1000‰，与规则图一致）
            TeamTable = parsed;
            TeamFromClient = true;
        }
        else
        {
            PersonalTable = parsed;
            PersonalFromClient = true;
        }
        return true;
    }

    /// <summary>概率表来源：true=客户端 BML，false=内置规则图。</summary>
    public static bool PersonalFromClient { get; private set; }
    public static bool TeamFromClient { get; private set; }

    private static int ParseWeight(XElement el, string attribute)
    {
        return int.TryParse((string)el.Attribute(attribute), out int value) ? value : 0;
    }

    /// <summary>人数 + 名次 → 档位。</summary>
    public static RankGroup Group(int playerCount, int rank)
    {
        int count = Math.Clamp(playerCount, 2, 8);
        int r = Math.Clamp(rank, 1, count);
        return RankByCount[count - 2][r - 1];
    }

    public static IReadOnlyDictionary<short, int[]> Table(byte gameType)
    {
        return gameType == 4 ? TeamTable : PersonalTable;
    }

    /// <summary>每局每人限量的道具（来自 itemGameRestrictionItemCount.xml 的 targetItemList）。</summary>
    private static readonly Dictionary<short, int> AllowLimits = new();

    /// <summary>读取客户端 itemGameRestrictionItemCount.xml。</summary>
    public static void LoadRestrictions(System.Xml.Linq.XDocument doc, Func<string, short> nameToId)
    {
        if (doc == null)
        {
            return;
        }
        AllowLimits.Clear();
        foreach (System.Xml.Linq.XElement el in doc.Descendants("item"))
        {
            short id = nameToId((string)el.Attribute("name") ?? "");
            if (id <= 0)
            {
                continue;
            }
            // 只处理 targetItemList（allowCount），忽略 disableItemList
            if (el.Attribute("allowCount") == null)
            {
                continue;
            }
            int limit = int.TryParse((string)el.Attribute("allowCount"), out int n) ? n : 2;
            AllowLimits[id] = limit;
        }
        Log.Debug($"[ItemRule] 限制表：限量 {AllowLimits.Count} 种（仅 targetItemList）");
    }

    /// <summary>该道具的每局上限；未配置时返回 0（表示不限）。</summary>
    public static int AllowLimit(short itemId)
    {
        return AllowLimits.TryGetValue(itemId, out int limit) ? limit : 0;
    }

    public static bool IsRare(short itemId)
    {
        return itemId == Angel || itemId == SlotLock || itemId == Thunderbolt;
    }

    /// <summary>
    /// 按档位加权抽一个道具。
    /// </summary>
    /// <param name="gameType">2=个人，4=组队</param>
    /// <param name="excludeRare">已达上限时排除「天使/锁/闪电」</param>
    public static short Roll(byte gameType, int playerCount, int rank, bool excludeRare)
    {
        return RollGroup(gameType, Group(playerCount, rank), excludeRare);
    }

    /// <summary>按指定档位加权抽一个道具。</summary>
    public static short RollGroup(byte gameType, RankGroup group, bool excludeRare)
    {
        IReadOnlyDictionary<short, int[]> table = Table(gameType);
        int g = (int)group;

        int total = 0;
        foreach (KeyValuePair<short, int[]> kv in table)
        {
            if (excludeRare && IsRare(kv.Key))
            {
                continue;
            }
            total += kv.Value[g];
        }
        if (total <= 0)
        {
            return Booster;
        }

        int pick = Random.Shared.Next(total);
        foreach (KeyValuePair<short, int[]> kv in table)
        {
            if (excludeRare && IsRare(kv.Key))
            {
                continue;
            }
            pick -= kv.Value[g];
            if (pick < 0)
            {
                return kv.Key;
            }
        }
        return Booster;
    }

    /// <summary>
    /// 计算名次（1 起）。
    ///
    /// 以客户端上报的名次为准：它包含 AI、与画面一致。
    /// 服务端的 room.Tracks 只在"吃到道具箱"时采样位置，累计里程是稀疏跳变，
    /// 既无法覆盖 AI，也不能真实反映名次（v15 用它排序导致"永远 1位"）。
    /// 若将来有稳定位置流（每帧上报），可再启用按里程排序。
    /// </summary>
    public static int ComputeRank(GameRoom room, int playerId, short clientRank, int playerCount)
    {
        int count = Math.Max(2, playerCount);
        return Math.Clamp(clientRank + 1, 1, count);
    }
}
