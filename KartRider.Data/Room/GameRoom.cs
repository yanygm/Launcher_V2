using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KartLibrary.File;
using KartRider.Common.Utilities;
using Profile;

namespace KartRider;

public class GameRoom
{
    // 房间唯一ID（创建后不可修改）
    public int RoomId { get; }
    public string RoomName { get; set; } = "";
    public uint track { get; set; } = 0;
    public uint trackTemp { get; set; } = 0;
    public uint StartTicks { get; set; } = 0;
    public uint EndTicks { get; set; } = 0;
    public byte SpeedType { get; set; } = 0;
    public byte GameType { get; set; } = 0;

    /// <summary>是否双人组队（每队 N 人、多支队伍并存；内部 GameType 仍为 3/4）</summary>
    public bool DuoTeam { get; set; } = false;

    /// <summary>双人组队每队人数（默认 2）</summary>
    public byte DuoTeamSize { get; set; } = 2;

    /// <summary>房间人数上限（建房时记录，用于推导双人队伍数）</summary>
    public int MaxPlayers { get; set; } = 8;

    /// <summary>双人队伍数 = 人数上限 / 2（2人1队、4人2队、6人3队、8人4队）</summary>
    public int DuoTeamCount
    {
        get
        {
            int mp = MaxPlayers > 0 ? MaxPlayers : 8;
            // 2 人房 = 1 支 2 人队（第二人坐副位 2 号格，其余列全锁）
            return Math.Max(1, Math.Min(4, mp / 2));
        }
    }

    /// <summary>下发给客户端的真实模式号（双人组队为 67/68）</summary>
    public byte WireGameType { get; set; } = 0;

    /// <summary>实际写进封包的模式字节</summary>
    public byte ModeByte => WireGameType != 0 ? WireGameType : GameType;

    public bool IsTeamMode => GameType == 3 || GameType == 4;

    /// <summary>结算是否已触发（防止 AI 收尾倒计时与玩家冲线重复结算）</summary>
    public bool SettleTriggered { get; set; }

    /// <summary>组队加速槽（按队伍独立累计；普通组队 1/2，双人 1..4）</summary>
    public Dictionary<byte, float> TeamGauge { get; } = new Dictionary<byte, float>();

    public float AddTeamGauge(byte team, float delta)
    {
        TeamGauge.TryGetValue(team, out float gauge);
        gauge += delta;
        if (gauge > 1f) gauge = 1f;
        TeamGauge[team] = gauge;
        return gauge;
    }

    public void ResetTeamGauge(byte team) => TeamGauge[team] = 0f;

    /// <summary>队伍数量：双人按人数上限推导；普通组队固定 2 支</summary>
    public int TeamCount => DuoTeam ? DuoTeamCount : 2;

    /// <summary>该队主位（上排）</summary>
    public int DuoMainSlot(byte team) => DuoTopSlots[team - 1];

    /// <summary>该队副位（下排）</summary>
    public int DuoSubSlot(byte team) => DuoBottomSlots[team - 1];

    public bool IsValidTeam(byte team) => team >= 1 && team <= TeamCount;

    public byte WireTeam(byte team) => DuoTeam ? DuoTeamMode.ToWireTeam(team) : team;

    /// <summary>双人上排（主位）slot：1~4 队</summary>
    public static readonly byte[] DuoTopSlots = { 0, 1, 4, 5 };
    /// <summary>双人下排（副位）slot：1~4 队</summary>
    public static readonly byte[] DuoBottomSlots = { 2, 3, 6, 7 };

    /// <summary>槽位 → 队伍号（1~4）</summary>
    /// <summary>槽位 → 队伍号（只在实际队伍数范围内映射）</summary>
    public byte DuoTeamOfSlot(byte slot)
    {
        for (int i = 0; i < DuoTeamCount; i++)
        {
            if (DuoTopSlots[i] == slot || DuoBottomSlots[i] == slot) return (byte)(i + 1);
        }
        return 1;
    }
    /// <summary>建房时房主所在频道（-1=未知，按“所有频道可见”处理）。</summary>
    public int Channel { get; set; } = -1;
    public int RoomMaster { get; set; } = 0;
    public byte[] RoomData { get; set; } = new byte[32];
    public byte RandomTrackGameType { get; set; } = 0;
    public float redGauge { get; set; } = 0;
    public float blueGauge { get; set; } = 0;
    public bool Lock { get; set; } = false;
    public bool Started { get; set; } = false;
    public int StartedPlayerCount { get; set; } = 0; // 游戏开始时的真实玩家数（不包含AI），用于失效赛道判定
    public string LockPwd { get; set; } = "";
    public SlotIdSet CloseSlotIds { get; set; } = new SlotIdSet();
    public ConcurrentDictionary<int, uint> TimeData { get; set; } = new();
    public ConcurrentDictionary<int, int> Ranking { get; set; } = new();
    public ConcurrentDictionary<string, bool> Ready { get; set; } = new(StringComparer.Ordinal);

