using System;
using System.IO;
using System.Text;
using KartLibrary.Consts;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.File;

public class Rho5 : IDisposable
{
    internal int DataBaseOffset;

    internal string anotherData = "";

    public byte PackageVersion { get; set; }

    public Stream BaseStream { get; set; }

    public Rho5FileInfo[] Files { get; private set; } = new Rho5FileInfo[0];


    public Rho5()
    {
    }

    public Rho5(string FileName, CountryCode region)
    {
        BaseStream = new FileStream(FileName, FileMode.Open);
        FileInfo fileInfo = new FileInfo(FileName);
        anotherData = "";
        switch (region)
        {
            case CountryCode.KR:
                anotherData = "y&errfV6GRS!e8JL";
                break;
            case CountryCode.CN:
                anotherData = "d$Bjgfc8@dH4TQ?k";
                break;
            case CountryCode.TW:
                anotherData = "t5rHKg-g9BA7%=qD";
                break;
        }

        Rho5DecryptStream rho5DecryptStream = new Rho5DecryptStream(BaseStream, fileInfo.Name, anotherData);
        BinaryReader binaryReader = new BinaryReader(rho5DecryptStream);
        int headerOffset = GetHeaderOffset(fileInfo.Name);
        int num = headerOffset + GetFileNamesOffset(fileInfo.Name);
        rho5DecryptStream.Seek(headerOffset, SeekOrigin.Begin);
        int num2 = binaryReader.ReadInt32();
        PackageVersion = binaryReader.ReadByte();
        int num3 = binaryReader.ReadInt32();
        if (num2 != PackageVersion + num3)
        {
            throw new Exception("rho5 header crc mismatch.");
        }

        rho5DecryptStream.Seek(num, SeekOrigin.Begin);
        rho5DecryptStream.SetToFilesInfoKey(fileInfo.Name, anotherData);
        Files = new Rho5FileInfo[num3];
        for (int i = 0; i < num3; i++)
        {
            Rho5FileInfo rho5FileInfo = new Rho5FileInfo
            {
                FullPath = binaryReader.ReadText(Encoding.GetEncoding("UTF-16")),
                FileInfoChecksum = binaryReader.ReadInt32(),
                Unknown = binaryReader.ReadInt32(),
                Offset = binaryReader.ReadInt32(),
                DecompressedSize = binaryReader.ReadInt32(),
                CompressedSize = binaryReader.ReadInt32(),
                Key = binaryReader.ReadBytes(16),
                BaseRho5 = this
            };
            Files[i] = rho5FileInfo;
        }

        DataBaseOffset = (int)rho5DecryptStream.Position + 1023 >> 10 << 10;
    }

    private int GetHeaderOffset(string name)
    {
        name = name.ToLower();
        int num = 0;
        string text = name;
        foreach (char c in text)
        {
            num += c;
        }

        long num2 = num * 2753184165u >> 32;
        int num3 = num - (int)num2;
        num3 >>= 1;
        num3 += (int)num2;
        num3 >>= 8;
        num3 *= 312;
        return num - num3 + 30;
    }

    private int GetFileNamesOffset(string name)
    {
        name = name.ToLower();
        int num = 0;
        string text = name;
        foreach (char c in text)
        {
            num += c;
        }

        num *= 3;
        long num2 = (long)num * 891408307L >> 32;
        int num3 = num - (int)num2;
        num3 >>= 1;
        num3 += (int)num2;
        num3 >>= 7;
        num3 *= 212;
        return num - num3 + 42;
    }

    public void Dispose()
    {
        if (BaseStream != null)
        {
            BaseStream.Close();
            BaseStream.Dispose();
        }
    }
}