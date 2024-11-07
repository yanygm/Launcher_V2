using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Ionic.Zlib;
using KartLibrary.Consts;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.File;

public class Rho5Archive : IRhoArchive<Rho5Folder, Rho5File>, IDisposable
{
    private class DataSavingInfo
    {
        public Rho5File? File;

        public byte[] Data;
    }

    private Rho5Folder _rootFolder;

    private Dictionary<int, FileStream> _rho5Streams;

    private Dictionary<string, Rho5FileHandler> _fileHandlers;

    private Dictionary<int, int> _dataBeginPoses;

    private bool _closed;

    public Rho5Folder RootFolder => _rootFolder;

    public bool IsClosed => _closed;

    public Rho5Archive()
    {
        _rootFolder = new Rho5Folder();
        _fileHandlers = new Dictionary<string, Rho5FileHandler>();
        _rho5Streams = new Dictionary<int, FileStream>();
        _dataBeginPoses = new Dictionary<int, int>();
    }

    public void Open(string dataPackPath, string dataPackName, CountryCode region)
    {
        if (!Directory.Exists(dataPackPath))
        {
            throw new Exception(dataPackPath + " doesn't exists.");
        }

        FileInfo[] files = new DirectoryInfo(dataPackPath).GetFiles();
        foreach (FileInfo fileInfo in files)
        {
            Match match = new Regex("^" + dataPackName + "_(\\d{5})\\.rho5$").Match(fileInfo.Name);
            if (match.Success)
            {
                int dataPackID = Convert.ToInt32(match.Groups[1].Value);
                openSingleFile(dataPackID, fileInfo.FullName, region);
            }
        }
    }

    public void Save(string dataPackPath, string dataPackName, CountryCode region, SavePattern savePattern = SavePattern.Auto)
    {
        int num = ((_rho5Streams.Count == 0) ? (-1) : _rho5Streams.Select<KeyValuePair<int, FileStream>, int>((KeyValuePair<int, FileStream> x) => x.Key).Max());
        string mixingString = getMixingString(region);
        Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
        Dictionary<int, Queue<Rho5File>> dictionary2 = new Dictionary<int, Queue<Rho5File>>();
        Queue<Rho5File> queue = new Queue<Rho5File>();
        Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
        for (int i = 0; i <= num; i++)
        {
            dictionary.Add(i, value: false);
            dictionary2.Add(i, new Queue<Rho5File>());
            dictionary3.Add(i, 0);
        }

        Queue<Rho5Folder> queue2 = new Queue<Rho5Folder>();
        queue2.Enqueue(_rootFolder);
        while (queue2.Count > 0)
        {
            Rho5Folder rho5Folder = queue2.Dequeue();
            foreach (Rho5Folder folder in rho5Folder.Folders)
            {
                queue2.Enqueue(folder);
            }

            foreach (Rho5File file in rho5Folder.Files)
            {
                if (file.IsModified && file._dataPackID >= 0)
                {
                    dictionary[file._dataPackID] = true;
                }

                if (file._dataPackID < 0)
                {
                    queue.Enqueue(file);
                }
                else
                {
                    dictionary2[file._dataPackID].Enqueue(file);
                }
            }
        }

        for (int j = 0; j <= num; j++)
        {
            if (savePattern == SavePattern.AlwaysRegeneration || (savePattern == SavePattern.GenerateIfModified && dictionary[j]))
            {
                string dataPackFilePath = getDataPackFilePath(dataPackPath, dataPackName, j);
                bool reopen = true;
                if (savePattern == SavePattern.GenerateIfModified && _rho5Streams.ContainsKey(j) && _rho5Streams[j].Name != dataPackFilePath)
                {
                    reopen = false;
                }

                saveSingleFileTo(dataPackPath, dataPackName ?? "", j, mixingString, dictionary2[j], int.MaxValue, reopen);
            }
        }

        int num2 = num + 1;
        while (queue.Count > 0)
        {
            saveSingleFileTo(dataPackPath, dataPackName ?? "", num2, mixingString, queue, 10485760, reopen: true);
            num2++;
        }
    }

    public void Close()
    {
        if (_closed)
        {
            throw new Exception("this archive is close or is not open from rho5 file.");
        }

        foreach (FileStream value in _rho5Streams.Values)
        {
            if (value.CanRead)
            {
                value.Close();
            }

            value.Dispose();
        }

        _rho5Streams.Clear();
        _rootFolder.Clear();
        releaseAllHandles();
        _closed = true;
    }

