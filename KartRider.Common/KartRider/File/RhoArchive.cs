using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zlib;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.File;

public class RhoArchive : IRhoArchive<RhoFolder, RhoFile>, IDisposable
{
    private class DataSavingInfo
    {
        public RhoDataInfo DataInfo = new RhoDataInfo();

        public RhoFile? File;

        public IDataSource DataSource;

        public byte[] Data;
    }

    private int _layerVersion;

    private FileStream? _rhoStream;

    private Dictionary<uint, RhoDataInfo> _dataInfoMap;

    private RhoFolder _rootFolder;

    private Dictionary<uint, RhoFileHandler> _fileHandlers;

    private uint _rhoKey;

    private uint _dataChecksum;

    private bool _disposed;

    private bool _closed;

    private bool _locked;

    internal readonly string[] _rhLayerIdentifiers = new string[2] { "Rh layer spec 1.0", "Rh layer spec 1.1" };

    internal const string _rhLayerSecondText = "KartRider (veblush & dew)";

    public RhoFolder RootFolder => _rootFolder;

    public bool IsClosed => _closed;

    public bool IsLocked => _locked;

    public RhoArchive()
    {
        _rootFolder = new RhoFolder();
        _fileHandlers = new Dictionary<uint, RhoFileHandler>();
        _dataInfoMap = new Dictionary<uint, RhoDataInfo>();
        _closed = true;
    }

    public void Open(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("");
        }

        if (!_closed)
        {
            throw new Exception("This RhoArchive instance has opened the other rho file. You should close the opened file to do this operation.");
        }

        _rhoStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        if (_rhoStream.Length < 128)
        {
            throw new InvalidOperationException();
        }

        _rhoKey = RhoKey.GetRhoKey(Path.GetFileNameWithoutExtension(filePath));
        BinaryReader binaryReader = new BinaryReader(_rhoStream);
        _rhoStream.Seek(0L, SeekOrigin.Begin);
        byte[] bytes = binaryReader.ReadBytes(64);
        string @string = Encoding.Unicode.GetString(bytes, 0, _rhLayerIdentifiers[1].Length << 1);
        int num = -1;
        for (int i = 0; i < _rhLayerIdentifiers.Length; i++)
        {
            if (@string == _rhLayerIdentifiers[i])
            {
                num = i;
                break;
            }
        }

        if (num < 0)
        {
            throw new Exception();
        }

        _layerVersion = num;
        _rhoStream.Seek(128L, SeekOrigin.Begin);
        byte[] array = binaryReader.ReadBytes(128);
        if (_layerVersion == 0)
        {
            array = RhoEncrypt.DecryptData(_rhoKey, array);
        }
        else if (_layerVersion == 1)
        {
            array = RhoEncrypt.DecryptHeaderInfo(array, _rhoKey);
        }

        int num2 = 0;
        byte[] key = new byte[0];
        uint num3 = 0u;
        using (MemoryStream input = new MemoryStream(array))
        {
            BinaryReader binaryReader2 = new BinaryReader(input);
            uint num4 = binaryReader2.ReadUInt32();
            uint num5 = IO.Adler.Adler32(0u, array, 4, 124);
            if (num4 != num5)
            {
                throw new Exception("rho file modified.");
            }

            binaryReader2.ReadInt32();
            num2 = binaryReader2.ReadInt32();
            num3 = binaryReader2.ReadUInt32() ^ _rhoKey;
            if (_layerVersion == 0)
            {
                key = binaryReader2.ReadBytes(32);
            }
            else if (_layerVersion == 1)
            {
                binaryReader2.ReadInt32();
                binaryReader2.ReadInt32();
                _dataChecksum = binaryReader2.ReadUInt32();
            }

            uint num6 = binaryReader2.ReadUInt32();
            binaryReader2.ReadInt32();
            if (num6 != 4229928824u)
            {
                throw new Exception("invalid archiveInfo end magic code.");
            }
        }

