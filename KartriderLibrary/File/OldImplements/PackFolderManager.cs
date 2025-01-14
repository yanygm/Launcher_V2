using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Xml;
using System.IO;
using System.ComponentModel;
using KartLibrary.Data;
using KartLibrary.Consts;

namespace KartLibrary.File
{
    [Obsolete("PackFolderManager class is deprecated. Use RhoStorageSystem instead.")]
    public class PackFolderManager
    {
        public bool Initizated { get; private set; } = false;
        public CountryCode regionCode = CountryCode.None;
        private struct ProcessObj
        {
            public string Path;

            public BinaryXmlTag Obj;

            public PackFolderInfo Parent;
        }

        public PackFolderManager()
        {

        }

        //private List<PackFolderInfo> RootFolder { get; init; } = new List<PackFolderInfo>();

        private PackFolderInfo RootFolder = new PackFolderInfo();

        private LinkedList<Rho> RhoPool { get; init; } = new LinkedList<Rho>();

        private LinkedList<Rho5> Rho5Pool { get; init; } = new LinkedList<Rho5>();

        public void OpenDataFolder(string aaaPkFilePath)
        {
            FileInfo fileInfo = new FileInfo(aaaPkFilePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException(aaaPkFilePath);
            Reset();
            FileStream fileStream = new FileStream(aaaPkFilePath, FileMode.Open);
            BinaryReader br = new BinaryReader(fileStream);
            int dataLen = br.ReadInt32();
            byte[] aaapkData = br.ReadKRData(dataLen);
            fileStream.Close();
            BinaryXmlDocument bxmlDoc = new BinaryXmlDocument();
            bxmlDoc.Read(Encoding.GetEncoding("UTF-16"), aaapkData);
            BinaryXmlTag rootTag = bxmlDoc.RootTag;
            Queue<ProcessObj> ProcessQue = new Queue<ProcessObj>();
            if (rootTag.Name != "PackFolder" || rootTag.GetAttribute("name") != "KartRider")
            {
                throw new NotSupportedException($"aaa.pk file not support.");
            }
            foreach (BinaryXmlTag subtag in rootTag.Children)
            {
                ProcessQue.Enqueue(new ProcessObj
                {
                    Obj = subtag,
                    Parent = null,
                    Path = ""
                });
            }
            HashSet<string> fileNameSet = new HashSet<string>();
            while (ProcessQue.Count > 0)
            {
                ProcessObj currectProcObj = ProcessQue.Dequeue();
                BinaryXmlTag currentTag = currectProcObj.Obj;
                switch (currentTag.Name)
                {
                    case "PackFolder":
                        string subname = currentTag.GetAttribute("name");
                        PackFolderInfo newSubFolder = new PackFolderInfo()
                        {
                            FolderName = currectProcObj.Parent is null ? $"{subname}" : subname,
                            FullName = currectProcObj.Parent is null ? $"{subname}" : $"{currectProcObj.Path}/{subname}",
                            ParentFolder = currectProcObj.Parent
                        };
                        foreach (BinaryXmlTag subTag in currentTag.Children)
                        {
                            ProcessObj newProcObj = new ProcessObj()
                            {
                                Path = newSubFolder.FullName,
                                Parent = newSubFolder,
                                Obj = subTag
                            };
                            ProcessQue.Enqueue(newProcObj);
                        }
                        if (currectProcObj.Parent is not null)
                            currectProcObj.Parent.Folders.Add(newSubFolder);
                        else
                            RootFolder.Folders.Add(newSubFolder);
                        break;
                    case "RhoFolder":
                        string name = currentTag.GetAttribute("name");
                        string fileName = currentTag.GetAttribute("fileName");
                        uint rhoFileKey = uint.Parse(currentTag.GetAttribute("key"));
                        if (fileNameSet.Contains(fileName))
                            continue;
                        fileNameSet.Add(fileName);
                        PackFolderInfo NewFolder = new PackFolderInfo()
                        {
                            FolderName = currectProcObj.Parent is null ? $"{name}" : name,
                            FullName = currectProcObj.Parent is null ? $"{name}" : $"{currectProcObj.Path}/{name}",
                            ParentFolder = currectProcObj.Parent
                        };
                        if (name == "")
                            NewFolder = currectProcObj.Parent;
                        else
                        {
                            if (currectProcObj.Parent is null)
                                RootFolder.Folders.Add(NewFolder);
                            else
                                currectProcObj.Parent.Folders.Add(NewFolder);
                        }
                        Rho rhoFile = new Rho($"{fileInfo.DirectoryName}\\{fileName}", rhoFileKey);
                        Queue<(PackFolderInfo, RhoDirectory)> dirQue = new Queue<(PackFolderInfo, RhoDirectory)>();
                        RhoDirectory rootDir = rhoFile.RootDirectory;
                        dirQue.Enqueue((NewFolder, rootDir));
                        while (dirQue.Count > 0)
                        {
                            (PackFolderInfo, RhoDirectory) curObj = dirQue.Dequeue();
                            PackFolderInfo currectPackFolder = curObj.Item1;
                            foreach (RhoFileInfo file in curObj.Item2.GetFiles())
                            {
                                PackFileInfo newFileInfo = new PackFileInfo()
                                {
                                    FileName = file.FullFileName,
                                    FileSize = file.FileSize,
                                    FullName = $"{currectPackFolder.FullName}/{file.FullFileName}",
                                    OriginalFile = file,
                                    PackFileType = PackFileType.RhoFile
                                };
                                currectPackFolder.Files.Add(newFileInfo);
                            }
                            foreach (RhoDirectory dir in curObj.Item2.GetDirectories())
                            {
                                PackFolderInfo subDirInfo = new PackFolderInfo()
                                {
                                    FolderName = dir.DirectoryName,
                                    FullName = $"{currectPackFolder.FullName}/{dir.DirectoryName}",
                                    ParentFolder = currectPackFolder
                                };
                                currectPackFolder.Folders.Add(subDirInfo);
                                dirQue.Enqueue((subDirInfo, dir));
                            }
                        }
                        RhoPool.AddLast(rhoFile);
                        break;
                }
            }

            PackFolderInfo[] ZETA_Folders = GetDirectories("zeta");
            if (Array.Exists(ZETA_Folders, x => x.FolderName == "kr"))
                regionCode = CountryCode.KR;
            else if (Array.Exists(ZETA_Folders, x => x.FolderName == "cn"))
                regionCode = CountryCode.CN;
            else if (Array.Exists(ZETA_Folders, x => x.FolderName == "tw"))
                regionCode = CountryCode.TW;

            foreach (string file in Directory.GetFiles(fileInfo.DirectoryName, "*.rho5"))
            {
                Rho5 rho5File = new Rho5(file, regionCode);
                Rho5Pool.AddLast(rho5File);
                foreach (Rho5FileInfo rho5FileInfo in rho5File.Files)
                {
                    string path = rho5FileInfo.FullPath;
                    string[] path_part = path.Split('/');
                    PackFolderInfo currentFolder = new PackFolderInfo()
                    {
                        Folders = RootFolder.Folders,
                        FolderName = "",
                        FullName = "",
                        ParentFolder = null
                    };
                    int depth = path_part.Length;
                    foreach (string part in path_part)
                    {
                        if (depth <= 1)
                        {
                            currentFolder.Files.Add(new PackFileInfo()
                            {
                                FileName = part,
                                FileSize = rho5FileInfo.DecompressedSize,
                                FullName = currentFolder.FullName == "" ? part : $"{currentFolder.FullName}/{part}",
                                PackFileType = PackFileType.Rho5File,
                                OriginalFile = rho5FileInfo
                            });
                        }
                        else
                        {
                            PackFolderInfo? foundFolder = currentFolder.Folders.Find(x => x.FolderName == part);
                            if (foundFolder is null)
                            {
                                currentFolder.Folders.Add(
                                foundFolder = new PackFolderInfo()
                                {
                                    FolderName = part,
                                    ParentFolder = currentFolder,
                                    FullName = currentFolder.FullName == "" ? part : $"{currentFolder.FullName}/{part}",
                                });
                            }
                            currentFolder = foundFolder;
                        }
                        depth--;
                    }
                }
            }
            for (int i = 0; i < RootFolder.Folders.Count; i++)
                RootFolder.Folders[i].ParentFolder = RootFolder;
            Initizated = true;
        }

        public async Task OpenDataFolderAsync(string aaaPkFilePath, ProgressChangedEventHandler? onProgressChange = null)
        {

        }

        public void OpenSingleFile(string rhoFile, CountryCode regionCode)
        {
            FileInfo fileInfo = new FileInfo(rhoFile);
            if (!fileInfo.Exists)
                throw new FileNotFoundException(rhoFile);
            Reset();
            if (rhoFile.EndsWith(".rho5"))
            {
                OpenRho5File(rhoFile, regionCode);
                return;
            }
            Rho rho = new Rho(rhoFile);
            Queue<(PackFolderInfo, RhoDirectory)> dirQue = new Queue<(PackFolderInfo, RhoDirectory)>();

            PackFolderInfo rootFolder = new PackFolderInfo()
            {
                FolderName = fileInfo.Name,
                FullName = $"{fileInfo.Name}",
                ParentFolder = null
            };
            RootFolder.Folders.Add(rootFolder);
            dirQue.Enqueue((rootFolder, rho.RootDirectory));

            while (dirQue.Count > 0)
            {
                (PackFolderInfo, RhoDirectory) curObj = dirQue.Dequeue();
                PackFolderInfo currectPackFolder = curObj.Item1;

                foreach (RhoFileInfo file in curObj.Item2.GetFiles())
                {
                    PackFileInfo newFileInfo = new PackFileInfo()
                    {
                        FileName = file.FullFileName,
                        FileSize = file.FileSize,
                        FullName = $"{currectPackFolder.FullName}/{file.FullFileName}",
                        OriginalFile = file,
                        PackFileType = PackFileType.RhoFile
                    };
                    currectPackFolder.Files.Add(newFileInfo);
                }
                foreach (RhoDirectory dir in curObj.Item2.GetDirectories())
                {
                    PackFolderInfo subDirInfo = new PackFolderInfo()
                    {
                        FolderName = dir.DirectoryName,
                        FullName = $"{currectPackFolder.FullName}/{dir.DirectoryName}",
                        ParentFolder = currectPackFolder
                    };
                    currectPackFolder.Folders.Add(subDirInfo);
                    dirQue.Enqueue((subDirInfo, dir));
                }
            }
            RhoPool.AddLast(rho);
            for (int i = 0; i < RootFolder.Folders.Count; i++)
                RootFolder.Folders[i].ParentFolder = RootFolder;
            Initizated = true;
        }

        private void OpenRho5File(string file, CountryCode regionCode)
        {
            Rho5 rho = new Rho5(file, regionCode);
            Rho5Pool.AddLast(rho);
            Rho5FileInfo[] files = rho.Files;
            foreach (Rho5FileInfo rho5FileInfo in files)
            {
                string[] array = rho5FileInfo.FullPath.Split('/');
                PackFolderInfo packFolderInfo = new PackFolderInfo
                {
                    Folders = RootFolder.Folders,
                    FolderName = "",
                    FullName = "",
                    ParentFolder = null
                };
                int num = array.Length;
                string[] array2 = array;
                foreach (string part in array2)
                {
                    if (num <= 1)
                    {
                        packFolderInfo.Files.Add(new PackFileInfo
                        {
                            FileName = part,
                            FileSize = rho5FileInfo.DecompressedSize,
                            FullName = ((packFolderInfo.FullName == "") ? part : (packFolderInfo.FullName + "/" + part)),
                            PackFileType = PackFileType.Rho5File,
                            OriginalFile = rho5FileInfo
                        });
                    }
                    else
                    {
                        PackFolderInfo packFolderInfo2 = packFolderInfo.Folders.Find((PackFolderInfo x) => x.FolderName == part);
                        if ((object)packFolderInfo2 == null)
                        {
                            List<PackFolderInfo> folders = packFolderInfo.Folders;
                            PackFolderInfo obj = new PackFolderInfo
                            {
                                FolderName = part,
                                ParentFolder = packFolderInfo,
                                FullName = ((packFolderInfo.FullName == "") ? part : (packFolderInfo.FullName + "/" + part))
                            };
                            packFolderInfo2 = obj;
                            folders.Add(obj);
                        }

                        packFolderInfo = packFolderInfo2;
                    }
                    num--;
                }
            }
        }

        public void OpenMultipleFiles(params string[] rhoFiles)
        {
            Reset();
            foreach (string rhoFile in rhoFiles)
            {
                FileInfo fileInfo = new FileInfo(rhoFile);
                if (!fileInfo.Exists)
                    throw new FileNotFoundException(rhoFile);
                Rho rho = new Rho(rhoFile);
                Queue<(PackFolderInfo, RhoDirectory)> dirQue = new Queue<(PackFolderInfo, RhoDirectory)>();

                PackFolderInfo rootFolder = new PackFolderInfo()
                {
                    FolderName = fileInfo.Name,
                    FullName = $"{fileInfo.Name}",
                    ParentFolder = null
                };
                RootFolder.Folders.Add(rootFolder);
                dirQue.Enqueue((rootFolder, rho.RootDirectory));

                while (dirQue.Count > 0)
                {
                    (PackFolderInfo, RhoDirectory) curObj = dirQue.Dequeue();
                    PackFolderInfo currectPackFolder = curObj.Item1;

                    foreach (RhoFileInfo file in curObj.Item2.GetFiles())
                    {
                        PackFileInfo newFileInfo = new PackFileInfo()
                        {
                            FileName = file.FullFileName,
                            FileSize = file.FileSize,
                            FullName = $"{currectPackFolder.FullName}/{file.FullFileName}",
                            OriginalFile = file,
                            PackFileType = PackFileType.RhoFile
                        };
                        currectPackFolder.Files.Add(newFileInfo);
                    }
                    foreach (RhoDirectory dir in curObj.Item2.GetDirectories())
                    {
                        PackFolderInfo subDirInfo = new PackFolderInfo()
                        {
                            FolderName = dir.DirectoryName,
                            FullName = $"{currectPackFolder.FullName}/{dir.DirectoryName}",
                            ParentFolder = currectPackFolder
                        };
                        currectPackFolder.Folders.Add(subDirInfo);
                        dirQue.Enqueue((subDirInfo, dir));
                    }
                }
                RhoPool.AddLast(rho);
                for (int i = 0; i < RootFolder.Folders.Count; i++)
                    RootFolder.Folders[i].ParentFolder = RootFolder;
                Initizated = true;
            }
        }

        public void Reset()
        {
            Initizated = false;
            while (RhoPool.Count > 0)
            {
                RhoPool.First?.Value.Dispose();
                RhoPool.RemoveFirst();
            }
            while (Rho5Pool.Count > 0)
            {
                Rho5Pool.First?.Value.Dispose();
                Rho5Pool.RemoveFirst();
            }
            RootFolder = new PackFolderInfo()
            {
                FolderName = "",
                FullName = "",
                ParentFolder = null
            };
        }

        public PackFolderInfo[]? GetDirectories(string Path)
        {
            if (Path == "")
                return RootFolder.Folders.ToArray();
            string[] path_sp = Path.Split('/');
            List<PackFolderInfo> currentFindFolders = RootFolder.Folders;
            foreach (string sp in path_sp)
            {
                PackFolderInfo? findFolder = currentFindFolders.Find(x => x.FolderName == sp);
                if (findFolder is null)
                    return null;
                currentFindFolders = findFolder.Folders;
            }
            return currentFindFolders.ToArray();
        }

        public PackFileInfo[]? GetFiles(string Path)
        {
            if (Path == "")
                return new PackFileInfo[0];
            string[] path_sp = Path.Split('/');
            PackFolderInfo currentFolder = new PackFolderInfo
            {
                Folders = RootFolder.Folders
            };
            int depth = path_sp.Length;
            foreach (string path in path_sp)
            {
                if (currentFolder == null)
                    return null;
                if (depth == 1)
                    return currentFolder.Files.ToArray();
                currentFolder = currentFolder.Folders.Find(x => x.FolderName == path);
                depth--;
            }
            return null;
        }

        public PackFileInfo? GetFile(string Path)
        {
            if (Path == "")
                return null;
            string[] path_sp = Path.Split('/');
            PackFolderInfo currentFolder = new PackFolderInfo
            {
                Folders = RootFolder.Folders
            };
            int depth = path_sp.Length;
            List<PackFileInfo> find_files = new List<PackFileInfo>();
            foreach (string path in path_sp)
            {
                if (currentFolder == null)
                    return null;
                if (depth == 1)
                {
                    find_files = currentFolder.Files;
                    break;
                }
                currentFolder = currentFolder.Folders.Find(x => x.FolderName == path);
                depth--;
            }
            PackFileInfo? output_file = find_files.Find(x => x.FileName == path_sp[^1]);
            if (output_file is not null)
                return output_file;
            else
                return null;
        }

        public PackFolderInfo GetRootFolder()
        {
            return (PackFolderInfo)RootFolder.Clone();
        }

    }
    public class PackFolderInfo : ICloneable
    {
        // Properties
        public string FolderName { get; set; }
        public string FullName { get; set; }
        public PackFolderInfo? ParentFolder { get; internal set; }
        internal List<PackFolderInfo> Folders { get; init; } = new List<PackFolderInfo>();
        internal List<PackFileInfo> Files { get; init; } = new List<PackFileInfo>();
        // Constructors
        public PackFolderInfo()
        {
            FolderName = "";
            FullName = "";
            ParentFolder = null;
        }
        public PackFolderInfo(string folderName, string fullName, PackFolderInfo? parentFolder, IEnumerable<PackFolderInfo> folders, IEnumerable<PackFileInfo> files)
        {
            FolderName = folderName;
            FullName = fullName;
            ParentFolder = parentFolder;
            Folders = new List<PackFolderInfo>(folders);
            Files = new List<PackFileInfo>(files);
        }

