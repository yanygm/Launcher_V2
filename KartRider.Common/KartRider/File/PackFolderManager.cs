using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ExcData;
using System.Xml.Linq;
using System.Xml;
using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.Xml;

namespace KartLibrary.File;

public class PackFolderManager
{
    private struct ProcessObj
    {
        public string Path;

        public BinaryXmlTag Obj;

        public PackFolderInfo Parent;
    }

    private PackFolderInfo RootFolder = new PackFolderInfo();

    public bool Initizated { get; private set; }

    private LinkedList<Rho> RhoPool { get; init; } = new LinkedList<Rho>();


    private LinkedList<Rho5> Rho5Pool { get; init; } = new LinkedList<Rho5>();


    public void OpenDataFolder(string aaaPkFilePath)
    {
        FileInfo fileInfo = new FileInfo(aaaPkFilePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(aaaPkFilePath);
        }

        Reset();
        using FileStream fileStream = new FileStream(aaaPkFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        int totalLength = binaryReader.ReadInt32();
        byte[] array = binaryReader.ReadKRData(totalLength);
        fileStream.Close();
        BinaryXmlDocument binaryXmlDocument = new BinaryXmlDocument();
        binaryXmlDocument.Read(Encoding.GetEncoding("UTF-16"), array);
        BinaryXmlTag rootTag = binaryXmlDocument.RootTag;
        Queue<ProcessObj> queue = new Queue<ProcessObj>();
        bool flag = rootTag.Name != "PackFolder";
        if (flag || ((flag | (rootTag.GetAttribute("name") != "KartRider")) ? true : false))
        {
            throw new NotSupportedException("aaa.pk file not support.");
        }

        foreach (BinaryXmlTag child in rootTag.Children)
        {
            queue.Enqueue(new ProcessObj
            {
                Obj = child,
                Parent = null,
                Path = ""
            });
        }

        while (queue.Count > 0)
        {
            ProcessObj processObj = queue.Dequeue();
            BinaryXmlTag obj = processObj.Obj;
            string name = obj.Name;
            if (!(name == "PackFolder"))
            {
                if (!(name == "RhoFolder"))
                {
                    continue;
                }

                string text = obj.GetAttribute("name");
                string text2 = obj.GetAttribute("fileName");
                PackFolderInfo packFolderInfo = new PackFolderInfo
                {
                    FolderName = (((object)processObj.Parent == null) ? (text ?? "") : text),
                    FullName = (((object)processObj.Parent == null) ? (text ?? "") : (processObj.Path + "/" + text)),
                    ParentFolder = processObj.Parent
                };
                if (text == "")
                {
                    packFolderInfo = processObj.Parent;
                }
                else if ((object)processObj.Parent == null)
                {
                    RootFolder.Folders.Add(packFolderInfo);
                }
                else
                {
                    processObj.Parent.Folders.Add(packFolderInfo);
                }

                Rho rho = new Rho(fileInfo.DirectoryName + "\\" + text2);
                Queue<(PackFolderInfo, RhoDirectory)> queue2 = new Queue<(PackFolderInfo, RhoDirectory)>();
                RhoDirectory rootDirectory = rho.RootDirectory;
                queue2.Enqueue((packFolderInfo, rootDirectory));
                while (queue2.Count > 0)
                {
                    (PackFolderInfo, RhoDirectory) tuple = queue2.Dequeue();
                    PackFolderInfo item = tuple.Item1;
                    RhoFileInfo[] files = tuple.Item2.GetFiles();
                    foreach (RhoFileInfo rhoFileInfo in files)
                    {
                        PackFileInfo item2 = new PackFileInfo
                        {
                            FileName = rhoFileInfo.FullFileName,
                            FileSize = rhoFileInfo.FileSize,
                            FullName = item.FullName + "/" + rhoFileInfo.FullFileName,
                            OriginalFile = rhoFileInfo,
                            PackFileType = PackFileType.RhoFile
                        };
                        item.Files.Add(item2);
                    }

                    RhoDirectory[] directories = tuple.Item2.GetDirectories();
                    foreach (RhoDirectory rhoDirectory in directories)
                    {
                        PackFolderInfo packFolderInfo2 = new PackFolderInfo
                        {
                            FolderName = rhoDirectory.DirectoryName,
                            FullName = item.FullName + "/" + rhoDirectory.DirectoryName,
                            ParentFolder = item
                        };
                        item.Folders.Add(packFolderInfo2);
                        queue2.Enqueue((packFolderInfo2, rhoDirectory));
                    }
                }

                RhoPool.AddLast(rho);
                continue;
            }

            string text3 = obj.GetAttribute("name");
            PackFolderInfo packFolderInfo3 = new PackFolderInfo
            {
                FolderName = (((object)processObj.Parent == null) ? (text3 ?? "") : text3),
                FullName = (((object)processObj.Parent == null) ? (text3 ?? "") : (processObj.Path + "/" + text3)),
                ParentFolder = processObj.Parent
            };
            foreach (BinaryXmlTag child2 in obj.Children)
            {
                ProcessObj processObj2 = default(ProcessObj);
                processObj2.Path = packFolderInfo3.FullName;
                processObj2.Parent = packFolderInfo3;
                processObj2.Obj = child2;
                ProcessObj item3 = processObj2;
                queue.Enqueue(item3);
            }

            if ((object)processObj.Parent != null)
            {
                processObj.Parent.Folders.Add(packFolderInfo3);
            }
            else
            {
                RootFolder.Folders.Add(packFolderInfo3);
            }
        }

        CountryCode regionCode = CountryCode.None;
        PackFolderInfo[] directories2 = GetDirectories("zeta");
        if (Array.Exists(directories2, (PackFolderInfo x) => x.FolderName == "kr"))
        {
            regionCode = CountryCode.KR;
        }
        else if (Array.Exists(directories2, (PackFolderInfo x) => x.FolderName == "cn"))
        {
            regionCode = CountryCode.CN;
        }
        else if (Array.Exists(directories2, (PackFolderInfo x) => x.FolderName == "tw"))
        {
            regionCode = CountryCode.TW;
        }

        string[] files2 = Directory.GetFiles(fileInfo.DirectoryName, "*.rho5");
        foreach (string file in files2)
        {
            OpenRho5File(file, regionCode);
        }

        for (int j = 0; j < RootFolder.Folders.Count; j++)
        {
            RootFolder.Folders[j].ParentFolder = RootFolder;
        }

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

    public async Task OpenDataFolderAsync(string aaaPkFilePath, ProgressChangedEventHandler? onProgressChange = null)
    {
    }

    public void OpenSingleFile(string rhoFile, CountryCode code)
    {
        FileInfo fileInfo = new FileInfo(rhoFile);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(rhoFile);
        }

        Reset();
        if (rhoFile.EndsWith(".rho5"))
        {
            OpenRho5File(rhoFile, code);
            return;
        }

        Rho rho = new Rho(rhoFile);
        Queue<(PackFolderInfo, RhoDirectory)> queue = new Queue<(PackFolderInfo, RhoDirectory)>();
        PackFolderInfo packFolderInfo = new PackFolderInfo
        {
            FolderName = fileInfo.Name,
            FullName = (fileInfo.Name ?? ""),
            ParentFolder = null
        };
        RootFolder.Folders.Add(packFolderInfo);
        queue.Enqueue((packFolderInfo, rho.RootDirectory));
        while (queue.Count > 0)
        {
            (PackFolderInfo, RhoDirectory) tuple = queue.Dequeue();
            PackFolderInfo item = tuple.Item1;
            RhoFileInfo[] files = tuple.Item2.GetFiles();
            foreach (RhoFileInfo rhoFileInfo in files)
            {
                PackFileInfo item2 = new PackFileInfo
                {
                    FileName = rhoFileInfo.FullFileName,
                    FileSize = rhoFileInfo.FileSize,
                    FullName = item.FullName + "/" + rhoFileInfo.FullFileName,
                    OriginalFile = rhoFileInfo,
                    PackFileType = PackFileType.RhoFile
                };
                item.Files.Add(item2);
            }

            RhoDirectory[] directories = tuple.Item2.GetDirectories();
            foreach (RhoDirectory rhoDirectory in directories)
            {
                PackFolderInfo packFolderInfo2 = new PackFolderInfo
                {
                    FolderName = rhoDirectory.DirectoryName,
                    FullName = item.FullName + "/" + rhoDirectory.DirectoryName,
                    ParentFolder = item
                };
                item.Folders.Add(packFolderInfo2);
                queue.Enqueue((packFolderInfo2, rhoDirectory));
            }
        }

        RhoPool.AddLast(rho);
        for (int j = 0; j < RootFolder.Folders.Count; j++)
        {
            RootFolder.Folders[j].ParentFolder = RootFolder;
        }

        Initizated = true;
    }

    public void OpenMultipleFiles(params string[] rhoFiles)
    {
        Reset();
        foreach (string text in rhoFiles)
        {
            FileInfo fileInfo = new FileInfo(text);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(text);
            }

            Rho rho = new Rho(text);
            Queue<(PackFolderInfo, RhoDirectory)> queue = new Queue<(PackFolderInfo, RhoDirectory)>();
            PackFolderInfo packFolderInfo = new PackFolderInfo
            {
                FolderName = fileInfo.Name,
                FullName = (fileInfo.Name ?? ""),
                ParentFolder = null
            };
            RootFolder.Folders.Add(packFolderInfo);
            queue.Enqueue((packFolderInfo, rho.RootDirectory));
            while (queue.Count > 0)
            {
                (PackFolderInfo, RhoDirectory) tuple = queue.Dequeue();
                PackFolderInfo item = tuple.Item1;
                RhoFileInfo[] files = tuple.Item2.GetFiles();
                foreach (RhoFileInfo rhoFileInfo in files)
                {
                    PackFileInfo item2 = new PackFileInfo
                    {
                        FileName = rhoFileInfo.FullFileName,
                        FileSize = rhoFileInfo.FileSize,
                        FullName = item.FullName + "/" + rhoFileInfo.FullFileName,
                        OriginalFile = rhoFileInfo,
                        PackFileType = PackFileType.RhoFile
                    };
                    item.Files.Add(item2);
                }

                RhoDirectory[] directories = tuple.Item2.GetDirectories();
                foreach (RhoDirectory rhoDirectory in directories)
                {
                    PackFolderInfo packFolderInfo2 = new PackFolderInfo
                    {
                        FolderName = rhoDirectory.DirectoryName,
                        FullName = item.FullName + "/" + rhoDirectory.DirectoryName,
                        ParentFolder = item
                    };
                    item.Folders.Add(packFolderInfo2);
                    queue.Enqueue((packFolderInfo2, rhoDirectory));
                }
            }

            RhoPool.AddLast(rho);
            for (int k = 0; k < RootFolder.Folders.Count; k++)
            {
                RootFolder.Folders[k].ParentFolder = RootFolder;
            }

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

        RootFolder = new PackFolderInfo
        {
            FolderName = "",
            FullName = "",
            ParentFolder = null
        };
    }

    public PackFolderInfo[]? GetDirectories(string Path)
    {
        if (Path == "")
        {
            return RootFolder.Folders.ToArray();
        }

        string[] array = Path.Split('/');
        List<PackFolderInfo> folders = RootFolder.Folders;
        string[] array2 = array;
        foreach (string sp in array2)
        {
            PackFolderInfo packFolderInfo = folders.Find((PackFolderInfo x) => x.FolderName == sp);
            if ((object)packFolderInfo == null)
            {
                return null;
            }

            folders = packFolderInfo.Folders;
        }

        return folders.ToArray();
    }

    public PackFileInfo[]? GetFiles(string Path)
    {
        if (Path == "")
        {
            return new PackFileInfo[0];
        }

        string[] array = Path.Split('/');
        PackFolderInfo packFolderInfo = new PackFolderInfo
        {
            Folders = RootFolder.Folders
        };
        int num = array.Length;
        string[] array2 = array;
        foreach (string path in array2)
        {
            if (packFolderInfo == null)
            {
                return null;
            }

            if (num == 1)
            {
                return packFolderInfo.Files.ToArray();
            }

            packFolderInfo = packFolderInfo.Folders.Find((PackFolderInfo x) => x.FolderName == path);
            num--;
        }

        return null;
    }

    public PackFileInfo? GetFile(string Path)
    {
        if (Path == "")
        {
            return null;
        }

        string[] path_sp = Path.Split('/');
        PackFolderInfo packFolderInfo = new PackFolderInfo
        {
            Folders = RootFolder.Folders
        };
        int num = path_sp.Length;
        List<PackFileInfo> list = new List<PackFileInfo>();
        string[] array = path_sp;
        foreach (string path in array)
        {
            if (packFolderInfo == null)
            {
                return null;
            }

            if (num == 1)
            {
                list = packFolderInfo.Files;
                break;
            }

            packFolderInfo = packFolderInfo.Folders.Find((PackFolderInfo x) => x.FolderName == path);
            num--;
        }

        PackFileInfo packFileInfo = list.Find((PackFileInfo x) => x.FileName == path_sp[^1]);
        if ((object)packFileInfo != null)
        {
            return packFileInfo;
        }

        return null;
    }

    public PackFolderInfo GetRootFolder()
    {
        return (PackFolderInfo)RootFolder.Clone();
    }
}
