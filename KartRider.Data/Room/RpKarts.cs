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
    public RpKartAssign(ushort kartId, bool isGood)
    {
        KartId = kartId;
        IsGood = isGood;
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
    private static string Key(RpPoolKind kind) => kind == RpPoolKind.Good ? "good" : "bad";

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
                    string k = Key(kind);
                    if (!bucket.TryGetValue(k, out var list) || list == null)
                    {
                        bucket[k] = BuildDefault(mode, kind);
                    }
                }
            }

            // 老版本存进去的是内部英文名（zenithXUN 之类）：这里统一按当前客户端数据刷新一次中文名
            foreach (RpPoolMode m2 in new[] { RpPoolMode.Speed, RpPoolMode.Item })
            {
                var bucket2 = Bucket(file, m2);
                foreach (RpPoolKind k2 in new[] { RpPoolKind.Good, RpPoolKind.Bad })
                {
                    foreach (var e in bucket2[Key(k2)])
                    {
                        string fresh = ResolveName(e.KartId);
                        if (!string.IsNullOrEmpty(fresh) && !fresh.StartsWith("车辆", StringComparison.Ordinal))
                        {
                            e.Name = fresh;
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

    private static List<RpKartEntry> BuildDefault(RpPoolMode mode, RpPoolKind kind)
    {
        // 首次启动：用客户端 bokbulbok.xml 里认可的车当默认池（这样抽车画面一定正确）。
        // 之后以 Profile/RpPools.json 为准，不再覆盖（运营可能自己加过车）。
        IEnumerable<ushort> ids;
        if (BokbulbokPool.Loaded)
        {
            ids = mode == RpPoolMode.Item
                ? (kind == RpPoolKind.Good ? BokbulbokPool.ItemGood : BokbulbokPool.ItemBad)
                : (kind == RpPoolKind.Good ? BokbulbokPool.SpeedGood : BokbulbokPool.SpeedBad);
        }
        else
        {
            // 客户端数据没读到时的兜底（等于官方 speed 段那 8 辆）
            var fb = mode == RpPoolMode.Item
                ? (kind == RpPoolKind.Good ? ClientGoodItem : ClientBadItem)
                : (kind == RpPoolKind.Good ? ClientGoodSpeed : ClientBadSpeed);
            ids = fb.Select(x => (ushort)x.Id);
        }

        var list = new List<RpKartEntry>();
        foreach (ushort id in ids)
        {
            list.Add(new RpKartEntry { KartId = id, Name = ResolveName(id) });
        }
        return list;
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
    {
        EnsureLoaded();
        lock (Sync)
        {
            var bucket = Bucket(_data, mode);
            string k = Key(kind);
            if (!bucket.TryGetValue(k, out var list) || list == null)
            {
                bucket[k] = list = new List<RpKartEntry>();
            }
            return list.ToList();
        }
    }

    /// <summary>加车（同池去重；车名从物品表反查，避免手填错）。</summary>
    public static bool Add(RpPoolMode mode, RpPoolKind kind, int kartId, string name = null)
    {
        EnsureLoaded();
        if (kartId <= 0)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ResolveName(kartId);
        }
        lock (Sync)
        {
            var list = Bucket(_data, mode)[Key(kind)];
            if (list.Any(x => x.KartId == kartId))
            {
                return false;
            }
            list.Add(new RpKartEntry { KartId = kartId, Name = name });
            Save();
        }
        Log.Debug($"[RP] 池子新增：{mode} / {kind} → {kartId} {name}");
        return true;
    }

    public static bool Remove(RpPoolMode mode, RpPoolKind kind, int kartId)
    {
        EnsureLoaded();
        bool removed;
        lock (Sync)
        {
            var list = Bucket(_data, mode)[Key(kind)];
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

    public static string ResolveName(int kartId)
    {
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
        if (!GameModes.IsRpSpeedMode(room.GameType))
        {
            return;
        }

        // 好运池 + 霉运池合并成一个抽奖池（2026-11-13 需求：完全随机，不再按队分 2 好 2 霉）
        var pool = new List<(RpKartEntry Entry, bool Good)>();
        foreach (var e in Get(RpPoolMode.Speed, RpPoolKind.Good))
        {
            pool.Add((e, true));
        }
        foreach (var e in Get(RpPoolMode.Speed, RpPoolKind.Bad))
        {
            pool.Add((e, false));
        }
        if (pool.Count == 0)
        {
            Log.Warn($"[RP] 房间 {room.RoomId} 车辆池为空，本局不分配（按各自装备车跑）");
            return;
        }

        // 洗牌后按顺序发；同一局内**全局不重复**，池子不够时才循环复用
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var racers = room._slots.OfType<Player>()
            .Where(p => !string.IsNullOrEmpty(p.Nickname))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var sb = new System.Text.StringBuilder();
        int idx = 0;
        foreach (var p in racers)
        {
            var item = pool[idx % pool.Count];
            idx++;
            room.RpKarts[p.Nickname] = new RpKartAssign((ushort)item.Entry.KartId, item.Good);
            sb.Append($"{p.Nickname}={(item.Good ? "好" : "霉")}{item.Entry.KartId}({item.Entry.Name}) ");
        }

        Log.Debug($"[RP] 房间 {room.RoomId} 本局车辆分配（池 {pool.Count} 抽 {racers.Count}）：{sb.ToString().TrimEnd()}");
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