        _dataInfoMap.EnsureCapacity(num2);
        _fileHandlers.EnsureCapacity(num2);
        for (int j = 0; j < num2; j++)
        {
            if (_layerVersion == 0)
            {
                RhoDataInfo rhoDataInfo = binaryReader.ReadBlockInfo10(key);
                _dataInfoMap.Add(rhoDataInfo.Index, rhoDataInfo);
            }
            else if (_layerVersion == 1)
            {
                RhoDataInfo rhoDataInfo2 = binaryReader.ReadBlockInfo(num3);
                _dataInfoMap.Add(rhoDataInfo2.Index, rhoDataInfo2);
                num3++;
            }
        }

        uint directoryDataKey = RhoKey.GetDirectoryDataKey(_rhoKey);
        Queue<(uint, RhoFolder)> queue = new Queue<(uint, RhoFolder)>();
        queue.Enqueue((uint.MaxValue, _rootFolder));
        while (queue.Count > 0)
        {
            (uint, RhoFolder) tuple = queue.Dequeue();
            using MemoryStream input2 = new MemoryStream(getData(tuple.Item1, directoryDataKey));
            BinaryReader binaryReader3 = new BinaryReader(input2);
            int num7 = binaryReader3.ReadInt32();
            for (int k = 0; k < num7; k++)
            {
                RhoFolder rhoFolder = new RhoFolder();
                string name = binaryReader3.ReadNullTerminatedText(wideString: true);
                uint item = binaryReader3.ReadUInt32();
                rhoFolder.Name = name;
                queue.Enqueue((item, rhoFolder));
                tuple.Item2.AddFolder(rhoFolder);
            }

            int num8 = binaryReader3.ReadInt32();
            for (int l = 0; l < num8; l++)
            {
                RhoFile rhoFile = new RhoFile();
                string text = binaryReader3.ReadNullTerminatedText(wideString: true);
                uint num9 = binaryReader3.ReadUInt32();
                int num10 = binaryReader3.ReadInt32();
                uint num11 = binaryReader3.ReadUInt32();
                int size = binaryReader3.ReadInt32();
                uint fileKey = RhoKey.GetFileKey(_rhoKey, text, num9);
                string text2 = Encoding.ASCII.GetString(BitConverter.GetBytes(num9)).TrimEnd('\0');
                RhoFileHandler rhoFileHandler = new RhoFileHandler(this, (RhoFileProperty)num10, num11, size, fileKey);
                RhoDataSource dataSource = new RhoDataSource(rhoFileHandler);
                rhoFile.DataSource = dataSource;
                rhoFile.Name = text + "." + text2;
                rhoFile.FileEncryptionProperty = (RhoFileProperty)num10;
                _fileHandlers.Add(num11, rhoFileHandler);
                tuple.Item2.AddFile(rhoFile);
            }
        }

