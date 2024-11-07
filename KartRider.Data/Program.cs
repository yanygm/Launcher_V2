using KartLibrary.File;
using Microsoft.Win32;
using RHOParser;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace KartRider
{
	internal static class Program
	{
		public static Launcher LauncherDlg;
		public static GetKart GetKartDlg;
		//public static int MAX_EQP_P;
		public static bool SpeedPatch;
		public static bool PreventItem;
		public static bool Developer_Name;
		public static string RootDirectory;

		static Program()
		{
			//Program.MAX_EQP_P = 35;
			Program.Developer_Name = true;
		}

		[STAThread]
		private static void Main()
		{
			string text = "HKEY_CURRENT_USER\\SOFTWARE\\TCGame\\kart";
			RootDirectory = (string)Registry.GetValue(text, "gamepath", null);
			if (File.Exists(Environment.CurrentDirectory + "\\KartRider.pin") && File.Exists(Environment.CurrentDirectory + "\\KartRider.exe"))
			{
				RootDirectory = Environment.CurrentDirectory + "\\";
				KartRhoFile.RhoFile(RootDirectory);
				KartRhoFile.packFolderManager.Reset();
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Launcher StartLauncher = new Launcher();
				Program.LauncherDlg = StartLauncher;
				Program.LauncherDlg.kartRiderDirectory = RootDirectory;
				Application.Run(StartLauncher);
			}
			else if (File.Exists(RootDirectory + "KartRider.pin") && File.Exists(RootDirectory + "KartRider.exe"))
			{
				KartRhoFile.RhoFile(RootDirectory);
				KartRhoFile.packFolderManager.Reset();
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Launcher StartLauncher = new Launcher();
				Program.LauncherDlg = StartLauncher;
				Program.LauncherDlg.kartRiderDirectory = RootDirectory;
				Application.Run(StartLauncher);
			}
			else
			{
				LauncherSystem.MessageBoxType3();
			}
		}
	}
}
