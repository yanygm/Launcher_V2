using KartLibrary.Encrypt;
using KartLibrary.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;

namespace KartLibrary.File
{
    /// <summary lang="en-us">
    /// <see cref="RhoFile"/> represents a Rho type archive. You can open and save Rho file with this class.
    /// </summary>
    /// <summary lang="zh-tw">
    /// <see cref="RhoFile"/>用來表示一個Rho檔案。你能藉此類型來開啟及儲存Rho類型檔案.
    /// </summary>
    public partial class RhoArchive : IRhoArchive<RhoFolder, RhoFile>
    {
        #region Members
        private int _layerVersion = 1; // 1.0 = 0, 1.1 = 1
        private FileStream? _rhoStream;

        private Dictionary<uint, RhoDataInfo> _dataInfoMap;
        private RhoFolder _rootFolder;

        private Dictionary<uint, RhoFileHandler> _fileHandlers;

        private uint _rhoKey;

        private uint _dataChecksum;

        private bool _disposed;
        private bool _closed;
        private bool _locked; // if this instance is locked, calling any methods of this instance is not allowed.
        #endregion

        #region Properties
        /// <summary>
        /// Root folder of current <see cref="RhoArchive"/>
        /// </summary>
        public RhoFolder RootFolder => _rootFolder;
        public bool IsClosed => _closed;

