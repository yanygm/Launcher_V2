using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public class Rho5Folder : IRhoFolder<Rho5Folder, Rho5File>, IRhoFolder, IDisposable, IModifiableRhoFolder<Rho5Folder, Rho5File>, IModifiableRhoFolder
{
    private Rho5Folder? _parent;

    private string _name;

    private Dictionary<string, Rho5File> _files;

    private Dictionary<string, Rho5Folder> _folders;

    private bool _prevCounterInitialized;

    private uint _prevParentUpdatsCounter = 200195823u;

    private uint _fullnameUpdatesCounter = 338843496u;

    private string _parentFullname = "";

    private string _originalName;

    private HashSet<Rho5File> _addedFiles;

    private HashSet<Rho5File> _removedFiles;

    private HashSet<Rho5Folder> _addedFolders;

    private HashSet<Rho5Folder> _removedFolders;

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
                throw new InvalidOperationException("Cannot set the name of root folder.");
            }

            _name = value;
            Queue<Rho5Folder> queue = new Queue<Rho5Folder>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                Rho5Folder rho5Folder = queue.Dequeue();
                rho5Folder._fullnameUpdatesCounter += 1268438869u;
                foreach (Rho5Folder folder in rho5Folder.Folders)
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

    public Rho5Folder? Parent => _parent;

    IRhoFolder? IRhoFolder.Parent => Parent;

    IModifiableRhoFolder? IModifiableRhoFolder.Parent => Parent;

    public IReadOnlyCollection<Rho5File> Files => _files.Values;

    public IReadOnlyCollection<Rho5Folder> Folders => _folders.Values;

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

    public Rho5Folder()
    {
        _originalName = "";
        _name = "";
        _files = new Dictionary<string, Rho5File>();
        _folders = new Dictionary<string, Rho5Folder>();
        _addedFiles = new HashSet<Rho5File>();
        _addedFolders = new HashSet<Rho5Folder>();
        _removedFiles = new HashSet<Rho5File>();
        _removedFolders = new HashSet<Rho5Folder>();
        _parent = null;
        _disposed = false;
        _isRootFolder = false;
        _prevCounterInitialized = false;
    }

    internal Rho5Folder(bool isRootFolder)
        : this()
    {
        _isRootFolder = true;
    }

    public Rho5File? GetFile(string path)
    {
        string[] array = path.Split('/');
        Rho5Folder rho5Folder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string text = array[i];
            if (text == ".")
            {
                continue;
            }

            if (text == "..")
            {
                if (rho5Folder.Parent == null)
                {
                    return null;
                }

                rho5Folder = rho5Folder.Parent;
            }
            else
            {
                if (!rho5Folder._folders.ContainsKey(text))
                {
                    return null;
                }

                rho5Folder = rho5Folder._folders[text];
            }
        }

        if (array.Length >= 1 && rho5Folder._files.ContainsKey(array[^1]))
        {
            return rho5Folder._files[array[^1]];
        }

        throw new Exception("File: " + path + " can not be found.");
    }

    public Rho5Folder? GetFolder(string path)
    {
        string[] array = path.Split('/');
        Rho5Folder rho5Folder = this;
        foreach (string text in array)
        {
            if (text == ".")
            {
                continue;
            }

            if (text == "..")
            {
                if (rho5Folder.Parent == null)
                {
                    return null;
                }

                rho5Folder = rho5Folder.Parent;
            }
            else
            {
                if (!rho5Folder._folders.ContainsKey(text))
                {
                    return null;
                }

                rho5Folder = rho5Folder._folders[text];
            }
        }

        return rho5Folder;
    }

    public void AddFile(Rho5File file)
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

    public void AddFile(string path, Rho5File file)
    {
        string[] array = path.Split('/');
        Rho5Folder rho5Folder = this;
        List<string> list = new List<string>();
        foreach (string text in array)
        {
            list.Add(text);
            if (!rho5Folder._folders.ContainsKey(text))
            {
                throw new Exception("Folder: " + string.Join('/', list.ToArray()) + " can not be found.");
            }

            rho5Folder = rho5Folder._folders[text];
        }

        if (array.Length > 1)
        {
            string name = file.Name;
            if (!rho5Folder._files.ContainsKey(name))
            {
                if (file.Parent != null)
                {
                    throw new Exception("The parent of adding file is in other folder.");
                }

                rho5Folder.AddFile(file);
                return;
            }

            throw new Exception($"File: {path}/{name} is exist.");
        }

        throw new Exception("Path: " + path + " is invalid.");
    }

    public void AddFolder(Rho5Folder folder)
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

    public void AddFolder(string path, Rho5Folder folder)
    {
        string[] array = path.Split('/');
        Rho5Folder rho5Folder = this;
        List<string> list = new List<string>();
        foreach (string text in array)
        {
            list.Add(text);
            if (!rho5Folder._folders.ContainsKey(text))
            {
                throw new Exception("Folder: " + string.Join('/', list.ToArray()) + " can not be found.");
            }

            rho5Folder = rho5Folder._folders[text];
        }

        if (array.Length > 1)
        {
            string name = folder.Name;
            if (!rho5Folder._folders.ContainsKey(name))
            {
                rho5Folder.AddFolder(folder);
                return;
            }

            throw new Exception($"Folder: {path}/{name} is exist.");
        }

        throw new Exception("Path: " + path + " is invalid.");
    }

    public bool ContainsFolder(string path)
    {
        string[] array = path.Split('/');
        Rho5Folder rho5Folder = this;
        foreach (string key in array)
        {
            if (rho5Folder._folders.ContainsKey(key))
            {
                rho5Folder = rho5Folder._folders[key];
                continue;
            }

            return false;
        }

        return array.Length != 0;
    }

    public bool ContainsFile(string path)
    {
        string[] array = path.Split('/');
        Rho5Folder rho5Folder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (rho5Folder._folders.ContainsKey(key))
            {
                rho5Folder = rho5Folder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0)
        {
            return rho5Folder._files.ContainsKey(array[^1]);
        }

        return false;
    }

    public bool RemoveFile(string fileFullName)
    {
        string[] array = fileFullName.Split('/');
        Rho5Folder rho5Folder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (rho5Folder._folders.ContainsKey(key))
            {
                rho5Folder = rho5Folder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0 && rho5Folder._files.ContainsKey(array[^1]))
        {
            rho5Folder.RemoveFile(rho5Folder._files[array[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFile(Rho5File fileToDelete)
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
        Rho5Folder rho5Folder = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            string key = array[i];
            if (rho5Folder._folders.ContainsKey(key))
            {
                rho5Folder = rho5Folder._folders[key];
                continue;
            }

            return false;
        }

        if (array.Length != 0 && rho5Folder._folders.ContainsKey(array[^1]))
        {
            rho5Folder.RemoveFolder(rho5Folder._folders[array[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFolder(Rho5Folder folderToDelete)
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
        foreach (Rho5Folder value in _folders.Values)
        {
            _removedFolders.Add(value);
        }

        foreach (Rho5File value2 in _files.Values)
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

    public override string ToString()
    {
        return "Rho5Folder:" + FullName;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (Rho5Folder value in _folders.Values)
            {
                value.Dispose();
            }

            foreach (Rho5File value2 in _files.Values)
            {
                value2.Dispose();
            }

            foreach (Rho5Folder removedFolder in _removedFolders)
            {
                removedFolder.Dispose();
            }

            foreach (Rho5File removedFile in _removedFiles)
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
        if (file is Rho5File file2)
        {
            AddFile(file2);
            return;
        }

        throw new Exception();
    }

    void IModifiableRhoFolder.AddFile(string path, IModifiableRhoFile file)
    {
        if (file is Rho5File file2)
        {
            AddFile(path, file2);
            return;
        }

        throw new Exception();
    }

    void IModifiableRhoFolder.AddFolder(IModifiableRhoFolder folder)
    {
        if (folder is Rho5Folder folder2)
        {
            AddFolder(folder2);
            return;
        }

        throw new Exception();
    }

    void IModifiableRhoFolder.AddFolder(string path, IModifiableRhoFolder folder)
    {
        if (folder is Rho5Folder folder2)
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

    private bool checkIfFilesModified()
    {
        foreach (Rho5File value in _files.Values)
        {
            if (value.IsModified)
            {
                return true;
            }
        }

        return false;
    }
}