using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
using KartRider.IO.Packet;

namespace KartRider;

internal static class RhoPacker
{
    private static readonly string[] datapack =
    {
        "boss", "character", "dialog", "dialog2", "effect", "etc_", "flyingPet", "gui", "item", "kart_", "myRoom",
        "pet", "sound", "stage", "stuff", "stuff2", "theme", "track", "trackThumb", "track_", "zeta", "zeta_"
    };

    public static void PackTool(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.EndsWith(".rho") || arg.EndsWith(".rho5"))
            {
                decode(arg);
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
                    encode(arg);
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
                        encodea(arg);
                    }
                }
            }
        }
    }

    private static void encodea(string input)
    {
        string output = input;
        if (!output.EndsWith(".rho"))
            output += ".rho";

        // 保持目录名不变，不做下划线替换
        string baseDir = Path.GetDirectoryName(input) ?? "";
        string dirName = Path.GetFileName(input);
        string targetPath = Path.Combine(baseDir, dirName);

        RhoArchive rhoArchive = new RhoArchive();
        GetAllFiles(targetPath, rhoArchive.RootFolder);

        rhoArchive.SaveTo(output);
    }

    private static void GetAllFiles(string folderPath, RhoFolder folder)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

        List<FileInfo> files = new List<FileInfo>(dirInfo.GetFiles());
        files.Sort(new CustomStringComparerFile());
        foreach (FileInfo file in files)
        {
            string extension = Path.GetExtension(file.Name);
            int fileSize = (int)file.Length;
            RhoFile item = new RhoFile();
            item.DataSource = new FileDataSource(file.FullName);
            item.Name = file.Name;
            item.FileEncryptionProperty = GetFileTypeByExtension(extension, fileSize);
            folder.AddFile(item);
        }

        List<DirectoryInfo> subdirs = new List<DirectoryInfo>(dirInfo.GetDirectories());
        subdirs.Sort(new CustomStringComparerDir());
        foreach (DirectoryInfo subdir in subdirs)
        {
            RhoFolder subFolder = new RhoFolder
            {
                Name = subdir.Name
            };
            folder.AddFolder(subFolder);
            // 递归处理子目录（包括空目录）
            GetAllFiles(subdir.FullName, subFolder);
        }
    }

    private static void encode(string input)
    {
        string aaaPath = Path.Combine(Directory.GetParent(input).FullName, "aaa.pk");
        string regionCode = KartRhoFile.GetRegionCode(aaaPath);
        CountryCode CC = (CountryCode)Enum.Parse(typeof(CountryCode), regionCode, true);
        string output = input;
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

    private static void decode(string input)
    {
        string output = input;
        if (output.EndsWith(".rho"))
            output = output.Replace(".rho", "");
        if (output.EndsWith(".rho5"))
            output = output.Replace(".rho5", "");
        CountryCode CC = CountryCode.CN;
        string aaaPath = Path.Combine(Directory.GetParent(input).FullName, "aaa.pk");
        if (File.Exists(aaaPath))
        {
            string regionCode = KartRhoFile.GetRegionCode(aaaPath);
            CC = (CountryCode)Enum.Parse(typeof(CountryCode), regionCode, true);
        }
        PackFolderManager packFolderManager = new PackFolderManager();
        packFolderManager.OpenSingleFile(input, CC);
        PackFolderInfo rootFolder = packFolderManager.GetRootFolder();
        
        // rho文件名（完整名称，包含.rho扩展名）
        string rhoFileName = Path.GetFileName(input);
        
        // 确保输出目录存在
        Directory.CreateDirectory(output);
        
        // 处理根目录的直接文件
        foreach (var file in rootFolder.GetFilesInfo())
        {
            // 移除rho文件名（包括.rho扩展名）
            string relativePath = file.FullName;
            if (relativePath.StartsWith(rhoFileName + "/"))
                relativePath = relativePath.Substring(rhoFileName.Length + 1);
            else if (relativePath.StartsWith(rhoFileName + "\\"))
                relativePath = relativePath.Substring(rhoFileName.Length + 1);
            
            string fullName = Path.Combine(output, relativePath);
            string dirName = Path.GetDirectoryName(fullName);
            if (!string.IsNullOrEmpty(dirName))
                Directory.CreateDirectory(dirName);
            byte[] data = file.GetData();
            using (FileStream fileStream = new FileStream(fullName, FileMode.OpenOrCreate))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }
        
        // 处理子目录 - rho文件内容的根目录名称（可能与rho文件名相同）
        foreach (PackFolderInfo packFolderInfo in rootFolder.GetFoldersInfo())
        {
            string folderName = packFolderInfo.FolderName;
            // 如果子目录名称与rho文件名相同，跳过这一层
            if (folderName == rhoFileName || folderName == Path.GetFileNameWithoutExtension(rhoFileName))
            {
                // 直接处理其内容到output目录
                foreach (var file in packFolderInfo.GetFilesInfo())
                {
                    string fullName = Path.Combine(output, file.FileName);
                    byte[] data = file.GetData();
                    using (FileStream fileStream = new FileStream(fullName, FileMode.OpenOrCreate))
                    {
                        fileStream.Write(data, 0, data.Length);
                    }
                }
                foreach (var subFolder in packFolderInfo.GetFoldersInfo())
                {
                    string subFolderPath = Path.Combine(output, subFolder.FolderName);
                    Directory.CreateDirectory(subFolderPath);
                    RhoFolders(output, subFolderPath, subFolder);
                }
            }
            else
            {
                string folderPath = Path.Combine(output, folderName);
                Directory.CreateDirectory(folderPath);
                RhoFolders(output, folderPath, packFolderInfo);
            }
        }
    }

    private static void RhoFolders(string output, string currentPath, PackFolderInfo rhoFolders)
    {
        // 确保当前目录存在
        Directory.CreateDirectory(currentPath);

        if (rhoFolders.GetFilesInfo() != null)
        {
            foreach (var item in rhoFolders.GetFilesInfo())
            {
                // 只取文件名部分
                string fileName = item.FileName;
                string fullName = Path.Combine(currentPath, fileName);
                byte[] data = item.GetData();
                using (FileStream fileStream = new FileStream(fullName, FileMode.OpenOrCreate))
                {
                    fileStream.Write(data, 0, data.Length);
                }
            }
        }
        if (rhoFolders.Folders != null)
        {
            foreach (var rhoFolder in rhoFolders.Folders)
            {
                // 只取目录名部分
                string folderName = rhoFolder.FolderName;
                string Folder = Path.Combine(currentPath, folderName);
                Directory.CreateDirectory(Folder);
                    Directory.CreateDirectory(Folder);
                RhoFolders(output, Folder, rhoFolder);
            }
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
        var filePath = Path.ChangeExtension(input, "xml");
        // 保存为UTF-8格式的XML文件
        File.WriteAllText(filePath, output_bml, Encoding.UTF8);
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
        var filePath = Path.ChangeExtension(input, "xml");
        // 保存为UTF-8格式的XML文件
        File.WriteAllText(filePath, output_bml, Encoding.UTF8);
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

    private static int Wcscmp(string s1, string s2)
    {
        if (s1 == s2)
            return 0;
        byte[] bytes1 = Encoding.Unicode.GetBytes(s1);
        byte[] bytes2 = Encoding.Unicode.GetBytes(s2);
        Array.Resize(ref bytes1, bytes1.Length + 2);
        Array.Resize(ref bytes2, bytes2.Length + 2);
        int maxLen = Math.Min(bytes1.Length, bytes2.Length) / 2;
        int i = 0;
        while (i < maxLen && BitConverter.ToUInt16(bytes1, i * 2) == BitConverter.ToUInt16(bytes2, i * 2))
        {
            i++;
        }
        if (i >= maxLen)
            return 0;
        ushort c1 = BitConverter.ToUInt16(bytes1, i * 2);
        ushort c2 = BitConverter.ToUInt16(bytes2, i * 2);
        return c1 - c2;
    }

    private class CustomStringComparerDir : IComparer<DirectoryInfo>
    {
        public int Compare(DirectoryInfo d1, DirectoryInfo d2)
        {
            return Wcscmp(d1.Name, d2.Name);
        }
    }

    private class CustomStringComparerFile : IComparer<FileInfo>
    {
        public int Compare(FileInfo f1, FileInfo f2)
        {
            string name1 = Path.GetFileNameWithoutExtension(f1.Name);
            string name2 = Path.GetFileNameWithoutExtension(f2.Name);
            if (name1 == name2)
                return Wcscmp(Path.GetExtension(f1.Name), Path.GetExtension(f2.Name));
            return Wcscmp(name1, name2);
        }
    }

    private static RhoFileProperty GetFileTypeByExtension(string ext, int fileSize)
    {
        switch (ext.ToLower())
        {
            case ".1s":
            case ".dds":
            case ".tga":
            case ".bmh":
            case ".bmx":
            case ".f30":
            case ".hdr":
            case ".fft":
            case ".wav":
                return RhoFileProperty.Compressed;
            case ".uset":
            case ".xml":
                return RhoFileProperty.Encrypted;
            case ".png":
            case ".kap":
                return (fileSize <= 256) ? RhoFileProperty.Encrypted : RhoFileProperty.PartialEncrypted;
            case ".ogg":
            case ".jpg":
            case ".flac":
            case ".ksv":
                return RhoFileProperty.PartialEncrypted;
            case ".bml":
                return RhoFileProperty.CompressedEncrypted;
            default:
                return RhoFileProperty.None;
        }
    }
}