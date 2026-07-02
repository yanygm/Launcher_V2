using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    // 检查房间是否有玩家，如果没有则删除房间
    public static void RemoveRoom(GameRoom room)
    {
        if (!(room.GetPlayerCount() > 0 || room.GetOBCount() > 0))
        {
            _rooms.TryRemove(room.RoomId, out _);
            RandomTrack.ClearUsedTracks($"[{room.RoomName}][{room.RoomId.ToString()}]");
        }
        else
        {
            MultyPlayer.GrSlotDataPacket(room.RoomId);
        }
    }

    // 获取指定页码的房间列表（每页10个）
    public static Dictionary<int, GameRoom> GetRoomsByPage(int pageIndex)
    {
        // 校验页码合法性（页码不能为负数）
        if (pageIndex < 0)
            throw new ArgumentException("页码索引不能小于0", nameof(pageIndex));

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
    public static byte AddPlayer(int roomId, string nickname, byte team, int playerType, SessionGroup client)
    {
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
        }
        return removed;
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