    /// <summary>开局等待期：置 true 后，客户端任意包都算“已就绪/存活”。</summary>
    public volatile bool ReadinessCollect;

    /// <summary>建房时选择的房间人数，等玩家真正进入房间后再锁格子。</summary>
    public int PendingMaxPlayers;

    /// <summary>是否已经按人数锁过格子（只做一次）。</summary>
    public bool RoomSizeApplied;

    /// <summary>本局每个玩家已获得的「天使/道具锁/闪电」数量（所有来源统一计数，上限 2）。</summary>
    public ConcurrentDictionary<int, int> RareItems { get; } = new();

    public int RareCount(int playerId)
    {
        return RareItems.TryGetValue(playerId, out int count) ? count : 0;
    }

    /// <summary>占用一个稀有道具名额；已满返回 false。</summary>
    public bool TryConsumeRare(int playerId, int max = 2)
    {
        int updated = RareItems.AddOrUpdate(playerId, 1, (_, old) => old + 1);
        if (updated > max)
        {
            RareItems[playerId] = max;
            return false;
        }
        return updated <= ItemRule.RareCapPerGame;
    }

    /// <summary>开局时清空稀有道具计数。</summary>
    public void ResetRareItems()
    {
        RareItems.Clear();
    }

    // 玩家实时赛道跟踪：id -> 坐标/圈数/累计里程（道具赛位置包走TCP逐连接填充）
    public ConcurrentDictionary<int, TrackPos> Tracks { get; set; } = new();

    /// <summary>房间状态互斥量：槽位增删、开局、结算等结构性变更都串行化在这个锁上。</summary>
    public readonly object Sync = new object();

    /// <summary>开局门闩：0=空闲，1=正在开局。</summary>
    public int StartGate;

    /// <summary>开局幂等门闩：每个房间每局只允许 arm 一次（多名玩家同时点开始）。</summary>
    public int StartArmed;

    private float[][] _aiSpecs = Array.Empty<float[]>();

    /// <summary>开局时生成一次 AI 参数，供本局所有客户端复用。</summary>
    public void PrepareAiSpecs()
    {
        int count = Math.Max(0, GetAiCount());
        var specs = new float[count][];
        for (int i = 0; i < count; i++)
        {
            float[] spec = null;
            try
            {
                List<float> raw = AI.GetAISpec(RandomTrackGameType);
                if (raw != null && raw.Count >= 6)
                {
                    spec = raw.ToArray();
                }
            }
            catch (Exception ex)
            {
                KartRider.Logging.Log.Warn($"[AI] 生成 AI 参数失败 room={RoomId}", ex);
            }
            specs[i] = spec ?? new float[6];
        }
        _aiSpecs = specs;
    }

    /// <summary>取第 index 个 AI 的参数（越界时返回全零，调用方不会空引用）。</summary>
    public float[] GetAiSpec(int index)
    {
        float[][] specs = _aiSpecs;
        if (specs == null || index < 0 || index >= specs.Length)
        {
            return new float[6];
        }
        return specs[index];
    }

    // 起点区判定（起跑线附近小区域）。
    // 旧条件 y<200 在该赛道全程不满足（y 最小 286），导致过线检测完全失效。
    private static bool IsStartZone(float x, float y) =>
        x >= 400f && x <= 520f && y >= 550f && y <= 650f;

