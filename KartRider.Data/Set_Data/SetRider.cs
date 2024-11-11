using System;
using System.IO;
using KartRider;

namespace Set_Data
{
	public static class SetRider
	{
		public static int ClubCode = 10000;
		public static int ClubMark_LOGO = 0;//343 베로
		public static int ClubMark_LINE = 0;
		public static string ClubName = "KartRider";
		public static string UserID = "Yany";
		public static uint UserNO = 614990519;
		public static string Nickname = "Yany";
		public static string RiderIntro = "";
		public static short Emblem1 = 0;
		public static short Emblem2 = 0;
		public static uint Lucci = 1000000;
		public static uint RP = 2000000000;
		public static uint Koin = 10000;
		public static int Premium = 5;//100
		public static byte Ranker = 0;
		public static short SlotChanger = 30000;
		public static uint pmap = 0;//3130 //1068 //2520
		public static byte IdentificationType = 1;

		public static void Save_SetRider()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Nickname + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Nickname);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_RiderIntro + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.RiderIntro);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Emblem1 + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Emblem1);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Emblem2 + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Emblem2);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Lucci + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Lucci);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_RP + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.RP);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Koin + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Koin);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_Premium + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.Premium);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_SlotChanger + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.SlotChanger);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_ClubMark_LOGO + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.ClubMark_LOGO);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_ClubMark_LINE + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.ClubMark_LINE);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRider_LoadFile + FileName.SetRider_ClubName + FileName.Extension, false))
			{
				streamWriter.Write(SetRider.ClubName);
			}
		}

		public static void Load_SetRider()
		{
			string Load_Nickname = FileName.SetRider_LoadFile + FileName.SetRider_Nickname + FileName.Extension;
			if (File.Exists(Load_Nickname))
			{
				string textValue = System.IO.File.ReadAllText(Load_Nickname);
				SetRider.Nickname = textValue;
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Nickname, false))
				{
					streamWriter.Write(SetRider.Nickname);
				}
			}
			//-------------------------------------------------------------------------
			string Load_RiderIntro = FileName.SetRider_LoadFile + FileName.SetRider_RiderIntro + FileName.Extension;
			if (File.Exists(Load_RiderIntro))
			{
				string textValue = System.IO.File.ReadAllText(Load_RiderIntro);
				SetRider.RiderIntro = textValue;
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_RiderIntro, false))
				{
					streamWriter.Write(SetRider.RiderIntro);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Emblem1 = FileName.SetRider_LoadFile + FileName.SetRider_Emblem1 + FileName.Extension;
			if (File.Exists(Load_Emblem1))
			{
				string textValue = System.IO.File.ReadAllText(Load_Emblem1);
				SetRider.Emblem1 = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Emblem1, false))
				{
					streamWriter.Write(SetRider.Emblem1);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Emblem2 = FileName.SetRider_LoadFile + FileName.SetRider_Emblem2 + FileName.Extension;
			if (File.Exists(Load_Emblem2))
			{
				string textValue = System.IO.File.ReadAllText(Load_Emblem2);
				SetRider.Emblem2 = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Emblem2, false))
				{
					streamWriter.Write(SetRider.Emblem2);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Lucci = FileName.SetRider_LoadFile + FileName.SetRider_Lucci + FileName.Extension;
			if (File.Exists(Load_Lucci))
			{
				string textValue = System.IO.File.ReadAllText(Load_Lucci);
				SetRider.Lucci = uint.Parse(textValue);
				if (SetRider.Lucci > SessionGroup.LucciMax)
				{
					SetRider.Lucci = SessionGroup.LucciMax;
					using (StreamWriter streamWriter = new StreamWriter(Load_Lucci, false))
					{
						streamWriter.Write(SessionGroup.LucciMax);
					}
				}
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Lucci, false))
				{
					streamWriter.Write(SetRider.Lucci);
				}
			}
			//-------------------------------------------------------------------------
			string Load_RP = FileName.SetRider_LoadFile + FileName.SetRider_RP + FileName.Extension;
			if (File.Exists(Load_RP))
			{
				string textValue = System.IO.File.ReadAllText(Load_RP);
				SetRider.RP = uint.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_RP, false))
				{
					streamWriter.Write(SetRider.RP);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Koin = FileName.SetRider_LoadFile + FileName.SetRider_Koin + FileName.Extension;
			if (File.Exists(Load_Koin))
			{
				string textValue = System.IO.File.ReadAllText(Load_Koin);
				SetRider.Koin = uint.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Koin, false))
				{
					streamWriter.Write(SetRider.Koin);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Premium = FileName.SetRider_LoadFile + FileName.SetRider_Premium + FileName.Extension;
			if (File.Exists(Load_Premium))
			{
				string textValue = System.IO.File.ReadAllText(Load_Premium);
				SetRider.Premium = int.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Premium, false))
				{
					streamWriter.Write(SetRider.Premium);
				}
			}
			//-------------------------------------------------------------------------
			string Load_SlotChanger = FileName.SetRider_LoadFile + FileName.SetRider_SlotChanger + FileName.Extension;
			if (File.Exists(Load_SlotChanger))
			{
				string textValue = System.IO.File.ReadAllText(Load_SlotChanger);
				SetRider.SlotChanger = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_SlotChanger, false))
				{
					streamWriter.Write(SetRider.SlotChanger);
				}
			}
			//-------------------------------------------------------------------------
			string Load_ClubMark_LOGO = FileName.SetRider_LoadFile + FileName.SetRider_ClubMark_LOGO + FileName.Extension;
			if (File.Exists(Load_ClubMark_LOGO))
			{
				string textValue = System.IO.File.ReadAllText(Load_ClubMark_LOGO);
				SetRider.ClubMark_LOGO = int.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_ClubMark_LOGO, false))
				{
					streamWriter.Write(SetRider.ClubMark_LOGO);
				}
			}
			//-------------------------------------------------------------------------
			string Load_ClubMark_LINE = FileName.SetRider_LoadFile + FileName.SetRider_ClubMark_LINE + FileName.Extension;
			if (File.Exists(Load_ClubMark_LINE))
			{
				string textValue = System.IO.File.ReadAllText(Load_ClubMark_LINE);
				SetRider.ClubMark_LINE = int.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_ClubMark_LINE, false))
				{
					streamWriter.Write(SetRider.ClubMark_LINE);
				}
			}
			//-------------------------------------------------------------------------
			string Load_ClubName = FileName.SetRider_LoadFile + FileName.SetRider_ClubName + FileName.Extension;
			if (File.Exists(Load_ClubName))
			{
				string textValue = System.IO.File.ReadAllText(Load_ClubName);
				SetRider.ClubName = textValue;
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_ClubName, false))
				{
					streamWriter.Write(SetRider.ClubName);
				}
			}
		}
	}
}