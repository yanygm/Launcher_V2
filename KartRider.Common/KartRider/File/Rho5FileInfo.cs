using System;
using System.IO;
using System.Text;
using Ionic.Zlib;
using KartLibrary.Encrypt;

namespace KartLibrary.File;

public class Rho5FileInfo
{
    internal Rho5 BaseRho5 { get; init; }

    public string FullPath { get; set; }

    public int Offset { get; set; }

    public int CompressedSize { get; set; }

    public int DecompressedSize { get; set; }

    public int Unknown { get; set; }

    public int FileInfoChecksum { get; set; }

    public byte[] Key { get; set; }

    public byte[] data { get; set; }

    public byte[] GetData()
    {
        //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
        //IL_00e1: Expected O, but got Unknown
        byte[] array = new byte[CompressedSize];
        byte[] array2 = new byte[DecompressedSize];
        byte[] packedFileKey = Rho5Key.GetPackedFileKey(Key, Rho5Key.GetFileKey_U1(BaseRho5.anotherData), FullPath);
        Rho5DecryptStream rho5DecryptStream = new Rho5DecryptStream(BaseRho5.BaseStream, packedFileKey);
        rho5DecryptStream.Seek(Offset * 1024 + BaseRho5.DataBaseOffset, SeekOrigin.Begin);
        rho5DecryptStream.Read(array, 0, (array.Length >= 1024) ? 1024 : array.Length);
        if (array.Length >= 1024)
        {
            BaseRho5.BaseStream.Read(array, 1024, array.Length - 1024);
        }

        new Rho5DecryptStream(new MemoryStream(array), packedFileKey).Read(array, 0, array.Length);
        using MemoryStream memoryStream = new MemoryStream(array);
        ((Stream)new ZlibStream((Stream)memoryStream, (CompressionMode)1)).Read(array2, 0, array2.Length);
        return array2;
    }

    private void dump_data(byte[] data)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (byte value in data)
        {
            StringBuilder stringBuilder2 = stringBuilder;
            StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder2);
            handler.AppendFormatted(value, "x2");
            handler.AppendLiteral(" ");
            stringBuilder2.Append(ref handler);
        }

        stringBuilder.Append("\n");
    }
}