        /// <summary>
        /// If this instance is locked, 
        /// calling any method of this instance is not allowed until the asynchronous method that sets lock is done.
        /// </summary>
        public bool IsLocked => _locked;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new instance of <see cref="RhoArchive"/>. 
        /// </summary>
        public RhoArchive()
        {
            _rootFolder = new RhoFolder();
            _fileHandlers = new Dictionary<uint, RhoFileHandler>();
            _dataInfoMap = new Dictionary<uint, RhoDataInfo>();
            _closed = true;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Opens Rho file.
        /// </summary>
        /// <param name="filePath">The file path of Rho file.</param>
        /// <exception cref="FileNotFoundException"> It will be thrown if required file can't be found. </exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public void Open(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"");
            if (!_closed)
                throw new Exception("This RhoArchive instance has opened the other rho file. You should close the opened file to do this operation.");
            _rhoStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            if (_rhoStream.Length < 0x80)
                throw new InvalidOperationException();

            _rhoKey = RhoKey.GetRhoKey(Path.GetFileNameWithoutExtension(filePath));

            BinaryReader reader = new BinaryReader(_rhoStream);

            // Checks identifier
            _rhoStream.Seek(0x0, SeekOrigin.Begin);
            byte[] identifierData = reader.ReadBytes(0x40);
            string converftedStr = Encoding.Unicode.GetString(identifierData, 0, _rhLayerIdentifiers[1].Length << 1);
            int layerVersion = -1;
            for (int i = 0; i < _rhLayerIdentifiers.Length; i++)
                if (converftedStr == _rhLayerIdentifiers[i])
                {
                    layerVersion = i;
                    break;
                }
            if (layerVersion < 0)
                throw new Exception();
            else
                _layerVersion = layerVersion;


            // Read rho archive info
            _rhoStream.Seek(0x80, SeekOrigin.Begin);
            byte[] rhoArchiveInfoData = reader.ReadBytes(0x80);
            if (_layerVersion == 0)
            {
                rhoArchiveInfoData = RhoEncrypt.DecryptData(_rhoKey, rhoArchiveInfoData);
            }
            else if (_layerVersion == 1)
            {
                rhoArchiveInfoData = RhoEncrypt.DecryptHeaderInfo(rhoArchiveInfoData, _rhoKey);
            }

            int dataInfoCount = 0;
            byte[] dataInfoKey10 = new byte[0];
            uint dataInfoKey11 = 0;

            // Decode rho archive info
            using (MemoryStream memStream = new MemoryStream(rhoArchiveInfoData))
            {
                BinaryReader memReader = new BinaryReader(memStream);
                uint infoDataChksum = memReader.ReadUInt32();
                uint verifyChkSum = Adler.Adler32(0, rhoArchiveInfoData, 4, 0x7C);
                if (infoDataChksum != verifyChkSum)
                    throw new Exception("rho file modified.");
                int versionChkCode = memReader.ReadInt32();
                dataInfoCount = memReader.ReadInt32();
                uint dataInfoWhiteningKey = memReader.ReadUInt32();
                dataInfoKey11 = dataInfoWhiteningKey ^ _rhoKey;
                if (_layerVersion == 0)
                {
                    dataInfoKey10 = memReader.ReadBytes(0x20);
                }
                else if (_layerVersion == 1)
                {
                    int u1 = memReader.ReadInt32();
                    int u2 = memReader.ReadInt32();
                    _dataChecksum = memReader.ReadUInt32();
                }
                uint endMagicCode = memReader.ReadUInt32();
                int u4 = memReader.ReadInt32();
                if (endMagicCode != 0xfc1f9778u)
                    throw new Exception("invalid archiveInfo end magic code.");
            }

            // Read data information collection.
            _dataInfoMap.EnsureCapacity(dataInfoCount);
            _fileHandlers.EnsureCapacity(dataInfoCount);
            for (int i = 0; i < dataInfoCount; i++)
            {
                if (_layerVersion == 0)
                {
                    RhoDataInfo dataInfo = reader.ReadBlockInfo10(dataInfoKey10);
                    _dataInfoMap.Add(dataInfo.Index, dataInfo);
                }
                else if (_layerVersion == 1)
                {
                    RhoDataInfo dataInfo = reader.ReadBlockInfo(dataInfoKey11);
                    _dataInfoMap.Add(dataInfo.Index, dataInfo);
                    dataInfoKey11++;
                }
            }

            // Read all folders and all files info.
            uint folderKey = RhoKey.GetDirectoryDataKey(_rhoKey);

            Queue<(uint folderDataIndex, RhoFolder folder)> procssQueue = new Queue<(uint folderDataIndex, RhoFolder folder)>();
            procssQueue.Enqueue((0xFFFFFFFF, _rootFolder));

            while (procssQueue.Count > 0)
            {
                var queObj = procssQueue.Dequeue();
                byte[] folderData = getData(queObj.folderDataIndex, folderKey);
                using (MemoryStream memStream = new MemoryStream(folderData))
                {
                    BinaryReader memReader = new BinaryReader(memStream);
                    int folderCount = memReader.ReadInt32();
                    for (int i = 0; i < folderCount; i++)
                    {
                        RhoFolder subFolder = new RhoFolder();
                        string name = memReader.ReadNullTerminatedText(true);
                        uint folderDataIndex = memReader.ReadUInt32();
                        subFolder.Name = name;
                        procssQueue.Enqueue((folderDataIndex, subFolder));
                        queObj.folder.AddFolder(subFolder);
                    }
                    int fileCount = memReader.ReadInt32();
                    for (int i = 0; i < fileCount; i++)
                    {
                        RhoFile subFile = new RhoFile();
                        string fileName = memReader.ReadNullTerminatedText(true);
                        uint extInt = memReader.ReadUInt32();
                        int fileProperty = memReader.ReadInt32();
                        uint dataIndex = memReader.ReadUInt32();
                        int fileSize = memReader.ReadInt32();
                        uint fileKey = RhoKey.GetFileKey(_rhoKey, fileName, extInt);
                        string fileExtension = Encoding.ASCII.GetString(BitConverter.GetBytes(extInt)).TrimEnd('\0');

                        RhoFileHandler fileHandler = new RhoFileHandler(this, (RhoFileProperty)fileProperty, dataIndex, fileSize, fileKey);
                        RhoDataSource bufferedDataSource = new RhoDataSource(fileHandler);
                        subFile.DataSource = bufferedDataSource;
                        subFile.Name = $"{fileName}.{fileExtension}";
                        subFile.FileEncryptionProperty = (RhoFileProperty)fileProperty;

                        _fileHandlers.Add(dataIndex, fileHandler);
                        queObj.folder.AddFile(subFile);
                    }
                }
            }

            _closed = false;
        }

