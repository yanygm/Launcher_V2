using System;
using System.IO;
using KartRider;
using ExcData;

namespace Set_Data
{
	public static class SetGameData
	{
		public static void Save_Nickname()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Nickname + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Nickname);
			}
		}

		public static void Save_RiderIntro()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_RiderIntro + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.RiderIntro);
			}
		}

		public static void Save_Emblem()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Emblem1 + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Emblem1);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Emblem2 + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Emblem2);
			}
		}

		public static void Save_RecordTimeAttack()
		{
			string trackName = StartGameData.StartTimeAttack_Track.ToString();
			if (KartExcData.track.ContainsKey(StartGameData.StartTimeAttack_Track))
			{
				trackName = KartExcData.track[StartGameData.StartTimeAttack_Track];
			}
			using (StreamWriter streamWriter = new StreamWriter("TimeAttack.log", true))
			{
				streamWriter.WriteLine("[{0}] SpeedType:{1}, GameType:{2}, Kart:{3}, FlyingPet:{4}, ResultType:{5}, RP:{6}, Lucci:{7}, Track:{8}, Record:{9}:{10}:{11}", DateTime.Now, StartGameData.StartTimeAttack_SpeedType, StartGameData.StartTimeAttack_GameType, StartGameData.Kart_id, StartGameData.FlyingPet_id, GameType.RewardType, GameType.TimeAttack_RP, GameType.TimeAttack_Lucci, trackName, GameType.min, GameType.sec, GameType.mil);
			}
		}

		public static void Save_RewardTimeAttack()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Lucci + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Lucci);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_RP + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.RP);
			}
		}

		public static void Save_TimeAttackDecLucci()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Lucci + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Lucci);
			}
		}

		public static void Save_SlotChanger()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_SlotChanger + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.SlotChanger);
			}
		}

		public static void Save_Card()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Card + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Card);
			}
		}
	}
}
