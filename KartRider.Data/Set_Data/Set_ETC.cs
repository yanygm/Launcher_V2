using System;
using System.IO;
using KartRider;

namespace Set_Data
{
	public static class Set_ETC
	{
		public static byte DataPack_Use = 0;
		public static bool DataPack = false;

		public static void Load_DataPack()
		{
			string Load_DataPack = FileName.config_LoadFile + FileName.SetETC_DataPack + FileName.Extension;
			if (File.Exists(Load_DataPack))
			{
				string textValue = System.IO.File.ReadAllText(Load_DataPack);
				Set_ETC.DataPack_Use = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_DataPack, false))
				{
					streamWriter.Write(Set_ETC.DataPack_Use);
				}
			}
			Set_ETC.Check_DataPack();
		}

		public static void Load_DataPack2()
		{
			string Load_DataPack = FileName.config_LoadFile + FileName.SetETC_DataPack + FileName.Extension;
			if (File.Exists(Load_DataPack))
			{
				string textValue = System.IO.File.ReadAllText(Load_DataPack);
				Set_ETC.DataPack_Use = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_DataPack, false))
				{
					streamWriter.Write(Set_ETC.DataPack_Use);
				}
			}
		}

		public static void Save_DataPack()
		{
			string LoadFile = FileName.config_LoadFile + FileName.SetETC_DataPack + FileName.Extension;
			using (StreamWriter streamWriter = new StreamWriter(LoadFile, false))
			{
				streamWriter.Write(Set_ETC.DataPack_Use);
			}
		}

		public static void Check_DataPack()
		{
			if (Set_ETC.DataPack_Use == 0)
			{
				Set_ETC.DataPack = false;
			}
			else
			{
				Set_ETC.DataPack = true;
			}
		}

		public static void Load_ALL()
		{
			Set_ETC.Load_DataPack();
		}

		public static void Load_ALL2()
		{
			Set_ETC.Load_DataPack2();
		}
	}
}