using System;
using System.IO;
using Ionic.Zlib;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.File;

public static class RhoBlockReader
{
    public static RhoDataInfo ReadBlockInfo(this BinaryReader reader, uint Key)
    {
        RhoDataInfo rhoDataInfo = new RhoDataInfo();
        byte[] data = reader.ReadBytes(32);
        data = RhoEncrypt.DecryptHeaderInfo(data, Key);
        IO.Adler.Adler32(0u, data, 0, data.Length);
        using MemoryStream input = new MemoryStream(data);
        BinaryReader binaryReader = new BinaryReader(input);
        rhoDataInfo.Index = binaryReader.ReadUInt32();
        rhoDataInfo.Offset = binaryReader.ReadUInt32() << 8;
        rhoDataInfo.DataSize = binaryReader.ReadInt32();
        rhoDataInfo.UncompressedSize = binaryReader.ReadInt32();
        rhoDataInfo.BlockProperty = (RhoBlockProperty)binaryReader.ReadInt32();
        rhoDataInfo.Checksum = binaryReader.ReadUInt32();
        return rhoDataInfo;
    }

    public static RhoDataInfo ReadBlockInfo10(this BinaryReader reader, byte[] Key)
    {
        RhoDataInfo rhoDataInfo = new RhoDataInfo();
        using MemoryStream input = new MemoryStream(RhoEncrypt.DecryptBlockInfoOld(reader.ReadBytes(32), Key));
        BinaryReader binaryReader = new BinaryReader(input);
        rhoDataInfo.Index = binaryReader.ReadUInt32();
        rhoDataInfo.Offset = binaryReader.ReadUInt32() << 8;
        rhoDataInfo.DataSize = binaryReader.ReadInt32();
        rhoDataInfo.UncompressedSize = binaryReader.ReadInt32();
        rhoDataInfo.BlockProperty = (RhoBlockProperty)binaryReader.ReadInt32();
        rhoDataInfo.Checksum = binaryReader.ReadUInt32();
        return rhoDataInfo;
    }

    public static byte[] ReadBlock(this BinaryReader reader, Rho RhoFile, uint BlockIndex, uint Key)
    {
        //IL_004d: Unknown result type (might be due to invalid IL or missing references)
        //IL_005c: Expected O, but got Unknown
        RhoDataInfo blockInfo = RhoFile.GetBlockInfo(BlockIndex);
        if (blockInfo == null)
        {
            return null;
        }

        reader.BaseStream.Seek(blockInfo.Offset, SeekOrigin.Begin);
        byte[] array = reader.ReadBytes(blockInfo.DataSize);
        if ((blockInfo.BlockProperty & RhoBlockProperty.Compressed) == RhoBlockProperty.Compressed)
        {
            using MemoryStream memoryStream = new MemoryStream(array);
            array = new byte[blockInfo.UncompressedSize];
            ((Stream)new ZlibStream((Stream)memoryStream, (CompressionMode)1)).Read(array, 0, array.Length);
        }

        if ((blockInfo.BlockProperty & RhoBlockProperty.PartialEncrypted) == RhoBlockProperty.PartialEncrypted)
        {
            RhoEncrypt.DecryptData(Key, array, 0, array.Length);
        }

        if (blockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
        {
            RhoDataInfo blockInfo2 = RhoFile.GetBlockInfo(BlockIndex + 1);
            if (blockInfo2 == null)
            {
                return array;
            }

            Array.Resize(ref array, blockInfo.DataSize + blockInfo2.DataSize);
            reader.BaseStream.Read(array, blockInfo.DataSize, blockInfo2.DataSize);
        }

        return array;
    }
}