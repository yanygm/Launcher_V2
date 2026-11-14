using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KartLibrary.File;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
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

    public bool IsTeamMode => GameModes.IsTeamGameType(GameType);

    /// <summary>夺旗赛红队（1）/蓝队（2）的夺旗数。</summary>
    public int RedFlagScore { get; set; }
    public int BlueFlagScore { get; set; }

    /// <summary>夺旗赛正赛打平后进入加时（GameControl state=6，比分清零，先夺旗者胜）。</summary>
    public bool FlagOvertime { get; set; }

    /// <summary>
    /// 正赛结束瞬间的总夺旗数（红+蓝）。官方结算记录 +15 的 short 写的就是这个值
    /// （抓包：正赛 3:1 → 4；加时赛 → 0），加时会清零比分但该值必须保留。
    /// </summary>
    public int FlagRegulationTotal { get; set; }

    /// <summary>夺旗赛/死斗 每个玩家的个人积分（夺旗数或击杀分）。</summary>
    public ConcurrentDictionary<int, int> ModeScores { get; } = new();

    /// <summary>死斗红队（1）/蓝队（2）的阵营积分。</summary>
    public int DeathMatchRedScore { get; set; }
    public int DeathMatchBlueScore { get; set; }

    /// <summary>死斗比分广播序号（GameDeathMatchSetScorePacket 第一个 int）。</summary>
    public int DeathMatchSequence { get; set; }

    /// <summary>正赛计时器是否已布置（每个房间每局一次）。</summary>
    public int ModeDeadlineArmed;

    /// <summary>
    /// 对局代际：每开一局 +1。
    /// 夺旗/死斗的到期定时器、结算定时器都是「一次性延时任务」，以前只认 room 对象，
    /// 上一局遗留的定时器会在下一局里正常触发并把新对局提前结束（表现为"3 分钟没到就突然结束"）。
    /// 定时器触发时必须比对代际，对不上就直接丢弃。
    /// </summary>
    public int RaceEpoch;

    /// <summary>房间名带「RP赛」创建出来的 RP 房（福不福：开赛前随机分配车辆）。</summary>
    public bool IsRpRoom { get; set; }

    /// <summary>本局分配到的车辆：昵称 → (车辆ID, 是否好运车)。每局开赛前重算，结算后清空。</summary>
    public ConcurrentDictionary<string, RpKartAssign> RpKarts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public int ModeScore(int playerId) =>
        ModeScores.TryGetValue(playerId, out int score) ? score : 0;

    /// <summary>累加玩家个人积分并返回新值。</summary>
    public int AddModeScore(int playerId, int delta)
    {
        return ModeScores.AddOrUpdate(playerId, delta, (_, old) => old + delta);
    }

    /// <summary>开赛前清空夺旗/死斗积分。</summary>
    public void ResetModeScores()
    {
        RedFlagScore = 0;
        BlueFlagScore = 0;
        FlagOvertime = false;
        FlagRegulationTotal = 0;
        DeathMatchRedScore = 0;
        DeathMatchBlueScore = 0;
        DeathMatchSequence = 0;
        ModeScores.Clear();
        Interlocked.Exchange(ref ModeDeadlineArmed, 0);
    }

    /// <summary>按阵营取夺旗/死斗积分。</summary>
    public int TeamModeScore(byte team)
    {
        if (GameModes.IsTeamFlagMode(GameType))
        {
            return team == 1 ? RedFlagScore : team == 2 ? BlueFlagScore : 0;
        }
        if (GameType == GameModes.DeathMatchTeam)
        {
            return team == 1 ? DeathMatchRedScore : team == 2 ? DeathMatchBlueScore : 0;
        }
        return 0;
    }

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

    // ===== 双人房：颜色 / 队伍 / 格子解耦后的辅助 =====

    /// <summary>双人房格位顺序：前 4 个 = 第 1 行，后 4 个 = 第 2 行（列 0..3）。</summary>
    public static readonly byte[] DuoSlots = { 0, 1, 4, 5, 2, 3, 6, 7 };

    /// <summary>(列, 行) → 槽位；行 0 = 第一行，行 1 = 第二行。</summary>
    public static byte DuoSlot(byte col, byte row) => DuoSlots[row * 4 + col];

    /// <summary>槽位 → 列号（0..3）。</summary>
    public static byte DuoColOf(byte slot) => (byte)(slot % 2 + slot / 4 * 2);

    /// <summary>槽位 → 行号（0 = 第一行，1 = 第二行）。</summary>
    public static byte DuoRowOf(byte slot) => (byte)(slot / 2 % 2);

    /// <summary>双人房可用列数（= 人数上限 / 2，1~4）。</summary>
    public int DuoColumns => Math.Max(1, Math.Min(4, DuoTeamCount));

    /// <summary>该格能否坐人：未锁定且没有成员。</summary>
    public bool DuoSlotFree(byte slot) => slot < 8 && !CloseSlotIds.Contains(slot) && _slots[slot] == null;

    /// <summary>某颜色当前玩家数（可按 ID 排除自己）。</summary>
    public int GetColorCount(byte color, int excludeId = -1)
    {
        int n = 0;
        foreach (RoomMember m in _slots)
        {
            if (m is Player p && p.Team == color && p.ID != excludeId)
            {
                n++;
            }
        }
        return n;
    }

    /// <summary>
    /// 颜色所属列的第一行槽位：列 = (颜色序号 - 1) % 可用列数。
    /// 8 人房 → 红0 / 黄1 / 绿4 / 蓝5；6 人房 → 0 / 1 / 4 / 0；4 人房 → 0 / 1 / 0 / 1。
    /// </summary>
    public byte DuoPreferredSlot(byte color)
    {
        if (color < 1) return 255;
        int col = (color - 1) % DuoColumns;
        return DuoSlot((byte)col, 0);
    }

    /// <summary>某列已有的颜色（0 = 该列还没有人）。</summary>
    public byte DuoColumnColor(byte col)
    {
        for (byte row = 0; row < 2; row++)
        {
            byte s = DuoSlot(col, row);
            if (_slots[s] is Player p && p.Team >= 1 && p.Team <= 4) return p.Team;
            if (_slots[s] is Ai a && a.Team >= 1 && a.Team <= 4) return a.Team;
        }
        return 0;
    }

    /// <summary>下一个颜色：优先没用过的（红黄绿蓝），都用过则取人数最少且 &lt; 2 的；无可用返回 0。</summary>
    public byte NextFreeColor()
    {
        for (byte c = 1; c <= 4; c++)
        {
            if (GetColorCount(c) == 0) return c;
        }
        byte best = 0;
        int bestN = int.MaxValue;
        for (byte c = 1; c <= 4; c++)
        {
            int n = GetColorCount(c);
            if (n < 2 && n < bestN) { bestN = n; best = c; }
        }
        return best;
    }

    /// <summary>下一个颜色（排除某个颜色，供 1v1 用）。</summary>
    public byte NextFreeColorExcept(byte except)
    {
        for (byte c = 1; c <= 4; c++)
        {
            if (c != except && GetColorCount(c) == 0) return c;
        }
        for (byte c = 1; c <= 4; c++)
        {
            if (c == except) continue;
            if (GetColorCount(c) < 2) return c;
        }
        return 0;
    }

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

    /// <summary>发出 GrCommandStartPacket 的时刻（Environment.TickCount64），4 秒缓冲从这里开始算。</summary>
    public long StartPacketTicks;

    /// <summary>开局等待期：每个玩家的 UDP 心跳次数（只统计 UDP，收到 2 次即通过）。</summary>
    public ConcurrentDictionary<string, int> Heartbeat { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 各客户端 GameControlPacket(state=0) 的原始载荷（不含包 ID）。
    /// 官方 state=1 就是把它回声回去：前 4 字节改成 1、再 4 字节写 StartTicks，其余原样。
    /// </summary>
    public ConcurrentDictionary<string, byte[]> StartControlTemplate { get; set; } = new(StringComparer.Ordinal);

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
    public byte TryAddDuoPlayer(string nickname, int playerType, SessionGroup client)
    {
        int columns = DuoColumns;

        if (columns == 1)
        {
            if (DuoSlotFree(DuoSlot(0, 0)))
            {
                byte first = NextFreeColor();
                if (first == 0) return 255;
                return AddDuoAt(nickname, playerType, client, DuoSlot(0, 0), first);
            }
            if (DuoSlotFree(DuoSlot(0, 1)))
            {
                byte second = NextFreeColorExcept(DuoColumnColor(0));
                if (second == 0) return 255;
                return AddDuoAt(nickname, playerType, client, DuoSlot(0, 1), second);
            }
            return 255;
        }

        // ① 第一行 / ② 第二行
        for (byte row = 0; row < 2; row++)
        {
            for (byte col = 0; col < columns; col++)
            {
                byte slot = DuoSlot(col, row);
                if (!DuoSlotFree(slot)) continue;

                byte color = DuoColumnColor(col);
                if (color == 0) color = NextFreeColor();
                else if (GetColorCount(color) >= 2) continue;
                if (color == 0) continue;

                return AddDuoAt(nickname, playerType, client, slot, color);
            }
        }

        // ③ 兜底：任意未锁定的空位（换色拆列后可能剩下）
        foreach (byte slot in DuoSlots)
        {
            if (!DuoSlotFree(slot)) continue;
            byte color = NextFreeColor();
            if (color == 0) break;
            return AddDuoAt(nickname, playerType, client, slot, color);
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

    /// <summary>槽位状态（下发给客户端的 PlayerType）：2=房主，3=已准备，4=观战，5=未准备，6=掉线未离开，7=AI。</summary>
    public const int PlayerTypeRoomMaster = 2;
    public const int PlayerTypeReady = 3;
    public const int PlayerTypeNotReady = 5;

    /// <summary>
    /// 按昵称在本房间定位真人（竞技位 0~7 与观战位 0~7 都找，不依赖 pmap）。
    ///
    /// 旧实现按 pmap 分流查槽位：档案反序列化不完整（Rider 为空）、改名卡改名、
    /// 换观战身份时都会查不到人 → 删除失败 → 玩家永远留在房间里（残留玩家）。
    /// </summary>
    public bool TryFindPlayer(string nickname, out byte slotId, out bool observer)
    {
        slotId = 255;
        observer = false;
        if (string.IsNullOrEmpty(nickname))
        {
            return false;
        }
        for (byte i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] is Player p && p.Nickname == nickname)
            {
                slotId = i;
                return true;
            }
        }
        for (byte i = 0; i < ObIDs.Length; i++)
        {
            if (ObIDs[i] is Player p && p.Nickname == nickname)
            {
                slotId = i;
                observer = true;
                return true;
            }
        }
        return false;
    }

    /// <summary>把 _IDs 里指向该成员的引用清空（ID 对不上时按引用再找一次，避免留下幽灵 ID）。</summary>
    private void ClearIdReference(RoomMember member)
    {
        for (int i = 0; i < _IDs.Length; i++)
        {
            if (ReferenceEquals(_IDs[i], member))
            {
                _IDs[i] = null;
                return;
            }
        }
    }

    // 移除指定格子的成员（如果是玩家，需检查是否触发删除）
    public bool RemoveMember(byte slotId, string nickname)
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            // 优先按昵称定位真实槽位：调用方给进来的 slotId 可能已经过期（换位/换队/档案异常），
            // 直接按 slotId 删会删不到人，玩家就残留在房间里。
            if (TryFindPlayer(nickname, out byte foundSlot, out bool observer))
            {
                return RemovePlayerAt(foundSlot, observer);
            }
            return false;
        }

        // nickname为空时处理AI移除
        RoomMember removedMember = _slots[slotId];
        if (removedMember == null)
            return false;

        if (removedMember is Ai ai)
        {
            ClearIdReference(ai);
            _slots[slotId] = null;
            MultyPlayer.GrSlotDataPacket(RoomId);
            return true;
        }
        return false;
    }

    /// <summary>按昵称移除真人（竞技位/观战位自动判别），用于离开房间、踢人、掉线清理。</summary>
    public bool RemoveMemberByNickname(string nickname)
    {
        return TryFindPlayer(nickname, out byte slotId, out bool observer)
               && RemovePlayerAt(slotId, observer);
    }

    /// <summary>删除真人占用的槽位（observer=true 时删观战位），必要时转移房主。</summary>
    private bool RemovePlayerAt(byte slotId, bool observer)
    {
        RoomMember[] arr = observer ? ObIDs : _slots;
        if (arr[slotId] is not Player player)
        {
            return false;
        }

        if (player.ID == RoomMaster)
        {
            // 房主离开：将房主转移给其他仍在房间的玩家（跳过离开者自身，
            // 否则 _IDs 中第一个 Player 就是离开的房主自己，导致房间失去房主）
            bool transferred = false;
            foreach (RoomMember member in _IDs)
            {
                if (member is Player p && !ReferenceEquals(p, player))
                {
                    RoomMaster = p.ID;
                    p.PlayerType = PlayerTypeRoomMaster;
                    transferred = true;
                    break;
                }
            }
            if (!transferred)
            {
                // 只有观战者时，房主转给观战者，避免房间彻底没有房主。
                // 但 pmap=590 是「纯观战」账号，没有开局权限，转给它会让房间再也开不了。
                foreach (RoomMember member in ObIDs)
                {
                    if (member is Player p && !ReferenceEquals(p, player))
                    {
                        uint obPmap = ProfileService.GetProfileConfig(p.Nickname)?.Rider?.pmap ?? 0;
                        if (obPmap == 590)
                        {
                            continue;   // 纯观战不继承房主（718 观战房主可以）
                        }
                        RoomMaster = p.ID;
                        p.PlayerType = PlayerTypeRoomMaster;
                        transferred = true;
                        break;
                    }
                }
            }
            if (!transferred)
            {
                // 房间内已无其他玩家，房间随后会被删除
                RoomMaster = player.ID;
            }
        }

        if (observer)
        {
            ObIDs[slotId] = null;
        }
        else
        {
            ClearIdReference(player);
            _slots[slotId] = null;

            // 房间已经没有赛车手了：把 pmap=590 的「纯观战」账号请出去。
            // 它们留在房间里会让 GetOBCount()>0，房间无法销毁，而且它们又不能开局，
            // 后续玩家进这个房间就永远开不了局。
            if (GetPlayerCount() == 0)
            {
                EvictPureObservers();
            }
        }

        RoomManager.RemoveRoom(this);
        return true;
    }

    /// <summary>把 pmap=590（纯观战）成员移出房间，并通知其客户端。</summary>
    private void EvictPureObservers()
    {
        foreach (RoomMember member in ObIDs.ToArray())
        {
            if (member is not Player ob || string.IsNullOrEmpty(ob.Nickname))
            {
                continue;
            }
            uint pmap = ProfileService.GetProfileConfig(ob.Nickname)?.Rider?.pmap ?? 0;
            if (pmap != 590)
            {
                continue;   // 718 观战房主保留
            }

            SessionGroup session = ob.Session;
            bool removed = RoomManager.RemovePlayerByNickname(RoomId, ob.Nickname);
            try
            {
                if (session?.Client != null)
                {
                    using (OutPacket outPacket = new OutPacket("ChLeaveRoomReplyPacket"))
                    {
                        outPacket.WriteBool(removed);
                        session.Client.Send(outPacket);
                    }
                }
            }
            catch
            {
            }
            try
            {
                using (OutPacket outPacket = new OutPacket("GrKickBroadcastPacket"))
                {
                    outPacket.WriteString(ob.Nickname);
                    MultyPlayer.BroadCast(RoomId, outPacket);
                }
            }
            catch
            {
            }
            KartRider.Logging.Log.Info($"[Room] 房间 {RoomId} 已无赛车手，移出纯观战 {ob.Nickname}（pmap=590）");
        }
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
