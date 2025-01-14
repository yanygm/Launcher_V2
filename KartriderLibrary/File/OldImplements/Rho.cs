using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KartLibrary.Encrypt;
using KartLibrary.IO;
using System.Diagnostics;
using KartLibrary.File;

namespace KartLibrary.File
{
    [Obsolete("Rho class is deprecated. Use RhoArchive instead.")]
    public class Rho : IDisposable
    {
        public Stream baseStream;
        private (double, string)[] MagicString =
        {
            (1.0d, "Rh layer spec 1.0") ,
            (1.1d, "Rh layer spec 1.1") ,
        };
        public double Version { get; private set; }
        public string FileName { get; private set; }

        public uint RhoFileKey = 0;

        public uint DataHash = 0;

        private uint BlockWhiteningKey = 0;

        private Dictionary<uint, RhoDataInfo> Blocks;

        public RhoDirectory RootDirectory { get; set; }

        public Rho(string FileName, uint rhoFileKey = 0)
        {
            if (!System.IO.File.Exists(FileName))
                throw new FileNotFoundException($"Exception: Could't find the file:{FileName}.", FileName);
            //Test
            FileStream fileStream = new FileStream(FileName, FileMode.Open);

            baseStream = new BufferedStream(fileStream, 4096); //Default: 4 KiB
            this.FileName = FileName;
            BinaryReader reader = new BinaryReader(baseStream);
            FileInfo fileInfo = new FileInfo(FileName);
            if (rhoFileKey == 0)
            {
                RhoFileKey = RhoKey.GetRhoKey(fileInfo.Name.Replace(".rho", ""));
            }
            else
            {
                RhoFileKey = rhoFileKey;
            }
            // Read Magic String
            byte[] magicStrBytes = reader.ReadBytes(0x22);
            string magicStr = Encoding.GetEncoding("UTF-16").GetString(magicStrBytes);
            int verIndex = Array.FindIndex(MagicString, x => x.Item2 == magicStr);
            if (verIndex == -1)
                throw new NotSupportedException($"Exception: This file:{FileName} is not supported.");
            Version = MagicString[verIndex].Item1;
            baseStream.Seek(0x80, SeekOrigin.Begin);
            // Part 2
            byte[] part2Data = reader.ReadBytes(0x80);
            if (Version == 1.0d)
                RhoEncrypt.DecryptData(RhoFileKey, part2Data, 0, part2Data.Length);
            else if (Version == 1.1d)
                part2Data = RhoEncrypt.DecryptHeaderInfo(part2Data, RhoFileKey);
            int BlockCount = 0;
            byte[] BlockInfoKeyOld = new byte[0]; // For 1.0 version
            uint BlockInfoKey = 0; // For 1.1 version
            using (MemoryStream ms = new MemoryStream(part2Data))
            {
                BinaryReader br = new BinaryReader(ms);
                uint part2Hash = br.ReadUInt32();
                uint checkHash = Adler.Adler32(0, part2Data, 4, 0x7C);
                if (part2Hash != checkHash)
                    throw new NotSupportedException($"Exception: This file:{FileName} was modified. [ Part 2 Hash not euqal ]");
                int MagicCode = br.ReadInt32();
                if (Version == 1.0d && MagicCode != 0x00010000 || Version == 1.1d && MagicCode != 0x00010001)
                    throw new NotSupportedException($"Exception: This file:{FileName} is not Rho File. [ Header check failure ]");
                BlockCount = br.ReadInt32(); // 10
                BlockWhiteningKey = br.ReadUInt32(); //14 // BlockInfoKey = RhoFileKey ^  BlockWhiteningKey. 
                BlockInfoKey = RhoFileKey ^ BlockWhiteningKey;
                //1.1: 14 bytes unknown
                if (Version == 1.0d)
                {
                    BlockInfoKeyOld = br.ReadBytes(32);
                }
                else if (Version == 1.1d)
                {
                    int u1a = br.ReadInt32(); //=1
                    int u2a = br.ReadInt32(); //=RhoKey - 397E40C3
                    DataHash = br.ReadUInt32(); // in aaa.pk file
                    //Debug.Print($"DataHash: {DataHash:x8}");

                }
                int EndMagicCode = br.ReadInt32(); // = FC1F9778

                Blocks = new Dictionary<uint, RhoDataInfo>(BlockCount);
            }

            baseStream.Seek(0x100, SeekOrigin.Begin);
            // Part 3
            for (int i = 0; i < BlockCount; i++)
            {
                if (Version == 1.0d)
                {
                    var blockInfo = reader.ReadBlockInfo10(BlockInfoKeyOld);
                    Blocks.Add(blockInfo.Index, blockInfo);
                }
                else if (Version == 1.1d)
                {
                    var blockInfo = reader.ReadBlockInfo(BlockInfoKey);
                    Blocks.Add(blockInfo.Index, blockInfo);
                    BlockInfoKey++;
                }
            }
            // Part 4
            RootDirectory = new RhoDirectory(this);
            RootDirectory.DirectoryName = "";
            RootDirectory.DirIndex = 0xFFFFFFFF;
            Queue<RhoDirectory> processQueue = new Queue<RhoDirectory>();
            processQueue.Enqueue(RootDirectory);
            while (processQueue.Count > 0)
            {
                RhoDirectory curDir = processQueue.Dequeue();
                byte[] dirData = reader.ReadBlock(this, curDir.DirIndex, RhoKey.GetDirectoryDataKey(RhoFileKey));

                curDir.GetFromDirInfo(dirData);
                foreach (RhoDirectory subdir in curDir.GetDirectories())
                {
                    processQueue.Enqueue(subdir);
                }
            }
            List<RhoDataInfo> trtt = new List<RhoDataInfo>();
            foreach (var blockKeyPair in Blocks)
            {
                trtt.Add(blockKeyPair.Value);
            }
        }
        public uint GetFileKey()
        {
            return RhoFileKey;
        }
        public uint GetDataHash()
        {
            return DataHash;
        }
        internal RhoDataInfo GetBlockInfo(uint Index)
        {
            if (!Blocks.ContainsKey(Index))
                return null;
            return Blocks[Index];
        }

        internal byte[] GetBlockData(uint BlockIndex, uint Key)
        {
            BinaryReader reader = new BinaryReader(baseStream);
            byte[] output = reader.ReadBlock(this, BlockIndex, Key);
            uint adler = Adler.Adler32(0, output, 0, output.Length);
            return output;
        }

        public RhoFileInfo GetFile(string Path)
        {
            string[] PathSplit = Path.Split('/');
            RhoDirectory rd = RootDirectory;
            for (int i = 1; i < PathSplit.Length - 1; i++)
            {
                string curPathName = PathSplit[i].Trim();
                if (curPathName == "")
                    continue;
                RhoDirectory nextDir = rd.GetDirectory(curPathName);
                if (nextDir is null)
                    return null;
                rd = nextDir;
            }
            return rd.GetFile(PathSplit[PathSplit.Length - 1]);
        }

        public void Dispose()
        {
            baseStream.Close();
            baseStream.Dispose();
            Blocks = null;
        }

        ~Rho()
        {
            Dispose();
        }
    }
}