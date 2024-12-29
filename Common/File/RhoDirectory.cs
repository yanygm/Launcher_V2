using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KartLibrary.File;

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
        using MemoryStream input = new MemoryStream(DirInfoData);
        BinaryReader binaryReader = new BinaryReader(input);
        int num = binaryReader.ReadInt32();
        Directories = new Dictionary<string, RhoDirectory>(num);
        for (int i = 0; i < num; i++)
        {
            RhoDirectory rhoDirectory = new RhoDirectory(BaseRho);
            StringBuilder stringBuilder = new StringBuilder();
            for (char c = (char)binaryReader.ReadInt16(); c != 0; c = (char)binaryReader.ReadInt16())
            {
                stringBuilder.Append(c);
            }

            uint dirIndex = binaryReader.ReadUInt32();
            rhoDirectory.DirectoryName = stringBuilder.ToString();
            rhoDirectory.DirIndex = dirIndex;
            Directories.Add(rhoDirectory.DirectoryName, rhoDirectory);
        }

        int num2 = binaryReader.ReadInt32();
        Files = new Dictionary<string, RhoFileInfo>(num2);
        for (int j = 0; j < num2; j++)
        {
            RhoFileInfo rhoFileInfo = new RhoFileInfo(BaseRho);
            StringBuilder stringBuilder2 = new StringBuilder();
            for (char c2 = (char)binaryReader.ReadInt16(); c2 != 0; c2 = (char)binaryReader.ReadInt16())
            {
                stringBuilder2.Append(c2);
            }

            rhoFileInfo.Name = stringBuilder2.ToString();
            stringBuilder2.Clear();
            uint num3 = binaryReader.ReadUInt32();
            rhoFileInfo.FileProperty = (RhoFileProperty)binaryReader.ReadInt32();
            rhoFileInfo.FileBlockIndex = binaryReader.ReadUInt32();
            rhoFileInfo.FileSize = binaryReader.ReadInt32();
            for (int k = 0; k < 4; k++)
            {
                char c2 = (char)((num3 >> (k << 3)) & 0xFFu);
                if (c2 != 0)
                {
                    stringBuilder2.Append(c2);
                }
            }

            rhoFileInfo.Extension = stringBuilder2.ToString();
            Files.Add(rhoFileInfo.FullFileName, rhoFileInfo);
            if (!counter.ContainsKey(rhoFileInfo.FileProperty))
            {
                counter.Add(rhoFileInfo.FileProperty, new Dictionary<string, int>());
            }

            if (!counter[rhoFileInfo.FileProperty].ContainsKey(rhoFileInfo.Extension))
            {
                counter[rhoFileInfo.FileProperty].Add(rhoFileInfo.Extension, 0);
            }

            counter[rhoFileInfo.FileProperty][rhoFileInfo.Extension]++;
        }
    }

    public RhoDirectory GetDirectory(string DirFileName)
    {
        if (Directories.ContainsKey(DirFileName))
        {
            return Directories[DirFileName];
        }

        return null;
    }

    public RhoFileInfo GetFile(string FileName)
    {
        if (Files.ContainsKey(FileName))
        {
            return Files[FileName];
        }

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