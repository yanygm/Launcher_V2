using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KartLibrary.Encrypt;
using System.Xml.Linq;
using KartLibrary.IO;
using Ionic.Zlib;

namespace KartLibrary.Data
{
    // KR is means "KartRider and Raycity".
    public static class DataProcessor
    {
        public static byte[] EncodeKRData(byte[] Data, bool Encrypted, bool Compressed, uint EncryptKey = 0)
        {
            using (MemoryStream ms = new MemoryStream(Data.Length))
            {
                BinaryWriter bw = new BinaryWriter(ms);
                long initialPos = bw.BaseStream.Position;
                const byte checkCode = 0x53;
                byte ProcessMode = (byte)((Encrypted ? 2 : 0) | (Compressed ? 1 : 0));
                uint Hash = IO.Adler.Adler32(0, Data, 0, Data.Length); ;
                int DecompressSize = Data.Length;
                byte[] processedData = Data;
                if (Compressed)
                {
                    using (MemoryStream mss = new MemoryStream())
                    {
                        ZlibStream zs = new ZlibStream(mss, CompressionMode.Compress);
                        zs.Write(processedData, 0, processedData.Length);
                        processedData = ms.ToArray();
                    }
                }
                if (Encrypted)
                {
                    processedData = RhoEncrypt.DecryptData(EncryptKey, processedData);
                }
                bw.Write(checkCode);
                bw.Write(ProcessMode);
                bw.Write(Hash);
                if (Encrypted)
                    bw.Write(EncryptKey);
                if (Compressed)
                    bw.Write(DecompressSize);
                bw.Write(processedData);
                return ms.ToArray();
            }
        }

        public static byte[] DecodeKRData(byte[] OriginalData)
        {
            using (MemoryStream ms = new MemoryStream(OriginalData))
            {
                BinaryReader br = new BinaryReader(ms);
                byte checkCode = br.ReadByte();
                if (checkCode != 0x53)
                    throw new Exception("It is not KRData Format.");
                byte ProcessMode = br.ReadByte();
                uint Hash = br.ReadUInt32();
                bool Encrypted = (ProcessMode & 2) == 2;
                bool Compressed = (ProcessMode & 1) == 1;
                uint EncryptKey = Encrypted ? br.ReadUInt32() : 0;
                int DecompressSize = Compressed ? br.ReadInt32() : 0;
                byte[] originalData = br.ReadBytes((int)(OriginalData.Length - br.BaseStream.Position));
                byte[] processedData = originalData;
                if (Encrypted)
                {
                    processedData = RhoEncrypt.DecryptData(EncryptKey, processedData);
                }
                if (Compressed)
                {
                    using (MemoryStream mss = new MemoryStream(processedData))
                    {
                        processedData = new byte[DecompressSize];
                        ZlibStream zs = new ZlibStream(mss, CompressionMode.Decompress);
                        zs.Read(processedData, 0, processedData.Length);
                    }
                }
                uint CheckHash = IO.Adler.Adler32(0, processedData, 0, processedData.Length);
                if (CheckHash != Hash)
                    throw new Exception("Exception: KRData hash is not qualified.");
                return processedData;
            }
        }

        // Extensions
        public static byte[] ReadKRData(this BinaryReader br, int TotalLength)
        {
            long initialPos = br.BaseStream.Position;
            byte checkCode = br.ReadByte();
            if (checkCode != 0x53)
                throw new Exception("It is not KRData Format.");
            byte ProcessMode = br.ReadByte();
            uint Hash = br.ReadUInt32();
            bool Encrypted = (ProcessMode & 2) == 2;
            bool Compressed = (ProcessMode & 1) == 1;
            uint EncryptKey = Encrypted ? br.ReadUInt32() : 0;
            int DecompressSize = Compressed ? br.ReadInt32() : 0;
            byte[] originalData = br.ReadBytes((int)(TotalLength - (br.BaseStream.Position - initialPos)));
            byte[] processedData = originalData;
            if (Encrypted)
            {
                processedData = RhoEncrypt.DecryptData(EncryptKey, processedData);
            }
            if (Compressed)
            {
                using (MemoryStream ms = new MemoryStream(processedData))
                {
                    processedData = new byte[DecompressSize];
                    Ionic.Zlib.ZlibStream zs = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Decompress);
                    zs.Read(processedData, 0, processedData.Length);
                }
            }
            uint CheckHash = IO.Adler.Adler32(0, processedData, 0, processedData.Length);
            if (CheckHash != Hash)
                throw new Exception("Exception: KRData hash is not qualified.");
            return processedData;
        }

        public static int WriteKRData(this BinaryWriter bw, byte[] Data, bool Encrypted, bool Compressed, uint EncryptKey = 0)
        {
            long initialPos = bw.BaseStream.Position;
            const byte checkCode = 0x53;
            byte ProcessMode = (byte)((Encrypted ? 2 : 0) | (Compressed ? 1 : 0));
            uint Hash = IO.Adler.Adler32(0, Data, 0, Data.Length); ;
            int DecompressSize = Data.Length;
            byte[] processedData = Data;
            if (Compressed)
            {
                using MemoryStream memoryStream = new MemoryStream();
                using (var zlib = new ZlibStream(memoryStream, CompressionMode.Compress))
                {
                    zlib.Write(processedData, 0, processedData.Length);
                }
                processedData = memoryStream.ToArray();
            }
            if (Encrypted)
            {
                processedData = RhoEncrypt.DecryptData(EncryptKey, processedData);
            }
            bw.Write(checkCode);
            bw.Write(ProcessMode);
            bw.Write(Hash);
            if (Encrypted)
                bw.Write(EncryptKey);
            if (Compressed)
                bw.Write(DecompressSize);
            bw.Write(processedData);
            return (int)(bw.BaseStream.Position - initialPos);
        }
    }
}