        _closed = false;
    }

    public void Save()
    {
        if (_closed)
        {
            throw new InvalidOperationException("Save operation only available if this RhoArchive instance is open from Rho file.");
        }
    }

    public void SaveTo(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);
        string text = Path.GetDirectoryName(fullPath) ?? "";
        if (!Directory.Exists(text))
        {
            throw new Exception("directory not exists.");
        }

        if (_rhoStream != null)
        {
            string fullPath2 = Path.GetFullPath(_rhoStream.Name);
            if (fullPath2 == text)
            {
                System.IO.File.Copy(fullPath2, fullPath2 + ".bak");
            }
        }

        uint rhoKey = RhoKey.GetRhoKey(Path.GetFileNameWithoutExtension(fullPath));
        Queue<DataSavingInfo> queue = new Queue<DataSavingInfo>();
        HashSet<uint> usedIndex = new HashSet<uint>();
        int dataOffset = 0;
        storeFolderAndFiles(RootFolder, queue, usedIndex, ref dataOffset, rhoKey);
        if (_rhoStream != null)
        {
            _rhoStream.Close();
            releaseAllHandlers();
        }

        uint num = 0u;
        foreach (DataSavingInfo item in queue)
        {
            num = IO.Adler.Adler32Combine(num, item.Data, 0, item.Data.Length);
        }

        if (_dataInfoMap != null)
        {
            _dataInfoMap.Clear();
        }
        else
        {
            _dataInfoMap = new Dictionary<uint, RhoDataInfo>(queue.Count);
        }

        if (_fileHandlers == null)
        {
            _fileHandlers = new Dictionary<uint, RhoFileHandler>(queue.Count);
        }

        FileStream fileStream = new FileStream(fullPath, FileMode.Create);
        int num2 = (queue.Count * 32 + 255) & 0x7FFFFF00;
        int num3 = 256 + num2;
        dataOffset += num3;
        BinaryWriter binaryWriter = new BinaryWriter(fileStream);
        binaryWriter.Write(Encoding.Unicode.GetBytes(_rhLayerIdentifiers[_layerVersion]));
        fileStream.Seek(64L, SeekOrigin.Begin);
        binaryWriter.Write(Encoding.Unicode.GetBytes("KartRider (veblush & dew)"));
        fileStream.Seek(128L, SeekOrigin.Begin);
        byte[] array = new byte[128];
        byte[] array2 = new byte[32];
        using (MemoryStream memoryStream = new MemoryStream(124))
        {
            BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream);
            binaryWriter2.Write(_layerVersion | 0x10000);
            binaryWriter2.Write(queue.Count);
            binaryWriter2.Write(982651820u);
            if (_layerVersion == 0)
            {
                binaryWriter2.Write(array2);
            }
            else if (_layerVersion == 1)
            {
                binaryWriter2.Write(1);
                binaryWriter2.Write(rhoKey - 964575427);
                binaryWriter2.Write(num);
                binaryWriter2.Write(4229928824u);
                binaryWriter2.Write(126);
            }

            memoryStream.Seek(0L, SeekOrigin.Begin);
            memoryStream.Read(array, 4, (int)memoryStream.Length);
        }

        Array.Copy(BitConverter.GetBytes(IO.Adler.Adler32(0u, array, 4, 124)), 0, array, 0, 4);
        if (_layerVersion == 0)
        {
            RhoEncrypt.EncryptData(rhoKey, array, 0, array.Length);
        }
        else if (_layerVersion == 1)
        {
            array = RhoEncrypt.EncryptHeaderInfo(array, rhoKey);
        }

        binaryWriter.Write(array);
        uint num4 = 0x3A9213ACu ^ rhoKey;
        fileStream.Seek(256L, SeekOrigin.Begin);
        foreach (DataSavingInfo item2 in queue)
        {
            byte[] array3 = new byte[32];
            using (MemoryStream memoryStream2 = new MemoryStream(32))
            {
                BinaryWriter binaryWriter3 = new BinaryWriter(memoryStream2);
                binaryWriter3.Write(item2.DataInfo.Index);
                binaryWriter3.Write((int)(item2.DataInfo.Offset + num3 >> 8));
                binaryWriter3.Write(item2.DataInfo.DataSize);
                binaryWriter3.Write(item2.DataInfo.UncompressedSize);
                binaryWriter3.Write((int)item2.DataInfo.BlockProperty);
                binaryWriter3.Write(item2.DataInfo.Checksum);
                memoryStream2.Seek(0L, SeekOrigin.Begin);
                memoryStream2.Read(array3, 0, array3.Length);
            }

            RhoDataInfo rhoDataInfo = new RhoDataInfo();
            rhoDataInfo.Index = item2.DataInfo.Index;
            rhoDataInfo.Offset = item2.DataInfo.Offset + num3;
            rhoDataInfo.DataSize = item2.DataInfo.DataSize;
            rhoDataInfo.UncompressedSize = item2.DataInfo.UncompressedSize;
            rhoDataInfo.BlockProperty = item2.DataInfo.BlockProperty;
            rhoDataInfo.Checksum = item2.DataInfo.Checksum;
            _dataInfoMap.Add(rhoDataInfo.Index, rhoDataInfo);
            if (_layerVersion == 0)
            {
                array3 = RhoEncrypt.EncryptBlockInfoOld(array3, array2);
            }
            else if (_layerVersion == 1)
            {
                array3 = RhoEncrypt.EncryptHeaderInfo(array3, num4++);
            }

            RhoEncrypt.DecryptHeaderInfo(array3, num4 - 1);
            binaryWriter.Write(array3);
        }

        while (queue.Count > 0)
        {
            DataSavingInfo dataSavingInfo = queue.Dequeue();
            fileStream.Seek(dataSavingInfo.DataInfo.Offset + num3, SeekOrigin.Begin);
            fileStream.Write(dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
            if (dataSavingInfo.File != null)
            {
                RhoFile file = dataSavingInfo.File;
                RhoFileHandler rhoFileHandler = new RhoFileHandler(this, file.FileEncryptionProperty, dataSavingInfo.DataInfo.Index, file.Size, RhoKey.GetFileKey(rhoKey, file.NameWithoutExt, file.getExtNum()));
                if (file.DataSource != null)
                {
                    file.DataSource.Dispose();
                }

                _fileHandlers.Add(dataSavingInfo.DataInfo.Index, rhoFileHandler);
                file.DataSource = new RhoDataSource(rhoFileHandler);
            }
        }

        if (fileStream.Position != dataOffset)
        {
            fileStream.Seek(dataOffset - 1, SeekOrigin.Begin);
            fileStream.WriteByte(0);
        }

        fileStream.Close();
        _rhoStream = new FileStream(fullPath, FileMode.Open);
        Queue<RhoFolder> queue2 = new Queue<RhoFolder>();
        queue2.Enqueue(RootFolder);
        while (queue2.Count > 0)
        {
            RhoFolder rhoFolder = queue2.Dequeue();
            rhoFolder.appliedChanges();
            foreach (RhoFolder folder in rhoFolder.Folders)
            {
                queue2.Enqueue(folder);
            }
        }
    }

    public void Close()
    {
        if (_closed)
        {
            throw new Exception("This archive is close or is not open from a file.");
        }

        if (_rhoStream != null && _rhoStream.CanRead)
        {
            _rhoStream.Close();
        }

        _rhoStream?.Dispose();
        _fileHandlers.Clear();
        _rootFolder.Clear();
    }

    public void Dispose()
    {
        if (!_closed)
        {
            Close();
        }

        releaseAllHandlers();
    }

    internal Stream? getRhoStream()
    {
        return _rhoStream;
    }

    internal byte[] getData(RhoFileHandler handler)
    {
        if (!_dataInfoMap.ContainsKey(handler._fileDataIndex))
        {
            throw new Exception("handler corrupted.");
        }

        return getData(handler._fileDataIndex, handler._key);
    }

    private byte[] getData(uint dataIndex, uint key)
    {
        //IL_0080: Unknown result type (might be due to invalid IL or missing references)
        //IL_008f: Expected O, but got Unknown
        if (!_dataInfoMap.ContainsKey(dataIndex))
        {
            throw new Exception("index not exist.");
        }

        FileStream fileStream = new FileStream(_rhoStream.SafeFileHandle, FileAccess.Read);
        RhoDataInfo rhoDataInfo = _dataInfoMap[dataIndex];
        fileStream.Seek(rhoDataInfo.Offset, SeekOrigin.Begin);
        byte[] array = new byte[rhoDataInfo.DataSize];
        fileStream.Read(array, 0, rhoDataInfo.DataSize);
        if ((rhoDataInfo.BlockProperty & RhoBlockProperty.Compressed) != 0)
        {
            using MemoryStream memoryStream = new MemoryStream(array);
            array = new byte[rhoDataInfo.UncompressedSize];
            ((Stream)new ZlibStream((Stream)memoryStream, (CompressionMode)1)).Read(array, 0, array.Length);
        }

        if ((rhoDataInfo.BlockProperty & RhoBlockProperty.PartialEncrypted) != 0)
        {
            RhoEncrypt.DecryptData(key, array, 0, array.Length);
        }

        if (rhoDataInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
        {
            RhoDataInfo rhoDataInfo2 = (_dataInfoMap.ContainsKey(dataIndex + 1) ? _dataInfoMap[dataIndex + 1] : null);
            if (rhoDataInfo2 != null)
            {
                Array.Resize(ref array, array.Length + rhoDataInfo2.DataSize);
                fileStream.Read(array, rhoDataInfo.DataSize, rhoDataInfo2.DataSize);
            }
        }

        return array;
    }

    private void storeFolderAndFiles(RhoFolder folder, Queue<DataSavingInfo> savingInfo, HashSet<uint> usedIndex, ref int dataOffset, uint outRhoKey)
    {
        //IL_01cf: Unknown result type (might be due to invalid IL or missing references)
        //IL_01d4: Unknown result type (might be due to invalid IL or missing references)
        //IL_01e1: Expected O, but got Unknown
        //IL_01e1: Unknown result type (might be due to invalid IL or missing references)
        //IL_01e7: Expected O, but got Unknown
        //IL_01ec: Expected O, but got Unknown
        if (folder.Name == "" && folder.Parent != null)
        {
            throw new Exception("folder name couldn't be empty.");
        }

        uint num;
        for (num = folder.getFolderDataIndex(); usedIndex.Contains(num); num += 1594090343)
        {
        }

        Queue<DataSavingInfo> queue = new Queue<DataSavingInfo>();
        byte[] array2;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            IReadOnlyCollection<RhoFolder> folders = folder.Folders;
            IReadOnlyCollection<RhoFile> files = folder.Files;
            binaryWriter.Write(folders.Count);
            foreach (RhoFolder item in folders)
            {
                uint folderDataIndex = item.getFolderDataIndex();
                binaryWriter.WriteNullTerminatedText(item.Name, wideString: true);
                binaryWriter.Write(folderDataIndex);
            }

            binaryWriter.Write(files.Count);
            foreach (RhoFile item2 in files)
            {
                if (item2.DataSource == null)
                {
                    throw new Exception("data source is null.");
                }

                uint extNum = item2.getExtNum();
                uint fileKey = RhoKey.GetFileKey(outRhoKey, item2.NameWithoutExt, extNum);
                int size = item2.Size;
                uint num2 = item2.getDataIndex(num);
                byte[] array = item2.DataSource.GetBytes();
                uint checksum = 0u;
                for (; usedIndex.Contains(num2) || usedIndex.Contains(num2 + 1); num2 += 1294060367)
                {
                }

                if (item2.FileEncryptionProperty == RhoFileProperty.Encrypted || item2.FileEncryptionProperty == RhoFileProperty.CompressedEncrypted)
                {
                    checksum = IO.Adler.Adler32(0u, array, 0, array.Length);
                    RhoEncrypt.EncryptData(fileKey, array, 0, array.Length);
                }
                else if (item2.FileEncryptionProperty == RhoFileProperty.PartialEncrypted)
                {
                    RhoEncrypt.EncryptData(fileKey, array, 0, Math.Min(256, array.Length));
                }

                if (item2.FileEncryptionProperty == RhoFileProperty.CompressedEncrypted || item2.FileEncryptionProperty == RhoFileProperty.Compressed)
                {
                    using MemoryStream memoryStream2 = new MemoryStream();
                    ZlibStream val = new ZlibStream((Stream)memoryStream2, (CompressionMode)0, (CompressionLevel)9, true);
                    ((Stream)val).Write(array, 0, array.Length);
                    ((Stream)val).Flush();
                    ((Stream)val).Close();
                    array = memoryStream2.ToArray();
                }

                binaryWriter.WriteNullTerminatedText(item2.NameWithoutExt, wideString: true);
                binaryWriter.Write(extNum);
                binaryWriter.Write((int)item2.FileEncryptionProperty);
                binaryWriter.Write(num2);
                binaryWriter.Write(size);
                DataSavingInfo dataSavingInfo = new DataSavingInfo();
                dataSavingInfo.File = item2;
                if (item2.FileEncryptionProperty == RhoFileProperty.PartialEncrypted)
                {
                    dataSavingInfo.Data = new byte[Math.Min(256, array.Length)];
                    dataSavingInfo.DataInfo.Index = num2;
                    dataSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.PartialEncrypted;
                    dataSavingInfo.DataInfo.DataSize = dataSavingInfo.Data.Length;
                    dataSavingInfo.DataInfo.UncompressedSize = dataSavingInfo.Data.Length;
                    dataSavingInfo.DataInfo.Checksum = 0u;
                    Array.Copy(array, 0, dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
                    usedIndex.Add(num2);
                    queue.Enqueue(dataSavingInfo);
                    if (array.Length > 256)
                    {
                        DataSavingInfo dataSavingInfo2 = new DataSavingInfo();
                        dataSavingInfo2.Data = new byte[array.Length - 256];
                        dataSavingInfo2.DataInfo.Index = num2 + 1;
                        dataSavingInfo2.DataInfo.BlockProperty = RhoBlockProperty.None;
                        dataSavingInfo2.DataInfo.DataSize = dataSavingInfo2.Data.Length;
                        dataSavingInfo2.DataInfo.UncompressedSize = dataSavingInfo2.Data.Length;
                        dataSavingInfo2.DataInfo.Checksum = 0u;
                        Array.Copy(array, 256, dataSavingInfo2.Data, 0, dataSavingInfo2.Data.Length);
                        usedIndex.Add(num2 + 1);
                        queue.Enqueue(dataSavingInfo2);
                    }
                }
                else
                {
                    dataSavingInfo.Data = array;
                    dataSavingInfo.DataInfo.Index = num2;
                    dataSavingInfo.DataInfo.Checksum = checksum;
                    dataSavingInfo.DataInfo.DataSize = array.Length;
                    dataSavingInfo.DataInfo.UncompressedSize = size;
                    switch (item2.FileEncryptionProperty)
                    {
                        case RhoFileProperty.None:
                            dataSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.None;
                            break;
                        case RhoFileProperty.Encrypted:
                            dataSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.FullEncrypted;
                            break;
                        case RhoFileProperty.Compressed:
                            dataSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.Compressed;
                            break;
                        case RhoFileProperty.CompressedEncrypted:
                            dataSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.CompressedEncrypted;
                            break;
                    }

                    usedIndex.Add(num2);
                    queue.Enqueue(dataSavingInfo);
                }
            }

            array2 = memoryStream.ToArray();
        }

        uint checksum2 = IO.Adler.Adler32(0u, array2, 0, array2.Length);
        RhoEncrypt.EncryptData(RhoKey.GetDirectoryDataKey(outRhoKey), array2, 0, array2.Length);
        DataSavingInfo dataSavingInfo3 = new DataSavingInfo();
        dataSavingInfo3.Data = array2;
        dataSavingInfo3.DataInfo.Offset = dataOffset;
        dataSavingInfo3.DataInfo.Index = num;
        dataSavingInfo3.DataInfo.Checksum = checksum2;
        dataSavingInfo3.DataInfo.DataSize = array2.Length;
        dataSavingInfo3.DataInfo.UncompressedSize = array2.Length;
        dataSavingInfo3.DataInfo.BlockProperty = RhoBlockProperty.FullEncrypted;
        usedIndex.Add(num);
        savingInfo.Enqueue(dataSavingInfo3);
        dataOffset = (dataOffset + dataSavingInfo3.DataInfo.DataSize + 255) & 0x7FFFFF00;
        foreach (RhoFolder folder2 in folder.Folders)
        {
            storeFolderAndFiles(folder2, savingInfo, usedIndex, ref dataOffset, outRhoKey);
        }

        while (queue.Count > 0)
        {
            DataSavingInfo dataSavingInfo4 = queue.Dequeue();
            dataSavingInfo4.DataInfo.Offset = dataOffset;
            savingInfo.Enqueue(dataSavingInfo4);
            dataOffset = (dataOffset + dataSavingInfo4.DataInfo.DataSize + 255) & 0x7FFFFF00;
        }
    }

    private void releaseAllHandlers()
    {
        foreach (RhoFileHandler value in _fileHandlers.Values)
        {
            value.releaseHandler();
        }

        _fileHandlers.Clear();
    }
}