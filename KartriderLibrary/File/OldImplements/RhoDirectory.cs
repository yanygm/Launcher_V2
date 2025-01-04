using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Encrypt;
using System.IO;

namespace KartLibrary.File
{
    [Obsolete("RhoDirectory class is deprecated. Use RhoFolder instead.")]
    public class RhoDirectory
    {
        public static Dictionary<RhoFileProperty, Dictionary<string, int>> counter = new Dictionary<RhoFileProperty, Dictionary<string, int>>();
        public Rho BaseRho { get; set; }
        public string DirectoryName { get; set; }
        public RhoDirectory Parent { get; set; }
        public Dictionary<string, RhoDirectory> Directories { get; private set; }
        public Dictionary<string, RhoFileInfo> Files { get; private set; }
        internal uint DirIndex { get; set; }
        public RhoDirectory(Rho BaseRho)
        {
            this.BaseRho = BaseRho;
            DirectoryName = "";

        }

        internal void GetFromDirInfo(byte[] DirInfoData)
        {
            using (MemoryStream ms = new MemoryStream(DirInfoData))
            {
                BinaryReader msReader = new BinaryReader(ms);
                int DirCount = msReader.ReadInt32();
                Directories = new Dictionary<string, RhoDirectory>(DirCount);
                for (int i = 0; i < DirCount; i++)
                {
                    RhoDirectory dir = new RhoDirectory(BaseRho);
                    StringBuilder strBuilder = new StringBuilder();
                    char tempChar = (char)msReader.ReadInt16();
                    while (tempChar != 0)
                    {
                        strBuilder.Append(tempChar);
                        tempChar = (char)msReader.ReadInt16();
                    }
                    uint dirInd = msReader.ReadUInt32();
                    dir.DirectoryName = strBuilder.ToString();
                    dir.DirIndex = dirInd;
                    Directories.Add(dir.DirectoryName, dir);
                }
                int FileCount = msReader.ReadInt32();
                Files = new Dictionary<string, RhoFileInfo>(FileCount);
                for (int i = 0; i < FileCount; i++)
                {
                    RhoFileInfo rfi = new RhoFileInfo(BaseRho);
                    StringBuilder strBuilder = new StringBuilder();
                    char tempChar = (char)msReader.ReadInt16();
                    while (tempChar != 0)
                    {
                        strBuilder.Append(tempChar);
                        tempChar = (char)msReader.ReadInt16();
                    }
                    rfi.Name = strBuilder.ToString();
                    strBuilder.Clear();
                    uint extInt = msReader.ReadUInt32();
                    rfi.FileProperty = (RhoFileProperty)msReader.ReadInt32();
                    rfi.FileBlockIndex = msReader.ReadUInt32();
                    rfi.FileSize = msReader.ReadInt32();
                    for (int j = 0; j < 4; j++)
                    {
                        tempChar = (char)(extInt >> (j << 3) & 0xFF);
                        if (tempChar != '\0')
                            strBuilder.Append(tempChar);
                    }
                    rfi.Extension = strBuilder.ToString();
                    Files.Add(rfi.FullFileName, rfi);
                    if (!counter.ContainsKey(rfi.FileProperty))
                        counter.Add(rfi.FileProperty, new Dictionary<string, int>());
                    if (!counter[rfi.FileProperty].ContainsKey(rfi.Extension))
                        counter[rfi.FileProperty].Add(rfi.Extension, 0);
                    counter[rfi.FileProperty][rfi.Extension]++;
                }
            }
        }

        public RhoDirectory GetDirectory(string DirFileName)
        {
            if (Directories.ContainsKey(DirFileName))
                return Directories[DirFileName];
            return null;
        }

        public RhoFileInfo GetFile(string FileName)
        {
            if (Files.ContainsKey(FileName))
                return Files[FileName];
            return null;
        }

        public RhoDirectory[] GetDirectories()
        {
            return Directories.Values.ToArray();
        }

        public RhoFileInfo[] GetFiles()
        {
            return Files.Values.ToArray();
        }

        public override int GetHashCode()
        {
            return (int)DirIndex;
        }
    }
}