        public PackFileInfo[] GetFilesInfo()
        {
            return Files.ToArray();
        }

        public PackFolderInfo[] GetFoldersInfo()
        {
            return Folders.ToArray();
        }

        public object Clone()
        {
            PackFolderInfo clone_obj = new PackFolderInfo()
            {
                FolderName = FolderName,
                FullName = FullName,
                ParentFolder = ParentFolder
            };
            Queue<(PackFolderInfo parent, PackFolderInfo proc_folder)> queue = new Queue<(PackFolderInfo parent, PackFolderInfo proc_folder)>();
            foreach (var sub_folder in Folders)
            {
                queue.Enqueue((clone_obj, sub_folder));
            }
            foreach (var sub_file in Files)
            {
                clone_obj.Files.Add((PackFileInfo)sub_file.Clone());
            }
            while (queue.Count > 0)
            {
                var curent_proc_obj = queue.Dequeue();
                var old_obj = curent_proc_obj.proc_folder;
                var new_obj_par = curent_proc_obj.parent;
                PackFolderInfo cur_clone_obj = new PackFolderInfo()
                {
                    FolderName = old_obj.FolderName,
                    FullName = old_obj.FullName,
                    ParentFolder = new_obj_par
                };
                new_obj_par.Folders.Add(cur_clone_obj);
                foreach (var obj_sub_folder in curent_proc_obj.proc_folder.Folders)
                {
                    queue.Enqueue((cur_clone_obj, obj_sub_folder));
                }
                foreach (var obj_sub_file in curent_proc_obj.proc_folder.Files)
                {
                    cur_clone_obj.Files.Add((PackFileInfo)obj_sub_file.Clone());
                }
            }
            return clone_obj;
        }

