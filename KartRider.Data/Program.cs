using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
using KartRider.IO.Packet;
using Microsoft.Win32;
using RHOParser;
using Profile;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace KartRider
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public static IntPtr consoleHandle;
        public static Launcher LauncherDlg;
        public static GetKart GetKartDlg;
        public static bool SpeedPatch;
        public static bool PreventItem;
        public static Encoding targetEncoding = Encoding.UTF8;
        public static CountryCode CC = CountryCode.CN;

        private static readonly string[] datapack =
        {
            "boss", "character", "dialog", "dialog2", "effect", "etc_", "flyingPet", "gui", "item", "kart_", "myRoom",
            "pet", "sound", "stage", "stuff", "stuff2", "theme", "track", "trackThumb", "track_", "zeta", "zeta_"
        };

        [STAThread]
        private static async Task Main(string[] args)
        {
            if (File.Exists(FileName.Update_File))
            {
                File.Delete(FileName.Update_File);
            }
            if (Directory.Exists(FileName.Update_Folder))
            {
                Directory.Delete(FileName.Update_Folder, true);
            }
            AllocConsole();

            // 初始化自适应编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SetAdaptiveConsoleEncoding();

            if (!await Update.UpdateDataAsync())
            {
                if (File.Exists(FileName.Load_CC))
                {
                    string textValue = System.IO.File.ReadAllText(FileName.Load_CC);
                    Program.CC = (CountryCode)Enum.Parse(typeof(CountryCode), textValue);
                }
                else
                {
                    if (!Directory.Exists(FileName.ProfileDir))
                    {
                        Directory.CreateDirectory(FileName.ProfileDir);
                    }
                    using (StreamWriter streamWriter = new StreamWriter(FileName.Load_CC, false))
                    {
                        streamWriter.Write(Program.CC.ToString());
                    }
                }
                if (args == null || args.Length == 0)
                {
                    string TCGame = "HKEY_CURRENT_USER\\SOFTWARE\\TCGame\\kart";
                    string RootDirectory = (string)Registry.GetValue(TCGame, "gamepath", null);
                    if (File.Exists(FileName.pinFile) && File.Exists(FileName.KartRider))
                    {
                        RootDirectory = FileName.appDir;
                    }
                    else if (File.Exists(RootDirectory + @"KartRider.pin") && File.Exists(RootDirectory + @"KartRider.exe"))
                    {
                        RootDirectory = Path.GetFullPath((string)Registry.GetValue(TCGame, "gamepath", null));
                    }
                    else
                    {
                        LauncherSystem.MessageBoxType3();
                        return;
                    }
                    if (!string.IsNullOrEmpty(RootDirectory))
                    {
                        try
                        {
                            KartRhoFile.Dump(Path.GetFullPath(Path.Combine(RootDirectory, @"Data\aaa.pk")));
                            KartRhoFile.packFolderManager.Reset();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"读取Data文件时出错: {ex.Message}");
                        }
                        consoleHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                        if (!File.Exists(FileName.Load_Console))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(FileName.Load_Console, false))
                            {
                                streamWriter.Write("0");
                            }
                        }
                        string textValue = System.IO.File.ReadAllText(FileName.Load_Console);
                        if (textValue == "0")
                        {
                            ShowWindow(consoleHandle, SW_HIDE);
                        }
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Launcher StartLauncher = new Launcher();
                        Program.LauncherDlg = StartLauncher;
                        Program.LauncherDlg.kartRiderDirectory = RootDirectory;
                        Launcher.KartRider = Path.GetFullPath(Path.Combine(RootDirectory, @"KartRider.exe"));
                        Launcher.pinFile = Path.GetFullPath(Path.Combine(RootDirectory, @"KartRider.pin"));
                        Launcher.pinFileBak = Path.GetFullPath(Path.Combine(RootDirectory, @"KartRider-bak.pin"));
                        Application.Run(StartLauncher);
                    }
                }
                else
                {
                    PackTool(args);
                }
            }
        }

        public static void SetAdaptiveConsoleEncoding()
        {
            try
            {
                // 1. 检测操作系统类型
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
                // 2. 优先尝试 UTF-8（跨平台通用）
                targetEncoding = Encoding.UTF8;

                // 3. Windows 中文环境特殊处理（部分终端默认 GBK）
                if (isWindows)
                {
                    // 获取系统默认区域语言
                    var systemCulture = CultureInfo.InstalledUICulture;
                    bool isChineseCulture = systemCulture.Name.StartsWith("zh-");

                    // 中文系统尝试使用 GBK（CP936），避免 UTF-8 在部分老终端乱码
                    if (isChineseCulture)
                    {
                        try
                        {
                            targetEncoding = Encoding.GetEncoding(936); // 936 对应 GBK
                        }
                        catch (ArgumentException)
                        {
                            // 极少数情况系统不支持 GBK，则 fallback 到 UTF-8
                            targetEncoding = Encoding.UTF8;
                        }
                    }
                }

                // 4. 应用编码设置（输出/输入保持一致）
                Console.OutputEncoding = targetEncoding;
                Console.InputEncoding = targetEncoding;

                // 5. 验证编码是否生效（可选）
                Console.WriteLine($"已适配编码: {targetEncoding.EncodingName}");
            }
            catch (Exception ex)
            {
                // 异常时使用系统默认编码作为最后保障
                Console.WriteLine($"编码设置失败，使用默认编码: {ex.Message}");
            }
        }

        public static void PackTool(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.EndsWith(".rho") || arg.EndsWith(".rho5"))
                {
                    decode(arg, arg);
                }
                else if (arg.EndsWith("aaa.xml"))
                {
                    AAAD(arg);
                }
                else if (arg.EndsWith(".xml"))
                {
                    XtoB(arg);
                }
                else if (arg.EndsWith(".bml"))
                {
                    BtoX(arg);
                }
                else if (arg.EndsWith(".pk"))
                {
                    AAAR(arg);
                }
                else
                {
                    if (!Directory.Exists(arg))
                        return;
                    var temp = Directory.GetDirectories(arg);
                    if (temp.All(dir => datapack.Contains(Path.GetFileName(dir))) && temp.Length != 0)
                    {
                        encode(arg, arg);
                    }
                    else
                    {
                        var files = Directory.GetFiles(arg, "*.rho");
                        if (files.Length > 0)
                        {
                            AAAC(arg, files);
                        }
                        else
                        {
                            encodea(arg, arg);
                            var parent = Path.GetDirectoryName(arg);
                            files = Directory.GetFiles(parent, "*.rho");
                            AAAC(parent, files);
                        }
                    }
                }
            }
        }

        private static void encodea(string input, string output)
        {
            if (!output.EndsWith(".rho"))
                output += ".rho";

            SaveFolder(input, output);
        }

        private static void SaveFolder(string intput, string output)
        {
            var rhoArchive = new RhoArchive();
            GetAllFiles(intput, new List<string>(), rhoArchive.RootFolder);

            rhoArchive.SaveTo(output);
            rhoArchive.Close();
        }

        private static void GetAllFiles(string folderPath, List<string> fileList, RhoFolder folder)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                var item = new RhoFile();
                item.DataSource = new FileDataSource(file);
                item.Name = Path.GetFileName(file);
                if (extension == ".bml" || extension == ".bmh" || extension == ".bmx" || extension == ".kap" ||
                    extension == ".ksv" || extension == ".1s" || extension == ".dds")
                    item.FileEncryptionProperty = RhoFileProperty.Compressed;
                else if (extension == ".xml")
                    item.FileEncryptionProperty = RhoFileProperty.Encrypted;
                else
                    item.FileEncryptionProperty = RhoFileProperty.None;
                folder.AddFile(item);
            }

            var subdirectories = Directory.GetDirectories(folderPath);
            foreach (var subdirectory in subdirectories)
            {
                var folder2 = new RhoFolder
                {
                    Name = Path.GetFileName(subdirectory)
                };
                folder.AddFolder(folder2);
                GetAllFiles(subdirectory, fileList, folder2);
            }
        }

        private static void encode(string input, string output)
        {
            var rho5Archive = new Rho5Archive();
            if (!output.EndsWith(".rho5"))
                output += ".rho5";
            var fileInfo = new FileInfo(output);
            if (fileInfo.Directory != null)
            {
                var fullName = fileInfo.Directory.FullName;
                if (!Directory.Exists(fullName))
                    Directory.CreateDirectory(fullName);
                var strArray = fileInfo.Name.Replace(".rho5", "").Split("_");
                var dataPackName = strArray[0];
                var dataPackID = 0;
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
                output = Path.GetDirectoryName(output);
            else if (output.EndsWith(".rho5"))
                output = output.Replace(".rho5", "");
            var packFolderManager = new PackFolderManager();
            packFolderManager.OpenSingleFile(input, CC);
            var packFolderInfoQueue = new Queue<PackFolderInfo>();
            packFolderInfoQueue.Enqueue(packFolderManager.GetRootFolder());
            packFolderManager.GetRootFolder();
            while (packFolderInfoQueue.Count > 0)
            {
                var packFolderInfos = packFolderInfoQueue.Dequeue();
                foreach (var packFolderInfo in packFolderInfos.GetFoldersInfo())
                {
                    var fileName = Path.GetFileNameWithoutExtension(packFolderInfo.FolderName);
                    RhoFolders(output, output, packFolderInfo);
                }
            }
        }

        private static void RhoFolders(string input, string output, PackFolderInfo rhoFolders)
        {
            if (rhoFolders.GetFilesInfo() != null)
                foreach (var item in rhoFolders.GetFilesInfo())
                {
                    var fullName = input + "/" + item.FullName.Replace(".rho", "");
                    var Name = Path.GetDirectoryName(fullName);
                    if (!Directory.Exists(Name))
                        Directory.CreateDirectory(Name);
                    var data = item.GetData();
                    using (var fileStream = new FileStream(fullName, FileMode.OpenOrCreate))
                    {
                        fileStream.Write(data, 0, data.Length);
                    }
                }

            if (rhoFolders.Folders != null)
                foreach (var rhoFolder in rhoFolders.Folders)
                {
                    var Folder = output + "/" + rhoFolder.FolderName;
                    RhoFolders(input, Folder, rhoFolder);
                }
        }

        private static void BtoX(string input)
        {
            if (!File.Exists(input))
                return;
            var data = File.ReadAllBytes(input);
            var bxd = new BinaryXmlDocument();
            bxd.Read(Encoding.GetEncoding("UTF-16"), data);
            var output_bml = bxd.RootTag.ToString();
            var output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
            var filePath = Path.ChangeExtension(input, "xml");
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(output_data, 0, output_data.Length);
            }
        }

        private static void XtoB(string input)
        {
            var xdoc = XDocument.Load(input);
            if (xdoc.Root == null)
                return;
            var childCounts = CountChildren(xdoc.Root, 0, new List<int>());
            using (var reader = XmlReader.Create(input))
            {
                using (var outPacket = new OutPacket())
                {
                    var Count = 0;
                    while (reader.Read())
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            var elementName = reader.Name;
                            var attCount = reader.AttributeCount;
                            outPacket.WriteString(elementName);
                            outPacket.WriteInt();
                            outPacket.WriteInt(attCount);
                            for (var i = 0; i < attCount; i++)
                            {
                                reader.MoveToAttribute(i);
                                var attName = reader.Name;
                                outPacket.WriteString(attName);
                                var attValue = reader.Value;
                                outPacket.WriteString(attValue);
                            }

                            outPacket.WriteInt(childCounts[Count]);
                            Count++;
                            reader.MoveToElement();
                        }

                    var byteArray = outPacket.ToArray();
                    var filePath = Path.ChangeExtension(input, "bml");
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(byteArray, 0, byteArray.Length);
                    }
                }
            }
        }

        public static List<int> CountChildren(XElement element, int level, List<int> childCounts)
        {
            var childCount = element.Elements().Count();
            childCounts.Add(childCount);
            foreach (var child in element.Elements()) CountChildren(child, level + 1, childCounts);
            return childCounts;
        }

        private static void AAAR(string input)
        {
            if (!File.Exists(input))
                return;
            using var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);
            var totalLength = binaryReader.ReadInt32();
            var array = binaryReader.ReadKRData(totalLength);
            fileStream.Close();
            var bxd = new BinaryXmlDocument();
            bxd.Read(Encoding.GetEncoding("UTF-16"), array);
            var output_bml = bxd.RootTag.ToString();
            var output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
            var filePath = Path.ChangeExtension(input, "xml");
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(output_data, 0, output_data.Length);
            }
        }

        private static void AAAD(string input)
        {
            var xdoc = XDocument.Load(input);
            if (xdoc.Root == null)
                return;
            var childCounts = CountChildren(xdoc.Root, 0, new List<int>());
            byte[] byteArray;
            using (var reader = XmlReader.Create(input))
            {
                using (var outPacket = new OutPacket())
                {
                    var Count = 0;
                    while (reader.Read())
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            var elementName = reader.Name;
                            var attCount = reader.AttributeCount;
                            outPacket.WriteString(elementName);
                            outPacket.WriteInt();
                            outPacket.WriteInt(attCount);
                            for (var i = 0; i < attCount; i++)
                            {
                                reader.MoveToAttribute(i);
                                var attName = reader.Name;
                                outPacket.WriteString(attName);
                                var attValue = reader.Value;
                                outPacket.WriteString(attValue);
                            }

                            outPacket.WriteInt(childCounts[Count]);
                            Count++;
                            reader.MoveToElement();
                        }

                    byteArray = outPacket.ToArray();
                }
            }

            var filePath = Path.ChangeExtension(input, "pk");
            using var fileStream = new FileStream(filePath, FileMode.Create);
            {
                var binaryWriter = new BinaryWriter(fileStream);
                binaryWriter.Write(0);
                var KRDataLength = binaryWriter.WriteKRData(byteArray, false, true);
                binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                binaryWriter.Write(KRDataLength);
            }
        }

        private static void AAAC(string input, string[] files)
        {
            string[] whitelist =
            {
                "_I04_sn", "_I05_sn", "_R01_sn", "_R02_sn", "_I02_sn", "_I01_sn", "_I03_sn", "_L01_", "_L02_", "_L03_03_",
                "_L03_", "_L04_", "bazzi_", "arthur_", "bero_", "brodi_", "camilla_", "chris_", "contender_", "crowdr_",
                "CSO_", "dao_", "dizni_", "erini_", "ethi_", "Guazi_", "halloween_", "homrunDao_", "innerWearSonogong_",
                "innerWearWonwon_", "Jianbing_", "kephi_", "kero_", "kwanwoo_", "Lingling_", "lodumani_", "mabi_", "Mahua_",
                "marid_", "mobi_", "mos_", "narin_", "neoul_", "neo_", "nymph_", "olympos_", "panda_", "referee_", "ren_",
                "Reto_", "run_", "zombie_", "santa_", "sophi_", "taki_", "tiera_", "tutu_", "twoTop_", "twotop_", "uni_",
                "wonwon_", "zhindaru_", "zombie_", "flyingBook_", "flyingMechanic_", "flyingRedlight_", "crow_",
                "dragonBoat_", "GiLin_", "maple_", "beach_", "village_", "china_", "factory_", "ice_", "mine_", "nemo_",
                "world_", "forest_", "_I", "_R", "_S", "_F", "_P", "_K", "_D", "_jp", "_A0"
            };
            string[] blacklist = { "character_" };

            var root = new XElement("PackFolder", new XAttribute("name", "KartRider"));
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var result = fileName;
                foreach (var white in whitelist) result = result.Replace(white, white.Replace("_", "!"));
                foreach (var black in blacklist) result = result.Replace(black.Replace("_", "!"), black);
                var splitParts = result.Split('_');
                var currentFolder = root;
                for (var i = 0; i < splitParts.Length - 1; i++)
                {
                    var folderName = splitParts[i];
                    var subFolder = currentFolder.Elements("PackFolder")
                        .FirstOrDefault(f => (string)f.Attribute("name") == folderName);
                    if (subFolder == null)
                    {
                        if (folderName == "character" || folderName == "flyingPet" || folderName == "pet" ||
                            folderName == "track")
                            subFolder = new XElement("PackFolder", new XAttribute("name", folderName),
                                new XAttribute("loadPass", "1"));
                        else
                            subFolder = new XElement("PackFolder", new XAttribute("name", folderName));
                        currentFolder.Add(subFolder);
                    }

                    currentFolder = subFolder;
                }

                var rho = new Rho(file);
                var rhoKey = rho.GetFileKey();
                var dataHash = rho.GetDataHash();
                var size = rho.baseStream.Length;
                var rhoFolderName = splitParts.Length > 0
                    ? Path.ChangeExtension(splitParts[splitParts.Length - 1], null)
                    : "";
                var rhoFolder = new XElement("RhoFolder",
                    new XAttribute("name", rhoFolderName.Replace('!', '_')),
                    new XAttribute("fileName", fileName),
                    new XAttribute("key", rhoKey.ToString()),
                    new XAttribute("dataHash", dataHash.ToString()),
                    new XAttribute("mediaSize", size.ToString()));
                currentFolder.Add(rhoFolder);
                rho.Dispose();
            }

            root.Save(input + "\\aaa.xml");
        }
    }
}
