using KartRider.Library.File;
using Launcher.App.Profile;
using Launcher.Library.Data;
using Launcher.Library.File;
using Launcher.Library.File.OldImplements;
using Launcher.Library.File.Rho;
using Launcher.Library.File.Rho5;
using Launcher.Library.IO;
using Launcher.Library.Xml;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Launcher.App.Utility
{
    /// <summary>
    /// A utility class including message boxes and helper functions.
    /// </summary>
	public static class Utils
    {
        #region messages

        public static void MsgKartIsRunning()
        {
            MessageBox.Show("跑跑卡丁车已经运行了!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void MsgMultiInstance()
        {
            MessageBox.Show("已经有一个程序在侦听该端口了! 不可以同时运行多个程序对同一端口进行侦听!\n点击确认退出程序.", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }

        public static void MsgFileNotFound()
        {
            Console.WriteLine($"Error: 找不到 {FileName.KartRider} 或 {FileName.PinFile}.");
            MessageBox.Show(FileName.KartRider + " 或 " + FileName.PinFile + " 找不到文件!\n点击确认退出程序.", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }

        public static void MsgErrorReadData()
        {
            MessageBox.Show("读取游戏Data内文件失败！\n请检查游戏是否完整，或尝试重新安装游戏！\n点击确认退出程序", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }

        public static void MsgWelcome()
        {
            StringBuilder welcomeMessage = new();
            welcomeMessage.AppendLine("欢迎你下载并使用跑跑卡丁车启动器！");
            welcomeMessage.AppendLine("当你看到这个提示，说明你第一次成功运行了该软件。");
            welcomeMessage.AppendLine("本启动器完全免费且开源，唯一下载方式为 GitHub Release，切勿相信任何收费下载渠道！");
            welcomeMessage.AppendLine("本软件由社区维护，源码开源托管于 GitHub，仓库链接请见软件主界面。");
            welcomeMessage.AppendLine("如果想获取更多帮助，可以打开位于主界面上的帮助文档链接。");
            welcomeMessage.AppendLine("如果觉得喜欢，或者本软件对你的练习有帮助，请考虑在 GitHub 上给本项目一个Star⭐");
            welcomeMessage.AppendLine("祝你游戏愉快！<(￣︶￣)↗[GO!]");
            MessageBox.Show(welcomeMessage.ToString(), "欢迎", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void TryKillKart()
        {
            Process[] gameProcesses = Process.GetProcessesByName("KartRider");
            if (gameProcesses.Length > 0)
            {
                if ((int)MessageBox.Show("确认要强制停止所有跑跑卡丁车游戏进程吗?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != 1)
                    return;

                foreach (Process gProcess in gameProcesses)
                {
                    try
                    {
                        gProcess.Kill();
                        gProcess.WaitForExit();
                        Console.WriteLine("成功强制关闭游戏进程!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"无法强制关闭游戏进程: {ex.Message}");
                    }
                }
                gameProcesses = Process.GetProcessesByName("KartRider");
                if (gameProcesses.Length == 0)
                    MessageBox.Show("所有游戏进程已成功关闭!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("部分游戏进程无法关闭, 请尝试使用任务管理器!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Console.WriteLine("没有找到正在运行的游戏进程!");
                MessageBox.Show("没有找到正在运行的跑跑卡丁车进程!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region utils

        public static bool CheckGameAvailability(string gamePath)
        {
            return !string.IsNullOrEmpty(gamePath)
                && File.Exists(Path.Combine(gamePath, FileName.KartRider))
                && File.Exists(Path.Combine(gamePath, FileName.PinFile));
        }

        public static bool TryOpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开超链接时发生错误: {ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Print a dividing line with '-' in the console for better readability.
        /// </summary>
        /// <param name="count">The number of '-' in the line, 50 for default</param>
        public static void PrintDivLine(ushort count = 50)
        {
            string line = "";
            while (count-- > 0)
                line += '-';
            Console.WriteLine(line);
        }

        /// <summary>
        /// Get current country code from ipinfo.io
        /// </summary>
        /// <returns>A 2 char string represents the country</returns>
        public static async Task<string> GetCountryAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://ipinfo.io/json");
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(json);
                        if (data.TryGetValue("country", out JToken? countryToken))
                        {
                            return countryToken.ToString();
                        }
                        else
                        {
                            Console.WriteLine("响应中未找到 'country' 字段.");
                            return "";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"请求 IP 地址失败, 状态码: {response.StatusCode}");
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"请求 IP 地址时发生异常: {ex.Message}");
                return "";
            }
        }

        #endregion

        #region Pack Tool

        private static readonly string[] dataPack =
        [
            "boss", "character", "dialog", "dialog2", "effect", "etc_", "flyingPet", "gui", "item", "kart_", "myRoom",
            "pet", "sound", "stage", "stuff", "stuff2", "theme", "track", "trackThumb", "track_", "zeta", "zeta_"
        ];

        private static readonly string[] whitelist =
        [
            "_I04_sn", "_I05_sn", "_R01_sn", "_R02_sn",
            "_I02_sn", "_I01_sn", "_I03_sn",
            "_L01_", "_L02_", "_L03_03_", "_L03_", "_L04_",
            "bazzi_", "arthur_", "bero_", "brodi_", "camilla_", "chris_", "contender_", "crowdr_", "CSO_", "dao_", "dizni_",
            "erini_", "ethi_", "Guazi_", "halloween_", "homrunDao_", "innerWearSonogong_", "innerWearWonwon_", "Jianbing_",
            "kephi_", "kero_", "kwanwoo_", "Lingling_", "lodumani_", "mabi_", "Mahua_", "marid_", "mobi_", "mos_", "narin_",
            "neoul_", "neo_", "nymph_", "olympos_", "panda_", "referee_", "ren_", "Reto_", "run_", "zombie_", "santa_",
            "sophi_", "taki_", "tiera_", "tutu_", "twoTop_", "twotop_", "uni_", "wonwon_", "zhindaru_", "zombie_", "flyingBook_",
            "flyingMechanic_", "flyingRedlight_", "crow_", "dragonBoat_", "GiLin_",
            "maple_", "beach_", "village_", "china_", "factory_", "ice_", "mine_", "nemo_", "world_", "forest_",
            "_I", "_R", "_S", "_F", "_P", "_K", "_D", "_jp", "_A0"
        ];

        private static readonly string[] blacklist = ["character_"];

        public static void ProcessPack(string[] args)
        {
            foreach (string arg in args)
            {
                if (!Directory.Exists(arg))
                {
                    Console.WriteLine($"无效的文件路径: {arg}");
                    continue;
                }

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
                    var temp = Directory.GetDirectories(arg);
                    if (temp.All(dir => dataPack.Contains(Path.GetFileName(dir))) && temp.Length != 0)
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
                            string? parent = Path.GetDirectoryName(arg);
                            if (parent == null)
                            {
                                Console.WriteLine($"Error: Unable to find .rho files in {parent}");
                                return;
                            }
                            files = Directory.GetFiles(parent, "*.rho");
                            AAAC(parent, files);
                        }
                    }
                }
            }
            Environment.Exit(0);
        }

        private static void encodea(string input, string output)
        {
            if (!output.EndsWith(".rho"))
                output += ".rho";

            SaveFolder(input, output);
        }

        private static void SaveFolder(string input, string output)
        {
            var rhoArchive = new RhoArchive();
            GetAllFiles(input, new List<string>(), rhoArchive.RootFolder);

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
                rho5Archive.SaveFolder(input, dataPackName, fullName, ProfileService.ProfileConfig.ServerSetting.CC, dataPackID);
            }
            else
            {
                Console.WriteLine($"路径不存在: {output}");
            }
        }

        private static void decode(string input, string output)
        {
            if (output.EndsWith(".rho"))
                output = Path.GetDirectoryName(output);
            else if (output.EndsWith(".rho5"))
                output = output.Replace(".rho5", "");
            var packFolderManager = new PackFolderManager();
            packFolderManager.OpenSingleFile(input, ProfileService.ProfileConfig.ServerSetting.CC);
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

        #endregion
    }
}
