using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public class KartStorageFolder : IRhoFolder<KartStorageFolder, KartStorageFile>, IRhoFolder, IDisposable, IModifiableRhoFolder<KartStorageFolder, KartStorageFile>, IModifiableRhoFolder
{
    private KartStorageFolder? _parent;

    private string _name;

    private Dictionary<string, KartStorageFile> _files;

    private Dictionary<string, KartStorageFolder> _folders;

    private bool _prevCounterInitialized;

    private uint _prevParentUpdatsCounter = 200195823u;

    private uint _fullnameUpdatesCounter = 338843496u;

    private string _parentFullname = "";

    private string _originalName;

    private HashSet<KartStorageFile> _addedFiles;

    private HashSet<KartStorageFile> _removedFiles;

    private HashSet<KartStorageFolder> _addedFolders;

    private HashSet<KartStorageFolder> _removedFolders;

    private bool _isRootFolder;

    private bool _disposed;

    internal RhoFolder? _sourceRhoFolder;

    internal Rho5Folder? _sourceRho5Folder;

    private RhoFolderStoreMode _rhoStoreMode;

    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            if (_isRootFolder)
            {
                throw new InvalidOperationException("Cannot set the name of root folder.");
            }

            _name = value;
            Queue<KartStorageFolder> queue = new Queue<KartStorageFolder>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                KartStorageFolder kartStorageFolder = queue.Dequeue();
                kartStorageFolder._fullnameUpdatesCounter += 1268438869u;
                foreach (KartStorageFolder folder in kartStorageFolder.Folders)
                {
                    queue.Enqueue(folder);
                }
            }
        }
    }

    public string FullName
    {
        get
        {
            if (_parent == null)
            {
                return _name;
            }

            if (!_prevCounterInitialized || _prevParentUpdatsCounter != _parent._fullnameUpdatesCounter)
            {
                _parentFullname = _parent.FullName;
                _prevParentUpdatsCounter = _parent._fullnameUpdatesCounter;
                _prevCounterInitialized = true;
            }

            if (_parent._name.Length <= 0)
            {
                return _name;
            }

            return _parentFullname + "/" + _name;
        }
    }

    public KartStorageFolder? Parent => _parent;

    public IReadOnlyCollection<KartStorageFile> Files => _files.Values;

    public IReadOnlyCollection<KartStorageFolder> Folders => _folders.Values;

    public bool IsRootFolder => _isRootFolder;

    public RhoFolderStoreMode RhoFolderStoreMode
    {
        get
        {
            return _rhoStoreMode;
        }
        set
        {
            _rhoStoreMode = value;
        }
    }

    IRhoFolder? IRhoFolder.Parent => Parent;

    IModifiableRhoFolder? IModifiableRhoFolder.Parent => Parent;

    IReadOnlyCollection<IRhoFile> IRhoFolder.Files => Files;

    IReadOnlyCollection<IModifiableRhoFile> IModifiableRhoFolder.Files => Files;

    IReadOnlyCollection<IRhoFolder> IRhoFolder.Folders => Folders;

    IReadOnlyCollection<IModifiableRhoFolder> IModifiableRhoFolder.Folders => Folders;

    internal bool HasModified
    {
        get
        {
            if (_addedFiles.Count <= 0 && _addedFolders.Count <= 0 && _removedFiles.Count <= 0 && _removedFolders.Count <= 0)
            {
                return _originalName != _name;
            }

            return true;
        }
    }

    public KartStorageFolder()
    {
        _originalName = "";
        _name = "";
        _files = new Dictionary<string, KartStorageFile>();
        _folders = new Dictionary<string, KartStorageFolder>();
        _addedFiles = new HashSet<KartStorageFile>();
        _addedFolders = new HashSet<KartStorageFolder>();
        _removedFiles = new HashSet<KartStorageFile>();
        _removedFolders = new HashSet<KartStorageFolder>();
        _rhoStoreMode = RhoFolderStoreMode.RhoFolder;
        _parent = null;
        _disposed = false;
        _isRootFolder = false;
        _prevCounterInitialized = false;
    }

    internal KartStorageFolder(bool isRootFolder)
        : this()
    {
        _isRootFolder = isRootFolder;
    }

    internal KartStorageFolder(bool isRootFolder, RhoFolder? rhoFolder, Rho5Folder? rho5Folder)
        : this(isRootFolder)
    {
        _sourceRhoFolder = rhoFolder;
        _sourceRho5Folder = rho5Folder;
    }

    public KartStorageFile? GetFile(string path)
    {
        string[] array = path.Split('/');
        KartStorageFolder kartStorageFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string text = array[i];
            if (text == ".")
            {
                continue;
            }

            if (text == "..")
            {
                if (kartStorageFolder.Parent == null)
                {
                    return null;
                }

                kartStorageFolder = kartStorageFolder.Parent;
            }
            else
            {
                if (!kartStorageFolder._folders.ContainsKey(text))
                {
                    return null;
                }

                kartStorageFolder = kartStorageFolder._folders[text];
            }
        }

        if (array.Length >= 1 && kartStorageFolder._files.ContainsKey(array[^1]))
        {
            return kartStorageFolder._files[array[^1]];
        }

        return null;
    }

    public KartStorageFolder? GetFolder(string path)
    {
        string[] array = path.Split('/');
        KartStorageFolder kartStorageFolder = this;
        foreach (string text in array)
        {
            if (text == ".")
            {
                continue;
            }

            if (text == "..")
            {
                if (kartStorageFolder.Parent == null)
                {
                    return null;
                }

                kartStorageFolder = kartStorageFolder.Parent;
            }
            else
            {
                if (!kartStorageFolder._folders.ContainsKey(text))
                {
                    return null;
                }

                kartStorageFolder = kartStorageFolder._folders[text];
            }
        }

        return kartStorageFolder;
    }

    public void AddFile(KartStorageFile file)
    {
        if (_files.ContainsKey(file.Name))
        {
            throw new Exception("File: " + file.Name + " is exist.");
        }

        if (file._parentFolder != null)
        {
            throw new Exception("The parent of a file you want to add is not null.");
        }

        if (_removedFiles.Contains(file))
        {
            if (file._sourceFile is RhoFile file2 && _sourceRhoFolder != null)
            {
                _sourceRhoFolder.AddFile(file2);
            }
            else if (file._sourceFile is Rho5File file3 && _sourceRho5Folder != null)
            {
                _sourceRho5Folder.AddFile(file3);
            }

            _removedFiles.Remove(file);
        }
        else
        {
            _addedFiles.Add(file);
        }

        _files.Add(file.Name, file);
        file._parentFolder = this;
    }

    public void AddFile(string path, KartStorageFile file)
    {
        string[] array = path.Split('/');
        KartStorageFolder kartStorageFolder = this;
        List<string> list = new List<string>();
        foreach (string text in array)
        {
            list.Add(text);
            if (!kartStorageFolder._folders.ContainsKey(text))
            {
                throw new Exception("Folder: " + string.Join('/', list.ToArray()) + " can not be found.");
            }

            kartStorageFolder = kartStorageFolder._folders[text];
        }

        if (array.Length > 1)
        {
            string name = file.Name;
            if (!kartStorageFolder._files.ContainsKey(name))
            {
                if (file.Parent != null)
                {
                    throw new Exception("The parent of adding file is in other folder.");
                }

                kartStorageFolder.AddFile(file);
                return;
            }

            throw new Exception($"File: {path}/{name} is exist.");
        }

        throw new Exception("Path: " + path + " is invalid.");
    }

    public void AddFolder(KartStorageFolder folder)
    {
        if (_folders.ContainsKey(folder.Name))
        {
            throw new Exception("Folder: " + folder.Name + " is exist.");
        }

        if (folder._parent != null)
        {
            throw new Exception("The parent of a folder you want to add is not null.");
        }

        if (_removedFolders.Contains(folder))
        {
            if (folder._sourceRhoFolder != null && _sourceRhoFolder != null)
            {
                _sourceRhoFolder.AddFolder(folder._sourceRhoFolder);
            }

            if (folder._sourceRho5Folder != null && _sourceRho5Folder != null)
            {
                _sourceRho5Folder.AddFolder(folder._sourceRho5Folder);
            }

            _removedFolders.Remove(folder);
        }
        else
        {
            _addedFolders.Add(folder);
        }

        _folders.Add(folder.Name, folder);
        folder._parent = this;
        folder._prevCounterInitialized = false;
        folder.Name = folder._name;
    }

    public void AddFolder(string path, KartStorageFolder folder)
    {
        string[] array = path.Split('/');
        KartStorageFolder kartStorageFolder = this;
        List<string> list = new List<string>();
        foreach (string text in array)
        {
            list.Add(text);
            if (!kartStorageFolder._folders.ContainsKey(text))
            {
                throw new Exception("Folder: " + string.Join('/', list.ToArray()) + " can not be found.");
            }

            kartStorageFolder = kartStorageFolder._folders[text];
        }

        if (array.Length > 1)
        {
            string name = folder.Name;
            if (!kartStorageFolder._folders.ContainsKey(name))
            {
                kartStorageFolder.AddFolder(folder);
                return;
            }

            throw new Exception($"Folder: {path}/{name} is exist.");
        }

        throw new Exception("Path: " + path + " is invalid.");
    }

    public bool ContainsFolder(string path)
    {
        string[] array = path.Split('/');
        KartStorageFolder kartStorageFolder = this;
        foreach (string key in array)
        {
            if (kartStorageFolder._folders.ContainsKey(key))
            {
                kartStorageFolder = kartStorageFolder._folders[key];
                continue;
            }

            return false;
        }

        return array.Length != 0;
    }

    public bool ContainsFile(string path)
    {
        string[] array = path.Split('/');
        KartStorageFolder kartStorageFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (kartStorageFolder._folders.ContainsKey(key))
            {
                kartStorageFolder = kartStorageFolder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0)
        {
            return kartStorageFolder._files.ContainsKey(array[^1]);
        }

        return false;
    }

    public bool RemoveFile(string fileFullName)
    {
        string[] array = fileFullName.Split('/');
        KartStorageFolder kartStorageFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (kartStorageFolder._folders.ContainsKey(key))
            {
                kartStorageFolder = kartStorageFolder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0 && kartStorageFolder._files.ContainsKey(array[^1]))
        {
            kartStorageFolder.RemoveFile(kartStorageFolder._files[array[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFile(KartStorageFile fileToDelete)
    {
        if (_files.ContainsKey(fileToDelete.Name) && _files[fileToDelete.Name] == fileToDelete)
        {
            if (_addedFiles.Contains(fileToDelete))
            {
                _addedFiles.Remove(fileToDelete);
            }
            else
            {
                if (fileToDelete._sourceFile is RhoFile fileToDelete2 && _sourceRhoFolder != null)
                {
                    _sourceRhoFolder.RemoveFile(fileToDelete2);
                }
                else if (fileToDelete._sourceFile is Rho5File fileToDelete3 && _sourceRho5Folder != null)
                {
                    _sourceRho5Folder.RemoveFile(fileToDelete3);
                }

                _removedFiles.Add(fileToDelete);
            }

            _files.Remove(fileToDelete.Name);
            fileToDelete._parentFolder = null;
            return true;
        }

        return false;
    }

    public bool RemoveFolder(string folderFullName)
    {
        string[] array = folderFullName.Split('/');
        KartStorageFolder kartStorageFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (kartStorageFolder._folders.ContainsKey(key))
            {
                kartStorageFolder = kartStorageFolder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0 && kartStorageFolder._folders.ContainsKey(array[^1]))
        {
            kartStorageFolder.RemoveFolder(kartStorageFolder._folders[array[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFolder(KartStorageFolder folderToDelete)
    {
        if (_folders.ContainsKey(folderToDelete.Name) && _folders[folderToDelete.Name] == folderToDelete)
        {
            if (_addedFolders.Contains(folderToDelete))
            {
                _addedFolders.Remove(folderToDelete);
            }
            else
            {
                if (folderToDelete._sourceRhoFolder != null && _sourceRhoFolder != null)
                {
                    _sourceRhoFolder.RemoveFolder(folderToDelete._sourceRhoFolder);
                }

                if (folderToDelete._sourceRho5Folder != null && _sourceRho5Folder != null)
                {
                    _sourceRho5Folder.RemoveFolder(folderToDelete._sourceRho5Folder);
                }

                _removedFolders.Add(folderToDelete);
            }

            _folders.Remove(folderToDelete.Name);
            folderToDelete._parent = null;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (KartStorageFolder value in _folders.Values)
        {
            RemoveFolder(value);
        }

        foreach (KartStorageFile value2 in _files.Values)
        {
            RemoveFile(value2);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (KartStorageFolder value in _folders.Values)
            {
                value.Dispose();
            }

            foreach (KartStorageFile value2 in _files.Values)
            {
                value2.Dispose();
            }

            foreach (KartStorageFolder removedFolder in _removedFolders)
            {
                removedFolder.Dispose();
            }

            foreach (KartStorageFile removedFile in _removedFiles)
            {
                removedFile.Dispose();
            }

            _folders.Clear();
            _files.Clear();
            _removedFolders.Clear();
            _removedFiles.Clear();
        }

        _folders = null;
        _files = null;
        _removedFolders = null;
        _removedFiles = null;
        _parent = null;
        _disposed = true;
    }

    public override string ToString()
    {
        return "KartStorageFolder:" + FullName;
    }

    IRhoFile? IRhoFolder.GetFile(string path)
    {
        return GetFile(path);
    }

    IRhoFolder? IRhoFolder.GetFolder(string path)
    {
        return GetFolder(path);
    }

    IModifiableRhoFile? IModifiableRhoFolder.GetFile(string path)
    {
        return GetFile(path);
    }

    IModifiableRhoFolder? IModifiableRhoFolder.GetFolder(string path)
    {
        return GetFolder(path);
    }

    void IModifiableRhoFolder.AddFile(IModifiableRhoFile file)
    {
        if (file is KartStorageFile file2)
        {
            AddFile(file2);
            return;
        }

        throw new Exception();
    }

    void IModifiableRhoFolder.AddFile(string path, IModifiableRhoFile file)
    {
        if (file is KartStorageFile file2)
        {
            AddFile(path, file2);
            return;
        }

        throw new Exception();
    }

    void IModifiableRhoFolder.AddFolder(IModifiableRhoFolder folder)
    {
        if (folder is KartStorageFolder folder2)
        {
            AddFolder(folder2);
            return;
        }

        throw new Exception();
    }

    void IModifiableRhoFolder.AddFolder(string path, IModifiableRhoFolder folder)
    {
        if (folder is KartStorageFolder folder2)
        {
            AddFolder(path, folder2);
            return;
        }

        throw new Exception();
    }

    internal void appliedChanges()
    {
        _originalName = _name;
        _addedFiles.Clear();
        _addedFolders.Clear();
        _removedFiles.Clear();
        _removedFolders.Clear();
    }
}