using System.IO;
using KartLibrary.Data;
using KartLibrary.IO;

namespace KartLibrary.Record;

public static class KartRecord
{
    public static KSVInfo ReadKSVFile(string FileName)
    {
        FileStream fileStream = new FileStream(FileName, FileMode.Open);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        int totalLength = binaryReader.ReadInt32();
        MemoryStream memoryStream = new MemoryStream(binaryReader.ReadKRData(totalLength));
        KSVInfo result = new BinaryReader(memoryStream).ReadKSVInfo();
        memoryStream.Close();
        fileStream.Close();
        return result;
    }

    public static KSVInfo ReadKSVFromBytes(byte[] data)
    {
        MemoryStream memoryStream = new MemoryStream(data);
        BinaryReader binaryReader = new BinaryReader(memoryStream);
        int totalLength = binaryReader.ReadInt32();
        MemoryStream memoryStream2 = new MemoryStream(binaryReader.ReadKRData(totalLength));
        KSVInfo result = new BinaryReader(memoryStream2).ReadKSVInfo();
        memoryStream2.Close();
        memoryStream.Close();
        return result;
    }

    public static KSVInfo OpenKSVFile(string FileName)
    {
        if (!System.IO.File.Exists(FileName))
        {
            throw new FileNotFoundException(FileName);
        }

        using FileStream input = new FileStream(FileName, FileMode.Open);
        BinaryReader binaryReader = new BinaryReader(input);
        int totalLength = binaryReader.ReadInt32();
        using MemoryStream input2 = new MemoryStream(binaryReader.ReadKRData(totalLength));
        return new BinaryReader(input2).ReadKSVInfo();
    }

    public static void SaveKSVFile(string FileName, KSVInfo ksvFile)
    {
        using FileStream fileStream = new FileStream(FileName, FileMode.Create);
        BinaryWriter binaryWriter = new BinaryWriter(fileStream);
        binaryWriter.Write(0);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            new BinaryWriter(memoryStream).WriteKSVInfo(ksvFile);
            byte[] data = memoryStream.ToArray();
            binaryWriter.WriteKRData(data, Encrypted: true, Compressed: true, 912888630u);
        }

        int value = (int)fileStream.Length - 4;
        fileStream.Seek(0L, SeekOrigin.Begin);
        binaryWriter.Write(value);
    }
}