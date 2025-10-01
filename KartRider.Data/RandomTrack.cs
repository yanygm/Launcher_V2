using System;
using KartRider.Common.Utilities;
using ExcData;
using RiderData;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace KartRider
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
				RandomTrack.GameType = "speed";
			}
			else if (StartGameData.StartTimeAttack_RandomTrackGameType == 1)
			{
				RandomTrack.GameType = "item";
			}
			RandomTrack.SetRandomType();
		}

		public static void SetRandomType()
		{
			if (StartGameData.StartTimeAttack_Track == 0)
			{
				RandomTrack.SetRandomTrack = "all";
			}
			else if (StartGameData.StartTimeAttack_Track == 1)
			{
				RandomTrack.SetRandomTrack = "clubSpeed";
			}
			else if (StartGameData.StartTimeAttack_Track == 3)
			{
				RandomTrack.SetRandomTrack = "hot1";
			}
			else if (StartGameData.StartTimeAttack_Track == 4)
			{
				RandomTrack.SetRandomTrack = "hot2";
			}
			else if (StartGameData.StartTimeAttack_Track == 5)
			{
				RandomTrack.SetRandomTrack = "hot3";
			}
			else if (StartGameData.StartTimeAttack_Track == 6)
			{
				RandomTrack.SetRandomTrack = "hot4";
			}
			else if (StartGameData.StartTimeAttack_Track == 7)
			{
				RandomTrack.SetRandomTrack = "hot5";
			}
			else if (StartGameData.StartTimeAttack_Track == 8)
			{
				RandomTrack.SetRandomTrack = "new";
			}
			else if (StartGameData.StartTimeAttack_Track == 23)
			{
				RandomTrack.SetRandomTrack = "crazy";
			}
			else if (StartGameData.StartTimeAttack_Track == 30)
			{
				RandomTrack.SetRandomTrack = "reverse";
			}
			else if (StartGameData.StartTimeAttack_Track == 40)
			{
				RandomTrack.SetRandomTrack = "speedAll";
			}
			else
			{
				RandomTrack.SetRandomTrack = "Unknown";
			}
			RandomTrack.RandomTrackSetList();
		}

		public static void RandomTrackSetList()
		{
			if (RandomTrack.SetRandomTrack == "all" || RandomTrack.SetRandomTrack == "speedAll")
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
					RandomTrack.GameTrack = KartExcData.track.ElementAt(randomIndex).Value;
					StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
				}
			}
			else if (RandomTrack.SetRandomTrack == "Unknown")
			{
				if (!KartExcData.track.ContainsKey(StartGameData.StartTimeAttack_Track))
				{
					StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
				}
				Console.WriteLine("RandomTrack: {0} / {1} / {2}", RandomTrack.GameType, RandomTrack.SetRandomTrack, RandomTrack.GameTrack);
			}
			else
			{
				XDocument doc = KartExcData.randomTrack;
				var TrackSet = doc.Descendants("RandomTrackSet")
					.FirstOrDefault(rts => (string)rts.Attribute("gameType") == RandomTrack.GameType && (string)rts.Attribute("randomType") == RandomTrack.SetRandomTrack);
				if (TrackSet != null)
				{
					Random random = new Random();
					var randomTrack = TrackSet.Descendants("track").ElementAt(random.Next(TrackSet.Descendants("track").Count()));
					RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
				}
				else
				{
					var TrackList = doc.Descendants("RandomTrackList")
						.FirstOrDefault(rts => (string)rts.Attribute("randomType") == RandomTrack.SetRandomTrack);
					if (TrackList != null)
					{
						Random random = new Random();
						var randomTrack = TrackList.Descendants("track").ElementAt(random.Next(TrackList.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
						StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
					}
				}
			}
			SpeedType.SpeedTypeData();
		}
	}
}
