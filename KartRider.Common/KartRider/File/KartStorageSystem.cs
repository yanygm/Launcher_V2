using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.Xml;

namespace KartLibrary.File;

public class KartStorageSystem : IDisposable
{
    private bool _useRho;

    private bool _useRho5;

    private bool _usePackFolderListFile;

    private CountryCode? _regionCode;

    private string? _clientPath;

    private string? _dataPath;

    private KartStorageFolder _rootFolder;

    private HashSet<RhoArchive> _rhoArchives;

    private HashSet<Rho5Archive> _rho5Archives;

    private bool _initialized;

    private bool _disposed;

    private bool _initializing;

    public KartStorageFolder RootFolder => _rootFolder;

    public bool IsInitialized => _initialized;

    public bool IsDisposed => _disposed;

    public KartStorageSystem(bool useRho, bool useRho5, bool usePackFolderListFile, CountryCode? regionCode, string? clientPath, string? dataPath)
    {
        _useRho = useRho;
        _useRho5 = useRho5;
        _usePackFolderListFile = usePackFolderListFile;
        _regionCode = regionCode;
        _clientPath = clientPath;
        _dataPath = dataPath;
        _rhoArchives = new HashSet<RhoArchive>();
        _rho5Archives = new HashSet<Rho5Archive>();
        _rootFolder = new KartStorageFolder(isRootFolder: true);
        _initialized = false;
        _initializing = false;
        _disposed = false;
    }

