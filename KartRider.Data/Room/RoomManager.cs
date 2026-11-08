using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Profile;
using KartRider.Logging;

namespace KartRider;

public static class RoomManager
{
    // 存储所有房间（键：房间ID，值：房间实例）
    public static ConcurrentDictionary<int, GameRoom> _rooms = new ConcurrentDictionary<int, GameRoom>();
    private static ConcurrentDictionary<string, int> _playerRoomMap = new ConcurrentDictionary<string, int>();
    private static int _nextRoomId = 1; // 下一个可用的房间ID（自增确保唯一）
    private const int PageSize = 10; // 每页10个

    // 创建新房间（返回房间ID）
    public static int CreateRoom()
    {
        // 客户端 ChJoinRoomRequestPacket 里房间号只有 1 字节（ReadByte），房号必须落在 1..255，
        // 否则房间在列表里能看到、点进去却对应到别的房间或直接进不去。
        // 这里在 1..255 内循环复用（旧实现是全局自增，跑一会儿就超过 255 了）。
        for (int i = 0; i < 255; i++)
        {
            int raw = Interlocked.Increment(ref _nextRoomId) - 1;
            int roomId = (raw % 255) + 1;
            if (_rooms.TryAdd(roomId, new GameRoom(roomId)))
            {
                return roomId;
            }
        }

        // 1..255 全被占用（异常情况）：退回自增，保证至少还能建房
        int fallbackId = Interlocked.Increment(ref _nextRoomId) - 1;
        _rooms.TryAdd(fallbackId, new GameRoom(fallbackId));
        return fallbackId;
    }

    /// <summary>清理没有真人的房间（AI 不阻止清理），防止 0 人房残留在列表里。</summary>
    public static void PurgeEmptyRooms()
    {
        foreach (var pair in _rooms.ToArray())
        {
            GameRoom room = pair.Value;
            if (room.GetPlayerCount() == 0 && room.GetOBCount() == 0)
            {
                RemoveRoom(room);
            }
        }
    }

