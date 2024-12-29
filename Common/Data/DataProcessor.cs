using System;
using System.IO;
using Ionic.Zlib;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.Data;

public static class DataProcessor
{
    public static byte[] EncodeKRData(byte[] Data, bool Encrypted, bool Compressed, uint EncryptKey = 0u)
    {
        using MemoryStream memoryStream = new MemoryStream(Data.Length);
        BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
        _ = binaryWriter.BaseStream.Position;
        byte value = (byte)((Encrypted ? 2u : 0u) | (Compressed ? 1u : 0u));
        uint value2 = IO.Adler.Adler32(0u, Data, 0, Data.Length);
        int value3 = Data.Length;
        byte[] array = Data;
        if (Compressed)
        {
            using MemoryStream stream = new MemoryStream();
            new ZlibStream(stream, CompressionMode.Compress).Write(array, 0, array.Length);
            array = memoryStream.ToArray();
        }

        if (Encrypted)
        {
            array = RhoEncrypt.DecryptData(EncryptKey, array);
        }

        binaryWriter.Write((byte)83);
        binaryWriter.Write(value);
        binaryWriter.Write(value2);
        if (Encrypted)
        {
            binaryWriter.Write(EncryptKey);
        }

        if (Compressed)
        {
            binaryWriter.Write(value3);
        }

        binaryWriter.Write(array);
        return memoryStream.ToArray();
    }

    public static byte[] DecodeKRData(byte[] OriginalData)
    {
        using MemoryStream input = new MemoryStream(OriginalData);
        BinaryReader binaryReader = new BinaryReader(input);
        if (binaryReader.ReadByte() != 83)
        {
            throw new Exception("It is not KRData Format.");
        }

        byte num = binaryReader.ReadByte();
        uint num2 = binaryReader.ReadUInt32();
        bool flag = (num & 2) == 2;
        bool num3 = (num & 1) == 1;
        uint key = (flag ? binaryReader.ReadUInt32() : 0u);
        int num4 = (num3 ? binaryReader.ReadInt32() : 0);
        byte[] array = binaryReader.ReadBytes((int)(OriginalData.Length - binaryReader.BaseStream.Position));
        if (flag)
        {
            array = RhoEncrypt.DecryptData(key, array);
        }

        if (num3)
        {
            using MemoryStream stream = new MemoryStream(array);
            array = new byte[num4];
            new ZlibStream(stream, CompressionMode.Decompress).Read(array, 0, array.Length);
        }

        if (IO.Adler.Adler32(0u, array, 0, array.Length) != num2)
        {
            throw new Exception("Exception: KRData hash is not qualified.");
        }

        return array;
    }

    public static byte[] ReadKRData(this BinaryReader br, int TotalLength)
    {
        long position = br.BaseStream.Position;
        if (br.ReadByte() != 83)
        {
            throw new Exception("It is not KRData Format.");
        }

        byte num = br.ReadByte();
        uint num2 = br.ReadUInt32();
        bool flag = (num & 2) == 2;
        bool num3 = (num & 1) == 1;
        uint key = (flag ? br.ReadUInt32() : 0u);
        int num4 = (num3 ? br.ReadInt32() : 0);
        byte[] array = br.ReadBytes((int)(TotalLength - (br.BaseStream.Position - position)));
        if (flag)
        {
            array = RhoEncrypt.DecryptData(key, array);
        }

        if (num3)
        {
            using MemoryStream memoryStream = new MemoryStream(array);
            array = new byte[num4];
            new ZlibStream(memoryStream, CompressionMode.Decompress).Read(array, 0, array.Length);
        }

        if (IO.Adler.Adler32(0u, array, 0, array.Length) != num2)
        {
            throw new Exception("Exception: KRData hash is not qualified.");
        }

        return array;
    }

    public static int WriteKRData(this BinaryWriter bw, byte[] Data, bool Encrypted, bool Compressed, uint EncryptKey = 0u)
    {
        long position = bw.BaseStream.Position;
        byte value = (byte)((Encrypted ? 2u : 0u) | (Compressed ? 1u : 0u));
        uint value2 = IO.Adler.Adler32(0u, Data, 0, Data.Length);
        int value3 = Data.Length;
        byte[] array = Data;
        if (Compressed)
        {
            using MemoryStream memoryStream = new MemoryStream();
            new ZlibStream(memoryStream, CompressionMode.Compress).Write(array, 0, array.Length);
            array = memoryStream.ToArray();
        }

        if (Encrypted)
        {
            array = RhoEncrypt.DecryptData(EncryptKey, array);
        }

        bw.Write((byte)83);
        bw.Write(value);
        bw.Write(value2);
        if (Encrypted)
        {
            bw.Write(EncryptKey);
        }

        if (Compressed)
        {
            bw.Write(value3);
        }

        bw.Write(array);
        return (int)(bw.BaseStream.Position - position);
    }
}