using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KartRider.IO.Packet;
using KartRider.Logging;
using Profile;

namespace KartRider;

public class TrackRank
{
    public uint UserNO { get; set; }
    public string Nickname { get; set; }
    public ushort Kart { get; set; }
    public uint Time { get; set; }
}

public static class TrackRankData
{
	public static Dictionary<byte, string> SpeedTypes = new Dictionary<byte, string> { { 0, "普通" }, { 1, "快速" }, { 2, "高速" }, { 3, "慢速" }, { 4, "无限" }, { 5, "CGS" }, { 6, "真·无限" }, { 7, "标准" }, { 8, "标准" } };
	public static Dictionary<byte, string> GameTypes = new Dictionary<byte, string> { { 0, "个人赛" }, { 1, "组队赛" } };

	/// <summary>榜单排名只下发前十名，但文件里保留所有成绩。</summary>
	public const int TopRankCount = 10;

	public static string GetSpeedTypeName(byte speedType)
	{
		return SpeedTypes.TryGetValue(speedType, out string name) ? name : $"{speedType}";
	}

	public static string GetGameTypeName(byte gameType)
	{
		return GameTypes.TryGetValue(gameType, out string name) ? name : $"{gameType}";
	}

    /// <summary>新格式文件名：[赛道名][trackId][速度档][比赛类型]（例：[城镇 高速公路][village_R01][标准][个人赛]）</summary>
    public static string GetRankFileBase(uint track, byte speedType, byte gameType)
    {
        string id = track.ToString();
        if (RandomTrack.TrackList.TryGetValue(track, out Track info) && !string.IsNullOrWhiteSpace(info?.ID))
        {
            id = info.ID.Trim();
        }
        string name = RandomTrack.GetTrackName(track);
        if (string.IsNullOrWhiteSpace(name) || name == track.ToString())
        {
            name = id;
        }
        return $"[{Sanitize(name)}][{Sanitize(id)}][{Sanitize(GetSpeedTypeName(speedType))}][{Sanitize(GetGameTypeName(gameType))}]";
    }

