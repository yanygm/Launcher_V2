using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
using KartRider.IO.Packet;
using Microsoft.Win32;
using RHOParser;
using Set_Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;
using System.Linq;

namespace KartRider
{
	internal static class Program
	{
		public static Launcher LauncherDlg;
		public static GetKart GetKartDlg;
		public static bool SpeedPatch;
		public static bool PreventItem;
		public static bool Developer_Name;
		public static string RootDirectory;
		public static CountryCode CC = CountryCode.CN;

		static Program()
		{
			Program.Developer_Name = true;
		}

		[STAThread]
		private static void Main(string[] args)
		{
			string input;
			string output;
			string Load_cc = AppDomain.CurrentDomain.BaseDirectory + FileName.config_LoadFile + FileName.SetRider_CC + FileName.Extension;
			if (File.Exists(Load_cc))
			{
				string textValue = System.IO.File.ReadAllText(Load_cc);
				CC = (CountryCode)Enum.Parse(typeof(CountryCode), textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_cc, false))
				{
					streamWriter.Write(CC.ToString());
				}
			}
			if (args == null || args.Length == 0)
			{
				string text = "HKEY_CURRENT_USER\\SOFTWARE\\TCGame\\kart";
				RootDirectory = (string)Registry.GetValue(text, "gamepath", null);
				if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "KartRider.pin") && File.Exists(AppDomain.CurrentDomain.BaseDirectory + "KartRider.exe"))
				{
					RootDirectory = AppDomain.CurrentDomain.BaseDirectory;
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
				input = "";
				output = "";
			}
			else if (args.Length == 1)
			{
				input = args[0];
				output = args[0];
			}
			else
			{
				if (args.Length != 2)
					return;
				input = args[0];
				output = args[1];
			}
			if (input.EndsWith(".rho") || input.EndsWith(".rho5"))
			{
				Program.decode(input, output);
			}
			else if(input.EndsWith(".xml"))
			{
				Program.XtoB(input, output);
			}
			else if (input.EndsWith(".bml"))
			{
				Program.BtoX(input, output);
			}
			else if (input.EndsWith(".pk"))
			{
				Program.AAAR(input, output);
			}
			else
			{
				if (!Directory.Exists(input))
					return;
				Program.encode(input, output);
			}
		}

		private static void encode(string input, string output)
		{
			Rho5Archive rho5Archive = new Rho5Archive();
			if (output.EndsWith(".rho"))
				return;
			if (!output.EndsWith(".rho5"))
				output += ".rho5";
			var fileInfo = new FileInfo(output);
			string fullName = "";
			if (fileInfo.Directory != null)
			{
				fullName = fileInfo.Directory.FullName;
				if (!Directory.Exists(fullName))
					Directory.CreateDirectory(fullName);
				string[] strArray = fileInfo.Name.Replace(".rho5", "").Split("_", StringSplitOptions.None);
				string dataPackName = strArray[0];
				int dataPackID = 0;
				if (strArray.Length == 2)
					dataPackID = Convert.ToInt32(strArray[1]);
				input = input.Replace("\\", "/");
				if (!input.EndsWith("/"))
					input += "/";
				rho5Archive.SaveFolder(input, dataPackName, fullName, CC, dataPackID);
			}
			else
			{
				Console.WriteLine($"路径不存在：{output}");
			}
		}

		private static void decode(string input, string output)
		{
			if (output.EndsWith(".rho"))
				output = output.Replace(".rho", "");
			if (output.EndsWith(".rho5"))
				output = output.Replace(".rho5", "");
			PackFolderManager packFolderManager = new PackFolderManager();
			packFolderManager.OpenSingleFile(input, CC);
			Queue<PackFolderInfo> packFolderInfoQueue = new Queue<PackFolderInfo>();
			packFolderInfoQueue.Enqueue(packFolderManager.GetRootFolder());
			packFolderManager.GetRootFolder();
			while (packFolderInfoQueue.Count > 0)
			{
				PackFolderInfo packFolderInfo1 = packFolderInfoQueue.Dequeue();
				foreach (PackFileInfo packFileInfo in packFolderInfo1.GetFilesInfo())
				{
					string str = output + "/" + Program.ReplacePath(packFileInfo.FullName);
					var fileInfo = new FileInfo(str);
					if (fileInfo.Directory != null)
					{
						string fullName = fileInfo.Directory.FullName;
						if (!Directory.Exists(fullName))
							Directory.CreateDirectory(fullName);
					}
					else
					{
						Console.WriteLine($"路径不存在：{str}");
					}
					using (FileStream fileStream = new FileStream(str, FileMode.OpenOrCreate))
					{
						byte[] data = packFileInfo.GetData();
						((Stream)fileStream).Write(data, 0, data.Length);
					}
				}
				foreach (PackFolderInfo packFolderInfo2 in packFolderInfo1.GetFoldersInfo())
					packFolderInfoQueue.Enqueue(packFolderInfo2);
			}
		}

		private static string ReplacePath(string file)
		{
			return file.IndexOf(".rho") > -1 ? file.Substring(0, file.IndexOf(".rho")).Replace("_", "/") + file.Substring(file.IndexOf(".rho") + 4) : file;
		}

		private static void BtoX(string input, string output)
		{
			byte[] data = File.ReadAllBytes(input);
			BinaryXmlDocument bxd = new BinaryXmlDocument();
			bxd.Read(Encoding.GetEncoding("UTF-16"), data);
			string output_bml = bxd.RootTag.ToString();
			Console.WriteLine(output_bml);
			byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
			string filePath = System.IO.Path.ChangeExtension(input, "xml");
			using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			{
				fs.Write(output_data, 0, output_data.Length);
			}
		}

		private static void XtoB(string input, string output)
		{
			XDocument xdoc = XDocument.Load(input);
			List<int> childCounts = CountChildren(xdoc.Root, 0, new List<int>());
			using (XmlReader reader = XmlReader.Create(input))
			{
				using (OutPacket outPacket = new OutPacket())
				{
					int Count = 0;
					while (reader.Read())
					{
						if (reader.NodeType == XmlNodeType.Element)
						{
							string elementName = reader.Name;
							int attCount = reader.AttributeCount;
							Console.WriteLine($"元素: {elementName}");
							outPacket.WriteString(elementName);
							outPacket.WriteInt(0);
							Console.WriteLine($"属性数量: {attCount}");
							outPacket.WriteInt(attCount);
							for (int i = 0; i < attCount; i++)
							{
								reader.MoveToAttribute(i);
								string attName = reader.Name;
								outPacket.WriteString(attName);
								string attValue = reader.Value;
								outPacket.WriteString(attValue);
								Console.WriteLine($"属性名: {attName}, 属性值: {attValue}");
							}
							Console.WriteLine($"子元素数量: {childCounts[Count]}");
							outPacket.WriteInt(childCounts[Count]);
							Count++;
							reader.MoveToElement();
						}
					}
					byte[] byteArray = outPacket.ToArray();
					string filePath = System.IO.Path.ChangeExtension(input, "bml");
					using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
					{
						fs.Write(byteArray, 0, byteArray.Length);
					}
				}
			}
		}

		public static List<int> CountChildren(XElement element, int level, List<int> childCounts)
		{
			int childCount = element.Elements().Count();
			childCounts.Add(childCount);
			foreach (XElement child in element.Elements())
			{
				CountChildren(child, level + 1, childCounts);
			}
			return childCounts;
		}

		private static void AAAR(string input, string output)
		{
			using FileStream fileStream = new FileStream(input, FileMode.Open, FileAccess.Read);
			BinaryReader binaryReader = new BinaryReader(fileStream);
			int totalLength = binaryReader.ReadInt32();
			byte[] array = binaryReader.ReadKRData(totalLength);
			fileStream.Close();
			BinaryXmlDocument bxd = new BinaryXmlDocument();
			bxd.Read(Encoding.GetEncoding("UTF-16"), array);
			string output_bml = bxd.RootTag.ToString();
			Console.WriteLine(output_bml);
			byte[] output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
			string filePath = System.IO.Path.ChangeExtension(input, "xml");
			using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			{
				fs.Write(output_data, 0, output_data.Length);
			}
		}
	}
}
