using System;
using System.IO;
using KartRider;

namespace Set_Data
{
	public static class config
	{
		public static byte PreventItem_Use = 0;
		public static byte SpeedPatch_Use = 0;
		public static byte SpeedType = 7;

		public static void Load_PreventItem()
		{
			string Load_PreventItem = FileName.config_LoadFile + FileName.config_PreventItem + FileName.Extension;
			if (File.Exists(Load_PreventItem))
			{
				string textValue = System.IO.File.ReadAllText(Load_PreventItem);
				config.PreventItem_Use = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_PreventItem, false))
				{
					streamWriter.Write(config.PreventItem_Use);
				}
			}
			config.Check_PreventItem();
		}

		public static void Load_SpeedPatch()
		{
			string Load_SpeedPatch = FileName.config_LoadFile + FileName.config_SpeedPatch + FileName.Extension;
			if (File.Exists(Load_SpeedPatch))
			{
				string textValue = System.IO.File.ReadAllText(Load_SpeedPatch);
				config.SpeedPatch_Use = byte.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_SpeedPatch, false))
				{
					streamWriter.Write(config.SpeedPatch_Use);
				}
			}
			config.Check_SpeedPatch();
		}

		public static void Check_PreventItem()
		{
			if (config.PreventItem_Use == 0)
			{
				Program.PreventItem = false;
			}
			else
			{
				Program.PreventItem = true;
			}
		}

		public static void Check_SpeedPatch()
		{
			if (config.SpeedPatch_Use == 0)
			{
				Program.SpeedPatch = false;
				Program.LauncherDlg.Text = "Launcher";
			}
			else
			{
				Program.SpeedPatch = true;
				Program.LauncherDlg.Text = "Launcher (속도 패치)";
			}
		}

		public static void Load_ALL()
		{
			config.Load_PreventItem();
			config.Load_SpeedPatch();
		}
	}
}