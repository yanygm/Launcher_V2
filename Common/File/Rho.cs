using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.File;

public class Rho : IDisposable
{
    internal Stream baseStream;

    private (double, string)[] MagicString = new (double, string)[2]
    {
        (1.0, "Rh layer spec 1.0"),
        (1.1, "Rh layer spec 1.1")
    };

    private uint RhoFileKey;

    private uint RhoDataHash;

    private uint BlockWhiteningKey;

    private Dictionary<uint, RhoDataInfo> Blocks;

    public double Version { get; private set; }

    public string FileName { get; private set; }

    public RhoDirectory RootDirectory { get; set; }

    public Rho(string FileName)
    {
        if (!System.IO.File.Exists(FileName))
        {
            throw new FileNotFoundException("Exception: Could't find the file:" + FileName + ".", FileName);
        }

        FileStream stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
        baseStream = new BufferedStream(stream, 4096);
        this.FileName = FileName;
        BinaryReader binaryReader = new BinaryReader(baseStream);
        FileInfo fileInfo = new FileInfo(FileName);
        RhoFileKey = RhoKey.GetRhoKey(fileInfo.Name.Replace(".rho", ""));
        byte[] bytes = binaryReader.ReadBytes(34);
        string magicStr = Encoding.GetEncoding("UTF-16").GetString(bytes);
        int num = Array.FindIndex(MagicString, ((double, string) x) => x.Item2 == magicStr);
        if (num == -1)
        {
            throw new NotSupportedException("Exception: This file is not supported.");
        }

        Version = MagicString[num].Item1;
        baseStream.Seek(128L, SeekOrigin.Begin);
        byte[] array = binaryReader.ReadBytes(128);
        if (Version == 1.0)
        {
            RhoEncrypt.DecryptData(RhoFileKey, array, 0, array.Length);
        }
        else if (Version == 1.1)
        {
            array = RhoEncrypt.DecryptHeaderInfo(array, RhoFileKey);           
        }

        byte[] subArray = new byte[4];
        Array.Copy(array, 24, subArray, 0, 4);
        RhoDataHash = BitConverter.ToUInt32(subArray, 0);

        int num2 = 0;
        byte[] key = new byte[0];
        uint num3 = 0u;
        using (MemoryStream input = new MemoryStream(array))
        {
            BinaryReader binaryReader2 = new BinaryReader(input);
            uint num4 = binaryReader2.ReadUInt32();
            uint num5 = Adler.Adler32(0u, array, 4, 124);
            if (num4 != num5)
            {
                throw new NotSupportedException("Exception: This file was modified. [ Part 2 Hash not euqal ]");
            }

            int num6 = binaryReader2.ReadInt32();
            if ((Version == 1.0 && num6 != 65536) || (Version == 1.1 && num6 != 65537))
            {
                throw new NotSupportedException("Exception: This file is not Rho File. [ Header check failure ]");
            }

            num2 = binaryReader2.ReadInt32();
            BlockWhiteningKey = binaryReader2.ReadUInt32();
            num3 = RhoFileKey ^ BlockWhiteningKey;
            if (Version == 1.0)
            {
                key = binaryReader2.ReadBytes(32);
            }
            else if (Version == 1.1)
            {
                binaryReader2.ReadInt32();
                binaryReader2.ReadInt32();
                binaryReader2.ReadInt32();
            }

            binaryReader2.ReadInt32();
            Blocks = new Dictionary<uint, RhoDataInfo>(num2);
        }

        baseStream.Seek(256L, SeekOrigin.Begin);
        for (int i = 0; i < num2; i++)
        {
            if (Version == 1.0)
            {
                RhoDataInfo rhoDataInfo = binaryReader.ReadBlockInfo10(key);
                Blocks.Add(rhoDataInfo.Index, rhoDataInfo);
            }
            else if (Version == 1.1)
            {
                RhoDataInfo rhoDataInfo2 = binaryReader.ReadBlockInfo(num3);
                Blocks.Add(rhoDataInfo2.Index, rhoDataInfo2);
                num3++;
            }
        }

        RootDirectory = new RhoDirectory(this);
        RootDirectory.DirectoryName = "";
        RootDirectory.DirIndex = uint.MaxValue;
        Queue<RhoDirectory> queue = new Queue<RhoDirectory>();
        queue.Enqueue(RootDirectory);
        while (queue.Count > 0)
        {
            RhoDirectory rhoDirectory = queue.Dequeue();
            byte[] dirInfoData = binaryReader.ReadBlock(this, rhoDirectory.DirIndex, RhoKey.GetDirectoryDataKey(RhoFileKey));
            rhoDirectory.GetFromDirInfo(dirInfoData);
            RhoDirectory[] directories = rhoDirectory.GetDirectories();
            foreach (RhoDirectory item in directories)
            {
                queue.Enqueue(item);
            }
        }

        List<RhoDataInfo> list = new List<RhoDataInfo>();
        foreach (KeyValuePair<uint, RhoDataInfo> block in Blocks)
        {
            list.Add(block.Value);
        }
    }

    internal uint GetFileKey()
    {
        return RhoFileKey;
    }

    internal uint GetDataHash()
    {
        return RhoDataHash;
    }

    internal RhoDataInfo GetBlockInfo(uint Index)
    {
        if (!Blocks.ContainsKey(Index))
        {
            return null;
        }

        return Blocks[Index];
    }

    internal byte[] GetBlockData(uint BlockIndex, uint Key)
    {
        byte[] array = new BinaryReader(baseStream).ReadBlock(this, BlockIndex, Key);
        Adler.Adler32(0u, array, 0, array.Length);
        return array;
    }

    public RhoFileInfo GetFile(string Path)
    {
        string[] array = Path.Split('/');
        RhoDirectory rhoDirectory = RootDirectory;
        for (int i = 1; i < array.Length - 1; i++)
        {
            string text = array[i].Trim();
            if (!(text == ""))
            {
                RhoDirectory directory = rhoDirectory.GetDirectory(text);
                if (directory == null)
                {
                    return null;
                }

                rhoDirectory = directory;
            }
        }

        return rhoDirectory.GetFile(array[^1]);
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