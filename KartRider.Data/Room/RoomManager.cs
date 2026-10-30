using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Profile;

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
        int roomId = Interlocked.Increment(ref _nextRoomId) - 1;
        _rooms.TryAdd(roomId, new GameRoom(roomId));
        return roomId;
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

    // 尝试向房间添加玩家
    /// <summary>双人组队入座（含 playerRoomMap 注册）。</summary>
    public static byte AddDuoPlayer(int roomId, string nickname, int playerType, SessionGroup client)
    {
        var room = GetRoom(roomId);
        if (room == null || string.IsNullOrEmpty(nickname))
            return 255;
        // 可能残留上一次房间的映射（异常退出/换房），先清掉再注册
        _playerRoomMap.TryRemove(nickname, out _);
        if (!_playerRoomMap.TryAdd(nickname, roomId))
        {
            Console.WriteLine("[DuoAdd] {0} 注册房间 {1} 失败", nickname, roomId);
            return 255;
        }
        if (!Profile.FileName.FileNames.ContainsKey(nickname))
        {
            Profile.FileName.Load(nickname);
        }
        byte added = room.TryAddDuoPlayer(nickname, playerType, client);
        if (added != 255)
        {
            return added;
        }
        Console.WriteLine("[DuoAdd] {0} 入座失败（room={1}, slots full?）", nickname, roomId);
        _playerRoomMap.TryRemove(nickname, out _);
        return 255;
    }

    public static byte AddPlayer(int roomId, string nickname, byte team, int playerType, SessionGroup client)
    {
        // 组队房间：未指定队伍时自动平衡（红蓝人数差不超过 1）
        if (team == 0)
        {
            var targetRoom = GetRoom(roomId);
            if (targetRoom != null && (targetRoom.GameType == 3 || targetRoom.GameType == 4))
            {
                int t1 = targetRoom.GetPlayerCount(1);
                int t2 = targetRoom.GetPlayerCount(2);
                team = t2 <= t1 ? (byte)2 : (byte)1;
            }
        }

        var room = GetRoom(roomId);

        if (room == null || string.IsNullOrEmpty(nickname))
            return 255;

        // 去重：严格区分大小写，TryAdd 原子操作
        if (!_playerRoomMap.TryAdd(nickname, roomId))
            return 255;

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
            // 2. 遍历房间的8个格子，查找昵称匹配的玩家
            foreach (var member in room._slots)
            {
                // 严格匹配昵称（含大小写）
                if (member is Player player && player.Nickname == nickname)
                {
                    return player;
                }
            }
            return null;
        }
    }

    // 双人组队换队：落到目标队的主位（上）；有人则副位（下）
    public static bool ChangeDuoTeam(int roomId, byte slotId, byte team)
    {
        if (!_rooms.TryGetValue(roomId, out var room) || !room.DuoTeam)
            return false;
        // 换队 = 只换颜色（队伍号 1~4），格子位置不变
        if (team < 1 || team > 4)
        {
            return false;
        }

        var member = TryGetSlotDetail(roomId, slotId) as RoomMember;
        if (member == null)
            return false;

        if (member is Player p0) { p0.Team = team; return true; }
        if (member is Ai a0) { a0.Team = team; return true; }
        return false;
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
