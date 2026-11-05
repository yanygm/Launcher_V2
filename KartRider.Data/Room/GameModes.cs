namespace KartRider;

/// <summary>
/// 多人模式判定与规则。
///
/// 模式号来自客户端 channel.xml / ChCreateRoomRequestPacket（抓包核对）：
///   7  - 夺旗赛（团队）
///   8  - 夺旗赛（个人）
///   17 - 挑战者模式（4 人同队）
///   57 - 死斗（个人）
///   58 - 死斗（团队）
///
/// 夺旗赛/死斗不是按冲线名次结算，而是按阵营积分结算：
///   - 夺旗赛：GameFlagRequestAddScorePacket(byte team) → GameFlagSetScorePacket(uint red, uint blue)
///   - 死斗：  GameDeathMatchRequestAddScorePacket(byte team) → GameDeathMatchSetScorePacket(int seq, int team, int score)
///   - 组队加速槽：GameTeamBoosterRequestAddGaugePacket(byte team, float value) → GameTeamBoosterSetGaugePacket(byte team, float gauge)
///
/// 抓包实测的正赛时长：夺旗赛 180000ms、死斗 150000ms（结算包里的 time 字段）。
/// </summary>
public static class GameModes
{
    public const byte FlagTeam = 7;
    public const byte FlagIndividual = 8;
    public const byte Challenger = 17;
    public const byte DeathMatchIndividual = 57;
    public const byte DeathMatchTeam = 58;

    /// <summary>夺旗赛正赛时长（抓包：结算 time = 180000）。</summary>
    public const int FlagRaceDurationMs = 180_000;

    /// <summary>死斗正赛时长（抓包：结算 time = 150000）。</summary>
    public const int DeathMatchDurationMs = 150_000;

    /// <summary>挑战者模式房间人数上限（抓包：4 人全部坐在蓝队 0~3 号格）。</summary>
    public const int ChallengerMaxPlayers = 4;

    public static bool IsFlagMode(byte gameType) =>
        gameType == FlagTeam || gameType == FlagIndividual;

    /// <summary>团队夺旗（红蓝两队各自累计夺旗数）。</summary>
    public static bool IsTeamFlagMode(byte gameType) => gameType == FlagTeam;

    public static bool IsDeathMatch(byte gameType) =>
        gameType == DeathMatchIndividual || gameType == DeathMatchTeam;

    public static bool IsChallenger(byte gameType) => gameType == Challenger;

    /// <summary>按红/蓝两队分席位（4+4 格）的模式。</summary>
    public static bool IsTeamGameType(byte gameType) =>
        gameType == 3 || gameType == 4 || gameType == FlagTeam ||
        gameType == DeathMatchTeam;

    /// <summary>按阵营积分结算的模式（没有冲线名次）。</summary>
    public static bool IsScoreMode(byte gameType) =>
        IsFlagMode(gameType) || gameType == DeathMatchTeam;

    /// <summary>会使用道具箱的模式（决定 AI 车辆池 / 随机赛道池）。</summary>
    public static bool IsItemEnabled(byte gameType) =>
        gameType == 2 || gameType == 4 || IsFlagMode(gameType) || IsDeathMatch(gameType);

    /// <summary>正赛时长；0 表示按冲线结算。</summary>
    public static int RaceDurationMs(byte gameType)
    {
        if (IsFlagMode(gameType))
        {
            return FlagRaceDurationMs;
        }
        if (IsDeathMatch(gameType))
        {
            return DeathMatchDurationMs;
        }
        return 0;
    }

    /// <summary>开启挑战者/AI 指令模式的房间：开局下发 GrCommandStartAiPacket。</summary>
    public static bool UsesAiStartPacket(byte gameType) => IsChallenger(gameType);

    /// <summary>房间人数上限修正：挑战者模式固定 4 人。</summary>
    public static int ClampMaxPlayers(byte gameType, int maxPlayers)
    {
        if (IsChallenger(gameType))
        {
            return ChallengerMaxPlayers;
        }
        return maxPlayers;
    }

    /// <summary>入座时的默认队伍：挑战者模式全员蓝队（2）；团队模式按人数平衡。</summary>
    public static byte DefaultTeam(byte gameType, int team1Count, int team2Count)
    {
        if (IsChallenger(gameType))
        {
            return 2;
        }
        if (!IsTeamGameType(gameType))
        {
            return 0;
        }
        return team2Count <= team1Count ? (byte)2 : (byte)1;
    }

    /// <summary>模式对应的赛道类型（客户端 track@zz.xml 的 gameType）；null 表示不限定。</summary>
    public static string TrackGameType(byte gameType)
    {
        if (IsFlagMode(gameType))
        {
            return "flag";
        }
        if (IsDeathMatch(gameType))
        {
            return "death";
        }
        return null;
    }

    /// <summary>模式显示名（日志用）。</summary>
    public static string Name(byte gameType) => gameType switch
    {
        FlagTeam => "夺旗赛(团队)",
        FlagIndividual => "夺旗赛(个人)",
        Challenger => "挑战者模式",
        DeathMatchIndividual => "死斗(个人)",
        DeathMatchTeam => "死斗(团队)",
        _ => $"模式{gameType}"
    };
}
