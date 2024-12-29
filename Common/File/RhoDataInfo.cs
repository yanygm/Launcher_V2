using System;

namespace KartLibrary.File;

public class RhoDataInfo : IComparable<RhoDataInfo>
{
    public uint Index { get; set; }

    public long Offset { get; set; }

    public int DataSize { get; set; }

    public int UncompressedSize { get; set; }

    public RhoBlockProperty BlockProperty { get; set; }

    public uint Checksum { get; set; }

    public int CompareTo(RhoDataInfo? other)
    {
        return Index.CompareTo(other?.Index);
    }

    public override int GetHashCode()
    {
        return (int)Index;
    }
}