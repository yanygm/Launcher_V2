using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KartRider.IO.Packet;
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

	public static string GetSpeedTypeName(byte speedType)
	{
		return SpeedTypes.TryGetValue(speedType, out string name) ? name : $"{speedType}";
	}

	public static string GetGameTypeName(byte gameType)
	{
		return GameTypes.TryGetValue(gameType, out string name) ? name : $"{gameType}";
	}

    public static void LoRpGetTrackRankPacket(SessionGroup Parent, uint track, byte SpeedType, byte GameType)
    {
        if (!Directory.Exists(FileName.Load_TrackRank))
        {
            Directory.CreateDirectory(FileName.Load_TrackRank);
        }
        string filePath = Path.Combine(FileName.Load_TrackRank, $"{track}_{SpeedType}_{GameType}.json");
        using (OutPacket outPacket = new OutPacket("LoRpGetTrackRankPacket"))
        {
            outPacket.WriteUInt(track);
            outPacket.WriteByte(SpeedType);
            outPacket.WriteByte(GameType);
            if (!File.Exists(filePath))
            {
                outPacket.WriteInt(0);
                Parent.Client.Send(outPacket);
                return;
            }
            else
            {
                List<TrackRank> trackRanks = JsonHelper.DeserializeNoBom<List<TrackRank>>(filePath);
                outPacket.WriteInt(trackRanks.Count);
                int index = 1;
                foreach (var rank in trackRanks)
                {
                    outPacket.WriteInt(index++);
                    outPacket.WriteUInt(rank.UserNO);
                    outPacket.WriteString(rank.Nickname);
                    outPacket.WriteInt(0);
                    outPacket.WriteUShort(rank.Kart);
                    outPacket.WriteUInt(rank.Time);
                }
                Parent.Client.Send(outPacket);
                return;
            }
        }
    }

    public static void AddTrackRank(uint track, byte SpeedType, byte GameType, TrackRank newRank)
    {
        if (!Directory.Exists(FileName.Load_TrackRank))
        {
            Directory.CreateDirectory(FileName.Load_TrackRank);
        }
        string filePath = Path.Combine(FileName.Load_TrackRank, $"{track}_{SpeedType}_{GameType}.json");
        List<TrackRank> trackRanks = new List<TrackRank>();
        if (File.Exists(filePath))
        {
            trackRanks = JsonHelper.DeserializeNoBom<List<TrackRank>>(filePath);
        }
        if (!ProfileService.SettingConfig.SoloRank)
        {
            var existingList = new List<TrackRank>();
            foreach (var group in trackRanks.GroupBy(t => t.Nickname))
            {
                existingList.Add(group.OrderBy(t => t.Time).First());
            }
            if (existingList.Count > 0)
            {
                File.WriteAllText(filePath, JsonHelper.Serialize(existingList));
            }
            var existing = trackRanks.FirstOrDefault(t => t.Nickname == newRank.Nickname);
            if (existing != null)
            {
                if (newRank.Time >= existing.Time)
                {
                    return;
                }
                trackRanks.Remove(existing);
            }
        }
        trackRanks.Add(newRank);
        trackRanks = trackRanks.OrderBy(t => t.Time).Take(10).ToList();
        if (trackRanks.Contains(newRank))
        {
			var timeSpan = GetTimeSpan(newRank.Time);
			int ranking = trackRanks.IndexOf(newRank) + 1;
			using (OutPacket outPacket = new OutPacket("PcSlaveNotice"))
			{
				outPacket.WriteString($"{newRank.Nickname} / {GetSpeedTypeName(SpeedType)}[{GetGameTypeName(GameType)}] / 第{ranking}名 / {RandomTrack.GetTrackName(track)} / {timeSpan.min}:{timeSpan.sec}:{timeSpan.mil}");
                foreach (SessionGroup Session in ClientManager.GetClients())
                {
                    Session.Client.Send(outPacket);
                }
            }
        }
        File.WriteAllText(filePath, JsonHelper.Serialize(trackRanks));
        return;
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
