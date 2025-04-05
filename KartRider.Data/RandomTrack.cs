using System;
using KartRider.Common.Utilities;
using ExcData;
using System.Xml;
using Set_Data;
using System.Xml.Linq;
using System.Linq;

namespace KartRider
{
	public class RandomTrack
	{
		public static string GameType = "item";
		public static string SetRandomTrack = "allRandom";
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
				RandomTrack.SetRandomTrack = "allRandom";
			}
			else if (StartGameData.StartTimeAttack_Track == 1)
			{
				RandomTrack.SetRandomTrack = "leagueRandom";
			}
			else if (StartGameData.StartTimeAttack_Track == 3)
			{
				RandomTrack.SetRandomTrack = "hot1Random";
			}
			else if (StartGameData.StartTimeAttack_Track == 4)
			{
				RandomTrack.SetRandomTrack = "hot2Random";
			}
			else if (StartGameData.StartTimeAttack_Track == 5)
			{
				RandomTrack.SetRandomTrack = "hot3Random";
			}
			else if (StartGameData.StartTimeAttack_Track == 6)
			{
				RandomTrack.SetRandomTrack = "hot4Random";
			}
			else if (StartGameData.StartTimeAttack_Track == 7)
			{
				RandomTrack.SetRandomTrack = "hot5Random";
			}
			else if (StartGameData.StartTimeAttack_Track == 8)
			{
				RandomTrack.SetRandomTrack = "newRandom";
			}
			else if (StartGameData.StartTimeAttack_Track == 30)
			{
				RandomTrack.SetRandomTrack = "reverseRandom";
			}
			else if (StartGameData.StartTimeAttack_Track == 40)
			{
				RandomTrack.SetRandomTrack = "speedAllRandom";
			}
			else
			{
				RandomTrack.SetRandomTrack = "Unknown";
			}
			RandomTrack.RandomTrackSetList();
		}

		public static void RandomTrackSetList()
		{
			if (RandomTrack.SetRandomTrack == "allRandom")
			{
				if (RandomTrack.GameType == "item")
				{
					XDocument doc = KartExcData.randomTrack;
					var itemTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "item");
					if (itemTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
				else if (RandomTrack.GameType == "speed")
				{
					XDocument doc = KartExcData.randomTrack;
					var speedTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed");
					if (speedTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
			}
			else if (RandomTrack.SetRandomTrack == "leagueRandom")
			{
				XDocument doc = KartExcData.randomTrack;
				var speedTrackSets = doc.Descendants("RandomTrackSet")
						  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "clubSpeed");
				if (speedTrackSets != null)
				{
					Random random = new Random();
					var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
					RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
				}
			}
			else if (RandomTrack.SetRandomTrack == "hot1Random")
			{
				if (RandomTrack.GameType == "item")
				{
					XDocument doc = KartExcData.randomTrack;
					var itemTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "item" && (string)rts.Attribute("randomType") == "hot1");
					if (itemTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
				else if (RandomTrack.GameType == "speed")
				{
					XDocument doc = KartExcData.randomTrack;
					var speedTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "hot1");
					if (speedTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
			}
			else if (RandomTrack.SetRandomTrack == "hot2Random")
			{
				if (RandomTrack.GameType == "item")
				{
					XDocument doc = KartExcData.randomTrack;
					var itemTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "item" && (string)rts.Attribute("randomType") == "hot2");
					if (itemTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
				else if (RandomTrack.GameType == "speed")
				{
					XDocument doc = KartExcData.randomTrack;
					var speedTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "hot2");
					if (speedTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
			}
			else if (RandomTrack.SetRandomTrack == "hot3Random")
			{
				if (RandomTrack.GameType == "item")
				{
					XDocument doc = KartExcData.randomTrack;
					var itemTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "item" && (string)rts.Attribute("randomType") == "hot3");
					if (itemTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
				else if (RandomTrack.GameType == "speed")
				{
					XDocument doc = KartExcData.randomTrack;
					var speedTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "hot3");
					if (speedTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
			}
			else if (RandomTrack.SetRandomTrack == "hot4Random")
			{
				if (RandomTrack.GameType == "item")
				{
					XDocument doc = KartExcData.randomTrack;
					var itemTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "item" && (string)rts.Attribute("randomType") == "hot4");
					if (itemTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
				else if (RandomTrack.GameType == "speed")
				{
					XDocument doc = KartExcData.randomTrack;
					var speedTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "hot4");
					if (speedTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
			}
			else if (RandomTrack.SetRandomTrack == "hot5Random")
			{
				if (RandomTrack.GameType == "item")
				{
					XDocument doc = KartExcData.randomTrack;
					var itemTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "item" && (string)rts.Attribute("randomType") == "hot5");
					if (itemTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
				else if (RandomTrack.GameType == "speed")
				{
					XDocument doc = KartExcData.randomTrack;
					var speedTrackSets = doc.Descendants("RandomTrackSet")
							  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "hot5");
					if (speedTrackSets != null)
					{
						Random random = new Random();
						var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
						RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
					}
				}
			}
			else if (RandomTrack.SetRandomTrack == "newRandom")
			{
				XDocument doc = KartExcData.randomTrack;
				var itemTrackSets = doc.Descendants("RandomTrackList")
						  .FirstOrDefault(rts => (string)rts.Attribute("randomType") == "new");
				if (itemTrackSets != null)
				{
					Random random = new Random();
					var randomTrack = itemTrackSets.Descendants("track").ElementAt(random.Next(itemTrackSets.Descendants("track").Count()));
					RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
				}
			}
			else if (RandomTrack.SetRandomTrack == "reverseRandom")
			{
				XDocument doc = KartExcData.randomTrack;
				var reverseTrackSets = doc.Descendants("RandomTrackList")
						  .FirstOrDefault(rts => (string)rts.Attribute("randomType") == "reverse");
				if (reverseTrackSets != null)
				{
					Random random = new Random();
					var randomTrack = reverseTrackSets.Descendants("track").ElementAt(random.Next(reverseTrackSets.Descendants("track").Count()));
					RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
				}
			}
			else if (RandomTrack.SetRandomTrack == "speedAllRandom")
			{
				XDocument doc = KartExcData.randomTrack;
				var speedTrackSets = doc.Descendants("RandomTrackSet")
						  .FirstOrDefault(rts => (string)rts.Attribute("gameType") == "speed" && (string)rts.Attribute("randomType") == "clubSpeed");
				if (speedTrackSets != null)
				{
					Random random = new Random();
					var randomTrack = speedTrackSets.Descendants("track").ElementAt(random.Next(speedTrackSets.Descendants("track").Count()));
					RandomTrack.GameTrack = (string)randomTrack.Attribute("id");
				}
			}
			if (RandomTrack.SetRandomTrack != "Unknown")
			{
				StartGameData.StartTimeAttack_Track = Adler32Helper.GenerateAdler32_UNICODE(RandomTrack.GameTrack, 0);
				Console.WriteLine("RandomTrack: {0} / {1} / {2}", RandomTrack.GameType, RandomTrack.SetRandomTrack, RandomTrack.GameTrack);
			}
			SpeedType.SpeedTypeData();
		}
	}
}
