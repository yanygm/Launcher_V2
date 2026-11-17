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

    // ===== 迷你道具赛（组队地板赛，wire mode 51）=====
    // 客户端这一模式只实现了 5 种道具：香蕉/力场/超级护盾/天使/路障。
    // 表为千分比（原表列和 100，×10）：
    //          1位   上位   中位   下位
    //   香蕉    900   700    200     0
    //   力场    100   100      0     0
    //   超级护盾  0   200    400   550
    //   天使      0     0    100   150
    //   路障      0     0    300   300
    public const byte MiniWireGameType = 51;
    public const short MiniBanana = 8;
    public const short MiniForceZone = 25;
    public const short MiniSuperShield = 18;

    //          1位   上位   中位   下位
    //   香蕉    900   600    100     0
    //   力场    100   100      0     0
    //   超级护盾  0   300    600   700
    //   天使      0     0      0   100
    //   路障      0     0    300   200
    private static readonly Dictionary<short, int[]> MiniTable = new Dictionary<short, int[]>
    {
        [8] = new[] { 900, 600, 100, 0 },      // 香蕉
        [25] = new[] { 100, 100, 0, 0 },       // 力场
        [18] = new[] { 0, 300, 600, 700 },     // 超级护盾
        [11] = new[] { 0, 0, 0, 100 },         // 天使
        [113] = new[] { 0, 0, 300, 200 },      // 路障
    };

    // ===== 迷你道具赛·个人加速赛（子频道 50）=====
    // 客户端这一模式只实现了 5 种道具：加速器 / 超级加速器 / 磁铁 / 警灯 / 超级盾牌。
    // 千分比（原表列和 100，×10）：
    //          1位   上位   中位   下位
    //   超级盾牌 1000     0      0      0
    //   加速器      0   650    400    300
    //   超级加速器   0   100    200    300
    //   磁铁        0    50    200    200
    //   警灯        0   200    200    200
    public const byte MiniAccelWireGameType = 50;
    public const short AccelBooster = 6;        // 加速器
    public const short AccelSuperBooster = 31;  // 超级加速器
    public const short AccelMagnet = 5;         // 磁铁
    public const short AccelSiren = 24;         // 警灯
    public const short AccelSuperShield = 18;   // 超级盾牌

    private static readonly Dictionary<short, int[]> MiniAccelTable = new Dictionary<short, int[]>
    {
        [18] = new[] { 1000, 0, 0, 0 },        // 超级盾牌
        [6] = new[] { 0, 650, 400, 300 },      // 加速器
        [31] = new[] { 0, 100, 200, 300 },     // 超级加速器
        [5] = new[] { 0, 50, 200, 200 },       // 磁铁
        [24] = new[] { 0, 200, 200, 200 },     // 警灯
    };

    /// <summary>是否个人加速赛房间（子频道号 50）。</summary>
    public static bool IsAccelMode(byte wireGameType) => wireGameType == MiniAccelWireGameType;

    /// <summary>是否个人加速赛合法道具（客户端只实现了这 5 种）。</summary>
    public static bool IsAccelItem(short itemId) => MiniAccelTable.ContainsKey(itemId);

    /// <summary>按档位抽一个个人加速赛道具。</summary>
    /// <param name="excludeItem">本次抽签不想要的源道具（更换道具时不能换成同一个）</param>
    public static short RollMiniAccel(int playerCount, int rank, short excludeItem = 0)
    {
        RankGroup group = Group(playerCount, rank);
        int g = (int)group;
        int total = 0;
        foreach (KeyValuePair<short, int[]> kv in MiniAccelTable)
        {
            if (excludeItem != 0 && kv.Key == excludeItem) continue;
            total += kv.Value[g];
        }
        if (total <= 0) return AccelBooster;
        int pick = Random.Shared.Next(total);
        int acc = 0;
        foreach (KeyValuePair<short, int[]> kv in MiniAccelTable)
        {
            if (excludeItem != 0 && kv.Key == excludeItem) continue;
            acc += kv.Value[g];
            if (pick < acc) return kv.Key;
        }
        return AccelBooster;
    }

    /// <summary>该档位权重最高的加速赛道具（兜底用）。</summary>
    public static short MiniAccelFallback(int playerCount, int rank)
    {
        RankGroup group = Group(playerCount, rank);
        int g = (int)group;
        short best = AccelBooster;
        int bestWeight = -1;
        foreach (KeyValuePair<short, int[]> kv in MiniAccelTable)
        {
            if (kv.Value[g] > bestWeight) { bestWeight = kv.Value[g]; best = kv.Key; }
        }
        return best;
    }

    /// <summary>是否迷你道具赛房间（房间线上模式号 51）。</summary>
    public static bool IsMiniMode(byte wireGameType) => wireGameType == MiniWireGameType;

    /// <summary>是否迷你赛合法道具（客户端只实现了这 5 种）。</summary>
    public static bool IsMiniItem(short itemId) => MiniTable.ContainsKey(itemId);

    /// <summary>迷你赛的稀有道具：天使 / 力场（每人每局各最多 2 个）。</summary>
    public static bool IsMiniRare(short itemId) => itemId == 11 || itemId == 25;

    /// <summary>按档位抽一个迷你赛道具；可分别排除天使 / 力场。</summary>
    /// <param name="excludeItem">本次抽签不想要的源道具（更换道具时不能换成同一个）</param>
    public static short RollMini(int playerCount, int rank, bool excludeAngel, bool excludeForceZone, short excludeItem = 0)
    {
        bool Skip(short id) => (excludeAngel && id == 11)
                               || (excludeForceZone && id == 25)
                               || (excludeItem != 0 && id == excludeItem);
        RankGroup group = Group(playerCount, rank);
        int g = (int)group;
        int total = 0;
        foreach (KeyValuePair<short, int[]> kv in MiniTable)
        {
            if (Skip(kv.Key)) continue;
            total += kv.Value[g];
        }
        if (total <= 0)
        {
            return MiniBanana;
        }
        int pick = Random.Shared.Next(total);
        int acc = 0;
        foreach (KeyValuePair<short, int[]> kv in MiniTable)
        {
            if (Skip(kv.Key)) continue;
            acc += kv.Value[g];
            if (pick < acc) return kv.Key;
        }
        return MiniBanana;
    }

    /// <summary>兼容旧调用：两种稀有一起排除。</summary>
    public static short RollMini(int playerCount, int rank, bool excludeRare)
        => RollMini(playerCount, rank, excludeRare, excludeRare);

    /// <summary>该档位权重最高的非稀有迷你道具（兜底用）。</summary>
    public static short MiniFallback(int playerCount, int rank)
    {
        RankGroup group = Group(playerCount, rank);
        int g = (int)group;
        short best = MiniBanana;
        int bestWeight = -1;
        foreach (KeyValuePair<short, int[]> kv in MiniTable)
        {
            if (IsMiniRare(kv.Key)) continue;
            if (kv.Value[g] > bestWeight) { bestWeight = kv.Value[g]; best = kv.Key; }
        }
        return best;
    }

    /// <summary>夺旗赛专用道具池（客户端 item/slot/itemProb_flag@zz.xml 的默认值，文件读不到时兜底）。</summary>
    /// <remarks>
    /// 夺旗赛客户端只实现了这一组道具：盾牌/导弹/磁铁/加速器/大魔王/天使/5连导弹/
    /// 幽灵/地雷/超级盾/滚动炸弹。下发普通道具池里的香蕉、云、水炸弹、飞碟等会让客户端
    /// 在吃道具时闪退（抓包确认的崩溃点就是 GameSlotPacket 回包之后）。
    /// 文件里只有 high/mid/low 三档，这里把 Top 档按 high 档处理。
    /// </remarks>
    private static Dictionary<short, int[]> FlagTable = new Dictionary<short, int[]>
    {
        [10] = new[] { 21, 21, 15, 9 },      // 盾牌
        [7] = new[] { 15, 15, 25, 35 },      // 导弹
        [5] = new[] { 7, 7, 5, 3 },          // 磁铁
        [6] = new[] { 14, 14, 10, 6 },       // 加速器
        [2] = new[] { 0, 0, 5, 3 },          // 大魔王
        [11] = new[] { 7, 7, 5, 3 },         // 天使
        [15] = new[] { 3, 3, 5, 7 },         // 5 连导弹
        [16] = new[] { 7, 7, 5, 3 },         // 幽灵
        [17] = new[] { 6, 6, 10, 14 },       // 地雷
        [18] = new[] { 7, 7, 5, 3 },         // 超级盾
        [19] = new[] { 6, 6, 10, 14 },       // 滚动炸弹
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
            int highRank = ParseWeight(el, "highrank");
            var weight = new int[4];
            // 夺旗赛概率表（itemProb_flag@zz.xml）只有 high/mid/low 三档：Top 档按 high 处理，
            // 否则总权重为 0 会被过滤掉，夺旗赛房间里就抽不到道具。
            weight[0] = GameModes.IsFlagMode(gameType) ? highRank : ParseWeight(el, "toprank");
            weight[1] = highRank;
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
        else if (GameModes.IsFlagMode(gameType))
        {
            // 夺旗赛专用表 itemProb_flag@zz.xml：只有 high/mid/low 三档，Top 档沿用 high
            FlagTable = parsed;
            FlagFromClient = true;
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
    public static bool FlagFromClient { get; private set; }

    /// <summary>该道具是否在夺旗赛客户端支持的道具列表里（防止下发非法道具导致闪退）。</summary>
    public static bool IsFlagItem(short itemId) => FlagTable.ContainsKey(itemId);

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
        // 夺旗赛使用客户端 itemProb_flag（只含夺旗赛客户端支持的道具，避免吃道具闪退）；
        // 4=组队道具、58=团队死斗沿用组队道具池；其余按个人池。
        if (GameModes.IsFlagMode(gameType))
        {
            return FlagTable;
        }
        return gameType == 4 || gameType == GameModes.DeathMatchTeam
            ? TeamTable
            : PersonalTable;
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
    /// <param name="excludeItem">本次抽签不想要的源道具（更换道具时不能换成同一个）</param>
    public static short Roll(byte gameType, int playerCount, int rank, bool excludeRare, short excludeItem = 0)
    {
        return RollGroup(gameType, Group(playerCount, rank), excludeRare, excludeItem);
    }

    /// <summary>按指定档位加权抽一个道具。</summary>
    public static short RollGroup(byte gameType, RankGroup group, bool excludeRare, short excludeItem = 0)
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
            // 更换道具：新道具不能和原道具相同，否则客户端表现为「用了变更卡但没变化」
            if (excludeItem != 0 && kv.Key == excludeItem)
            {
                continue;
            }
            total += kv.Value[g];
        }
        if (total <= 0)
        {
            // 全被排除时退回原道具（总比给个非法值好），但只在确实只剩它时才会走到
            return excludeItem != 0 ? excludeItem : Booster;
        }

        int pick = Random.Shared.Next(total);
        foreach (KeyValuePair<short, int[]> kv in table)
        {
            if (excludeRare && IsRare(kv.Key))
            {
                continue;
            }
            if (excludeItem != 0 && kv.Key == excludeItem)
            {
                continue;
            }
            pick -= kv.Value[g];
            if (pick < 0)
            {
                return kv.Key;
            }
        }
        return excludeItem != 0 ? excludeItem : Booster;
    }

    /// <summary>
    /// 计算名次（1 起）。
    ///
    /// 以客户端上报的名次为准：它包含 AI、与画面一致。
    /// </summary>
    public static int ComputeRank(GameRoom room, int playerId, short clientRank, int playerCount)
    {
        int count = Math.Max(2, playerCount);
        return Math.Clamp(clientRank + 1, 1, count);
    }
}