    /// <summary>
    /// 开局等待期：任意客户端包都视为“客户端已进入加载/存活”，用于替代不可靠的 UDP 同步信号。
    /// </summary>
    public static void MarkPlayerPacket(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return;
        }
        if (!_playerRoomMap.TryGetValue(nickname, out int roomId))
        {
            return;
        }
        var room = GetRoom(roomId);
        if (room == null || !room.ReadinessCollect)
        {
            return;
        }
        if (room.Ready.TryGetValue(nickname, out bool ready) && !ready)
        {
            room.Ready[nickname] = true;
        }
    }

    // 检查房间是否有玩家，如果没有则删除房间
    public static void RemoveRoom(GameRoom room)
    {
        if (room == null)
        {
            return;
        }
        if (room.GetPlayerCount() > 0 || room.GetOBCount() > 0)
        {
            MultyPlayer.GrSlotDataPacket(room.RoomId);
            return;
        }

        _rooms.TryRemove(room.RoomId, out _);
        RoomScore.Clear(room.RoomId);   // 房间销毁：清掉自动计分状态

        // 二次确认：删除与 AddPlayer 之间存在窗口期，若期间真的有玩家进来了就把房间挂回去，
        // 否则 _playerRoomMap 会指向一个不存在的房间（永久幽灵，无法离开/重进）。
        if (room.GetPlayerCount() > 0 || room.GetOBCount() > 0)
        {
            _rooms.TryAdd(room.RoomId, room);
            MultyPlayer.GrSlotDataPacket(room.RoomId);
        }
    }

    // 获取指定页码的房间列表（每页10个）
    public static Dictionary<int, GameRoom> GetRoomsByPage(int pageIndex)
        => GetRoomsByPage(pageIndex, -1, -1, out _, false);

    /// <summary>
    /// 按频道（以及可选按模式）过滤后分页。
    /// channel &lt; 0 表示不过滤；room.Channel &lt; 0 视为“未知频道”，对所有频道可见（兼容旧房间）。
    /// </summary>
    public static Dictionary<int, GameRoom> GetRoomsByPage(int pageIndex, int channel, int gameType, out int totalCount, bool filterGameType)
    {
        if (pageIndex < 0)
            pageIndex = 0;

        IEnumerable<KeyValuePair<int, GameRoom>> query = _rooms;
        if (channel >= 0)
        {
            query = query.Where(kvp => kvp.Value.Channel < 0 || kvp.Value.Channel == channel);
        }
        if (filterGameType && gameType >= 0)
        {
            query = query.Where(kvp => kvp.Value.GameType == (byte)gameType);
        }

        var list = query.OrderBy(kvp => kvp.Key).ToList();
        totalCount = list.Count;
        if (totalCount == 0)
            return new Dictionary<int, GameRoom>();

        return list.Skip(pageIndex * PageSize).Take(PageSize).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static Dictionary<int, GameRoom> GetRoomsByPageLegacy(int pageIndex)
    {
        int totalCount = _rooms.Count;
        // 计算总页数（从0开始的最大页码+1）
        int totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        // 如果请求的页码超出范围，返回空字典
        if (pageIndex >= totalPages)
            return new Dictionary<int, GameRoom>();

        // 按Key排序后分页（保持结果一致性）
        var pagedItems = _rooms
            .OrderBy(kvp => kvp.Key) // 按房间ID排序，可替换为其他排序字段
            .Skip(pageIndex * PageSize) // 直接用页码索引计算跳过的数量（无需减1）
            .Take(PageSize)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return pagedItems;
    }

    /// <summary>
    /// 注册「昵称 → 房间」映射前，清掉上一次残留的房间归属。
    ///
    /// 残留来源：房主没走完离开流程就再次建房、客户端异常断线时槽位已找不到
    /// （GetPlayerSlotId 返回 -1，RemovePlayer 里的映射清理被跳过）等。
    /// 不清理的话第二次建房 TryAdd 必然失败，AddPlayer 返回 255，
    /// 建房处理器拿不到槽位就会静默 return（客户端收不到回包 → 卡死/崩溃）。
    /// </summary>
    private static void DetachPreviousRoom(string nickname, int targetRoomId)
    {
        if (string.IsNullOrEmpty(nickname))
            return;
        if (!_playerRoomMap.TryRemove(nickname, out int oldRoomId))
            return;
        if (oldRoomId == targetRoomId)
        {
            // 同一房间重复入座：必须先清掉残留的同名旧槽，否则会变成两个同名 Player
            int sameSlot;
            try { sameSlot = GetPlayerSlotId(oldRoomId, nickname); }
            catch (NullReferenceException) { return; }
            if (sameSlot >= 0)
            {
                Log.Debug($"[AddPlayer] {nickname} 同房间重复入座，先清理旧槽位 {sameSlot}");
                RemovePlayer(oldRoomId, (byte)sameSlot, nickname);
            }
            return;
        }

        GameRoom oldRoom = GetRoom(oldRoomId);
        if (oldRoom == null)
            return;

        int slotId;
        try
        {
            slotId = GetPlayerSlotId(oldRoomId, nickname);
        }
        catch (NullReferenceException)
        {
            // 档案未加载完整（Rider 为空）时无法定位槽位，映射已经清掉，直接放弃
            return;
        }
        if (slotId >= 0)
        {
            RemovePlayer(oldRoomId, (byte)slotId, nickname);
        }
    }

    // 尝试向房间添加玩家
    /// <summary>双人组队入座（含 playerRoomMap 注册）。</summary>
    public static byte AddDuoPlayer(int roomId, string nickname, int playerType, SessionGroup client)
    {
        var room = GetRoom(roomId);
        if (room == null || string.IsNullOrEmpty(nickname))
            return 255;
        // 可能残留上一次房间的映射（异常退出/换房），先清掉再注册
        DetachPreviousRoom(nickname, roomId);
        if (!_playerRoomMap.TryAdd(nickname, roomId))
        {
            Log.Info($"[DuoAdd] {nickname} 注册房间 {roomId} 失败");
            return 255;
        }
        if (!Profile.FileName.FileNames.ContainsKey(nickname))
        {
            Profile.FileName.Load(nickname);
        }
        byte added;
        lock (room.Sync)
        {
            added = room.TryAddDuoPlayer(nickname, playerType, client);
        }
        if (added != 255)
        {
            return added;
        }
        Log.Info($"[DuoAdd] {nickname} 入座失败（room={roomId}, slots full?）");
        _playerRoomMap.TryRemove(nickname, out _);
        return 255;
    }

    public static byte AddPlayer(int roomId, string nickname, byte team, int playerType, SessionGroup client)
    {
        var room = GetRoom(roomId);

        // 团队模式（含夺旗赛/死斗）与挑战者模式：未指定队伍时按模式规则分配
        if (team == 0 && room != null)
        {
            if (GameModes.IsTeamGameType(room.GameType) || GameModes.IsChallenger(room.GameType))
            {
                team = GameModes.DefaultTeam(room.GameType, room.GetPlayerCount(1), room.GetPlayerCount(2));
            }
            else if (room.GameType == 3 || room.GameType == 4)
            {
                // 组队房间：未指定队伍时自动平衡（红蓝人数差不超过 1）
                int t1 = room.GetPlayerCount(1);
                int t2 = room.GetPlayerCount(2);
                team = t2 <= t1 ? (byte)2 : (byte)1;
            }
        }

        if (room == null || string.IsNullOrEmpty(nickname))
            return 255;

        // 可能残留上一次房间的映射（异常退出/换房/房主没走完离开流程），先清掉再注册，
        // 否则第二次建房时 TryAdd 必然失败 → AddPlayer 返回 255 → 客户端拿不到回包而卡死
        DetachPreviousRoom(nickname, roomId);

        // 去重：严格区分大小写，TryAdd 原子操作（并发下同一昵称只会有一个成功）
        if (!_playerRoomMap.TryAdd(nickname, roomId))
        {
            // 先清一次残留同名槽位再重试；仍失败才放弃（绝不拆整个房间）
            int dupSlot = GetPlayerSlotId(roomId, nickname);
            if (dupSlot >= 0)
            {
                Log.Debug($"[AddPlayer] {nickname} 房间 {roomId} 内已有同名槽位 {dupSlot}，先清理");
                RemovePlayer(roomId, (byte)dupSlot, nickname);
                _playerRoomMap.TryRemove(nickname, out _);
            }
            if (!_playerRoomMap.TryAdd(nickname, roomId))
            {
                Log.Debug($"[AddPlayer] {nickname} 注册房间 {roomId} 失败（其它房间残留映射）");
                return 255;
            }
        }

        // 确保玩家配置文件已加载
        if (!Profile.FileName.FileNames.ContainsKey(nickname))
        {
            Profile.FileName.Load(nickname);
        }

        // 存储原始昵称
        byte added = room.TryAddPlayer(nickname, team, playerType, client);
        if (added != 255)
        {
            return added;
        }

        // 添加失败，回滚 playerRoomMap
        _playerRoomMap.TryRemove(nickname, out _);

        // 添加失败检查房间是否为空，是则立即删除
        RemoveRoom(room);

        return 255;
    }

    public static int GetPlayerSlotId(int roomId, string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return -1;

        // 1. 检查房间是否存在
        if (!_rooms.TryGetValue(roomId, out var room))
            return -1;

        uint pmap = Profile.ProfileService.GetProfileConfig(nickname).Rider.pmap;
        if (pmap == 718 || pmap == 590)
        {
            foreach (var member in room.ObIDs)
            {
                // 匹配玩家类型且昵称一致
                if (member is Player player && player.Nickname == nickname)
                {
                    return player.SlotId; // 返回玩家所在的格子ID
                }
            }
            return -1;
        }
        else
        {
            // 2. 遍历房间所有格子，查找目标玩家
            foreach (var member in room._slots)
            {
                // 匹配玩家类型且昵称一致
                if (member is Player player && player.Nickname == nickname)
                {
                    return player.SlotId; // 返回玩家所在的格子ID
                }
            }
            return -1;
        }
    }

    public static int TryGetRoomId(string nickname)
    {
        int roomId = -1;
        if (string.IsNullOrEmpty(nickname))
            return roomId;

        // 严格匹配大小写（"Zhang"和"zhang"会返回不同结果）
        return _playerRoomMap.TryGetValue(nickname, out roomId) ? roomId : -1;
    }

    // 从房间移除成员（如果是玩家且移除后无玩家，则删除房间）
    /// <summary>
    /// 按会话引用清理房间槽位（不依赖昵称索引）：把该会话占用的所有槽位全部移除。
    /// 返回是否清理到了至少一个槽位。
    /// </summary>
    public static bool RemovePlayerBySession(SessionGroup session)
    {
        if (session == null) return false;
        bool removed = false;
        foreach (var pair in _rooms.ToArray())
        {
            GameRoom room = pair.Value;
            if (room == null) continue;
            for (byte s = 0; s < room._slots.Length; s++)
            {
                if (room._slots[s] is Player p && ReferenceEquals(p.Session, session))
                {
                    Log.Debug($"[Cleanup] 房间 {room.RoomId} 清理会话占用的槽位 {s}（{p.Nickname}）");
                    removed |= RemovePlayer(room.RoomId, s, p.Nickname);
                }
            }
        }
        return removed;
    }

    public static bool RemovePlayer(int roomId, byte slotId, string nickname)
    {
        var room = GetRoom(roomId);
        if (room == null)
            return false;

        bool removed = room.RemoveMember(slotId, nickname);
        if (removed)
        {
            // 清理并重整排名
            room.CleanupRankings();

            if (!string.IsNullOrEmpty(nickname))
            {
                _playerRoomMap.TryRemove(nickname, out _); // 原子删除
            }

            // 没有真人了就销毁房间（AI 不阻止销毁），避免 0 人房永远留在列表里
            if (room.GetPlayerCount() == 0 && room.GetOBCount() == 0)
            {
                RemoveRoom(room);
            }
        }
        return removed;
    }

    // 昵称变更时同步房间相关数据（改名卡使用后调用）
    // 需要同步：玩家所在房间映射、房间内成员对象的昵称、Ready 字典键
    public static void OnNicknameChanged(string oldNickname, string newNickname)
    {
        if (string.IsNullOrEmpty(oldNickname) || string.IsNullOrEmpty(newNickname))
            return;
        if (oldNickname == newNickname)
            return;

        // 1. 重映射玩家所在房间（防止旧昵称成为幽灵映射，导致开局/掉线清理时查不到房间）
        if (_playerRoomMap.TryRemove(oldNickname, out int roomId))
        {
            _playerRoomMap[newNickname] = roomId;
        }

        // 2. 更新房间内成员对象的昵称与 Ready 键（成员对象与 _IDs 引用同一实例，同步 _slots 即可）
        foreach (var room in _rooms.Values)
        {
            foreach (RoomMember member in room._slots)
            {
                if (member is Player player && player.Nickname == oldNickname)
                {
                    player.Nickname = newNickname;
                }
            }
            foreach (RoomMember member in room.ObIDs)
            {
                if (member is Player player && player.Nickname == oldNickname)
                {
                    player.Nickname = newNickname;
                }
            }
            if (room.Ready.TryRemove(oldNickname, out bool ready))
            {
                room.Ready[newNickname] = ready;
            }
        }
    }

    // 获取指定房间中指定位置的状态
    public static SlotStatus TryGetSlotStatus(int roomId, byte slotId)
    {
        SlotStatus status = SlotStatus.Empty;
        // 检查房间是否存在
        if (!_rooms.TryGetValue(roomId, out var room))
            return status;

        try
        {
            status = room.GetSlotStatus(slotId);
            return status;
        }
        catch (ArgumentOutOfRangeException)
        {
            // 格子ID无效时返回false
            return status;
        }
    }

    // 扩展：获取指定位置的详细成员信息（玩家昵称或AI属性）
    public static object TryGetSlotDetail(int roomId, byte slotId)
    {
        object detail = null;
        if (!_rooms.TryGetValue(roomId, out var room))
            return detail;

        var member = room.GetSlotMember(slotId);
        if (member == null)
            return detail; // 空位置，detail为null

        if (member is Player player)
            detail = player; // 玩家
        else if (member is Ai ai)
            detail = ai; // AI返回完整对象（可按需简化）
        return detail;
    }

    // 扩展：获取指定ID的详细成员信息（玩家昵称或AI属性）
    public static object TryGetIdDetail(int roomId, int Id)
    {
        object detail = null;
        if (!_rooms.TryGetValue(roomId, out var room))
            return detail;

        var member = room.GetIdMember(Id);
        if (member == null)
            return detail; // 空位置，detail为null

        if (member is Player player)
            detail = player; // 玩家
        else if (member is Ai ai)
            detail = ai; // AI返回完整对象（可按需简化）
        return detail;
    }

    public static Player GetPlayer(int roomId, string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return null;

        // 1. 先检查房间是否存在
        if (!_rooms.TryGetValue(roomId, out var room))
            return null;
        uint pmap = ProfileService.GetProfileConfig(nickname).Rider.pmap;
        if (pmap == 718 || pmap == 590)
        {
            foreach (var member in room.ObIDs)
            {
                // 严格匹配昵称（含大小写）
                if (member is Player player && player.Nickname == nickname)
                {
                    return player;
                }
            }
            return null;
        }
        else
        {
            // 2. 遍历房间的8个格子，查找昵称匹配的玩家。
            //    如果出现两个同名（历史幽灵），优先返回"会话仍然存活"的那个，
            //    避免指令落到死对象上导致玩家卡死。
            Player firstMatch = null;
            foreach (var member in room._slots)
            {
                if (member is not Player player || player.Nickname != nickname) continue;

                SessionGroup indexed = ClientManager.GetParent(nickname);
                bool alive = player.Session?.Client != null
                             && player.Session.Client.mDisconnected == 0
                             && ReferenceEquals(indexed, player.Session);
                if (alive) return player;
                firstMatch ??= player;
            }
            return firstMatch;
        }
    }

    // 双人组队换队：落到目标队的主位（上）；有人则副位（下）
    public static bool ChangeDuoTeam(int roomId, byte slotId, byte team)
    {
        if (!_rooms.TryGetValue(roomId, out var room) || !room.DuoTeam)
            return false;
        lock (room.Sync)
        {
            return ChangeDuoTeamLocked(room, slotId, team);
        }
    }

    private static bool ChangeDuoTeamLocked(GameRoom room, byte slotId, byte team)
    {
        if (team < 1 || team > 4)
            return false;

        var member = TryGetSlotDetail(room.RoomId, slotId) as RoomMember;
        if (member == null)
            return false;

        byte current = TeamOf(member);
        if (current == team)
            return true;   // 同色，视为成功

        int memberId = member is Player p0 ? p0.ID : ((Ai)member).ID;

        // 目标颜色已满 2 人（不含自己）→ 不生效
        if (room.GetColorCount(team, memberId) >= 2)
            return false;

        // 2 人房 1v1：不能和对手同色，位置不动
        if (room.DuoColumns == 1)
        {
            byte opponent = 0;
            foreach (RoomMember m in room._slots)
            {
                if (m == null || ReferenceEquals(m, member)) continue;
                byte t = TeamOf(m);
                if (t >= 1 && t <= 4) { opponent = t; break; }
            }
            if (opponent == team)
                return false;
            SetTeam(member, team);
            return true;
        }

        byte dest = 255;

        int duoCols = room.DuoColumns;

        // ⓪ 目标颜色已经有玩家 → 吸附过去：先同列的另一格，再同一排的其它空位
        //    （这是"换到队友同色"时的预期行为，优先于"颜色所属列"）
        foreach (RoomMember m in room._slots)
        {
            if (m == null || ReferenceEquals(m, member)) continue;
            if (TeamOf(m) != team) continue;
            byte col = GameRoom.DuoColOf(m.SlotId);
            byte row = GameRoom.DuoRowOf(m.SlotId);
            byte otherRow = (byte)(1 - row);

            byte s = GameRoom.DuoSlot(col, otherRow);
            if (s == slotId || room.DuoSlotFree(s)) dest = s;

            if (dest == 255)
            {
                // 同列放不下 → 同一排的其它空位
                for (byte c = 0; c < duoCols && dest == 255; c++)
                {
                    byte s2 = GameRoom.DuoSlot(c, otherRow);
                    if (s2 != slotId && room.DuoSlotFree(s2)) dest = s2;
                }
            }
            break;
        }

        // ① 目标颜色还没有玩家 → 用"颜色所属列"（列 = (颜色-1) % 列数）
        //    方案 B：该列第一行被「别的颜色」占着时，**原地改色、不挪过去**，
        //    避免不同颜色被"吸附"到同一列（4/6 人房颜色共用列时尤其明显）。
        if (dest == 255)
        {
            byte colorCol = (byte)((team - 1) % duoCols);
            byte top = GameRoom.DuoSlot(colorCol, 0);
            if (top == slotId || room.DuoSlotFree(top))
            {
                dest = top;
            }
            else
            {
                // 第一行被别的颜色占了：先找「其它列的第一行空位」，都没有才原地不动
                // （避免为了换色被塞进别人那一列的第二行）
                for (byte c = 0; c < duoCols && dest == 255; c++)
                {
                    byte s2 = GameRoom.DuoSlot(c, 0);
                    if (s2 != slotId && room.DuoSlotFree(s2)) dest = s2;
                }
                if (dest == 255)
                {
                    dest = slotId;
                }
            }
        }

        // ① 目标颜色已有 1 人 → 同列另一格
        foreach (RoomMember m in room._slots)
        {
            if (dest != 255) break;
            if (m == null || ReferenceEquals(m, member)) continue;
            if (TeamOf(m) != team) continue;
            byte col = GameRoom.DuoColOf(m.SlotId);
            for (byte row = 0; row < 2 && dest == 255; row++)
            {
                byte s = GameRoom.DuoSlot(col, row);
                if (s != slotId && room.DuoSlotFree(s)) dest = s;
            }
            break;
        }

        // ② 第一行空位
        int columns = duoCols;
        for (byte col = 0; col < columns && dest == 255; col++)
        {
            byte s = GameRoom.DuoSlot(col, 0);
            if (s != slotId && room.DuoSlotFree(s)) dest = s;
        }
        // ③ 第二行空位
        for (byte col = 0; col < columns && dest == 255; col++)
        {
            byte s = GameRoom.DuoSlot(col, 1);
            if (s != slotId && room.DuoSlotFree(s)) dest = s;
        }
        // ④ 任意剩余空位（拆列）
        foreach (byte s in GameRoom.DuoSlots)
        {
            if (dest != 255) break;
            if (s != slotId && room.DuoSlotFree(s)) dest = s;
        }

        // 注意：dest == 自己原来那格时绝不能走移动分支（否则先写再置空会把玩家从房间里抹掉 → 客户端黑屏）
        if (dest != 255 && dest != slotId)
        {
            room._slots[dest] = member;
            room._slots[slotId] = null;
            if (member is Player pm) pm.SlotId = dest;
            else if (member is Ai am) am.SlotId = dest;
        }

        SetTeam(member, team);
        return true;
    }

    /// <summary>取成员当前颜色（1~4，0 = 未分配）。</summary>

    private static byte TeamOf(RoomMember member)
    {
        if (member is Player p) return p.Team;
        if (member is Ai a) return a.Team;
        return 0;
    }

    /// <summary>设置成员颜色（同时作为结算用的队伍号）。</summary>
    private static void SetTeam(RoomMember member, byte team)
    {
        if (member is Player p) p.Team = team;
        else if (member is Ai a) a.Team = team;
    }

    // 更换指定房间中指定位置成员的队伍
    public static bool ChangeMemberTeam(int roomId, byte slotId, byte team)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return false;
        if (team == 2)
        {
            for (int i = 0; i < 4; i++)
            {
                if (room.ChangeSlotId(slotId, (byte)i))
                {
                    var member = TryGetSlotDetail(roomId, (byte)i);
                    if (member is Player player)
                    {
                        player.SlotId = (byte)i;
                        player.Team = team;
                        return true;
                    }
                    else if (member is Ai ai)
                    {
                        ai.SlotId = (byte)i;
                        ai.Team = team;
                        return true;
                    }
                }
            }
            var member1 = TryGetSlotDetail(roomId, slotId);
            if (member1 is Player player1)
            {
                player1.Team = team;
                return false;
            }
            else if (member1 is Ai ai1)
            {
                ai1.Team = team;
                return false;
            }
        }
        else if (team == 1)
        {
            for (int i = 4; i < 8; i++)
            {
                if (room.ChangeSlotId(slotId, (byte)i))
                {
                    var member = TryGetSlotDetail(roomId, (byte)i);
                    if (member is Player player)
                    {
                        player.SlotId = (byte)i;
                        player.Team = team;
                        return true;
                    }
                    else if (member is Ai ai)
                    {
                        ai.SlotId = (byte)i;
                        ai.Team = team;
                        return true;
                    }
                }
            }
            var member1 = TryGetSlotDetail(roomId, slotId);
            if (member1 is Player player1)
            {
                player1.Team = team;
                return false;
            }
            else if (member1 is Ai ai1)
            {
                ai1.Team = team;
                return false;
            }
        }
        return false;
    }

    public static GameRoom GetRoom(int roomId) =>
        _rooms.TryGetValue(roomId, out var room) ? room : null;

    public static ConcurrentDictionary<int, GameRoom> GetRoomsDict() => _rooms;
}

public class InvalidTrackEntry
{
    public string Time { get; set; }
    public string ID { get; set; }
    public string Name { get; set; }
}
