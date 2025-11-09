using System.Collections.Generic;
using System.IO;
using System.Linq;
using KartRider;
using Profile;
using RHOParser;

public static class TimeAttack
{
    public static List<string> MissionList = new List<string>();
    public static List<string> Competitive = new List<string>();
    public static Dictionary<uint, TrackData> TrackDictionary = new Dictionary<uint, TrackData>();

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
            return trackLevel.Level;
        }
        else
        {
            var initial = new TrainingMission { Track = Track, Level = 1 };
            TrainingMission.Add(initial);
            return initial.Level;
        }
        File.WriteAllText(filename.TrainingMission_LoadFile, JsonHelper.Serialize(TrainingMission));
    }
}

public class TrainingMission
{
    public uint Track { get; set; }
    public byte Level { get; set; }
}