    // 更新玩家位置跟踪：累计里程、检测过线圈数
    public void UpdateTrack(int id, float x, float y, uint ts1)
    {
        if (!Tracks.TryGetValue(id, out TrackPos t))
        {
            // 首次采样：默认在起点区（起跑线附近），圈数从 0 开始
            Tracks[id] = new TrackPos
            {
                X = x,
                Y = y,
                Ts = ts1,
                InStartZone = IsStartZone(x, y)
            };
            return;
        }

        // 起点区判定提前：起点区内（开局倒车/原地微调）不累计里程、不建立方向参考
        bool inStart = IsStartZone(x, y);

        // ts1 不连续（断线/重连/时钟域跳变）：不累计位移，防止坐标跳变污染里程；
        // 无时间戳包(ts1==0)沿用上次时间戳，视为连续采样，避免与有时间戳包交替上报时误判断线
        uint effTs = ts1 == 0 ? t.Ts : ts1;
        bool gap = !t.HasPrev || effTs < t.Ts || effTs - t.Ts > 30000;
        if (!gap)
        {
            float dx = x - t.X;
            float dy = y - t.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > 0.001f) // 忽略原地不动
            {
                if (!t.HasDir)
                {
                    if (!inStart)
                    {
                        // 已离开起点区的首段位移：累计并建立前进方向参考，
                        // 避免开局倒车把方向基准建反（倒车方向留在起点区内，被 inStart 拦截）
                        t.TotalDist += dist;
                        t.DirX = dx / dist;
                        t.DirY = dy / dist;
                        t.HasDir = true;
                    }
                }
                else
                {
                    // 投影到前进方向：正向位移（proj>=0）按投影计入里程并平滑跟随赛道转向；
                    // 反向位移（proj<0，掉头/发卡弯/倒车）不累计也不扣减，仅让方向跟随新位移。
                    // 旧逻辑反向时保持原方向且按投影扣减，发卡弯（>90° 转向）下方向卡死、
                    // 里程被反复扣减到 0（日志实测：真实路径 3294，旧逻辑只记 115）。
                    float proj = dx * t.DirX + dy * t.DirY;
                    if (proj >= 0f)
                    {
                        t.TotalDist += proj;
                        // 平滑更新前进方向（当前位移与旧方向各半归一），跟随赛道转弯
                        float mixX = t.DirX + dx / dist;
                        float mixY = t.DirY + dy / dist;
                        float ml = (float)Math.Sqrt(mixX * mixX + mixY * mixY);
                        if (ml > 0.001f)
                        {
                            t.DirX = mixX / ml;
                            t.DirY = mixY / ml;
                        }
                    }
                    else
                    {
                        t.DirX = dx / dist;
                        t.DirY = dy / dist;
                    }
                }
            }
        }

        // 圈数：离开起点区后再次进入起点区 = 过线，圈数 +1
        if (t.HasPrev && !t.InStartZone && inStart)
        {
            t.Lap++;
            if (t.DistAtLap > 0)
            {
                float lapLen = t.TotalDist - t.DistAtLap;
                if (lapLen > 200f) // 过滤异常短圈（误检）
                {
                    t.LapLength = lapLen; // 记录最近一圈长度，用于距离分档归一化
                }
            }
            t.DistAtLap = t.TotalDist;
        }