    private static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "_";
        }
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            text = text.Replace(c, '_');
        }
        return text.Replace('/', '_').Replace('\\', '_').Trim();
    }

    private static string NewRankPath(uint track, byte speedType, byte gameType)
    {
        return Path.Combine(FileName.Load_TrackRank, $"{GetRankFileBase(track, speedType, gameType)}.json");
    }

    private static string OldRankPath(uint track, byte speedType, byte gameType)
    {
        return Path.Combine(FileName.Load_TrackRank, $"{track}_{speedType}_{gameType}.json");
    }

    private static List<TrackRank> ReadRanks(string path)
    {
        if (!File.Exists(path))
        {
            return new List<TrackRank>();
        }
        try
        {
            return JsonHelper.DeserializeNoBom<List<TrackRank>>(path) ?? new List<TrackRank>();
        }
        catch (Exception ex)
        {
            Log.Info($"[TrackRank] 读取 {Path.GetFileName(path)} 失败: {ex.Message}");
            return new List<TrackRank>();
        }
    }

    /// <summary>
    /// 读取成绩：新格式 + 旧格式（{track}_{speed}_{game}.json）一起读，重复条目合并。
    /// 返回按成绩升序排好的全量列表。
    /// </summary>
    public static List<TrackRank> LoadRanks(uint track, byte speedType, byte gameType, out bool hasLegacy)
    {
        if (!Directory.Exists(FileName.Load_TrackRank))
        {
            Directory.CreateDirectory(FileName.Load_TrackRank);
        }

        string newPath = NewRankPath(track, speedType, gameType);
        string oldPath = OldRankPath(track, speedType, gameType);
        hasLegacy = File.Exists(oldPath);

        var merged = new List<TrackRank>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (TrackRank rank in ReadRanks(newPath).Concat(ReadRanks(oldPath)))
        {
            if (rank == null)
            {
                continue;
            }
            string key = $"{rank.Nickname}|{rank.UserNO}|{rank.Kart}|{rank.Time}";
            if (seen.Add(key))
            {
                merged.Add(rank);
            }
        }
        return merged.OrderBy(r => r.Time).ToList();
    }

    /// <summary>保存为新格式，并删掉同赛道的旧格式文件。</summary>
    public static void SaveRanks(uint track, byte speedType, byte gameType, List<TrackRank> ranks)
    {
        if (!Directory.Exists(FileName.Load_TrackRank))
        {
            Directory.CreateDirectory(FileName.Load_TrackRank);
        }
        File.WriteAllText(NewRankPath(track, speedType, gameType), JsonHelper.Serialize(ranks));
        string oldPath = OldRankPath(track, speedType, gameType);
        if (File.Exists(oldPath))
        {
            File.Delete(oldPath);
            Log.Info($"[TrackRank] 已把旧格式 {Path.GetFileName(oldPath)} 合并进新格式并删除");
        }
    }

    public static void LoRpGetTrackRankPacket(SessionGroup Parent, uint track, byte SpeedType, byte GameType)
    {
        List<TrackRank> trackRanks = LoadRanks(track, SpeedType, GameType, out _)
            .Where(r => r != null && r.Time > 0)
            .Take(TopRankCount)
            .ToList();

        using (OutPacket outPacket = new OutPacket("LoRpGetTrackRankPacket"))
        {
            outPacket.WriteUInt(track);
            outPacket.WriteByte(SpeedType);
            outPacket.WriteByte(GameType);
            outPacket.WriteInt(trackRanks.Count);
            int index = 1;
            foreach (TrackRank rank in trackRanks)
            {
                outPacket.WriteInt(index++);
                outPacket.WriteUInt(rank.UserNO);
                outPacket.WriteString(rank.Nickname);
                outPacket.WriteInt(0);
                outPacket.WriteUShort(rank.Kart);
                outPacket.WriteUInt(rank.Time);
            }
            Parent.Client.Send(outPacket);
        }
    }

    public static void AddTrackRank(uint track, byte SpeedType, byte GameType, TrackRank newRank)
    {
        List<TrackRank> trackRanks = LoadRanks(track, SpeedType, GameType, out bool hasLegacy);

        if (!ProfileService.SettingConfig.SoloRank)
        {
            // 每人只保留最好一条：已有更快成绩就不再记录
            var best = trackRanks.Where(t => t.Nickname == newRank.Nickname).OrderBy(t => t.Time).FirstOrDefault();
            if (best != null)
            {
                if (newRank.Time >= best.Time)
                {
                    if (hasLegacy)
                    {
                        SaveRanks(track, SpeedType, GameType, trackRanks);
                    }
                    return;
                }
                trackRanks.RemoveAll(t => t.Nickname == newRank.Nickname);
            }
        }

        trackRanks.Add(newRank);
        // 文件里保留全部成绩，只有下发榜单时才截取前十
        trackRanks = trackRanks.Where(t => t != null).OrderBy(t => t.Time).ToList();
        int ranking = trackRanks.IndexOf(newRank) + 1;
        if (ranking >= 1 && ranking <= TopRankCount)
        {
			var timeSpan = GetTimeSpan(newRank.Time);
			using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
			{
				outPacket.WriteString($"{newRank.Nickname} / {GetSpeedTypeName(SpeedType)}[{GetGameTypeName(GameType)}] / 第{ranking}名 / {RandomTrack.GetTrackName(track)} / {timeSpan.min}:{timeSpan.sec}:{timeSpan.mil}");
                foreach (SessionGroup Session in ClientManager.GetClients())
                {
                    Session.Client.Send(outPacket);
                }
            }
        }
        SaveRanks(track, SpeedType, GameType, trackRanks);
    }

    /// <summary>
    /// 取某个玩家在某张图上**指定模式**的最好成绩（毫秒，0 = 没有成绩）。
    /// 只读 TrackRank/{track}_{speedType}_{gameType}.json 这一个文件，不做跨模式回退。
    /// </summary>
    public static uint GetBestTime(uint track, byte speedType, byte gameType, string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return 0;
        }

        uint best = 0;
        foreach (TrackRank rank in LoadRanks(track, speedType, gameType, out _))
        {
            if (rank == null || rank.Time == 0 || !string.Equals(rank.Nickname, nickname, StringComparison.Ordinal))
            {
                continue;
            }
            if (best == 0 || rank.Time < best)
            {
                best = rank.Time;
            }
        }
        return best;
    }

    public static (uint min, uint sec, uint mil) GetTimeSpan(uint time)
    {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds((long)time);
        uint min = (uint)timeSpan.Minutes;
        uint sec = (uint)timeSpan.Seconds;
        uint mil = (uint)timeSpan.Milliseconds;
        return (min, sec, mil);
    }
}