    public void Initialize(int maxThreads = 0)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException("Cannot initialize a disposed KartStorageSystem.");
        }

        _initializing = true;
        try
        {
            if (_useRho)
            {
                initializeRho(maxThreads);
            }

            if (_useRho5)
            {
                initializeRho5(maxThreads);
            }
        }
        finally
        {
            _initializing = false;
        }

        _initialized = true;
    }

    public KartStorageFolder? GetFolder(string folderPath)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("This KartStorageSystem haven't been initialized.");
        }

        if (_disposed)
        {
            throw new InvalidOperationException("This KartStorageSystem has been disposed.");
        }

        return _rootFolder.GetFolder(folderPath);
    }

    public KartStorageFile? GetFile(string filePath)
    {
        return _rootFolder.GetFile(filePath);
    }

    public void Close()
    {
        if (!_initialized)
        {
            return;
        }

        if (_initializing)
        {
            throw new InvalidOperationException("There are an initialize task running. Please call \"Close\" method after the initialization task has finished.");
        }

        _initialized = false;
        foreach (RhoArchive rhoArchive in _rhoArchives)
        {
            rhoArchive.Dispose();
        }

        foreach (Rho5Archive rho5Archive in _rho5Archives)
        {
            rho5Archive.Dispose();
        }

        _rhoArchives.Clear();
        _rho5Archives.Clear();
        _rootFolder.Clear();
    }

    public void Dispose()
    {
        dispose(disposing: true);
    }

    private void initializeRho(int maxThreads = 0)
    {
        if (maxThreads == 0)
        {
            maxThreads = Environment.ProcessorCount;
        }

        Queue<(KartStorageFolder, FileInfo)> queue = new Queue<(KartStorageFolder, FileInfo)>();
        string text = _dataPath ?? (_clientPath + "\\Data");
        if (!Directory.Exists(text))
        {
            throw new Exception("Data folder does not exist.");
        }

        if (_usePackFolderListFile)
        {
            if (_dataPath == null && _clientPath == null)
            {
                throw new Exception("You must give Dat folder path or Kartrider client path in constructor or builder.");
            }

            string text2 = text + "\\aaa.pk";
            if (!System.IO.File.Exists(text2))
            {
                throw new Exception(text2 + " does not exist");
            }

            using FileStream input = new FileStream(text2, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(input);
            int totalLength = binaryReader.ReadInt32();
            byte[] array = binaryReader.ReadKRData(totalLength);
            BinaryXmlDocument binaryXmlDocument = new BinaryXmlDocument();
            binaryXmlDocument.Read(Encoding.Unicode, array);
            if (binaryXmlDocument.RootTag.Name != "PackFolder")
            {
                throw new Exception("It is not valid aaa.pk file.");
            }

            Queue<(KartStorageFolder, BinaryXmlTag)> queue2 = new Queue<(KartStorageFolder, BinaryXmlTag)>();
            queue2.Enqueue((_rootFolder, binaryXmlDocument.RootTag));
            while (queue2.Count > 0)
            {
                (KartStorageFolder, BinaryXmlTag) tuple = queue2.Dequeue();
                foreach (BinaryXmlTag child in tuple.Item2.Children)
                {
                    if (child.Name == "PackFolder")
                    {
                        string text3 = child.GetAttribute("name");
                        if (text3 == null)
                        {
                            throw new Exception();
                        }

                        KartStorageFolder kartStorageFolder = new KartStorageFolder();
                        kartStorageFolder.Name = (tuple.Item1.IsRootFolder ? (text3 + "_") : text3);
                        kartStorageFolder.RhoFolderStoreMode = RhoFolderStoreMode.PackFolder;
                        tuple.Item1.AddFolder(kartStorageFolder);
                        queue2.Enqueue((kartStorageFolder, child));
                    }
                    else if (child.Name == "RhoFolder")
                    {
                        string text4 = child.GetAttribute("name");
                        string text5 = child.GetAttribute("fileName");
                        if (text4 == null || text5 == null)
                        {
                            throw new Exception();
                        }

                        string text6 = Path.Combine(text, text5);
                        if (!System.IO.File.Exists(text6))
                        {
                            throw new Exception("Rho: " + text6 + " doesn't exist");
                        }

                        FileInfo item = new FileInfo(text6);
                        if (text4.Length == 0)
                        {
                            queue.Enqueue((tuple.Item1, item));
                            continue;
                        }

                        KartStorageFolder kartStorageFolder2 = new KartStorageFolder();
                        kartStorageFolder2.Name = text4;
                        kartStorageFolder2.RhoFolderStoreMode = RhoFolderStoreMode.RhoFolder;
                        tuple.Item1.AddFolder(kartStorageFolder2);
                        queue.Enqueue((kartStorageFolder2, item));
                    }
                }
            }
        }
        else
        {
            FileInfo[] files = new DirectoryInfo(text).GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                Match match = new Regex("^(\\S+?_)(?:(\\S+?)_)*(\\S+?){0,1}\\.rho$").Match(fileInfo.Name);
                if (!match.Success || match.Groups.Count < 1)
                {
                    continue;
                }

                KartStorageFolder kartStorageFolder3 = _rootFolder;
                for (int j = 1; j < match.Groups.Count; j++)
                {
                    Group group = match.Groups[j];
                    if (!group.Success)
                    {
                        continue;
                    }

                    for (int k = 0; k < group.Captures.Count; k++)
                    {
                        string value = group.Captures[k].Value;
                        KartStorageFolder kartStorageFolder4 = kartStorageFolder3.GetFolder(value);
                        if (kartStorageFolder4 == null)
                        {
                            KartStorageFolder kartStorageFolder5 = new KartStorageFolder();
                            kartStorageFolder5.Name = value;
                            kartStorageFolder3.AddFolder(kartStorageFolder5);
                            kartStorageFolder4 = kartStorageFolder5;
                        }

                        kartStorageFolder3 = kartStorageFolder4;
                    }
                }

                queue.Enqueue((kartStorageFolder3, fileInfo));
            }
        }

        new ParallelOptions().MaxDegreeOfParallelism = maxThreads;
        Parallel.ForEach((IEnumerable<(KartStorageFolder, FileInfo)>)queue, (Action<(KartStorageFolder, FileInfo)>)delegate ((KartStorageFolder mountFolder, FileInfo rhoFileInfo) rhoFileInfo)
        {
            RhoArchive rhoArchive = new RhoArchive();
            mountRho(rhoFileInfo.mountFolder, rhoArchive, rhoFileInfo.rhoFileInfo.FullName);
            lock (_rhoArchives)
            {
                _rhoArchives.Add(rhoArchive);
            }
        });
        queue.Clear();
    }

    private void mountRho(KartStorageFolder mountFolder, RhoArchive rhoArchive, string fileName)
    {
        rhoArchive.Open(fileName);
        Queue<(KartStorageFolder, RhoFolder)> queue = new Queue<(KartStorageFolder, RhoFolder)>();
        queue.Enqueue((mountFolder, rhoArchive.RootFolder));
        while (queue.Count > 0)
        {
            (KartStorageFolder, RhoFolder) tuple = queue.Dequeue();
            lock (tuple.Item1)
            {
                tuple.Item1._sourceRhoFolder = tuple.Item2;
                foreach (RhoFolder folder in tuple.Item2.Folders)
                {
                    KartStorageFolder kartStorageFolder = new KartStorageFolder();
                    kartStorageFolder.Name = folder.Name;
                    queue.Enqueue((kartStorageFolder, folder));
                    tuple.Item1.AddFolder(kartStorageFolder);
                }

                foreach (RhoFile file2 in tuple.Item2.Files)
                {
                    KartStorageFile file = new KartStorageFile(file2);
                    tuple.Item1.AddFile(file);
                }
            }
        }
    }

    private void initializeRho5(int maxThreads = 0)
    {
        if (maxThreads == 0)
        {
            maxThreads = Environment.ProcessorCount;
        }

        new Queue<(KartStorageFolder, FileInfo)>();
        string dataFolder = _dataPath ?? (_clientPath + "\\Data");
        dataFolder = Path.GetFullPath(dataFolder);
        if (!Directory.Exists(dataFolder))
        {
            throw new Exception("Data folder does not exist.");
        }

        Regex regex = new Regex("^(DataPack\\d+)_(\\d+)\\.rho5$");
        DirectoryInfo directoryInfo = new DirectoryInfo(dataFolder);
        HashSet<string> hashSet = new HashSet<string>();
        FileInfo[] files = directoryInfo.GetFiles();
        foreach (FileInfo fileInfo in files)
        {
            Match match = regex.Match(fileInfo.Name);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                if (!hashSet.Contains(value))
                {
                    hashSet.Add(value);
                }
            }
        }

        new ParallelOptions().MaxDegreeOfParallelism = maxThreads;
        Parallel.ForEach((IEnumerable<string>)hashSet, (Action<string>)delegate (string dataPackName)
        {
            Rho5Archive rho5Archive = new Rho5Archive();
            mountRho5(rho5Archive, dataFolder, dataPackName);
            lock (_rho5Archives)
            {
                _rho5Archives.Add(rho5Archive);
            }
        });
    }

    private void mountRho5(Rho5Archive archive, string dataPackPath, string dataPackName)
    {
        archive.Open(dataPackPath, dataPackName, _regionCode.GetValueOrDefault());
        Queue<(KartStorageFolder, Rho5Folder)> queue = new Queue<(KartStorageFolder, Rho5Folder)>();
        queue.Enqueue((_rootFolder, archive.RootFolder));
        while (queue.Count > 0)
        {
            (KartStorageFolder, Rho5Folder) tuple = queue.Dequeue();
            lock (tuple.Item1)
            {
                foreach (Rho5Folder folder in tuple.Item2.Folders)
                {
                    KartStorageFolder kartStorageFolder = tuple.Item1.GetFolder(folder.Name);
                    if (kartStorageFolder == null)
                    {
                        kartStorageFolder = new KartStorageFolder(isRootFolder: false, null, folder);
                        kartStorageFolder.Name = folder.Name;
                        tuple.Item1.AddFolder(kartStorageFolder);
                    }

                    queue.Enqueue((kartStorageFolder, folder));
                }

                foreach (Rho5File file2 in tuple.Item2.Files)
                {
                    KartStorageFile file = new KartStorageFile(file2);
                    tuple.Item1.AddFile(file);
                }

                tuple.Item1.appliedChanges();
            }
        }
    }

    protected virtual void dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (_initializing)
            {
                throw new InvalidOperationException("You can't dispose KartStorageSystem when asynchronous \"Initialize\" method is running.");
            }

            Close();
        }

        _disposed = true;
    }
}