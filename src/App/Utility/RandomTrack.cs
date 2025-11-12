using Launcher.App.ExcData;
using Launcher.App.KartSpec;
using Launcher.App.Rider;
using Launcher.Library.Utilities;
using System.Xml.Linq;

namespace Launcher.App.Utility
{
    public class RandomTrack
    {
        public static string GameType = "item";
        public static string SetRandomTrack = "all";
        public static string GameTrack = "village_R01";

        public static string GetTrackName(uint trackId)
        {
            if (KartExcData.track.ContainsKey(trackId))
            {
                return KartExcData.track[trackId];
            }
            else
            {
                return trackId.ToString();
            }
        }

        public static void SetGameType()
        {
            if (StartGameData.StartTimeAttack_RandomTrackGameType == 0)
            {
                GameType = "speed";
            }
            else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
            {
                GameType = "item";
            }
            SetRandomType();
        }

        public static void SetRandomType()
        {
            if (StartGameData.StartTimeAttack_Track == 0)
            {
                SetRandomTrack = "all";
            }
            else if (StartGameData.StartTimeAttack_Track == 1)
            {
                SetRandomTrack = "clubSpeed";
            }
            else if (StartGameData.StartTimeAttack_Track == 3)
            {
                SetRandomTrack = "hot1";
            }
            else if (StartGameData.StartTimeAttack_Track == 4)
            {
                SetRandomTrack = "hot2";
            }
            else if (StartGameData.StartTimeAttack_Track == 5)
            {
                SetRandomTrack = "hot3";
            }
            else if (StartGameData.StartTimeAttack_Track == 6)
            {
                SetRandomTrack = "hot4";
            }
            else if (StartGameData.StartTimeAttack_Track == 7)
            {
                SetRandomTrack = "hot5";
            }
            else if (StartGameData.StartTimeAttack_Track == 8)
            {
                SetRandomTrack = "new";
            }
            else if (StartGameData.StartTimeAttack_Track == 23)
            {
                SetRandomTrack = "crazy";
            }
            else if (StartGameData.StartTimeAttack_Track == 30)
            {
                SetRandomTrack = "reverse";
            }
            else if (StartGameData.StartTimeAttack_Track == 40)
            {
                SetRandomTrack = "speedAll";
            }
            else
            {
                SetRandomTrack = "Unknown";
            }
            RandomTrackSetList();
        }

        public static void RandomTrackSetList()
        {
            if (SetRandomTrack == "all" || SetRandomTrack == "speedAll")
            {
                Random random = new Random();
                if (FavoriteItem.FavoriteTrackList.Count > 0)
                {
                    int randomIndex = random.Next(FavoriteItem.FavoriteTrackList.Count);
                    StartGameData.StartTimeAttack_Track = uint.Parse(FavoriteItem.FavoriteTrackList[randomIndex][1]);
                }
                else
                {
                    int randomIndex = random.Next(KartExcData.track.Count);
                    GameTrack = KartExcData.track.ElementAt(randomIndex).Value;
                    StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }
            }
            else if (SetRandomTrack == "Unknown")
            {
                if (!KartExcData.track.ContainsKey(StartGameData.StartTimeAttack_Track))
                {
                    StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }
                Console.WriteLine("RandomTrack: {0} / {1} / {2}", GameType, SetRandomTrack, GameTrack);
            }
            else
            {
                XDocument doc = KartExcData.randomTrack;
                var TrackSet = doc.Descendants("RandomTrackSet")
                    .FirstOrDefault(rts => (string)rts.Attribute("gameType") == GameType && (string)rts.Attribute("randomType") == SetRandomTrack);
                if (TrackSet != null)
                {
                    Random random = new Random();
                    var randomTrack = TrackSet.Descendants("track").ElementAt(random.Next(TrackSet.Descendants("track").Count()));
                    GameTrack = (string)randomTrack.Attribute("id");
                    StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                }
                else
                {
                    var TrackList = doc.Descendants("RandomTrackList")
                        .FirstOrDefault(rts => (string)rts.Attribute("randomType") == SetRandomTrack);
                    if (TrackList != null)
                    {
                        Random random = new Random();
                        var randomTrack = TrackList.Descendants("track").ElementAt(random.Next(TrackList.Descendants("track").Count()));
                        GameTrack = (string)randomTrack.Attribute("id");
                        StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(GameTrack, 0);
                    }
                }
            }
            SpeedType.SpeedTypeData();
        }
    }
}