    public void Dispose()
    {
        foreach (FileStream value in _rho5Streams.Values)
        {
            if (value.CanRead)
            {
                value.Close();
            }

            value.Dispose();
        }

        _rho5Streams.Clear();
        releaseAllHandles();
    }

    private void openSingleFile(int dataPackID, string filePath, CountryCode region)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("");
        }

        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        string mixingString = getMixingString(region);
        string fileName = Path.GetFileName(filePath);
        Rho5DecryptStream rho5DecryptStream = new Rho5DecryptStream(fileStream, fileName, mixingString);
        BinaryReader binaryReader = new BinaryReader(rho5DecryptStream);
        int headerOffset = getHeaderOffset(fileName);
        int num = headerOffset + getFilesInfoOffset(fileName);
        rho5DecryptStream.Seek(headerOffset, SeekOrigin.Begin);
        int num2 = binaryReader.ReadInt32();
        int num3 = binaryReader.ReadByte();
        int num4 = binaryReader.ReadInt32();
        if (num2 != num3 + num4)
        {
            throw new Exception("Rho5 header crc mismatch.");
        }

        if (num3 != 2)
        {
            throw new Exception("unsupported package version.");
        }

        rho5DecryptStream.Seek(num, SeekOrigin.Begin);
        rho5DecryptStream.SetToFilesInfoKey(fileName, mixingString);
        for (int i = 0; i < num4; i++)
        {
            string text = binaryReader.ReadText();
            int num5 = binaryReader.ReadInt32();
            int num6 = binaryReader.ReadInt32();
            int num7 = binaryReader.ReadInt32();
            int num8 = binaryReader.ReadInt32();
            int num9 = binaryReader.ReadInt32();
            byte[] array = binaryReader.ReadBytes(16);
            int num10 = num6 + num7 + num8 + num9;
            byte[] array2 = array;
            foreach (byte b in array2)
            {
                num10 += b;
            }

            if (num5 != num10)
            {
                throw new Exception("fileInfo checksum mismatch.");
            }

            byte[] packedFileKey = Rho5Key.GetPackedFileKey(array, Rho5Key.GetFileKey_U1(mixingString), text);
            Rho5FileHandler rho5FileHandler = new Rho5FileHandler(this, dataPackID, num7, num8, num9, packedFileKey, array);
            Rho5Folder rho5Folder = _rootFolder;
            string[] array3 = text.Split('/');
            for (int k = 0; k < array3.Length - 1; k++)
            {
                string text2 = array3[k];
                if (!rho5Folder.ContainsFolder(text2))
                {
                    rho5Folder.AddFolder(new Rho5Folder
                    {
                        Name = text2
                    });
                    rho5Folder.appliedChanges();
                }

                rho5Folder = rho5Folder.GetFolder(text2);
            }

            Rho5File rho5File = new Rho5File();
            rho5File.DataSource = new Rho5DataSource(rho5FileHandler);
            rho5File.Name = array3[^1];
            rho5File._dataPackID = dataPackID;
            rho5File.appliedChanges();
            rho5Folder.AddFile(rho5File);
            rho5Folder.appliedChanges();
            _fileHandlers.Add(text, rho5FileHandler);
        }

        if (!_dataBeginPoses.ContainsKey(dataPackID))
        {
            _dataBeginPoses.Add(dataPackID, 0);
        }

        _dataBeginPoses[dataPackID] = ((int)rho5DecryptStream.Position + 1023) & 0x7FFFFC00;
        _rho5Streams.Add(dataPackID, fileStream);
    }

    public void SaveFolder(string dir, string dataPackName, string output, CountryCode code, int dataPackID = 0)
    {
        string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
        Queue<Rho5File> queue = new Queue<Rho5File>();
        string[] array = files;
        foreach (string text in array)
        {
            Rho5File item = new Rho5File
            {
                DataSource = new FileDataSource(text),
                Name = text.Replace(dir, "").Replace("\\", "/")
            };
            queue.Enqueue(item);
        }

        saveSingleFileTo(output, dataPackName, dataPackID, getMixingString(code), queue);
    }

    private void saveSingleFileTo(string dataPackPath, string dataPackName, int dataPackID, string mixingStr, Queue<Rho5File> allFileQueue, int maxSize = 1992294400, bool reopen = false)
    {
        //IL_01e0: Unknown result type (might be due to invalid IL or missing references)
        //IL_01e5: Unknown result type (might be due to invalid IL or missing references)
        //IL_01f2: Expected O, but got Unknown
        //IL_01f2: Unknown result type (might be due to invalid IL or missing references)
        //IL_01f8: Expected O, but got Unknown
        //IL_01fd: Expected O, but got Unknown
        string dataPackFilePath = getDataPackFilePath(dataPackPath, dataPackName, dataPackID);
        if (!Directory.Exists(Path.GetDirectoryName(dataPackFilePath) ?? ""))
        {
            throw new Exception("directory not exists.");
        }

        string fileName = Path.GetFileName(dataPackFilePath);
        MemoryStream memoryStream = new MemoryStream(Math.Min(maxSize, 21943040));
        Rho5EncryptStream rho5EncryptStream = new Rho5EncryptStream(memoryStream);
        BinaryWriter binaryWriter = new BinaryWriter(rho5EncryptStream);
        if (_rho5Streams.ContainsKey(dataPackID) && _rho5Streams[dataPackID].Name == dataPackFilePath)
        {
            reopen = true;
        }

        maxSize = (int)Math.Round((double)maxSize * 1.05);
        int num = 0;
        int num2 = 0;
        Queue<Rho5File> queue = new Queue<Rho5File>();
        while (allFileQueue.Count > 0 && num2 <= maxSize && num2 < maxSize)
        {
            Rho5File rho5File = allFileQueue.Dequeue();
            num += 40 + (rho5File.FullName.Length << 1);
            if (!rho5File.HasDataSource)
            {
                throw new Exception();
            }

            num2 += rho5File.Size;
            queue.Enqueue(rho5File);
        }

        int headerOffset = getHeaderOffset(fileName);
        int num3 = headerOffset + getFilesInfoOffset(fileName);
        rho5EncryptStream.SetLength(headerOffset);
        rho5EncryptStream.Seek(headerOffset, SeekOrigin.Begin);
        rho5EncryptStream.SetToHeaderKey(fileName, mixingStr);
        int value = 2 + queue.Count;
        binaryWriter.Write(value);
        binaryWriter.Write((byte)2);
        binaryWriter.Write(queue.Count);
        binaryWriter.Flush();
        MemoryStream memoryStream2 = new MemoryStream(num);
        BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream2);
        int num4 = (num3 + num + 1023) & 0x7FFFFC00;
        int num5 = num4;
        rho5EncryptStream.SetLength(num4);
        while (queue.Count > 0)
        {
            Rho5File rho5File2 = queue.Dequeue();
            if (!rho5File2.HasDataSource)
            {
                throw new Exception();
            }

            byte[] bytes = rho5File2.GetBytes();
            byte[] array = MD5.HashData(bytes);
            byte[] packedFileKey = Rho5Key.GetPackedFileKey(array, Rho5Key.GetFileKey_U1(mixingStr), rho5File2.FullName);
            byte[] array2;
            using (MemoryStream memoryStream3 = new MemoryStream())
            {
                ZlibStream val = new ZlibStream((Stream)memoryStream3, (CompressionMode)0, true);
                ((Stream)val).Write(bytes, 0, bytes.Length);
                ((Stream)val).Flush();
                ((Stream)val).Close();
                array2 = memoryStream3.ToArray();
                Rho5EncryptStream rho5EncryptStream2 = new Rho5EncryptStream(memoryStream3, packedFileKey);
                rho5EncryptStream2.Seek(0L, SeekOrigin.Begin);
                rho5EncryptStream2.Write(array2, 0, Math.Min(array2.Length, 1024));
                rho5EncryptStream2.Flush();
                array2 = memoryStream3.ToArray();
            }

            int num6 = 7 + (num5 - num4 >> 10) + bytes.Length + array2.Length;
            byte[] array3 = array;
            foreach (byte b in array3)
            {
                num6 += b;
            }

            binaryWriter2.WriteKRString(rho5File2.FullName);
            binaryWriter2.Write(num6);
            binaryWriter2.Write(7);
            binaryWriter2.Write(num5 - num4 >> 10);
            binaryWriter2.Write(bytes.Length);
            binaryWriter2.Write(array2.Length);
            binaryWriter2.Write(array);
            rho5EncryptStream.SetKey(packedFileKey);
            rho5EncryptStream.Seek(num5, SeekOrigin.Begin);
            rho5EncryptStream.Write(array2, 0, array2.Length);
            rho5EncryptStream.Flush();
            if (reopen)
            {
                Rho5FileHandler rho5FileHandler = new Rho5FileHandler(this, dataPackID, num5 - num4 >> 10, bytes.Length, array2.Length, packedFileKey, array);
                Rho5DataSource dataSource = new Rho5DataSource(rho5FileHandler);
                rho5File2.DataSource = dataSource;
                if (_fileHandlers.ContainsKey(rho5File2.FullName))
                {
                    _fileHandlers[rho5File2.FullName].releaseHandler();
                    _fileHandlers.Remove(rho5File2.FullName);
                }

                _fileHandlers.Add(rho5File2.FullName, rho5FileHandler);
            }

            num5 = (num5 + array2.Length + 1023) & 0x7FFFFC00;
            rho5EncryptStream.SetLength(num5);
        }

        rho5EncryptStream.Seek(num3, SeekOrigin.Begin);
        rho5EncryptStream.SetToFilesInfoKey(fileName, mixingStr);
        byte[] array4 = memoryStream2.ToArray();
        memoryStream2.Close();
        rho5EncryptStream.Write(array4, 0, array4.Length);
        rho5EncryptStream.Flush();
        if (reopen)
        {
            if (_rho5Streams.ContainsKey(dataPackID))
            {
                _rho5Streams[dataPackID].Dispose();
                _rho5Streams.Remove(dataPackID);
            }

            if (_dataBeginPoses.ContainsKey(dataPackID))
            {
                _dataBeginPoses.Remove(dataPackID);
            }
        }

        using (FileStream stream = new FileStream(dataPackFilePath, FileMode.Create))
        {
            memoryStream.WriteTo(stream);
        }

        if (reopen)
        {
            _rho5Streams.Add(dataPackID, new FileStream(dataPackFilePath, FileMode.Open, FileAccess.Read));
            _dataBeginPoses.Add(dataPackID, num4);
        }

        memoryStream.Dispose();
    }

    private int getHeaderOffset(string fileName)
    {
        fileName = fileName.ToLower();
        int num = 0;
        string text = fileName;
        foreach (char c in text)
        {
            num += c;
        }

        long num2 = num * 2753184165u >> 32;
        int num3 = num - (int)num2;
        num3 >>= 1;
        num3 += (int)num2;
        num3 >>= 8;
        num3 *= 312;
        return num - num3 + 30;
    }

    private int getFilesInfoOffset(string fileName)
    {
        fileName = fileName.ToLower();
        int num = 0;
        string text = fileName;
        foreach (char c in text)
        {
            num += c;
        }

        num *= 3;
        long num2 = (long)num * 891408307L >> 32;
        int num3 = num - (int)num2;
        num3 >>= 1;
        num3 += (int)num2;
        num3 >>= 7;
        num3 *= 212;
        return num - num3 + 42;
    }

    private string getMixingString(CountryCode region)
    {
        return region switch
        {
            CountryCode.KR => "y&errfV6GRS!e8JL",
            CountryCode.CN => "d$Bjgfc8@dH4TQ?k",
            CountryCode.TW => "t5rHKg-g9BA7%=qD",
            _ => throw new Exception(""),
        };
    }

    private string getDataPackFilePath(string dataPackPath, string dataPackName, int dataPackID)
    {
        return Path.Combine(Path.GetFullPath(dataPackPath), $"{dataPackName}_{dataPackID:00000}.rho5");
    }

    private void releaseAllHandles()
    {
        foreach (Rho5FileHandler value in _fileHandlers.Values)
        {
            value.releaseHandler();
        }

        _fileHandlers.Clear();
    }

    internal byte[] getData(Rho5FileHandler handler)
    {
        //IL_0103: Unknown result type (might be due to invalid IL or missing references)
        //IL_0114: Expected O, but got Unknown
        if (!_rho5Streams.ContainsKey(handler._dataPackID) || !_dataBeginPoses.ContainsKey(handler._dataPackID))
        {
            throw new Exception("Invalid data pack id in file handler.");
        }

        FileStream fileStream = _rho5Streams[handler._dataPackID];
        if (fileStream == null || !fileStream.CanRead)
        {
            throw new Exception("");
        }

        FileStream fileStream2 = new FileStream(fileStream.SafeFileHandle, FileAccess.Read);
        Rho5DecryptStream rho5DecryptStream = new Rho5DecryptStream(fileStream2, handler._key);
        int num = _dataBeginPoses[handler._dataPackID] + (handler._offset << 10);
        rho5DecryptStream.Seek(num, SeekOrigin.Begin);
        byte[] array = new byte[handler._compressedSize];
        byte[] array2 = new byte[handler._decompressedSize];
        rho5DecryptStream.Read(array, 0, (array.Length >= 1024) ? 1024 : array.Length);
        if (array.Length >= 1024)
        {
            fileStream2.Read(array, 1024, array.Length - 1024);
        }

        using MemoryStream baseStream = new MemoryStream(array);
        ((Stream)new ZlibStream((Stream)new Rho5DecryptStream(baseStream, handler._key), (CompressionMode)1)).Read(array2, 0, array2.Length);
        return array2;
    }
}
