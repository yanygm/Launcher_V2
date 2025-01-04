using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KartLibrary.IO;
using KartLibrary.Data;

namespace KartLibrary.Record
{
    public static class KartRecord
    {
        public static KSVInfo ReadKSVFile(string FileName)
        {
            FileStream fs = new FileStream(FileName, FileMode.Open);
            BinaryReader reader = new BinaryReader(fs);
            int FileSize = reader.ReadInt32();
            byte[] originalData = reader.ReadKRData(FileSize);
            MemoryStream ms = new MemoryStream(originalData);
            BinaryReader memReader = new BinaryReader(ms);
            KSVInfo output = memReader.ReadKSVInfo();
            ms.Close();
            fs.Close();
            return output;
        }

        public static KSVInfo ReadKSVFromBytes(byte[] data)
        {
            MemoryStream dataMS= new MemoryStream(data);
            BinaryReader reader = new BinaryReader(dataMS);
            int FileSize = reader.ReadInt32();
            byte[] originalData = reader.ReadKRData(FileSize);
            MemoryStream ms = new MemoryStream(originalData);
            BinaryReader memReader = new BinaryReader(ms);
            KSVInfo output = memReader.ReadKSVInfo();
            ms.Close();
            dataMS.Close();
            return output;
        }

        public static KSVInfo OpenKSVFile(string FileName)
        {
            if (!System.IO.File.Exists(FileName))
                throw new FileNotFoundException(FileName);
            using(FileStream fileStream = new FileStream(FileName, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(fileStream);
                int totalLen = reader.ReadInt32();
                byte[] decryptData = reader.ReadKRData(totalLen);
                using (MemoryStream decryptDataStream = new MemoryStream(decryptData))
                {
                    BinaryReader dataReader = new BinaryReader(decryptDataStream);
                    KSVInfo output = dataReader.ReadKSVInfo();
                    return output;
                }
            }
        }

        public static void SaveKSVFile(string FileName,KSVInfo ksvFile)
        {
            using (FileStream fileStream = new FileStream(FileName, FileMode.Create))
            {
                BinaryWriter writer = new BinaryWriter(fileStream);
                writer.Write(0);
                using (MemoryStream outputDataStream = new MemoryStream())
                {
                    BinaryWriter dataWriter = new BinaryWriter(outputDataStream);
                    dataWriter.WriteKSVInfo(ksvFile);
                    byte[] rawData = outputDataStream.ToArray();
                    writer.WriteKRData(rawData, true, true, 0x36699336);
                }
                int total_len = (int)fileStream.Length - 4;
                fileStream.Seek(0, SeekOrigin.Begin);
                writer.Write(total_len);
            }
        }
    }
}
