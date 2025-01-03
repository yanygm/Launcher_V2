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
using System.Xml;
using System.Linq;
using System.Xml.Linq;

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
			string Load_CC = AppDomain.CurrentDomain.BaseDirectory + "CountryCode.ini";
			if (File.Exists(Load_CC))
			{
				string textValue = System.IO.File.ReadAllText(Load_CC);
				Program.CC = (CountryCode)Enum.Parse(typeof(CountryCode), textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_CC, false))
				{
					streamWriter.Write(Program.CC.ToString());
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
			if (input.EndsWith(".xml"))
			{
				Program.XtoB(input);
			}
			if (input.EndsWith("aaa.bml"))
			{
				Program.AAAD(input);
			}
			else if (input.EndsWith(".bml"))
			{
				Program.BtoX(input);
			}
			if (input.EndsWith(".pk"))
			{
				Program.AAAR(input);
			}
			else
			{
				if (!Directory.Exists(input))
					return;
				if (input.Contains("_0"))
				{
					Program.encode(input, output);
				}
				else
				{
					string[] files = Directory.GetFiles(input, "*.rho");
					if (files.Length > 0)
					{
						Program.AAAC(input, files);
					}
					else
					{
						Program.encodea(input, output);
					}
				}
			}
		}

		private static void encodea(string input, string output)
		{
			RhoArchive rhoArchive = new RhoArchive();
			if (!output.EndsWith(".rho"))
				output += ".rho";

			rhoArchive.SaveFolder(input, output, 1);
		}

		private static void encode(string input, string output)
		{
			Rho5Archive rho5Archive = new Rho5Archive();
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

		private static void BtoX(string input)
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

		private static void XtoB(string input)
		{
			XDocument xdoc = XDocument.Load(input);
			if (xdoc.Root == null)
				return;
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

		private static void AAAR(string input)
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

		private static void AAAD(string input)
		{
			byte[] array = File.ReadAllBytes(input);
			string filePath = System.IO.Path.ChangeExtension(input, "pk");
			using FileStream fileStream = new FileStream(filePath, FileMode.Create);
			BinaryWriter binaryWriter = new BinaryWriter(fileStream);
			binaryWriter.WriteKRData(array, false, true);
		}

		private static void AAAC(string input, string[] files)
		{
			string[] whitelist = { "_I04_sn", "_I05_sn", "_R01_sn", "_R02_sn", "_I02_sn", "_I01_sn", "_I03_sn", "_L01_", "_L02_", "_L03_03_", "_L03_", "_L04_", "bazzi_", "arthur_", "bero_", "brodi_", "camilla_", "chris_", "contender_", "crowdr_", "CSO_", "dao_", "dizni_", "erini_", "ethi_", "Guazi_", "halloween_", "homrunDao_", "innerWearSonogong_", "innerWearWonwon_", "Jianbing_", "kephi_", "kero_", "kwanwoo_", "Lingling_", "lodumani_", "mabi_", "Mahua_", "marid_", "mobi_", "mos_", "narin_", "neoul_", "neo_", "nymph_", "olympos_", "panda_", "referee_", "ren_", "Reto_", "run_", "zombie_", "santa_", "sophi_", "taki_", "tiera_", "tutu_", "twoTop_", "twotop_", "uni_", "wonwon_", "zhindaru_", "zombie_", "flyingBook_", "flyingMechanic_", "flyingRedlight_", "crow_", "dragonBoat_", "GiLin_", "maple_", "beach_", "village_", "china_", "factory_", "ice_", "mine_", "nemo_", "world_", "forest_", "_I", "_R", "_S", "_F", "_P", "_K", "_D", "_jp" };
			string[] blacklist = { "character_" };
			string Whitelist = AppDomain.CurrentDomain.BaseDirectory + "Whitelist.ini";
			string Blacklist = AppDomain.CurrentDomain.BaseDirectory + "Blacklist.ini";
			if (File.Exists(Whitelist))
			{
				whitelist = File.ReadAllLines(Whitelist);
			}
			else
			{
				using (StreamWriter writer = new StreamWriter(Whitelist))
				{
					foreach (string white in whitelist)
					{
						writer.WriteLine(white);
					}
				}
			}
			if (File.Exists(Blacklist))
			{
				blacklist = File.ReadAllLines(Blacklist);
			}
			else
			{
				using (StreamWriter blackr = new StreamWriter(Blacklist))
				{
					foreach (string black in blacklist)
					{
						blackr.WriteLine(black);
					}
				}
			}
			XElement root = new XElement("PackFolder", new XAttribute("name", "KartRider"));
			foreach (string file in files)
			{
				string fileName = Path.GetFileName(file);
				string result = fileName;
				foreach (string white in whitelist)
				{
					result = result.Replace(white, white.Replace("_", "!"));
				}
				foreach (string black in blacklist)
				{
					result = result.Replace(black.Replace("_", "!"), black);
				}
				Console.WriteLine(result);
				string[] splitParts = result.Split('_');
				XElement currentFolder = root;
				for (int i = 0; i < splitParts.Length - 1; i++)
				{
					string folderName = splitParts[i];
					XElement? subFolder = currentFolder.Elements("PackFolder")
													 .FirstOrDefault(f => (string?)f.Attribute("name") == folderName);
					if (subFolder == null)
					{
						if (folderName == "character" || folderName == "flyingPet" || folderName == "pet" || folderName == "track")
						{
							subFolder = new XElement("PackFolder", new XAttribute("name", folderName), new XAttribute("loadPass", "1"));
						}
						else
						{
							subFolder = new XElement("PackFolder", new XAttribute("name", folderName));
						}
						currentFolder.Add(subFolder);
					}
					currentFolder = subFolder;
				}
				Rho rho = new Rho(file);
                uint rhoKey = rho.GetFileKey();
                uint dataHash = rho.GetDataHash();
                long size = rho.baseStream.Length;
				string rhoFolderName = splitParts.Length > 0 ? Path.ChangeExtension(splitParts[splitParts.Length - 1], null) : "";
				XElement rhoFolder = new XElement("RhoFolder",
					new XAttribute("name", rhoFolderName.Replace('!', '_')),
					new XAttribute("fileName", fileName),
					new XAttribute("key", rhoKey.ToString()),
					new XAttribute("dataHash", dataHash.ToString()),
					new XAttribute("mediaSize", size.ToString()));
				currentFolder.Add(rhoFolder);
			}

			root.Save(input + "\\aaa.xml");
		}
	}
}
