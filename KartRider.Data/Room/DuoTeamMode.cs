using System;
using System.Linq;

namespace KartRider;

/// <summary>
/// 双人组队模式（Duo Team）。
///
/// 原有组队模式（GameType 3 竞速 / 4 道具）固定为「左 4 格 = 蓝队(2)、右 4 格 = 红队(1)」两支队伍；
/// 双人组队模式在其基础上把 8 个格子按「每队 DuoTeamSize 人」顺序切分成多支队伍
/// （默认每队 2 人 → 最多 4 支队伍），队伍号 1..N 直接作为 Team 字段下发给客户端，
/// 结算时每支队伍独立累计积分，取总分最高的队伍为获胜队。
///
/// ===== 关键：模式号来自官方抓包，不是自造值 =====
/// 官方服务端下发的双人组队房间模式号为 68（0x44），三处互相印证：
///   1) ChCreateRoomRequestPacket 里 gameType 字段是加密字节，抓包值 0xB1 = encrypt(68)
///   2) GrSessionDataPacket 里房间名的后两个明文字节为「68, 速度」，抓包值为 44 07
///   3) GameNextStagePacket 首字节 = 44，HUD 结算 / 房间列表里也是 0x44
/// 并且官方房间里客户端可以发 GrChangeTeamPacket 选 4 队、GameResultPacket 的获胜队字节为 04，
/// 说明客户端原生支持 1~4 队，无需把队伍号压缩成 1/2。
///
/// 因此房间内部仍用 GameType 3/4 复用既有逻辑，但下发给客户端时
/// 房间模式字段（GrSessionDataPacket / 房间列表 / ChJoinRoomReplyPacket / GameNextStagePacket …）
/// 一律使用 GameRoom.WireGameType = 68，客户端才会渲染成多队伍房间。
///
/// 启用方式（任选其一即可）：
/// 1. 频道方式（推荐）：客户端与服务端共用的 channel.xml 中，双人组队频道的 gameType 写 68
///    &lt;Channel name="双人组队" createSpeed="7" gameType="68"/&gt;
/// 2. 服务端追加频道：channel.xml 未配置时，服务端会在频道列表末尾自动追加该频道
///    （见 GameSupport.EnsureDuoTeamChannels，可用 settings.json 的 DuoTeamChannel 关闭）。
/// 3. 房间名方式：房间名包含「双人组队」或「[2人]」时也会启用双人组队（便于快速测试）。
/// </summary>
public static class DuoTeamMode
{
    /// <summary>双人组队竞速（官方客户端 ClientRes 枚举 kSpeedDuo = 68）</summary>
    public const byte SpeedGameType = 68;

    /// <summary>双人组队道具（官方客户端 ClientRes 枚举 kItemDuo = 67）</summary>
    public const byte ItemGameType = 67;

    /// <summary>双人组队（竞速）模式号，保留旧名字便于老代码引用</summary>
    public const byte GameType = SpeedGameType;

    /// <summary>兼容早期自造值：5 = 双人组队竞速</summary>
    public const byte LegacySpeedGameType = 5;

    /// <summary>兼容早期自造值：6 = 双人组队道具</summary>
    public const byte LegacyItemGameType = 6;

    /// <summary>每队默认人数</summary>
    public const byte DefaultTeamSize = 2;

    /// <summary>每队人数上限（等于 4 时即原组队模式）</summary>
    public const byte MaxTeamSize = 4;

    /// <summary>
    /// 客户端自带的队伍配色数量：红(1)、蓝(2)、绿(3)、黄(4)。
    /// 客户端资源里只有这 4 套队伍贴图（stage/common 下的 red/blue/green/yellowteam_new），
    /// 第 5 支队伍起没有对应颜色，因此下发队伍号时按 4 循环，避免客户端拿到越界值。
    /// </summary>
    public const byte WireTeamCount = 4;

    /// <summary>内部队伍号（1..8）→ 下发客户端用的队伍号（1..4 循环）</summary>
    public static byte ToWireTeam(int team)
    {
        if (team < 1)
            return 1;
        return (byte)(((team - 1) % WireTeamCount) + 1);
    }

    /// <summary>房间名关键字（任一包含即启用双人组队）</summary>
    private static readonly string[] RoomNameKeywords = { "双人组队", "雙人組隊", "[2人]" };

    /// <summary>是否为双人组队专属的 gameType（官方 68 竞速 / 67 道具，兼容早期的 5/6）</summary>
    public static bool IsDuoGameType(byte gameType) =>
        gameType == SpeedGameType || gameType == ItemGameType
        || gameType == LegacySpeedGameType || gameType == LegacyItemGameType;

    /// <summary>
    /// 把双人模式的 gameType 归一化回组队模式的 3/4。
    /// 官方客户端枚举：kSpeedDuo = 68（竞速双人）、kItemDuo = 67（道具双人），
    /// 因此 68 归 3（竞速组队）、67 归 4（道具组队）；早期自造的 5 归 3、6 归 4。
    /// </summary>
    public static byte ToBaseGameType(byte gameType) => gameType switch
    {
        SpeedGameType => 3,
        ItemGameType => 4,
        LegacySpeedGameType => 3,
        LegacyItemGameType => 4,
        _ => gameType,
    };

    /// <summary>基础模式反推下发用的模式号：竞速组队 → 68，道具组队 → 67</summary>
    public static byte ToWireGameType(byte baseGameType) => baseGameType switch
    {
        3 => SpeedGameType,
        4 => ItemGameType,
        _ => baseGameType,
    };

    /// <summary>规范化每队人数：只接受 1~4，超出范围回退到默认值 2</summary>
    public static byte NormalizeTeamSize(byte size) =>
        size >= 1 && size <= MaxTeamSize ? size : DefaultTeamSize;

    /// <summary>房间名是否包含双人组队关键字</summary>
    public static bool IsDuoRoomName(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
            return false;
        return RoomNameKeywords.Any(k => roomName.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
