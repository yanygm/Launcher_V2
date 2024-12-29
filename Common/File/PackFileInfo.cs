using System;

namespace KartLibrary.File;

public class PackFileInfo : ICloneable
{
    public string FileName { get; set; }

    public string FullName { get; set; }

    public int FileSize { get; set; }

    public PackFileType PackFileType { get; set; }

    public object OriginalFile { get; set; }

    public byte[] GetData()
    {
        if (PackFileType == PackFileType.RhoFile && OriginalFile is RhoFileInfo rhoFileInfo)
        {
            return rhoFileInfo.GetData();
        }

        if (PackFileType == PackFileType.Rho5File && OriginalFile is Rho5FileInfo rho5FileInfo)
        {
            return rho5FileInfo.GetData();
        }

        return null;
    }

    public object Clone()
    {
        return new PackFileInfo
        {
            FileName = FileName,
            FullName = FullName,
            FileSize = FileSize,
            PackFileType = PackFileType,
            OriginalFile = OriginalFile
        };
    }

    public static bool operator ==(PackFileInfo objA, PackFileInfo objB)
    {
        if ((object)objA != null && (object)objB != null && objA.FullName != null && objB.FullName != null)
        {
            return objA.FullName == objB.FullName;
        }

        return false;
    }

    public static bool operator !=(PackFileInfo objA, PackFileInfo objB)
    {
        if ((object)objA != null && (object)objB != null && objA.FullName != null && objB.FullName != null)
        {
            return !(objA.FullName == objB.FullName);
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackFileInfo packFileInfo)
        {
            return packFileInfo == this;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() - FullName.GetHashCode();
    }
}