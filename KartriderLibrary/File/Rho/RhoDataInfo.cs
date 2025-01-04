using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KartLibrary.Encrypt;
using KartLibrary.IO;
using System.Diagnostics;

namespace KartLibrary.File
{
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
    //Extension
    public static class RhoBlockReader
    {
        public static RhoDataInfo ReadBlockInfo(this BinaryReader reader, uint Key)
        {
            RhoDataInfo output = new RhoDataInfo();
            byte[] blockInfoData = reader.ReadBytes(0x20);
            //Debug.Print($"adler_raw: {Adler.Adler32(0, blockInfoData, 0, blockInfoData.Length):x8}");
            blockInfoData = RhoEncrypt.DecryptHeaderInfo(blockInfoData, Key);
            uint hash = Adler.Adler32(0, blockInfoData, 0, blockInfoData.Length);
            using (MemoryStream ms = new MemoryStream(blockInfoData))
            {
                BinaryReader msReader = new BinaryReader(ms);
                output.Index = msReader.ReadUInt32();
                output.Offset = msReader.ReadUInt32() << 8;
                output.DataSize = msReader.ReadInt32();
                output.UncompressedSize = msReader.ReadInt32();
                output.BlockProperty = (RhoBlockProperty)msReader.ReadInt32();
                output.Checksum = msReader.ReadUInt32();
            }
            return output;
        }

        // For Rho layer 1.0
        public static RhoDataInfo ReadBlockInfo10(this BinaryReader reader, byte[] Key)
        {
            RhoDataInfo output = new RhoDataInfo();
            byte[] blockInfoData = reader.ReadBytes(0x20);
            blockInfoData = RhoEncrypt.DecryptBlockInfoOld(blockInfoData, Key);
            using (MemoryStream ms = new MemoryStream(blockInfoData))
            {
                BinaryReader msReader = new BinaryReader(ms);
                output.Index = msReader.ReadUInt32();
                output.Offset = msReader.ReadUInt32() << 8;
                output.DataSize = msReader.ReadInt32();
                output.UncompressedSize = msReader.ReadInt32();
                output.BlockProperty = (RhoBlockProperty)msReader.ReadInt32();
                output.Checksum = msReader.ReadUInt32();
            }

            return output;
        }

        public static byte[] ReadBlock(this BinaryReader reader, Rho RhoFile, uint BlockIndex, uint Key)
        {
            RhoDataInfo BlockInfo = RhoFile.GetBlockInfo(BlockIndex);
            if (BlockInfo is null)
                return null;
            reader.BaseStream.Seek(BlockInfo.Offset, SeekOrigin.Begin);
            byte[] BlockData = reader.ReadBytes(BlockInfo.DataSize);
            //Debug.Print($"B:{BlockIndex:x8}: {Adler.Adler32(0, BlockData, 0, BlockData.Length):x8}");
            if ((BlockInfo.BlockProperty & RhoBlockProperty.Compressed) == RhoBlockProperty.Compressed)
            {
                using (MemoryStream ms = new MemoryStream(BlockData))
                {
                    BlockData = new byte[BlockInfo.UncompressedSize];
                    Ionic.Zlib.ZlibStream ds = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Decompress);
                    ds.Read(BlockData, 0, BlockData.Length);
                }
            }
            if ((BlockInfo.BlockProperty & RhoBlockProperty.PartialEncrypted) == RhoBlockProperty.PartialEncrypted) // Encrypted or PartialEncrypted
            {
                RhoEncrypt.DecryptData(Key, BlockData, 0, BlockData.Length);
            }
            if (BlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted) // PartialEncrypted
            {
                RhoDataInfo secPartInfo = RhoFile.GetBlockInfo(BlockIndex + 1);
                if (secPartInfo is null)
                    return BlockData;
                Array.Resize(ref BlockData, BlockInfo.DataSize + secPartInfo.DataSize);
                reader.BaseStream.Read(BlockData, BlockInfo.DataSize, secPartInfo.DataSize);
            }
            //Debug.Print($"A:{BlockIndex:x8}: {Adler.Adler32(0, BlockData, 0, BlockData.Length):x8}");
            return BlockData;
        }
    }

    public enum RhoBlockProperty
    {
        None,
        Compressed = 2,
        PartialEncrypted = 4,
        FullEncrypted = 5,
        CompressedEncrypted = FullEncrypted | Compressed
    }
}