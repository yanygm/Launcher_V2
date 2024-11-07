using System;
using System.Collections.Generic;
using System.Text;
using KartLibrary.IO;

namespace KartLibrary.File;

public class RhoFolder : IRhoFolder<RhoFolder, RhoFile>, IRhoFolder, IDisposable, IModifiableRhoFolder<RhoFolder, RhoFile>, IModifiableRhoFolder
{
    private RhoFolder? _parent;

    private string _name;

    private Dictionary<string, RhoFile> _files;

    private Dictionary<string, RhoFolder> _folders;

    private uint _prevParentUpdatsCounter = 195935983u;

    private uint _fullnameUpdatesCounter = 338843496u;

    private uint? _folderDataIndex;

    private bool _prevCounterInitialized;

    private string _parentFullname = "";

    private string _originalName;

    private HashSet<RhoFile> _addedFiles;

    private HashSet<RhoFile> _removedFiles;

    private HashSet<RhoFolder> _addedFolders;

    private HashSet<RhoFolder> _removedFolders;

    private bool _isRootFolder;

    private bool _disposed;

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
                throw new InvalidOperationException("cannot set the name of root folder.");
            }

            _name = value;
            _folderDataIndex = null;
            Queue<RhoFolder> queue = new Queue<RhoFolder>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                RhoFolder rhoFolder = queue.Dequeue();
                rhoFolder._fullnameUpdatesCounter += 1268438869u;
                foreach (RhoFolder folder in rhoFolder.Folders)
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

    public RhoFolder? Parent => _parent;

    IRhoFolder? IRhoFolder.Parent => _parent;

    IModifiableRhoFolder? IModifiableRhoFolder.Parent => _parent;

    public IReadOnlyCollection<RhoFile> Files => _files.Values;

    public IReadOnlyCollection<RhoFolder> Folders => _folders.Values;

    IReadOnlyCollection<IRhoFile> IRhoFolder.Files => Files;

    IReadOnlyCollection<IModifiableRhoFile> IModifiableRhoFolder.Files => Files;

    IReadOnlyCollection<IRhoFolder> IRhoFolder.Folders => Folders;

    IReadOnlyCollection<IModifiableRhoFolder> IModifiableRhoFolder.Folders => Folders;

    internal bool HasModified
    {
        get
        {
            if (_addedFiles.Count <= 0 && _addedFolders.Count <= 0 && _removedFiles.Count <= 0 && _removedFolders.Count <= 0 && !(_originalName != _name))
            {
                return checkIfFilesModified();
            }

            return true;
        }
    }

    public RhoFolder()
    {
        _originalName = "";
        _name = "";
        _files = new Dictionary<string, RhoFile>();
        _folders = new Dictionary<string, RhoFolder>();
        _addedFiles = new HashSet<RhoFile>();
        _addedFolders = new HashSet<RhoFolder>();
        _removedFiles = new HashSet<RhoFile>();
        _removedFolders = new HashSet<RhoFolder>();
        _parent = null;
        _disposed = false;
        _isRootFolder = false;
        _prevCounterInitialized = false;
    }

    internal RhoFolder(bool isRootFolder)
        : this()
    {
        _isRootFolder = true;
    }

    public RhoFile? GetFile(string path)
    {
        string[] array = path.Split('/');
        RhoFolder rhoFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string text = array[i];
            if (text == ".")
            {
                continue;
            }

            if (text == "..")
            {
                if (rhoFolder.Parent == null)
                {
                    return null;
                }

                rhoFolder = rhoFolder.Parent;
            }
            else
            {
                if (!rhoFolder._folders.ContainsKey(text))
                {
                    return null;
                }

                rhoFolder = rhoFolder._folders[text];
            }
        }

        if (array.Length >= 1 && rhoFolder._files.ContainsKey(array[^1]))
        {
            return rhoFolder._files[array[^1]];
        }

        return null;
    }

    public RhoFolder? GetFolder(string path)
    {
        string[] array = path.Split('/');
        RhoFolder rhoFolder = this;
        foreach (string text in array)
        {
            if (text == ".")
            {
                continue;
            }

            if (text == "..")
            {
                if (rhoFolder.Parent == null)
                {
                    return null;
                }

                rhoFolder = rhoFolder.Parent;
            }
            else
            {
                if (!rhoFolder._folders.ContainsKey(text))
                {
                    return null;
                }

                rhoFolder = rhoFolder._folders[text];
            }
        }

        return rhoFolder;
    }

    public void AddFile(RhoFile file)
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
            _removedFiles.Remove(file);
        }
        else
        {
            _addedFiles.Add(file);
        }

        _files.Add(file.Name, file);
        file._parentFolder = this;
    }

    public void AddFile(string path, RhoFile file)
    {
        string[] array = path.Split('/');
        RhoFolder rhoFolder = this;
        List<string> list = new List<string>();
        foreach (string text in array)
        {
            list.Add(text);
            if (!rhoFolder._folders.ContainsKey(text))
            {
                throw new Exception("Folder: " + string.Join('/', list.ToArray()) + " can not be found.");
            }

            rhoFolder = rhoFolder._folders[text];
        }

        if (array.Length > 1)
        {
            string name = file.Name;
            if (!rhoFolder._files.ContainsKey(name))
            {
                if (file.Parent != null)
                {
                    throw new Exception("The parent of adding file is in other folder.");
                }

                rhoFolder.AddFile(file);
                return;
            }

            throw new Exception($"File: {path}/{name} is exist.");
        }

        throw new Exception("Path: " + path + " is invalid.");
    }

    public void AddFolder(RhoFolder folder)
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

    public void AddFolder(string path, RhoFolder folder)
    {
        string[] array = path.Split('/');
        RhoFolder rhoFolder = this;
        List<string> list = new List<string>();
        foreach (string text in array)
        {
            list.Add(text);
            if (!rhoFolder._folders.ContainsKey(text))
            {
                throw new Exception("Folder: " + string.Join('/', list.ToArray()) + " can not be found.");
            }

            rhoFolder = rhoFolder._folders[text];
        }

        if (array.Length > 1)
        {
            string name = folder.Name;
            if (!rhoFolder._folders.ContainsKey(name))
            {
                rhoFolder.AddFolder(folder);
                return;
            }

            throw new Exception($"Folder: {path}/{name} is exist.");
        }

        throw new Exception("Path: " + path + " is invalid.");
    }

    public bool ContainsFolder(string path)
    {
        string[] array = path.Split('/');
        RhoFolder rhoFolder = this;
        foreach (string key in array)
        {
            if (rhoFolder._folders.ContainsKey(key))
            {
                rhoFolder = rhoFolder._folders[key];
                continue;
            }

            return false;
        }

        return array.Length != 0;
    }

    public bool ContainsFile(string path)
    {
        string[] array = path.Split('/');
        RhoFolder rhoFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (rhoFolder._folders.ContainsKey(key))
            {
                rhoFolder = rhoFolder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0)
        {
            return rhoFolder._files.ContainsKey(array[^1]);
        }

        return false;
    }

    public bool RemoveFile(string fileFullName)
    {
        string[] array = fileFullName.Split('/');
        RhoFolder rhoFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (rhoFolder._folders.ContainsKey(key))
            {
                rhoFolder = rhoFolder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0 && rhoFolder._files.ContainsKey(array[^1]))
        {
            rhoFolder.RemoveFile(rhoFolder._files[array[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFile(RhoFile fileToDelete)
    {
        if (_files.ContainsKey(fileToDelete.Name) && _files[fileToDelete.Name] == fileToDelete)
        {
            if (_addedFiles.Contains(fileToDelete))
            {
                _addedFiles.Remove(fileToDelete);
            }
            else
            {
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
        RhoFolder rhoFolder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (rhoFolder._folders.ContainsKey(key))
            {
                rhoFolder = rhoFolder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0 && rhoFolder._folders.ContainsKey(array[^1]))
        {
            rhoFolder.RemoveFolder(rhoFolder._folders[array[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFolder(RhoFolder folderToDelete)
    {
        if (_folders.ContainsKey(folderToDelete.Name) && _folders[folderToDelete.Name] == folderToDelete)
        {
            if (_addedFolders.Contains(folderToDelete))
            {
                _addedFolders.Remove(folderToDelete);
            }
            else
            {
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
        foreach (RhoFolder value in _folders.Values)
        {
            _removedFolders.Add(value);
        }

        foreach (RhoFile value2 in _files.Values)
        {
            _removedFiles.Add(value2);
        }

        _folders.Clear();
        _files.Clear();
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
            foreach (RhoFolder value in _folders.Values)
            {
                value.Dispose();
            }

            foreach (RhoFile value2 in _files.Values)
            {
                value2.Dispose();
            }

            foreach (RhoFolder removedFolder in _removedFolders)
            {
                removedFolder.Dispose();
            }

            foreach (RhoFile removedFile in _removedFiles)
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
        return "RhoFolder:" + FullName;
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
        if (file is RhoFile file2)
        {
            AddFile(file2);
            return;
        }

        throw new Exception("");
    }

    void IModifiableRhoFolder.AddFile(string path, IModifiableRhoFile file)
    {
        if (file is RhoFile file2)
        {
            AddFile(path, file2);
            return;
        }

        throw new Exception("");
    }

    void IModifiableRhoFolder.AddFolder(IModifiableRhoFolder folder)
    {
        if (folder is RhoFolder folder2)
        {
            AddFolder(folder2);
            return;
        }

        throw new Exception("");
    }

    void IModifiableRhoFolder.AddFolder(string path, IModifiableRhoFolder folder)
    {
        if (folder is RhoFolder folder2)
        {
            AddFolder(path, folder2);
            return;
        }

        throw new Exception("");
    }

    internal uint getFolderDataIndex()
    {
        if (_parent != null && _prevParentUpdatsCounter != _parent._fullnameUpdatesCounter)
        {
            _folderDataIndex = null;
        }

        uint? folderDataIndex = _folderDataIndex;
        if (!folderDataIndex.HasValue)
        {
            if (_name.Length == 0 && _parent == null)
            {
                return uint.MaxValue;
            }

            string fullName = FullName;
            byte[] bytes = Encoding.Unicode.GetBytes(fullName);
            _folderDataIndex = Adler.Adler32(0u, bytes, 0, bytes.Length);
        }

        return _folderDataIndex.Value;
    }

    internal void appliedChanges()
    {
        _originalName = _name;
        _addedFiles.Clear();
        _addedFolders.Clear();
        _removedFiles.Clear();
        _removedFolders.Clear();
    }

    private bool checkIfFilesModified()
    {
        foreach (RhoFile value in _files.Values)
        {
            if (value.IsModified)
            {
                return true;
            }
        }

        return false;
    }
}