        // Operator Overloads
        public static bool operator ==(PackFolderInfo objA, PackFolderInfo objB)
        {
            return objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null && objA.FullName == objB.FullName;
        }
        public static bool operator !=(PackFolderInfo objA, PackFolderInfo objB)
        {
            return !(objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null && objA.FullName == objB.FullName);
        }

        // Overrides
        public override bool Equals(object? obj)
        {
            if (obj is PackFolderInfo folderInfo)
            {
                return folderInfo == this;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + FullName.GetHashCode();
        }
    }
    public class PackFileInfo : ICloneable
    {
        public string FileName { get; set; }
        public string FullName { get; set; }
        public int FileSize { get; set; }
        public PackFileType PackFileType { get; set; }
        public object OriginalFile { get; set; }

        public byte[] GetData()
        {
            if (PackFileType == PackFileType.RhoFile && OriginalFile is RhoFileInfo fileInfo)
            {
                return fileInfo.GetData();
            }
            else if (PackFileType == PackFileType.Rho5File && OriginalFile is Rho5FileInfo file5Info)
            {
                return file5Info.GetData();
            }
            else
                return null;
        }
        public object Clone()
        {
            PackFileInfo clone_obj = new PackFileInfo()
            {
                FileName = FileName,
                FullName = FullName,
                FileSize = FileSize,
                PackFileType = PackFileType,
                OriginalFile = OriginalFile,
            };
            return clone_obj;
        }

        //Operator Overloads
        public static bool operator ==(PackFileInfo objA, PackFileInfo objB)
        {
            return objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null && objA.FullName == objB.FullName;
        }
        public static bool operator !=(PackFileInfo objA, PackFileInfo objB)
        {
            return !(objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null && objA.FullName == objB.FullName);
        }

        // Overrides
        public override bool Equals(object? obj)
        {
            if (obj is PackFileInfo fileInfo)
            {
                return fileInfo == this;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() - FullName.GetHashCode();
        }
    }

    public enum PackFileType
    {
        RhoFile,
        Rho5File
    }
}