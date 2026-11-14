using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KartRider.Logging;
using Profile;

namespace KartRider;

/// <summary>RP 赛车辆池类别。</summary>
public enum RpPoolKind
{
    Good = 0,   // 好运车
    Bad = 1,    // 霉运车
}

/// <summary>RP 赛玩法（决定用哪一套池子）。</summary>
/// <summary>池子里的物品类别（对应客户端 itemCatId）。</summary>
public enum RpItemCat
{
    Kart = 3,        // 卡丁车
    Goggle = 8,      // 眼镜
    Balloon = 9,     // 气球
    HeadBand = 11,   // 头饰
    PetNormal = 21,  // 宠物
    Pet = 52,        // 飞行宠物
}

public enum RpPoolMode
{
    Speed = 0,  // 组队竞速无限 RP
    Item = 1,   // 组队道具 RP（暂未启用）
}

/// <summary>池子里的一辆车。</summary>
public sealed class RpKartEntry
{
    public int KartId { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>一台车被分配到某个玩家身上的结果。</summary>
public readonly struct RpKartAssign
{
    public readonly ushort KartId;
    public readonly bool IsGood;

    /// <summary>本局搭配的飞行宠物（0 = 不配）。</summary>
    public readonly ushort PetId;

    /// <summary>道具 RP 的其它部位（0 = 不配）：眼镜 / 气球 / 头饰 / 宠物。</summary>
    public readonly ushort GoggleId;
    public readonly ushort BalloonId;
    public readonly ushort HeadBandId;
    public readonly ushort PetNormalId;

    public RpKartAssign(ushort kartId, bool isGood, ushort petId = 0,
                        ushort goggleId = 0, ushort balloonId = 0,
                        ushort headBandId = 0, ushort petNormalId = 0)
    {
        KartId = kartId;
        IsGood = isGood;
        PetId = petId;
        GoggleId = goggleId;
        BalloonId = balloonId;
        HeadBandId = headBandId;
        PetNormalId = petNormalId;
    }
}

/// <summary>
/// RP 赛（福不福）车辆池。
///
/// 数据来源：官服抓包确认「每局开赛前服务端重发 GrSlotDataPacket，把 Set_Kart 换成随机车」，
/// 没有专用的分配包，所以服务端只要维护一份池子、每局抽签写进槽位即可。
///
/// 存盘：Profile/RpPools.json，首次启动用内置默认池生成。
/// 后台（AdminWeb → RP竞速赛）可增删，改完立即生效（下一次开局就用新池子）。
/// </summary>
public static class RpKartPools
{
    private sealed class PoolFile
    {
        public Dictionary<string, List<RpKartEntry>> Speed { get; set; } = new();
        public Dictionary<string, List<RpKartEntry>> Item { get; set; } = new();

        /// <summary>飞宠分配概率（0~100）。键：speed:good / speed:bad / item:good / item:bad</summary>
        public Dictionary<string, int> PetChance { get; set; } = new();

        /// <summary>道具 RP：每局发放「道具换位卡」的概率（0~100，默认 30）。
        /// 实现方式是按概率给房间广播 GrGetBokbulBokSlotChanger。</summary>
        public int CardChanceGood { get; set; } = 30;

        /// <summary>霉运车发卡概率（默认 0，和官服一致：霉运车不给卡）</summary>
        public int CardChanceBad { get; set; } = 0;
    }

    private static readonly object Sync = new();
    private static PoolFile _data;
    private static bool _loaded;
    private static string FilePath => Path.Combine(FileName.ProfileDir, "RpPools.json");

    // ------------------------------------------------------------------ 默认池
    //
    // 来源：客户端 kart\zeta_\cn\content\bokbulbok\bokbulbok.xml
    //   <bokbulbokInfo type='speed'>  grade='1' = 好运车，grade='2' = 霉运车
    //      好运：1564 诛仙四剑 迅 / 1574 黑骑士 迅 / 1587 疾影 迅
    //      霉运：1401 R8-V1 / 1430 黑骑士 V1 / 1440 光明骑士 V1 / 1459 天蝎 V1 / 1466 游侠 V1
    //   <bokbulbokInfo type='item'>  好运：1565 / 1575 / 1588 / 1600
    //                                霉运：1455 / 1467 / 1487 / 1496 / 1510
    //
    // ⚠ 关键：客户端就是按这份文件判断「好运/霉运」的画面。服务端下发池子外的车，
    //   客户端查不到 grade，抽车动画会显示成默认（霉运）画面。
    //   所以默认池直接用客户端认可的车；后台仍可添加其它车，但页面上会标 ⚠。
    private static readonly (int Id, string Name)[] ClientGoodSpeed =
    {
        (1564, "诛仙四剑 迅"),
        (1574, "黑骑士 迅"),
        (1587, "疾影 迅"),
    };

    private static readonly (int Id, string Name)[] ClientBadSpeed =
    {
        (1401, "R8-V1"),
        (1430, "黑骑士 V1"),
        (1440, "光明骑士 V1"),
        (1459, "天蝎 V1"),
        (1466, "游侠 V1"),
    };

    private static readonly (int Id, string Name)[] ClientGoodSpeedPets =
    {
        (85, "飞行火箭"), (97, "飞行机械鸟"), (115, "飞行葫芦瓶"), (122, "飞行精灵"), (127, "飞行兔子宝宝"),
    };

    private static readonly (int Id, string Name)[] ClientGoodItemPets = { };

    // 道具 RP 其它部位的内置兜底（= 客户端 bokbulbok.xml item 段）
    private static readonly Dictionary<RpItemCat, (int Id, string Name)[]> ClientItemParts = new()
    {
        [RpItemCat.Goggle] = new[] { (31, "防尘眼镜"), (38, ""), (56, ""), (58, ""), (100, ""), (149, "") },
        [RpItemCat.Balloon] = new[] { (151, ""), (190, ""), (191, ""), (207, ""), (998, ""), (1108, ""), (1363, "") },
        [RpItemCat.HeadBand] = new[] { (54, ""), (79, ""), (80, ""), (91, ""), (152, ""), (177, ""), (466, ""), (643, "") },
        [RpItemCat.PetNormal] = new[] { (61, ""), (105, ""), (107, ""), (116, ""), (120, ""), (147, ""), (172, ""), (186, ""), (187, "") },
    };

    private static readonly (int Id, string Name)[] ClientGoodItem =
    {
        (1565, "车辆1565"),
        (1575, "车辆1575"),
        (1588, "车辆1588"),
        (1600, "车辆1600"),
    };

    private static readonly (int Id, string Name)[] ClientBadItem =
    {
        (1455, "车辆1455"),
        (1467, "车辆1467"),
        (1487, "车辆1487"),
        (1496, "车辆1496"),
        (1510, "车辆1510"),
    };

    /// <summary>客户端 bokbulbok.xml 认可的车辆 → grade（1 好运 / 2 霉运）；0 表示客户端不认识。</summary>
    public static int ClientGrade(int kartId) => BokbulbokPool.Grade(kartId);

    // ------------------------------------------------------------------ 读写
    private static string Key(RpPoolKind kind, RpItemCat cat)
    {
        string suffix = cat switch
        {
            RpItemCat.Kart => "",
            RpItemCat.Pet => "Pet",
            _ => ((int)cat).ToString(),
        };
        return (kind == RpPoolKind.Good ? "good" : "bad") + suffix;
    }

    /// <summary>某个玩法用到的部位分类（竞速只有车 + 飞宠；道具还有眼镜/气球/头饰/宠物）。</summary>
    public static RpItemCat[] CatsOf(RpPoolMode mode) =>
        mode == RpPoolMode.Item
            ? new[] { RpItemCat.Kart, RpItemCat.Goggle, RpItemCat.Balloon, RpItemCat.HeadBand, RpItemCat.PetNormal, RpItemCat.Pet }
            : new[] { RpItemCat.Kart, RpItemCat.Pet };

    private static Dictionary<string, List<RpKartEntry>> Bucket(PoolFile f, RpPoolMode mode)
    {
        if (mode == RpPoolMode.Speed)
        {
            f.Speed ??= new Dictionary<string, List<RpKartEntry>>();
            return f.Speed;
        }
        f.Item ??= new Dictionary<string, List<RpKartEntry>>();
        return f.Item;
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }
        lock (Sync)
        {
            if (_loaded)
            {
                return;
            }
            PoolFile file = null;
            try
            {
                if (File.Exists(FilePath))
                {
                    file = JsonHelper.DeserializeNoBom<PoolFile>(FilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[RP] 读取 RpPools.json 失败，改用默认池：{ex.Message}");
            }

            file ??= new PoolFile();

            // 缺哪一块就补哪一块（默认池只作为初值，之后完全由文件决定）
            foreach (RpPoolMode mode in new[] { RpPoolMode.Speed, RpPoolMode.Item })
            {
                var bucket = Bucket(file, mode);
                foreach (RpPoolKind kind in new[] { RpPoolKind.Good, RpPoolKind.Bad })
                {
                    foreach (RpItemCat cat in new[] { RpItemCat.Kart, RpItemCat.Pet, RpItemCat.Goggle, RpItemCat.Balloon, RpItemCat.HeadBand, RpItemCat.PetNormal })
                    {
                        string k = Key(kind, cat);
                        if (!bucket.TryGetValue(k, out var list) || list == null)
                        {
                            bucket[k] = BuildDefault(mode, kind, cat);
                        }
                    }
                }
            }

            // 老存档里可能是"空数组"（早期版本没有部位数据）：空就按默认池补上
            foreach (RpPoolMode m3 in new[] { RpPoolMode.Speed, RpPoolMode.Item })
            {
                var bucket3 = Bucket(file, m3);
                foreach (RpPoolKind k3 in new[] { RpPoolKind.Good, RpPoolKind.Bad })
                {
                    foreach (RpItemCat c3 in new[] { RpItemCat.Kart, RpItemCat.Pet, RpItemCat.Goggle, RpItemCat.Balloon, RpItemCat.HeadBand, RpItemCat.PetNormal })
                    {
                        string key3 = Key(k3, c3);
                        if (bucket3.TryGetValue(key3, out var lst3) && (lst3 == null || lst3.Count == 0))
                        {
                            bucket3[key3] = BuildDefault(m3, k3, c3);
                        }
                    }
                }
            }

            // 老版本存进去的是内部英文名（zenithXUN 之类）：这里统一按当前客户端数据刷新一次中文名
            foreach (RpPoolMode m2 in new[] { RpPoolMode.Speed, RpPoolMode.Item })
            {
                var bucket2 = Bucket(file, m2);
                foreach (RpPoolKind k2 in new[] { RpPoolKind.Good, RpPoolKind.Bad })
                {
                    foreach (RpItemCat c2 in new[] { RpItemCat.Kart, RpItemCat.Pet })
                    {
                        foreach (var e in bucket2[Key(k2, c2)])
                        {
                            string fresh = ResolveName(c2, e.KartId);
                            if (!string.IsNullOrEmpty(fresh) && !fresh.StartsWith("车辆", StringComparison.Ordinal))
                            {
                                e.Name = fresh;
                            }
                        }
                    }
                }
            }

            _data = file;
            _loaded = true;
            Save();
            Log.Info($"[RP] 车辆池已就绪：竞速 好运 {_data.Speed["good"].Count} / 霉运 {_data.Speed["bad"].Count}，" +
                     $"道具 好运 {_data.Item["good"].Count} / 霉运 {_data.Item["bad"].Count}");
        }
    }

    private static List<RpKartEntry> BuildDefault(RpPoolMode mode, RpPoolKind kind, RpItemCat cat)
    {
        if (cat != RpItemCat.Kart && cat != RpItemCat.Pet)
        {
            // 道具 RP 的其它部位（眼镜/气球/头饰/宠物）：好运池取客户端表，霉运池留空
            IEnumerable<ushort> partIds;
            if (kind == RpPoolKind.Good && mode == RpPoolMode.Item)
            {
                if (BokbulbokPool.ItemParts.TryGetValue((int)cat, out var pl) && pl != null && pl.Count > 0)
                {
                    partIds = pl;
                }
                else if (ClientItemParts.TryGetValue(cat, out var fb))
                {
                    partIds = fb.Select(x => (ushort)x.Id);
                }
                else
                {
                    partIds = Enumerable.Empty<ushort>();
                }
            }
            else
            {
                partIds = Enumerable.Empty<ushort>();
            }
            return partIds.Select(id => new RpKartEntry { KartId = id, Name = ResolveName(cat, id) }).ToList();
        }

        if (cat == RpItemCat.Pet)
        {
            // 飞宠：官方规则是「好运车配飞宠」，所以默认只给好运池；霉运池留空（后台可自行加）
            IEnumerable<ushort> petIds;
            if (kind == RpPoolKind.Good)
            {
                var fromClient = mode == RpPoolMode.Item ? BokbulbokPool.ItemGoodPets : BokbulbokPool.SpeedGoodPets;
                petIds = (fromClient != null && fromClient.Count > 0)
                    ? fromClient
                    : (mode == RpPoolMode.Item ? ClientGoodItemPets : ClientGoodSpeedPets).Select(x => (ushort)x.Id);
            }
            else
            {
                petIds = Enumerable.Empty<ushort>();
            }
            return petIds.Select(id => new RpKartEntry { KartId = id, Name = ResolveName(RpItemCat.Pet, id) }).ToList();
        }

        // 车辆：首次启动用客户端 bokbulbok.xml 里认可的车当默认池
        IEnumerable<ushort> ids;
        if (BokbulbokPool.Loaded)
        {
            ids = mode == RpPoolMode.Item
                ? (kind == RpPoolKind.Good ? BokbulbokPool.ItemGood : BokbulbokPool.ItemBad)
                : (kind == RpPoolKind.Good ? BokbulbokPool.SpeedGood : BokbulbokPool.SpeedBad);
        }
        else
        {
            var fb = mode == RpPoolMode.Item
                ? (kind == RpPoolKind.Good ? ClientGoodItem : ClientBadItem)
                : (kind == RpPoolKind.Good ? ClientGoodSpeed : ClientBadSpeed);
            ids = fb.Select(x => (ushort)x.Id);
        }

        var list = new List<RpKartEntry>();
        foreach (ushort id in ids)
        {
            list.Add(new RpKartEntry { KartId = id, Name = ResolveName(RpItemCat.Kart, id) });
        }
        return list;
    }

    private static string ChanceKey(RpPoolMode mode, RpPoolKind kind, RpItemCat cat) =>
        (mode == RpPoolMode.Item ? "item:" : "speed:")
        + (kind == RpPoolKind.Good ? "good" : "bad") + ":" + ((int)cat);

    /// <summary>某个部位的默认分配概率（%）。</summary>
    private static int DefaultChance(RpPoolMode mode, RpPoolKind kind, RpItemCat cat)
    {
        if (cat == RpItemCat.Kart)
        {
            return 100;      // 车一定会分
        }
        if (kind == RpPoolKind.Bad)
        {
            return 0;        // 霉运车默认什么都不配
        }
        if (mode == RpPoolMode.Speed)
        {
            return 100;      // 竞速：好运车必配飞宠（官服行为）
        }
        return cat switch
        {
            RpItemCat.Goggle => 60,
            RpItemCat.Balloon => 60,
            RpItemCat.HeadBand => 60,
            RpItemCat.PetNormal => 30,
            RpItemCat.Pet => 30,
            _ => 0,
        };
    }

    public static int GetChance(RpPoolMode mode, RpPoolKind kind, RpItemCat cat)
    {
        EnsureLoaded();
        lock (Sync)
        {
            _data.PetChance ??= new Dictionary<string, int>();
            string k = ChanceKey(mode, kind, cat);
            if (!_data.PetChance.TryGetValue(k, out int v))
            {
                v = DefaultChance(mode, kind, cat);
                _data.PetChance[k] = v;
            }
            return Math.Clamp(v, 0, 100);
        }
    }

    public static bool SetChance(RpPoolMode mode, RpPoolKind kind, RpItemCat cat, int percent)
    {
        EnsureLoaded();
        int v = Math.Clamp(percent, 0, 100);
        lock (Sync)
        {
            _data.PetChance ??= new Dictionary<string, int>();
            _data.PetChance[ChanceKey(mode, kind, cat)] = v;
            Save();
        }
        Log.Debug($"[RP] 分配概率：{mode} / {kind} / {cat} = {v}%");
        return true;
    }

    // 兼容旧调用（飞宠）
    public static int GetPetChance(RpPoolMode mode, RpPoolKind kind) => GetChance(mode, kind, RpItemCat.Pet);
    public static bool SetPetChance(RpPoolMode mode, RpPoolKind kind, int percent) => SetChance(mode, kind, RpItemCat.Pet, percent);

    /// <summary>道具 RP 的道具卡概率（0~100，默认 30）。</summary>
    public static int GetCardChance(RpPoolKind kind)
    {
        EnsureLoaded();
        lock (Sync)
        {
            return Math.Clamp(kind == RpPoolKind.Good ? _data.CardChanceGood : _data.CardChanceBad, 0, 100);
        }
    }

    public static bool SetCardChance(RpPoolKind kind, int percent)
    {
        EnsureLoaded();
        lock (Sync)
        {
            if (kind == RpPoolKind.Good) _data.CardChanceGood = Math.Clamp(percent, 0, 100);
            else _data.CardChanceBad = Math.Clamp(percent, 0, 100);
            Save();
        }
        return true;
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(FileName.ProfileDir);
            JsonHelper.WriteAllTextAtomic(FilePath, JsonHelper.Serialize(_data));
        }
        catch (Exception ex)
        {
            Log.Warn($"[RP] 保存 RpPools.json 失败：{ex.Message}");
        }
    }

    // ------------------------------------------------------------------ 对外接口
    public static List<RpKartEntry> Get(RpPoolMode mode, RpPoolKind kind)
        => Get(mode, kind, RpItemCat.Kart);

    public static List<RpKartEntry> Get(RpPoolMode mode, RpPoolKind kind, RpItemCat cat)
    {
        EnsureLoaded();
        lock (Sync)
        {
            var bucket = Bucket(_data, mode);
            string k = Key(kind, cat);
            if (!bucket.TryGetValue(k, out var list) || list == null)
            {
                bucket[k] = list = new List<RpKartEntry>();
            }
            return list.ToList();
        }
    }

    /// <summary>加车（同池去重；车名从物品表反查，避免手填错）。</summary>
    public static bool Add(RpPoolMode mode, RpPoolKind kind, int kartId, string name = null)
        => Add(mode, kind, RpItemCat.Kart, kartId, name);

    public static bool Add(RpPoolMode mode, RpPoolKind kind, RpItemCat cat, int kartId, string name = null)
    {
        EnsureLoaded();
        if (kartId <= 0)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ResolveName(cat, kartId);
        }
        lock (Sync)
        {
            var list = Bucket(_data, mode)[Key(kind, cat)];
            if (list.Any(x => x.KartId == kartId))
            {
                return false;
            }
            list.Add(new RpKartEntry { KartId = kartId, Name = name });
            Save();
        }
        Log.Debug($"[RP] 池子新增：{mode} / {kind} / {cat} → {kartId} {name}");
        return true;
    }

    public static bool Remove(RpPoolMode mode, RpPoolKind kind, int kartId)
        => Remove(mode, kind, RpItemCat.Kart, kartId);

    public static bool Remove(RpPoolMode mode, RpPoolKind kind, RpItemCat cat, int kartId)
    {
        EnsureLoaded();
        bool removed;
        lock (Sync)
        {
            var list = Bucket(_data, mode)[Key(kind, cat)];
            removed = list.RemoveAll(x => x.KartId == kartId) > 0;
            if (removed)
            {
                Save();
            }
        }
        if (removed)
        {
            Log.Debug($"[RP] 池子移除：{mode} / {kind} → {kartId}");
        }
        return removed;
    }

    public static string ResolveName(int kartId) => ResolveName(RpItemCat.Kart, kartId);

    public static string ResolveName(RpItemCat cat, int kartId)
    {
        ushort catId = (ushort)cat;
        if (RiderData.NewRider.items.TryGetValue(catId, out var dict)
            && dict != null
            && dict.TryGetValue((ushort)kartId, out string cnName)
            && !string.IsNullOrEmpty(cnName))
        {
            return cnName;
        }
        if (cat == RpItemCat.Pet)
        {
            return $"飞宠{kartId}";
        }
        // 优先用客户端商店道具表里的中文名（NewRider.items[3] = itemCatId 3 车辆）
        // Kart.kartName 是 itemTable.kml 的内部英文名（如 zenithXUN），不适合展示
        if (RiderData.NewRider.items.TryGetValue(3, out var karts)
            && karts != null
            && karts.TryGetValue((ushort)kartId, out string cn)
            && !string.IsNullOrEmpty(cn))
        {
            return cn;
        }
        if (Kart.kartName.TryGetValue(kartId, out var table) && !string.IsNullOrEmpty(table?.Name))
        {
            return table.Name;
        }
        return $"车辆{kartId}";
    }

    // ------------------------------------------------------------------ 分配
    /// <summary>
    /// 给一局 RP 赛分配车辆，写进 room.RpKarts。
    ///
    /// 规则（2026-11-13 需求）：
    ///   - 每队 4 人：2 好运 + 2 霉运；不足 4 人按「好运 = ceil(n/2)」分配
    ///   - 同一局内**全局不重复**（跨队也不重复；池子不够时才允许重复）
    /// </summary>
    public static void AssignForRace(GameRoom room)
    {
        if (room == null)
        {
            return;
        }
        room.RpKarts.Clear();
        if (!GameModes.IsRpMode(room.GameType))
        {
            return;
        }

        var mode = GameModes.IsRpItemMode(room.GameType) ? RpPoolMode.Item : RpPoolMode.Speed;

        // 车：好运池 + 霉运池合并成一个抽奖池
        var pool = new List<(RpKartEntry Entry, bool Good)>();
        foreach (var e in Get(mode, RpPoolKind.Good, RpItemCat.Kart))
        {
            pool.Add((e, true));
        }
        foreach (var e in Get(mode, RpPoolKind.Bad, RpItemCat.Kart))
        {
            pool.Add((e, false));
        }
        if (pool.Count == 0)
        {
            Log.Warn($"[RP] 房间 {room.RoomId} 车辆池为空，本局不分配（按各自装备车跑）");
            return;
        }
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var racers = room._slots.OfType<Player>()
            .Where(p => !string.IsNullOrEmpty(p.Nickname))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        // 每个部位各自准备一份洗好的池子（好运/霉运分别洗）
        var partCats = CatsOf(mode).Where(c => c != RpItemCat.Kart).ToArray();
        var partPools = partCats.ToDictionary(
            c => c,
            c => new
            {
                Good = Shuffle(Get(mode, RpPoolKind.Good, c).Select(e => (ushort)e.KartId).ToList()),
                Bad = Shuffle(Get(mode, RpPoolKind.Bad, c).Select(e => (ushort)e.KartId).ToList()),
            });
        var partIdx = partCats.ToDictionary(c => c, _ => 0);

        int goodCount = pool.Count(x => x.Good);
        Log.Debug($"[RP] 房间 {room.RoomId} 车池构成：好运 {goodCount} / 霉运 {pool.Count - goodCount}，参赛 {racers.Count} 人");

        var sb = new System.Text.StringBuilder();
        int idx = 0;
        foreach (var p in racers)
        {
            var item = pool[idx % pool.Count];
            idx++;
            var kind = item.Good ? RpPoolKind.Good : RpPoolKind.Bad;

            // 逐个部位按概率判定：抽中就从对应池子里不重复地取一个
            ushort Pick(RpItemCat cat)
            {
                // 竞速只看飞宠、道具才看眼镜/气球/头饰/宠物：该模式没有的部位直接跳过
                if (!partPools.TryGetValue(cat, out var buckets))
                {
                    return 0;
                }
                int chance = GetChance(mode, kind, cat);
                if (chance <= 0)
                {
                    return 0;
                }
                if (chance < 100 && Random.Shared.Next(100) >= chance)
                {
                    return 0;
                }
                var list = item.Good ? buckets.Good : buckets.Bad;
                if (list.Count == 0)
                {
                    // 该分类当前池为空：再试另一个（后台只填了一边的情况）
                    list = item.Good ? buckets.Bad : buckets.Good;
                }
                if (list.Count == 0)
                {
                    return 0;
                }
                int i = partIdx[cat]++;
                return list[i % list.Count];
            }

            ushort cardId = 0;

            var assign = new RpKartAssign(
                (ushort)item.Entry.KartId, item.Good,
                petId: Pick(RpItemCat.Pet),
                goggleId: Pick(RpItemCat.Goggle),
                balloonId: Pick(RpItemCat.Balloon),
                headBandId: Pick(RpItemCat.HeadBand),
                petNormalId: Pick(RpItemCat.PetNormal));

            room.RpKarts[p.Nickname] = assign;

            sb.Append($"{p.Nickname}={(item.Good ? "好" : "霉")}{item.Entry.KartId}({item.Entry.Name})");
            if (assign.GoggleId != 0) sb.Append($"+镜{assign.GoggleId}");
            if (assign.BalloonId != 0) sb.Append($"+球{assign.BalloonId}");
            if (assign.HeadBandId != 0) sb.Append($"+饰{assign.HeadBandId}");
            if (assign.PetNormalId != 0) sb.Append($"+宠{assign.PetNormalId}");
            if (assign.PetId != 0) sb.Append($"+飞{assign.PetId}");

            sb.Append(' ');
        }

        Log.Debug($"[RP] 房间 {room.RoomId} 本局分配（{mode}）：{sb.ToString().TrimEnd()}");
    }

    /// <summary>飞宠池（来自客户端 bokbulbok.xml 的 itemCatId=52）。</summary>
    public static List<ushort> GetPetPool(RpPoolMode mode)
    {
        var src = mode == RpPoolMode.Item ? BokbulbokPool.ItemGoodPets : BokbulbokPool.SpeedGoodPets;
        return src.ToList();
    }

    private static List<ushort> Shuffle(List<ushort> src)
    {
        var list = src.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static List<RpKartEntry> Shuffle(List<RpKartEntry> src)
    {
        var list = src.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