        t.HasPrev = true;
        t.InStartZone = inStart;
        t.X = x;
        t.Y = y;
        if (ts1 != 0)
        {
            t.Ts = ts1; // 无时间戳包不覆盖，保持上次有效时间戳
        }
    }

    // 结束时深拷贝快照，防止结算期间玩家断线
    public RoomMember[] SnapshotMembers { get; set; }

    // 8个格子（0-7）
    public RoomMember[] _slots = new RoomMember[8];
    public RoomMember[] _IDs = new RoomMember[8];

    public RoomMember[] ObIDs = new RoomMember[8];

    // 固定的 ObID 映射：槽位 0-7 对应 ID 8-15
    private static readonly int[] FixedObIds = { 8, 9, 10, 11, 12, 13, 14, 15 };

    // 构造函数：初始化房间ID（由外部传入唯一ID）
    public GameRoom(int roomId)
    {
        RoomId = roomId; // 房间ID创建后固定不变
    }

    public int GetOBCount()
    {
        int count = 0;
        foreach (var member in ObIDs)
        {
            if (member is Player) // 仅统计玩家类型
                count++;
        }
        return count;
    }

    // 统计当前房间内的玩家数量（不包含AI）
    public int GetPlayerCount(byte team = 0)
    {
        int count = 0;
        foreach (var member in _slots)
        {
            if (member is Player && team == 0) // 仅统计玩家类型
                count++;
            else if (member is Player player && player.Team == team)
                count++;
        }
        return count;
    }

    // 统计当前房间内的Ai数量
    public int GetAiCount()
    {
        int count = 0;
        foreach (var member in _slots)
        {
            if (member is Ai)
                count++;
        }
        return count;
    }

    // 统计当前房间内的玩家数量
    /// <summary>统计某队的参赛者数量（Player 与 Ai 都算），用于组队开局校验。</summary>
    public int GetTeamMemberCount(byte team)
    {
        int count = 0;
        foreach (RoomMember member in _slots)
        {
            if (member is Player p && p.Team == team)
            {
                count++;
            }
            else if (member is Ai a && a.Team == team)
            {
                count++;
            }
        }
        return count;
    }

    public int GetCount()
    {
        int count = 0;
        foreach (var member in _slots)
        {
            if (member is Player || member is Ai)
                count++;
        }
        return count;
    }

    // 清理并重整排名：删除已离开的玩家，排名靠后的往前移
    public void CleanupRankings()
    {
        // 获取房间内所有当前玩家的ID
        var currentPlayerIds = _IDs
            .Where(m => m is Player)
            .Select(m => ((Player)m).ID)
            .ToList();

        // 删除排名中已离开的玩家
        var keysToRemove = Ranking.Keys.Where(k => !currentPlayerIds.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            Ranking.TryRemove(key, out _);
        }

        // 重整排名，使其连续 (0, 1, 2, ...)
        var sortedRankings = Ranking.OrderBy(kvp => kvp.Value).ToList();
        for (int i = 0; i < sortedRankings.Count; i++)
        {
            Ranking[sortedRankings[i].Key] = i;
        }
    }

    public SlotStatus GetSlotStatus(byte slotId)
    {
        var member = _slots[slotId];
        if (member == null)
            return SlotStatus.Empty;       // 空位置
        if (member is Player)
            return SlotStatus.Player;      // 玩家
        if (member is Ai)
            return SlotStatus.Ai;          // AI
        if (member is Close)
            return SlotStatus.Close;
        return SlotStatus.Empty;           // 理论上不会走到这里（基类不会直接实例化）
    }

    // 辅助方法：获取指定位置的具体成员（可用于获取详细信息）
    public RoomMember GetSlotMember(byte slotId)
    {
        return _slots[slotId];
    }

    public RoomMember GetIdMember(int Id)
    {
        return _IDs[Id];
    }

    // 尝试添加玩家（成功后自动检查是否需要删除房间）
    /// <summary>双人组队入座：先补有主队员队伍的副位，再按 1~4 队顺序开主位。</summary>
    /// <summary>当前未被占用的最小颜色（1~4），默认红优先</summary>
    public byte DuoDefaultColor()
    {
        bool[] used = new bool[5];
        foreach (RoomMember m in _slots)
        {
            if (m is Player p && p.Team >= 1 && p.Team <= 4) used[p.Team] = true;
            else if (m is Ai a && a.Team >= 1 && a.Team <= 4) used[a.Team] = true;
        }
        for (byte c = 1; c <= 4; c++) if (!used[c]) return c;
        return 1;
    }

    /// <summary>双人入座：按队伍位顺序，先补副位（跟随主位颜色），再开新主位（默认最小空闲颜色）。</summary>
    public byte TryAddDuoPlayer(string nickname, int playerType, SessionGroup client)
    {
        for (int i = 0; i < 4; i++)
        {
            int main = DuoTopSlots[i], sub = DuoBottomSlots[i];
            if (CloseSlotIds.Contains((byte)main) || CloseSlotIds.Contains((byte)sub)) continue;
            if (_slots[main] is Player pm && _slots[sub] == null)
                return AddDuoAt(nickname, playerType, client, (byte)sub, pm.Team);
        }
        for (int i = 0; i < 4; i++)
        {
            int main = DuoTopSlots[i];
            if (CloseSlotIds.Contains((byte)main)) continue;
            if (_slots[main] == null)
                return AddDuoAt(nickname, playerType, client, (byte)main, DuoDefaultColor());
        }
        return 255;
    }

    private byte AddDuoAt(string nickname, int playerType, SessionGroup client, byte slot, byte team)
    {
        int id = Array.IndexOf(_IDs, null);
        if (id < 0) return 255;
        _slots[slot] = new Player
        {
            ID = id,
            SlotId = slot,
            Nickname = nickname,
            PlayerType = playerType,
            Team = team,
            Session = client
        };
        _IDs[id] = _slots[slot];
        return slot;
    }

    public byte TryAddPlayer(string nickname, byte team, int playerType, SessionGroup client)
    {
        uint pmap = ProfileService.GetProfileConfig(nickname).Rider.pmap;
        if (pmap == 718 || pmap == 590)
        {
            for (byte i = 0; i < 8; i++)
            {
                if (ObIDs[i] == null)
                {
                    ObIDs[i] = new Player
                    {
                        ID = FixedObIds[i],
                        SlotId = i,
                        Nickname = nickname,
                        PlayerType = 4,
                        Session = client
                    };
                    return i;
                }
            }
            return 255;
        }
        else if (team == 2)
        {
            for (byte i = 0; i < 4; i++)
            {
                if (_slots[i] == null)
                {
                    int id = Array.IndexOf(_IDs, null);
                    _slots[i] = new Player
                    {
                        ID = id,
                        SlotId = i,
                        Nickname = nickname,
                        PlayerType = playerType,
                        Team = team,
                        Session = client
                    };
                    _IDs[id] = _slots[i];
                    return i;
                }
            }
            return 255; // 房间已满
        }
        else if (team == 1)
        {
            for (byte i = 4; i < 8; i++)
            {
                if (_slots[i] == null)
                {
                    int id = Array.IndexOf(_IDs, null);
                    _slots[i] = new Player
                    {
                        ID = id,
                        SlotId = i,
                        Nickname = nickname,
                        PlayerType = playerType,
                        Team = team,
                        Session = client
                    };
                    _IDs[id] = _slots[i];
                    return i;
                }
            }
            return 255; // 房间已满
        }
        else if (team == 0)
        {
            for (byte i = 0; i < 8; i++)
            {
                if (_slots[i] == null)
                {
                    int id = Array.IndexOf(_IDs, null);
                    _slots[i] = new Player
                    {
                        ID = id,
                        SlotId = i,
                        Nickname = nickname,
                        PlayerType = playerType,
                        Team = team,
                        Session = client
                    };
                    _IDs[id] = _slots[i];
                    return i;
                }
            }
            return 255; // 房间已满
        }
        else
        {
            return 255; // 未知队伍
        }
    }

    // 移除指定格子的成员（如果是玩家，需检查是否触发删除）
    public bool RemoveMember(byte slotId, string nickname)
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            uint pmap = ProfileService.GetProfileConfig(nickname).Rider.pmap;
            if (pmap == 718 || pmap == 590)
            {
                if (ObIDs[slotId] is Player p1)
                {
                    if (p1.ID == RoomMaster)
                    {
                        // 房主离开：将房主转移给其他仍在房间的玩家（跳过离开者自身）
                        bool transferred = false;
                        foreach (RoomMember member in _IDs)
                        {
                            if (member is Player p2 && p2.ID != p1.ID)
                            {
                                RoomMaster = p2.ID;
                                p2.PlayerType = 2;
                                transferred = true;
                                break;
                            }
                        }
                        if (!transferred)
                        {
                            // 房间内已无其他玩家，房间随后会被删除
                            RoomMaster = p1.ID;
                        }
                    }
                    ObIDs[slotId] = null;
                    RoomManager.RemoveRoom(this);
                    return true;
                }
                return false;
            }
            else
            {
                RoomMember removedMember = _slots[slotId];
                if (removedMember == null)
                    return false; // 格子已为空

                if (removedMember is Player player)
                {
                    if (player.ID == RoomMaster)
                    {
                        // 房主离开：将房主转移给其他仍在房间的玩家（跳过离开者自身，
                        // 否则 _IDs 中第一个 Player 就是离开的房主自己，导致房间失去房主）
                        bool transferred = false;
                        foreach (RoomMember member in _IDs)
                        {
                            if (member is Player p && p.ID != player.ID)
                            {
                                RoomMaster = p.ID;
                                p.PlayerType = 2;
                                transferred = true;
                                break;
                            }
                        }
                        if (!transferred)
                        {
                            // 房间内已无其他玩家，房间随后会被删除
                            RoomMaster = player.ID;
                        }
                    }
                    _IDs[player.ID] = null;
                    _slots[slotId] = null;
                    RoomManager.RemoveRoom(this);
                    return true;
                }
            }
        }
        else
        {
            // nickname为空时处理AI移除
            RoomMember removedMember = _slots[slotId];
            if (removedMember == null)
                return false;

            if (removedMember is Ai ai)
            {
                _IDs[ai.ID] = null;
                _slots[slotId] = null;
                MultyPlayer.GrSlotDataPacket(RoomId);
                return true;
            }
        }
        return false;
    }

    // 其他方法：设置AI、获取格子信息等（沿用之前的逻辑，略）
    public byte TrySetAi(Ai aiData, byte team)
    {
        if (aiData == null)
            return 255;

        if (team == 2)
        {
            for (byte i = 0; i < 4; i++)
            {
                if (_slots[i] == null)
                {
                    int id = Array.IndexOf(_IDs, null);
                    aiData.ID = id;
                    aiData.Team = team;
                    _slots[i] = aiData;
                    _slots[i].SlotId = i;
                    _IDs[id] = _slots[i];
                    return i;
                }
            }
            return 255; // 房间已满
        }
        else if (team == 1)
        {
            for (byte i = 4; i < 8; i++)
            {
                if (_slots[i] == null)
                {
                    int id = Array.IndexOf(_IDs, null);
                    aiData.ID = id;
                    aiData.Team = team;
                    _slots[i] = aiData;
                    _slots[i].SlotId = i;
                    _IDs[id] = _slots[i];
                    return i;
                }
            }
            return 255; // 房间已满
        }
        else if (team == 0)
        {
            for (byte i = 0; i < 8; i++)
            {
                if (_slots[i] == null)
                {
                    int id = Array.IndexOf(_IDs, null);
                    aiData.ID = id;
                    aiData.Team = team;
                    _slots[i] = aiData;
                    _slots[i].SlotId = i;
                    _IDs[id] = _slots[i];
                    return i;
                }
            }
            return 255; // 房间已满
        }
        else
        {
            return 255; // 未知队伍
        }
    }

    public bool ChangeSlotId(byte slotId, byte newSlotId)
    {
        if (_slots[newSlotId] != null)
            return false;

        _slots[newSlotId] = _slots[slotId];
        _slots[slotId] = null;
        return true;
    }

    public bool AddClose(byte slotId, int ID)
    {
        if (_slots[slotId] != null)
            return false;

        if (_IDs[ID] != null)
            return false;

        Close close = new Close();
        close.ID = ID;
        close.PlayerType = 1;
        _slots[slotId] = close;
        _IDs[ID] = close;
        CloseSlotIds.Add(slotId);
        return true;
    }

    public bool RemoveClose(byte slotId, int ID)
    {
        if (_slots[slotId] == null)
            return false;

        if (_IDs[ID] == null)
            return false;

        if (_slots[slotId] is Close && _IDs[ID] is Close)
        {
            _slots[slotId] = null;
            _IDs[ID] = null;
            CloseSlotIds.Remove(slotId);
            return true;
        }
        return false;
    }
}