        public void Save()
        {
            if (_closed)
                throw new InvalidOperationException("Save operation only available if this RhoArchive instance is open from Rho file.");
        }
        /// <summary>
        /// Save current <see cref="RhoArchive"/> instance to Rho file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="Exception"></exception>
        public void SaveTo(string filePath)
        {
            const uint dataInfoWhiteningKey = 0x3a9213ac;
            string fullName = Path.GetFullPath(filePath);
            string fullDirName = Path.GetDirectoryName(fullName) ?? "";
            if (!Directory.Exists(fullDirName))
            {
                throw new Exception("directory not exists.");
            }
            if (_rhoStream is not null)
            {
                string curRhoFullName = Path.GetFullPath(_rhoStream.Name);
                if (curRhoFullName == fullDirName)
                    System.IO.File.Copy(curRhoFullName, $"{curRhoFullName}.bak");
            }
            string outFileName = Path.GetFileNameWithoutExtension(fullName);
            uint outRhoKey = RhoKey.GetRhoKey(outFileName);

            Queue<DataSavingInfo> dataSavingQueue = new Queue<DataSavingInfo>();
            HashSet<uint> usedIndex = new HashSet<uint>();
            int dataEndOffset = 0;
            storeFolderAndFiles(RootFolder, dataSavingQueue, usedIndex, ref dataEndOffset, outRhoKey);
            if (_rhoStream is not null)
            {
                _rhoStream.Close();
                releaseAllHandlers();
            }
            uint outDataHash = 0;
            foreach (DataSavingInfo dataSavingInfo in dataSavingQueue)
                outDataHash = Adler.Adler32Combine(outDataHash, dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
            if (_dataInfoMap is not null)
                _dataInfoMap.Clear();
            else
                _dataInfoMap = new Dictionary<uint, RhoDataInfo>(dataSavingQueue.Count);
            if (_fileHandlers is null)
                _fileHandlers = new Dictionary<uint, RhoFileHandler>(dataSavingQueue.Count);
            // Begin write to out file.
            FileStream outFileStream = new FileStream(fullName, FileMode.Create);
            int dataInfoSize = (((dataSavingQueue.Count) * 0x20) + 0xFF) & (0x7FFFFF00);
            int dataBeginOffset = 0x100 + dataInfoSize;
            dataEndOffset += dataBeginOffset;

            // Write Identifier Text
            BinaryWriter outWriter = new BinaryWriter(outFileStream);
            outWriter.Write(Encoding.Unicode.GetBytes(_rhLayerIdentifiers[_layerVersion]));
            outFileStream.Seek(0x40, SeekOrigin.Begin);
            outWriter.Write(Encoding.Unicode.GetBytes(_rhLayerSecondText));

            // Write Header
            outFileStream.Seek(0x80, SeekOrigin.Begin);
            byte[] rhoHeaderData = new byte[0x80]; //Without header checksum
            byte[] dataInfoKey10 = new byte[0x20];
            using (MemoryStream memStream = new MemoryStream(0x7C))
            {
                BinaryWriter memWriter = new BinaryWriter(memStream);
                memWriter.Write(_layerVersion | 0x10000);
                memWriter.Write(dataSavingQueue.Count);
                memWriter.Write(dataInfoWhiteningKey);
                if (_layerVersion == 0)
                {
                    memWriter.Write(dataInfoKey10);
                }
                else if (_layerVersion == 1)
                {
                    memWriter.Write((int)1);
                    memWriter.Write(outRhoKey - 0x397E40C3);
                    memWriter.Write(outDataHash);
                    memWriter.Write(0xFC1F9778);
                    memWriter.Write((int)0x7E);
                }
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.Read(rhoHeaderData, 4, (int)memStream.Length);
            }
            uint rhoHeaderChksum = Adler.Adler32(0, rhoHeaderData, 4, 0x7C);
            Array.Copy(BitConverter.GetBytes(rhoHeaderChksum), 0, rhoHeaderData, 0, 0x04);
            if (_layerVersion == 0)
                RhoEncrypt.EncryptData(outRhoKey, rhoHeaderData, 0, rhoHeaderData.Length);
            else if (_layerVersion == 1)
                rhoHeaderData = RhoEncrypt.EncryptHeaderInfo(rhoHeaderData, outRhoKey);
            outWriter.Write(rhoHeaderData);

            // Write Data Info
            uint dataInfoKey11 = dataInfoWhiteningKey ^ outRhoKey;

            outFileStream.Seek(0x100, SeekOrigin.Begin);
            foreach (DataSavingInfo dataSavingInfo in dataSavingQueue)
            {
                byte[] dataInfoEncData = new byte[0x20];
                using (MemoryStream memStream = new MemoryStream(0x20))
                {
                    BinaryWriter memWriter = new BinaryWriter(memStream);
                    memWriter.Write(dataSavingInfo.DataInfo.Index);
                    memWriter.Write((int)((dataSavingInfo.DataInfo.Offset + dataBeginOffset) >> 8));
                    memWriter.Write(dataSavingInfo.DataInfo.DataSize);
                    memWriter.Write(dataSavingInfo.DataInfo.UncompressedSize);
                    memWriter.Write((int)dataSavingInfo.DataInfo.BlockProperty);
                    memWriter.Write(dataSavingInfo.DataInfo.Checksum);

                    memStream.Seek(0, SeekOrigin.Begin);
                    memStream.Read(dataInfoEncData, 0, dataInfoEncData.Length);
                }
                RhoDataInfo rhoDataInfo = new RhoDataInfo();
                rhoDataInfo.Index = dataSavingInfo.DataInfo.Index;
                rhoDataInfo.Offset = dataSavingInfo.DataInfo.Offset + dataBeginOffset;
                rhoDataInfo.DataSize = dataSavingInfo.DataInfo.DataSize;
                rhoDataInfo.UncompressedSize = dataSavingInfo.DataInfo.UncompressedSize;
                rhoDataInfo.BlockProperty = dataSavingInfo.DataInfo.BlockProperty;
                rhoDataInfo.Checksum = dataSavingInfo.DataInfo.Checksum;
                _dataInfoMap.Add(rhoDataInfo.Index, rhoDataInfo);

                if (_layerVersion == 0)
                    dataInfoEncData = RhoEncrypt.EncryptBlockInfoOld(dataInfoEncData, dataInfoKey10);
                else if (_layerVersion == 1)
                    dataInfoEncData = RhoEncrypt.EncryptHeaderInfo(dataInfoEncData, dataInfoKey11++);
                byte[] dbgg = RhoEncrypt.DecryptHeaderInfo(dataInfoEncData, dataInfoKey11 - 1);
                outWriter.Write(dataInfoEncData);
            }

            // Write Data
            while (dataSavingQueue.Count > 0)
            {
                DataSavingInfo dataSavingInfo = dataSavingQueue.Dequeue();
                outFileStream.Seek(dataSavingInfo.DataInfo.Offset + dataBeginOffset, SeekOrigin.Begin);
                outFileStream.Write(dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
                if (dataSavingInfo.File is not null)
                {
                    RhoFile file = dataSavingInfo.File;
                    RhoFileHandler fileHandler = new RhoFileHandler(this, file.FileEncryptionProperty, dataSavingInfo.DataInfo.Index, file.Size, RhoKey.GetFileKey(outRhoKey, file.NameWithoutExt, file.getExtNum()));
                    if (file.DataSource is not null)
                        file.DataSource.Dispose();
                    _fileHandlers.Add(dataSavingInfo.DataInfo.Index, fileHandler);
                    file.DataSource = new RhoDataSource(fileHandler);
                }
            }
            if (outFileStream.Position != dataEndOffset)
            {
                outFileStream.Seek(dataEndOffset - 1, SeekOrigin.Begin);
                outFileStream.WriteByte(0x00);
            }
            outFileStream.Close();
            _rhoStream = new FileStream(fullName, FileMode.Open);

            // send applied changes event to RhoFolder instances
            Queue<RhoFolder> folderQueue = new Queue<RhoFolder>();
            folderQueue.Enqueue(RootFolder);
            while (folderQueue.Count > 0)
            {
                RhoFolder folder = folderQueue.Dequeue();
                folder.appliedChanges();
                foreach (RhoFolder subFolder in folder.Folders)
                    folderQueue.Enqueue(subFolder);
            }
        }

        public void Close()
        {
            if (_closed)
                throw new Exception("This archive is close or is not open from a file.");
            if (_rhoStream is not null && _rhoStream.CanRead)
                _rhoStream.Close();
            _rhoStream?.Dispose();
            _fileHandlers.Clear();
            _rootFolder.Clear();
        }

        public void Dispose()
        {
            if (!_closed)
                Close();
            releaseAllHandlers();
        }

        internal Stream? getRhoStream()
        {
            return _rhoStream;
        }

        internal byte[] getData(RhoFileHandler handler)
        {
            if (!_dataInfoMap.ContainsKey(handler._fileDataIndex))
                throw new Exception("handler corrupted.");
            return getData(handler._fileDataIndex, handler._key);
        }

        private byte[] getData(uint dataIndex, uint key)
        {
            if (!_dataInfoMap.ContainsKey(dataIndex))
                throw new Exception("index not exist.");
            FileStream clonedRhoStream = new FileStream(_rhoStream.SafeFileHandle, FileAccess.Read);
            RhoDataInfo dataInfo = _dataInfoMap[dataIndex];

            clonedRhoStream.Seek(dataInfo.Offset, SeekOrigin.Begin);
            byte[] outData = new byte[dataInfo.DataSize];
            clonedRhoStream.Read(outData, 0, dataInfo.DataSize);

            if ((dataInfo.BlockProperty & RhoBlockProperty.Compressed) != RhoBlockProperty.None)
            {
                using (MemoryStream memStream = new MemoryStream(outData))
                {
                    outData = new byte[dataInfo.UncompressedSize];
                    Ionic.Zlib.ZlibStream decompressStream = new Ionic.Zlib.ZlibStream(memStream, Ionic.Zlib.CompressionMode.Decompress);
                    int readed = decompressStream.Read(outData, 0, outData.Length);
                }
            }
            if ((dataInfo.BlockProperty & RhoBlockProperty.PartialEncrypted) != RhoBlockProperty.None)
            {
                RhoEncrypt.DecryptData(key, outData, 0, outData.Length);
            }
            if (dataInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
            {
                RhoDataInfo? secDatainfo = _dataInfoMap.ContainsKey(dataIndex + 1) ? _dataInfoMap[dataIndex + 1] : null;
                if (secDatainfo is not null)
                {
                    Array.Resize(ref outData, outData.Length + secDatainfo.DataSize);
                    clonedRhoStream.Read(outData, dataInfo.DataSize, secDatainfo.DataSize);
                }
            }
            return outData;
        }

        private void storeFolderAndFiles(RhoFolder folder, Queue<DataSavingInfo> savingInfo, HashSet<uint> usedIndex, ref int dataOffset, uint outRhoKey)
        {
            if (folder.Name == "" && folder.Parent is not null)
                throw new Exception("folder name couldn't be empty.");
            uint folderDataIndex = folder.getFolderDataIndex();
            while (usedIndex.Contains(folderDataIndex))
                folderDataIndex += 0x5F03E367;
            byte[] folderData;

            Queue<DataSavingInfo> fileSavingInfoQueue = new Queue<DataSavingInfo>();

            // Encode folder
            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryWriter memWriter = new BinaryWriter(memStream);
                IReadOnlyCollection<RhoFolder> subFolders = folder.Folders;
                IReadOnlyCollection<RhoFile> subFiles = folder.Files;
                memWriter.Write(subFolders.Count);
                foreach (RhoFolder subFolder in subFolders)
                {
                    uint subFolderDataIndex = subFolder.getFolderDataIndex();
                    memWriter.WriteNullTerminatedText(subFolder.Name, true);
                    memWriter.Write(subFolderDataIndex);
                }
                memWriter.Write(subFiles.Count);
                foreach (RhoFile subFile in subFiles)
                {
                    if (subFile.DataSource is null)
                        throw new Exception("data source is null.");

                    uint extNum = subFile.getExtNum();
                    uint fileKey = RhoKey.GetFileKey(outRhoKey, subFile.NameWithoutExt, extNum);
                    int fileSize = subFile.Size;
                    uint fileDataIndex = subFile.getDataIndex(folderDataIndex);
                    byte[] fileData = subFile.DataSource.GetBytes();
                    uint fileChksum = 0;

                    while (((usedIndex.Contains(fileDataIndex) || usedIndex.Contains(fileDataIndex + 1))))
                        fileDataIndex += 0x4D21CB4F;

                    if (subFile.FileEncryptionProperty == RhoFileProperty.Encrypted || subFile.FileEncryptionProperty == RhoFileProperty.CompressedEncrypted)
                    {
                        fileChksum = Adler.Adler32(0, fileData, 0, fileData.Length);
                        RhoEncrypt.EncryptData(fileKey, fileData, 0, fileData.Length);
                    }
                    else if (subFile.FileEncryptionProperty == RhoFileProperty.PartialEncrypted)
                    {
                        RhoEncrypt.EncryptData(fileKey, fileData, 0, Math.Min(0x100, fileData.Length));
                    }
                    if (subFile.FileEncryptionProperty == RhoFileProperty.CompressedEncrypted || subFile.FileEncryptionProperty == RhoFileProperty.Compressed)
                    {
                        using MemoryStream ms = new MemoryStream();
                        using (var compressStream = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression, true))
                        {
                            compressStream.Write(fileData, 0, fileData.Length);
                        }
                        fileData = ms.ToArray();
                    }

                    memWriter.WriteNullTerminatedText(subFile.NameWithoutExt, true);
                    memWriter.Write(extNum);
                    memWriter.Write((int)subFile.FileEncryptionProperty);
                    memWriter.Write(fileDataIndex);
                    memWriter.Write(fileSize);

                    DataSavingInfo fileSavingInfo = new DataSavingInfo();
                    fileSavingInfo.File = subFile;
                    if (subFile.FileEncryptionProperty == RhoFileProperty.PartialEncrypted)
                    {
                        fileSavingInfo.Data = new byte[Math.Min(0x100, fileData.Length)];
                        fileSavingInfo.DataInfo.Index = fileDataIndex;
                        fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.PartialEncrypted;
                        fileSavingInfo.DataInfo.DataSize = fileSavingInfo.Data.Length;
                        fileSavingInfo.DataInfo.UncompressedSize = fileSavingInfo.Data.Length;
                        fileSavingInfo.DataInfo.Checksum = 0;
                        Array.Copy(fileData, 0, fileSavingInfo.Data, 0, fileSavingInfo.Data.Length);
                        usedIndex.Add(fileDataIndex);
                        fileSavingInfoQueue.Enqueue(fileSavingInfo);
                        if (fileData.Length > 0x100)
                        {
                            DataSavingInfo secFileSavingInfo = new DataSavingInfo();
                            secFileSavingInfo.Data = new byte[fileData.Length - 0x100];
                            secFileSavingInfo.DataInfo.Index = fileDataIndex + 1;
                            secFileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.None;
                            secFileSavingInfo.DataInfo.DataSize = secFileSavingInfo.Data.Length;
                            secFileSavingInfo.DataInfo.UncompressedSize = secFileSavingInfo.Data.Length;
                            secFileSavingInfo.DataInfo.Checksum = 0;
                            Array.Copy(fileData, 0x100, secFileSavingInfo.Data, 0, secFileSavingInfo.Data.Length);
                            usedIndex.Add(fileDataIndex + 1);
                            fileSavingInfoQueue.Enqueue(secFileSavingInfo);
                        }
                    }
                    else
                    {
                        fileSavingInfo.Data = fileData;
                        fileSavingInfo.DataInfo.Index = fileDataIndex;
                        fileSavingInfo.DataInfo.Checksum = fileChksum;
                        fileSavingInfo.DataInfo.DataSize = fileData.Length;
                        fileSavingInfo.DataInfo.UncompressedSize = fileSize;
                        switch (subFile.FileEncryptionProperty)
                        {
                            case RhoFileProperty.None:
                                fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.None;
                                break;
                            case RhoFileProperty.Encrypted:
                                fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.FullEncrypted;
                                break;
                            case RhoFileProperty.Compressed:
                                fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.Compressed;
                                break;
                            case RhoFileProperty.CompressedEncrypted:
                                fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.CompressedEncrypted;
                                break;
                        }
                        usedIndex.Add(fileDataIndex);
                        fileSavingInfoQueue.Enqueue(fileSavingInfo);
                    }

                }
                folderData = memStream.ToArray();
            }

            uint folderDataDecChksum = Adler.Adler32(0, folderData, 0, folderData.Length);
            uint folderKey = RhoKey.GetDirectoryDataKey(outRhoKey);
            RhoEncrypt.EncryptData(folderKey, folderData, 0, folderData.Length);

            DataSavingInfo folderSavingInfo = new DataSavingInfo();
            folderSavingInfo.Data = folderData;
            folderSavingInfo.DataInfo.Offset = dataOffset;
            folderSavingInfo.DataInfo.Index = folderDataIndex;
            folderSavingInfo.DataInfo.Checksum = folderDataDecChksum;
            folderSavingInfo.DataInfo.DataSize = folderData.Length;
            folderSavingInfo.DataInfo.UncompressedSize = folderData.Length;
            folderSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.FullEncrypted;
            usedIndex.Add(folderDataIndex);
            savingInfo.Enqueue(folderSavingInfo);
            dataOffset = (dataOffset + folderSavingInfo.DataInfo.DataSize + 0xFF) & 0x7FFFFF00;
            foreach (RhoFolder subFolder in folder.Folders)
                storeFolderAndFiles(subFolder, savingInfo, usedIndex, ref dataOffset, outRhoKey);
            while (fileSavingInfoQueue.Count > 0)
            {
                DataSavingInfo fileSavingInfo = fileSavingInfoQueue.Dequeue();
                fileSavingInfo.DataInfo.Offset = dataOffset;
                savingInfo.Enqueue(fileSavingInfo);
                dataOffset = (dataOffset + fileSavingInfo.DataInfo.DataSize + 0xFF) & 0x7FFFFF00;
            }
        }

        private void releaseAllHandlers()
        {
            foreach (RhoFileHandler handler in _fileHandlers.Values)
                handler.releaseHandler();
            _fileHandlers.Clear();
        }
        #endregion

        #region Structs
        private class DataSavingInfo
        {
            public RhoDataInfo DataInfo = new RhoDataInfo();
            public RhoFile? File;
            public IDataSource DataSource;
            public byte[] Data;
        }
        #endregion
    }

    // Static
    public partial class RhoArchive
    {
        #region Constants
        internal readonly string[] _rhLayerIdentifiers = new string[]
        {
            "Rh layer spec 1.0",
            "Rh layer spec 1.1"
        };
        internal const string _rhLayerSecondText = "KartRider (veblush & dew)"; //Kartrider Rh layer author.
        #endregion
    }
}