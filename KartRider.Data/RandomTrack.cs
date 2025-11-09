using ExcData;
using KartRider.Common.Utilities;
using RiderData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace KartRider
{
    public class RandomTrack
    {
        public static Dictionary<uint, string> track = new Dictionary<uint, string>();
        public static XDocument randomTrack = new XDocument();

        public static string GameTrack = "village_R01";

        public static string GetTrackName(uint trackId)
        {
            if (track.ContainsKey(trackId))
            {
                return track[trackId];
            }
            else
            {
                return trackId.ToString();
            }
        }

        public static uint GetRandomTrack(string Nickname, byte GameType, uint Track)
        {
            string RandomTrackGameType = "speed";
            string RandomTrackSetRandomTrack = "all";
            string RandomTrackGameTrack = "village_R01";

            if (GameType == 0)
            {
                RandomTrackGameType = "speed";
            }
            else if (GameType == 1)
            {
                RandomTrackGameType = "item";
            }

            if (Track == 0)
            {
                RandomTrackSetRandomTrack = "all";
            }
            else if (Track == 1)
            {
                RandomTrackSetRandomTrack = "clubSpeed";
            }
            else if (Track == 3)
            {
                RandomTrackSetRandomTrack = "hot1";
            }
            else if (Track == 4)
            {
                RandomTrackSetRandomTrack = "hot2";
            }
            else if (Track == 5)
            {
                RandomTrackSetRandomTrack = "hot3";
            }
            else if (Track == 6)
            {
                RandomTrackSetRandomTrack = "hot4";
            }
            else if (Track == 7)
            {
                RandomTrackSetRandomTrack = "hot5";
            }
            else if (Track == 8)
            {
                RandomTrackSetRandomTrack = "new";
            }
            else if (Track == 23)
            {
                RandomTrackSetRandomTrack = "crazy";
            }
            else if (Track == 30)
            {
                RandomTrackSetRandomTrack = "reverse";
            }
            else if (Track == 40)
            {
                RandomTrackSetRandomTrack = "speedAll";
            }
            else
            {
                RandomTrackSetRandomTrack = "Unknown";
            }

            if (RandomTrackSetRandomTrack == "all" || RandomTrackSetRandomTrack == "speedAll")
            {
                Random random = new Random();
                List<uint> availableTracks;
                if (FavoriteItem.FavoriteTrackLists.TryGetValue(Nickname, out var favoriteTracks)
                    && favoriteTracks.GetAllTracks().Count > 0)
                {
                    availableTracks = favoriteTracks.GetAllTracks();
                    return availableTracks[random.Next(availableTracks.Count)];
                }
                else
                {
                    int randomIndex = random.Next(track.Count);
                    var selectedTrack = track.ElementAt(randomIndex).Value;
                    return Adler32Helper.GenerateAdler32_UNICODE(selectedTrack, 0);
                }
            }
            else if (RandomTrackSetRandomTrack == "Unknown")
            {
                if (track.ContainsKey(Track))
                {
                    return Track;
                }
                else
                {
                    return Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
                }
                Console.WriteLine("RandomTrack: {0} / {1} / {2}", RandomTrackGameType, RandomTrackSetRandomTrack, RandomTrack.GameTrack);
            }
            else
            {
                XDocument doc = randomTrack;
                var TrackSet = doc.Descendants("RandomTrackSet")
                    .FirstOrDefault(rts => (string)rts.Attribute("gameType") == RandomTrackGameType && (string)rts.Attribute("randomType") == RandomTrackSetRandomTrack);
                if (TrackSet != null)
                {
                    Random random = new Random();
                    var randomTrack = TrackSet.Descendants("track").ElementAt(random.Next(TrackSet.Descendants("track").Count()));
                    RandomTrackGameTrack = (string)randomTrack.Attribute("id");
                }
                else
                {
                    var TrackList = doc.Descendants("RandomTrackList")
                        .FirstOrDefault(rts => (string)rts.Attribute("randomType") == RandomTrackSetRandomTrack);
                    if (TrackList != null)
                    {
                        Random random = new Random();
                        var randomTrack = TrackList.Descendants("track").ElementAt(random.Next(TrackList.Descendants("track").Count()));
                        RandomTrackGameTrack = (string)randomTrack.Attribute("id");
                    }
                }
                return Adler32Helper.GenerateAdler32_UNICODE(RandomTrackGameTrack, 0);
            }
        }
    }
}