// 房间成员基类
public abstract class RoomMember
{
    public byte SlotId { get; set; } // 格子ID（0-7）
}

// 玩家类
public class Player : RoomMember
{
    public int ID { get; set; }
    public string Nickname { get; set; } // 玩家昵称
    public int PlayerType { get; set; } // 玩家类型
    public byte Team { get; set; }
    public SessionGroup Session { get; set; }
    public uint LastPacketReceived { get; set; } = 0;

    /// <summary>比赛中掉线：保留槽位并显示 out，下次开局时清理。</summary>
}

// AI类
public class Ai : RoomMember
{
    public int ID { get; set; }
    public short Character { get; set; }
    public short Rid { get; set; }
    public short Kart { get; set; }
    public short Balloon { get; set; }
    public short HeadBand { get; set; }
    public short Goggle { get; set; }
    public byte Team { get; set; }
}

public class Close : RoomMember
{
    public int ID { get; set; }
    public int PlayerType { get; set; }
}

// 玩家实时赛道跟踪数据：坐标、圈数、累计里程
public class TrackPos
{
    public float X { get; set; }          // 最近一次主向坐标 f1
    public float Y { get; set; }          // 最近一次横向坐标 f2
    public uint Ts { get; set; }          // 最近一次时间戳 ts1
    public bool HasPrev { get; set; }     // 是否有上一采样点
    public bool InStartZone { get; set; } // 上一采样是否在起点区
    public int Lap { get; set; }          // 已完成的圈数
    public float TotalDist { get; set; }  // 累计路径距离（里程）
    public float DistAtLap { get; set; }  // 过线时的累计距离（用于估算单圈长度）
    public float LapLength { get; set; }  // 最近一圈长度
    public bool HasDir { get; set; }      // 是否已建立前进方向参考
    public float DirX { get; set; }       // 前进方向单位向量 X
    public float DirY { get; set; }       // 前进方向单位向量 Y
}

public enum SlotStatus
{
    Empty,    // 空位置
    Player,   // 玩家
    Ai,       // AI
    Close     // 关闭
}
