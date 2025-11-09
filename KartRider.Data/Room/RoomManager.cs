using System;
using System.Collections.Generic;
using System.Linq;

namespace KartRider;

public static class RoomManager
{
    // 存储所有房间（键：房间ID，值：房间实例）
    public static Dictionary<int, GameRoom> _rooms = new Dictionary<int, GameRoom>();
    private static Dictionary<string, int> _playerRoomMap = new Dictionary<string, int>();
    private static int _nextRoomId = 1; // 下一个可用的房间ID（自增确保唯一）
    private const int PageSize = 10;  // 每页10个

    // 创建新房间（返回房间ID）
    public static int CreateRoom()
    {
        int roomId = _nextRoomId++;
        _rooms.Add(roomId, new GameRoom(roomId));
        return roomId;
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
            .OrderBy(kvp => kvp.Key)  // 按房间ID排序，可替换为其他排序字段
            .Skip(pageIndex * PageSize)  // 直接用页码索引计算跳过的数量（无需减1）
            .Take(PageSize)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return pagedItems;
    }

    // 尝试向房间添加玩家
    public static bool AddPlayer(int roomId, string nickname, byte team, int playerType)
    {
        var room = GetRoom(roomId);

        if (room == null || string.IsNullOrEmpty(nickname))
            return false;

        // 去重：严格区分大小写
        if (_playerRoomMap.ContainsKey(nickname))
            return false;

        // 存储原始昵称
        byte added = room.TryAddPlayer(nickname, team, playerType);
        if (added != 255)
        {
            _playerRoomMap[nickname] = roomId;
            return true;
        }
        return false;
    }

    public static int GetPlayerSlotId(int roomId, string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return -1;

        lock (_rooms)
        {
            // 1. 检查房间是否存在
            if (!_rooms.TryGetValue(roomId, out var room))
                return -1;

            // 2. 遍历房间所有格子，查找目标玩家
            for (byte slotId = 0; slotId < 8; slotId++)
            {
                var member = room.GetSlotMember(slotId);
                // 匹配玩家类型且昵称一致
                if (member is Player player && player.Nickname == nickname)
                {
                    return slotId; // 返回玩家所在的格子ID
                }
            }
        }
        // 未找到玩家（或玩家不在该房间）
        return -1;
    }

    public static int TryGetRoomId(string nickname)
    {
        int roomId = -1;
        if (string.IsNullOrEmpty(nickname))
            return roomId;

        // 严格匹配大小写（“Zhang”和“zhang”会返回不同结果）
        return _playerRoomMap.TryGetValue(nickname, out roomId) ? roomId : -1;
    }

    // 从房间移除成员（如果是玩家且移除后无玩家，则删除房间）
    public static bool RemovePlayer(int roomId, byte slotId)
    {
        var room = GetRoom(roomId);
        if (room == null)
            return false;

        var member = room.GetSlot(slotId);
        string originalNick = (member as Player)?.Nickname;

        bool removed = room.RemoveMember(slotId, out bool shouldDeleteRoom);
        if (removed)
        {
            if (!string.IsNullOrEmpty(originalNick))
            {
                _playerRoomMap.Remove(originalNick); // 区分大小写删除
            }
            if (shouldDeleteRoom)
            {
                _rooms.Remove(roomId);
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

    public static object GetPlayerID(int roomId, int id)
    {
        lock (_rooms)
        {
            // 1. 先检查房间是否存在
            if (!_rooms.TryGetValue(roomId, out var room))
                return null;

            // 2. 遍历房间的8个格子，查找昵称匹配的玩家
            for (byte slotId = 0; slotId < 8; slotId++)
            {
                var member = room.GetSlotMember(slotId);
                // 严格匹配昵称（含大小写）
                if (member is Player player && player.ID == id)
                {
                    return player;
                }
                else if (member is Ai ai && ai.ID == id)
                {
                    return ai;
                }
            }
        }
        return null; // 未找到玩家
    }

    public static Player GetPlayer(int roomId, string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return null;

        lock (_rooms)
        {
            // 1. 先检查房间是否存在
            if (!_rooms.TryGetValue(roomId, out var room))
                return null;

            // 2. 遍历房间的8个格子，查找昵称匹配的玩家
            for (byte slotId = 0; slotId < 8; slotId++)
            {
                var member = room.GetSlotMember(slotId);
                // 严格匹配昵称（含大小写）
                if (member is Player player && player.Nickname == nickname)
                {
                    return player;
                }
            }
        }
        return null; // 未找到玩家
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
                var status = TryGetSlotStatus(roomId, (byte)i);
                if (status == SlotStatus.Empty)
                {
                    var Object = TryGetSlotDetail(roomId, (byte)i);
                    if (Object is Player player)
                    {
                        player.SlotId = (byte)i;
                        player.Team = team;
                        return true;
                    }
                    if (Object is Ai ai)
                    {
                        ai.SlotId = (byte)i;
                        ai.Team = team;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        else if (team == 1)
        {
            for (int i = 4; i < 8; i++)
            {
                var status = TryGetSlotStatus(roomId, (byte)i);
                if (status == SlotStatus.Empty)
                {
                    var Object = TryGetSlotDetail(roomId, (byte)i);
                    if (Object is Player player)
                    {
                        player.SlotId = (byte)i;
                        player.Team = team;
                        return true;
                    }
                    if (Object is Ai ai)
                    {
                        ai.SlotId = (byte)i;
                        ai.Team = team;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        return false;
    }

    public static GameRoom GetRoom(int roomId) => _rooms.TryGetValue(roomId, out var room) ? room : null;
}
