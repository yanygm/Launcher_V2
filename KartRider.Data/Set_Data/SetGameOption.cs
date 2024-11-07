using System;
using System.IO;

namespace Set_Data
{
	public static class SetGameOption
	{
		public static ushort Version;
		public static float Set_BGM = 1f;
		public static float Set_Sound = 1f;
		public static byte Main_BGM = 0;
		public static byte Sound_effect = 1;
		public static byte Full_screen = 1;
		public static byte Unk1 = 1;
		public static byte Unk2 = 1;
		public static byte Unk3 = 1;
		public static byte Unk4 = 1;
		public static byte Unk5 = 0;
		public static byte Unk6 = 0;
		public static byte Unk7 = 0;
		public static byte Unk8 = 1;
		public static byte Unk9 = 1;
		public static byte Unk10 = 1;
		public static byte Unk11 = 14; //녹화 품질
		public static byte BGM_Check = 1;
		public static byte Sound_Check = 1;
		public static byte Unk12 = 1;
		public static byte Unk13 = 1;
		public static byte GameType = 0;
		public static byte SetGhost = 1;
		public static byte SpeedType = 7;
		public static byte Unk14 = 1;
		public static byte Unk15 = 1;
		public static byte Unk16 = 1;
		public static byte Unk17 = 1;
		public static byte Set_screen = 0;
		public static byte Unk18 = 0;

