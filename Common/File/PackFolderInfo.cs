using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public class PackFolderInfo : ICloneable
{
    public string FolderName { get; set; }

    public string FullName { get; set; }

    public PackFolderInfo? ParentFolder { get; internal set; }

    internal List<PackFolderInfo> Folders { get; init; } = new List<PackFolderInfo>();


    internal List<PackFileInfo> Files { get; init; } = new List<PackFileInfo>();


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
        PackFolderInfo packFolderInfo = new PackFolderInfo
        {
            FolderName = FolderName,
            FullName = FullName,
            ParentFolder = ParentFolder
        };
        Queue<(PackFolderInfo, PackFolderInfo)> queue = new Queue<(PackFolderInfo, PackFolderInfo)>();
        foreach (PackFolderInfo folder in Folders)
        {
            queue.Enqueue((packFolderInfo, folder));
        }

        foreach (PackFileInfo file in Files)
        {
            packFolderInfo.Files.Add((PackFileInfo)file.Clone());
        }

        while (queue.Count > 0)
        {
            (PackFolderInfo, PackFolderInfo) tuple = queue.Dequeue();
            PackFolderInfo item = tuple.Item2;
            PackFolderInfo item2 = tuple.Item1;
            PackFolderInfo packFolderInfo2 = new PackFolderInfo
            {
                FolderName = item.FolderName,
                FullName = item.FullName,
                ParentFolder = item2
            };
            item2.Folders.Add(packFolderInfo2);
            foreach (PackFolderInfo folder2 in tuple.Item2.Folders)
            {
                queue.Enqueue((packFolderInfo2, folder2));
            }

            foreach (PackFileInfo file2 in tuple.Item2.Files)
            {
                packFolderInfo2.Files.Add((PackFileInfo)file2.Clone());
            }
        }

        return packFolderInfo;
    }

    public static bool operator ==(PackFolderInfo objA, PackFolderInfo objB)
    {
        if ((object)objA != null && (object)objB != null && objA.FullName != null && objB.FullName != null)
        {
            return objA.FullName == objB.FullName;
        }

        return false;
    }

    public static bool operator !=(PackFolderInfo objA, PackFolderInfo objB)
    {
        if ((object)objA != null && (object)objB != null && objA.FullName != null && objB.FullName != null)
        {
            return !(objA.FullName == objB.FullName);
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackFolderInfo packFolderInfo)
        {
            return packFolderInfo == this;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() + FullName.GetHashCode();
    }
}