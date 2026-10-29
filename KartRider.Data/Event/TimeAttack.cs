using KartRider;
using Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public static class TimeAttack
{
    public static XDocument timeAttackMission = new XDocument();
    public static XDocument timeAttackCompetitive = new XDocument();
    public static XDocument timeAttackCompetitiveData = new XDocument();

    public static byte GetTrackLevel(string Nickname, uint track)
    {
        if (!FileName.FileNames.ContainsKey(Nickname))
        {
            FileName.Load(Nickname);
        }
        var filename = FileName.FileNames[Nickname];
        var TrainingMission = new List<TrainingMission>();
        if (File.Exists(filename.TrainingMission_LoadFile))
        {
            TrainingMission = JsonHelper.DeserializeNoBom<List<TrainingMission>>(filename.TrainingMission_LoadFile);
        }
        var trackLevel = TrainingMission.FirstOrDefault(item => item.Track == track);
        if (trackLevel != null)
        {
            return trackLevel.Level;
        }
        else
        {
            return 0;
        }
    }

    public static byte TrainingMission(string Nickname, uint Track)
    {
        if (!FileName.FileNames.ContainsKey(Nickname))
        {
            FileName.Load(Nickname);
        }
        var filename = FileName.FileNames[Nickname];
        var TrainingMission = new List<TrainingMission>();
        if (File.Exists(filename.TrainingMission_LoadFile))
        {
            TrainingMission = JsonHelper.DeserializeNoBom<List<TrainingMission>>(filename.TrainingMission_LoadFile);
        }
        var trackLevel = TrainingMission.FirstOrDefault(item => item.Track == Track);
        if (trackLevel != null)
        {
            trackLevel.Level++;
            File.WriteAllText(filename.TrainingMission_LoadFile, JsonHelper.Serialize(TrainingMission));
            return trackLevel.Level;
        }
        else
        {
            var initial = new TrainingMission { Track = Track, Level = 1 };
            TrainingMission.Add(initial);
            File.WriteAllText(filename.TrainingMission_LoadFile, JsonHelper.Serialize(TrainingMission));
            return initial.Level;
        }
    }

    public static List<string> GetMissionList()
    {
        DateTime now = DateTime.Now;

        // 解析当前流中的任务
        var currentMissionList = timeAttackMission.Descendants("duelMission")
            .Where(mission =>
            {
                string period = mission.Element("missionSet")?.Attribute("period")?.Value;
                if (string.IsNullOrEmpty(period)) return false;

                string[] timeRange = period.Split('~');
                if (timeRange.Length != 2) return false;

                if (!DateTime.TryParse(timeRange[0], out DateTime startTime)) return false;
                if (!DateTime.TryParse(timeRange[1], out DateTime endTime)) return false;

                return now >= startTime && now <= endTime;
            })
            .Select(mission => mission.Attribute("trackId")?.Value)
            .Where(trackId => !string.IsNullOrEmpty(trackId))
            .ToList();

        return currentMissionList;
    }

    public static List<string> GetCompetitive()
    {
        var extractor = new TrackIdExtractor();
        return extractor.GetCurrentWeekTrackIds(timeAttackCompetitive);
    }

    public static Dictionary<uint, TrackData> GetTrackDictionary()
    {
        CompleteTrackScoreCalculator calculator = new CompleteTrackScoreCalculator();
        return calculator.LoadFromXml(timeAttackCompetitiveData);
    }
}

public class TrainingMission
{
    public uint Track { get; set; }
    public byte Level { get; set; }
}