		public static void Save_SetGameOption()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_BGM + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Set_BGM);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_Sound + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Set_Sound);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Main_BGM + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Main_BGM);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Sound_effect + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Sound_effect);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Full_screen + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Full_screen);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk1 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk1);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk2 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk2);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk3 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk3);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk4 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk4);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk5 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk5);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk6 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk6);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk7 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk7);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk8 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk8);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk9 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk9);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk10 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk10);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk11 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk11);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_BGM_Check + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.BGM_Check);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Sound_Check + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Sound_Check);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk12 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk12);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk13 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk13);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_GameType + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.GameType);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_SetGhost + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.SetGhost);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_SpeedType + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.SpeedType);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk14 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk14);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk15 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk15);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk16 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk16);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk17 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk17);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_screen + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Set_screen);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk18 + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Unk18);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetGameOption_LoadFile + FileName.SetGameOption_Version + FileName.Extension, false))
			{
				streamWriter.Write(SetGameOption.Version);
			}
		}

		public static void Load_SetGameOption()
		{
			string Load_Set_BGM = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_BGM + FileName.Extension;
			if (File.Exists(Load_Set_BGM))
			{
				string textValue = System.IO.File.ReadAllText(Load_Set_BGM);
				SetGameOption.Set_BGM = float.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Set_BGM, false))
				{
					streamWriter.Write(SetGameOption.Set_BGM);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Set_Sound = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_Sound + FileName.Extension;
			if (File.Exists(Load_Set_Sound))
			{
				string textValue = System.IO.File.ReadAllText(Load_Set_Sound);
				SetGameOption.Set_Sound = float.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Set_Sound, false))
				{
					streamWriter.Write(SetGameOption.Set_Sound);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Main_BGM = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Main_BGM + FileName.Extension;
			if (File.Exists(Load_Main_BGM))
			{
				string textValue = System.IO.File.ReadAllText(Load_Main_BGM);
				SetGameOption.Main_BGM = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Main_BGM, false))
				{
					streamWriter.Write(SetGameOption.Main_BGM);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Sound_effect = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Sound_effect + FileName.Extension;
			if (File.Exists(Load_Sound_effect))
			{
				string textValue = System.IO.File.ReadAllText(Load_Sound_effect);
				SetGameOption.Sound_effect = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Sound_effect, false))
				{
					streamWriter.Write(SetGameOption.Sound_effect);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Full_screen = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Full_screen + FileName.Extension;
			if (File.Exists(Load_Full_screen))
			{
				string textValue = System.IO.File.ReadAllText(Load_Full_screen);
				SetGameOption.Full_screen = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Full_screen, false))
				{
					streamWriter.Write(SetGameOption.Full_screen);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk1 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk1 + FileName.Extension;
			if (File.Exists(Load_Unk1))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk1);
				SetGameOption.Unk1 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk1, false))
				{
					streamWriter.Write(SetGameOption.Unk1);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk2 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk2 + FileName.Extension;
			if (File.Exists(Load_Unk2))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk2);
				SetGameOption.Unk2 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk2, false))
				{
					streamWriter.Write(SetGameOption.Unk2);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk3 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk3 + FileName.Extension;
			if (File.Exists(Load_Unk3))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk3);
				SetGameOption.Unk3 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk3, false))
				{
					streamWriter.Write(SetGameOption.Unk3);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk4 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk4 + FileName.Extension;
			if (File.Exists(Load_Unk4))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk4);
				SetGameOption.Unk4 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk4, false))
				{
					streamWriter.Write(SetGameOption.Unk4);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk5 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk5 + FileName.Extension;
			if (File.Exists(Load_Unk5))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk5);
				SetGameOption.Unk5 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk5, false))
				{
					streamWriter.Write(SetGameOption.Unk5);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk6 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk6 + FileName.Extension;
			if (File.Exists(Load_Unk6))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk6);
				SetGameOption.Unk6 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk6, false))
				{
					streamWriter.Write(SetGameOption.Unk6);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk7 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk7 + FileName.Extension;
			if (File.Exists(Load_Unk7))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk7);
				SetGameOption.Unk7 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk7, false))
				{
					streamWriter.Write(SetGameOption.Unk7);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk8 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk8 + FileName.Extension;
			if (File.Exists(Load_Unk8))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk8);
				SetGameOption.Unk8 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk8, false))
				{
					streamWriter.Write(SetGameOption.Unk8);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk9 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk9 + FileName.Extension;
			if (File.Exists(Load_Unk9))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk9);
				SetGameOption.Unk9 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk9, false))
				{
					streamWriter.Write(SetGameOption.Unk9);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk10 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk10 + FileName.Extension;
			if (File.Exists(Load_Unk10))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk10);
				SetGameOption.Unk10 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk10, false))
				{
					streamWriter.Write(SetGameOption.Unk10);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk11 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk11 + FileName.Extension;
			if (File.Exists(Load_Unk11))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk11);
				SetGameOption.Unk11 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk11, false))
				{
					streamWriter.Write(SetGameOption.Unk11);
				}
			}
			//-------------------------------------------------------------------------
			string Load_BGM_Check = FileName.SetGameOption_LoadFile + FileName.SetGameOption_BGM_Check + FileName.Extension;
			if (File.Exists(Load_BGM_Check))
			{
				string textValue = System.IO.File.ReadAllText(Load_BGM_Check);
				SetGameOption.BGM_Check = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_BGM_Check, false))
				{
					streamWriter.Write(SetGameOption.BGM_Check);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Sound_Check = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Sound_Check + FileName.Extension;
			if (File.Exists(Load_Sound_Check))
			{
				string textValue = System.IO.File.ReadAllText(Load_Sound_Check);
				SetGameOption.Sound_Check = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Sound_Check, false))
				{
					streamWriter.Write(SetGameOption.Sound_Check);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk12 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk12 + FileName.Extension;
			if (File.Exists(Load_Unk12))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk12);
				SetGameOption.Unk12 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk12, false))
				{
					streamWriter.Write(SetGameOption.Unk12);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk13 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk13 + FileName.Extension;
			if (File.Exists(Load_Unk13))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk13);
				SetGameOption.Unk13 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk13, false))
				{
					streamWriter.Write(SetGameOption.Unk13);
				}
			}
			//-------------------------------------------------------------------------
			string Load_GameType = FileName.SetGameOption_LoadFile + FileName.SetGameOption_GameType + FileName.Extension;
			if (File.Exists(Load_GameType))
			{
				string textValue = System.IO.File.ReadAllText(Load_GameType);
				SetGameOption.GameType = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_GameType, false))
				{
					streamWriter.Write(SetGameOption.GameType);
				}
			}
			//-------------------------------------------------------------------------
			string Load_SetGhost = FileName.SetGameOption_LoadFile + FileName.SetGameOption_SetGhost + FileName.Extension;
			if (File.Exists(Load_SetGhost))
			{
				string textValue = System.IO.File.ReadAllText(Load_SetGhost);
				SetGameOption.SetGhost = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_SetGhost, false))
				{
					streamWriter.Write(SetGameOption.SetGhost);
				}
			}
			//-------------------------------------------------------------------------
			string Load_SpeedType = FileName.SetGameOption_LoadFile + FileName.SetGameOption_SpeedType + FileName.Extension;
			if (File.Exists(Load_SpeedType))
			{
				string textValue = System.IO.File.ReadAllText(Load_SpeedType);
				SetGameOption.SpeedType = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_SpeedType, false))
				{
					streamWriter.Write(SetGameOption.SpeedType);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk14 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk14 + FileName.Extension;
			if (File.Exists(Load_Unk14))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk14);
				SetGameOption.Unk14 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk14, false))
				{
					streamWriter.Write(SetGameOption.Unk14);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk15 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk15 + FileName.Extension;
			if (File.Exists(Load_Unk15))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk15);
				SetGameOption.Unk15 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk15, false))
				{
					streamWriter.Write(SetGameOption.Unk15);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk16 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk16 + FileName.Extension;
			if (File.Exists(Load_Unk16))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk16);
				SetGameOption.Unk16 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk16, false))
				{
					streamWriter.Write(SetGameOption.Unk16);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk17 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk17 + FileName.Extension;
			if (File.Exists(Load_Unk17))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk17);
				SetGameOption.Unk17 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk17, false))
				{
					streamWriter.Write(SetGameOption.Unk17);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Set_screen = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Set_screen + FileName.Extension;
			if (File.Exists(Load_Set_screen))
			{
				string textValue = System.IO.File.ReadAllText(Load_Set_screen);
				SetGameOption.Set_screen = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Set_screen, false))
				{
					streamWriter.Write(SetGameOption.Set_screen);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Unk18 = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Unk18 + FileName.Extension;
			if (File.Exists(Load_Unk18))
			{
				string textValue = System.IO.File.ReadAllText(Load_Unk18);
				SetGameOption.Unk18 = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Unk18, false))
				{
					streamWriter.Write(SetGameOption.Unk18);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Version = FileName.SetGameOption_LoadFile + FileName.SetGameOption_Version + FileName.Extension;
			if (File.Exists(Load_Version))
			{
				string textValue = System.IO.File.ReadAllText(Load_Version);
				SetGameOption.Version = ushort.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Version, false))
				{
					streamWriter.Write(SetGameOption.Version);
				}
			}
		}
	}
}
