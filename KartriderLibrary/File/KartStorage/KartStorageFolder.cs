using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public class KartStorageFolder : IRhoFolder<KartStorageFolder, KartStorageFile>, IModifiableRhoFolder<KartStorageFolder, KartStorageFile>
    {
        #region Members
        private KartStorageFolder? _parent;
        private string _name;

        private Dictionary<string, KartStorageFile> _files;
        private Dictionary<string, KartStorageFolder> _folders;

        private bool _prevCounterInitialized;
        private uint _prevParentUpdatsCounter = 0xBEE_BEEFu;
        private uint _fullnameUpdatesCounter = 0x14325768u;

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
        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set
            {
                if (_isRootFolder)
                    throw new InvalidOperationException("Cannot set the name of root folder.");
                _name = value;
                Queue<KartStorageFolder> affectedFolders = new Queue<KartStorageFolder>();
                affectedFolders.Enqueue(this);
                while (affectedFolders.Count > 0)
                {
                    KartStorageFolder affectedFolder = affectedFolders.Dequeue();
                    affectedFolder._fullnameUpdatesCounter += 0x4B9AD755u;
                    foreach (KartStorageFolder folder in affectedFolder.Folders)
                        affectedFolders.Enqueue(folder);
                }
            }
        }

        public string FullName
        {
            get
            {
                if (_parent is null)
                    return _name;
                else
                {
                    if (!_prevCounterInitialized || _prevParentUpdatsCounter != _parent._fullnameUpdatesCounter)
                    {
                        _parentFullname = _parent.FullName;
                        _prevParentUpdatsCounter = _parent._fullnameUpdatesCounter;
                        _prevCounterInitialized = true;
                    }
                    return _parent._name.Length > 0 ? $"{_parentFullname}/{_name}" : _name;
                }
            }
        }

        public KartStorageFolder? Parent => _parent;

        public IReadOnlyCollection<KartStorageFile> Files => _files.Values;

        public IReadOnlyCollection<KartStorageFolder> Folders => _folders.Values;

        public bool IsRootFolder => _isRootFolder;

        public RhoFolderStoreMode RhoFolderStoreMode
        {
            get => _rhoStoreMode;
            set => _rhoStoreMode = value;
        }

        IRhoFolder? IRhoFolder.Parent => Parent;

        IModifiableRhoFolder? IModifiableRhoFolder.Parent => Parent;

        IReadOnlyCollection<IRhoFile> IRhoFolder.Files => Files;

        IReadOnlyCollection<IModifiableRhoFile> IModifiableRhoFolder.Files => Files;

        IReadOnlyCollection<IRhoFolder> IRhoFolder.Folders => Folders;

        IReadOnlyCollection<IModifiableRhoFolder> IModifiableRhoFolder.Folders => Folders;


        internal bool HasModified =>
            _addedFiles.Count > 0 ||
            _addedFolders.Count > 0 ||
            _removedFiles.Count > 0 ||
            _removedFolders.Count > 0 ||
            _originalName != _name;

        #endregion

        #region Constructors
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

        internal KartStorageFolder(bool isRootFolder) : this()
        {
            _isRootFolder = isRootFolder;
        }

        internal KartStorageFolder(bool isRootFolder, RhoFolder? rhoFolder, Rho5Folder? rho5Folder) : this(isRootFolder)
        {
            _sourceRhoFolder = rhoFolder;
            _sourceRho5Folder = rho5Folder;
        }
        #endregion

        #region Methods
        public KartStorageFile? GetFile(string path)
        {
            string[] splittedPath = path.Split('/');
            KartStorageFolder findFolders = this;
            for (int i = 0; i < splittedPath.Length - 1; i++)
            {
                string folderName = splittedPath[i];
                if (folderName == ".")
                    continue;
                else if (folderName == "..")
                    if (findFolders.Parent is null)
                        return null;
                    else
                        findFolders = findFolders.Parent;
                else if (!findFolders._folders.ContainsKey(folderName))
                    return null;
                else
                {
                    findFolders = findFolders._folders[folderName];
                }
            }
            if (splittedPath.Length >= 1 && findFolders._files.ContainsKey(splittedPath[^1]))
            {
                return findFolders._files[splittedPath[^1]];
            }
            else
            {
                return null;
            }
        }

        public KartStorageFolder? GetFolder(string path)
        {
            string[] splittedPath = path.Split('/');
            KartStorageFolder findFolder = this;
            for (int i = 0; i < splittedPath.Length; i++)
            {
                string folderName = splittedPath[i];
                if (folderName == ".")
                    continue;
                else if (folderName == "..")
                    if (findFolder.Parent is null)
                        return null;
                    else
                        findFolder = findFolder.Parent;
                else if (!findFolder._folders.ContainsKey(folderName))
                    return null;
                else
                {
                    findFolder = findFolder._folders[folderName];
                }
            }
            return findFolder;
        }

        public void AddFile(KartStorageFile file)
        {
            if (_files.ContainsKey(file.Name))
                throw new Exception($"File: {file.Name} is exist.");
            else
            {
                if (file._parentFolder is not null)
                    throw new Exception("The parent of a file you want to add is not null.");
                else
                {
                    if (_removedFiles.Contains(file))
                    {
                        if (file._sourceFile is RhoFile rhoFile && _sourceRhoFolder is not null)
                        {
                            _sourceRhoFolder.AddFile(rhoFile);
                        }
                        else if (file._sourceFile is Rho5File rho5File && _sourceRho5Folder is not null)
                        {
                            _sourceRho5Folder.AddFile(rho5File);
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
            }
        }

        public void AddFile(string path, KartStorageFile file)
        {
            string[] splittedPath = path.Split('/');
            KartStorageFolder findFolders = this;
            List<string> folderNameList = new List<string>();
            for (int i = 0; i < splittedPath.Length; i++)
            {
                string folderName = splittedPath[i];
                folderNameList.Add(folderName);
                if (!findFolders._folders.ContainsKey(folderName))
                    throw new Exception($"Folder: {string.Join('/', folderNameList.ToArray())} can not be found.");
                else
                {
                    findFolders = findFolders._folders[folderName];
                }
            }
            if (splittedPath.Length > 1)
            {
                string fileName = file.Name;
                if (!findFolders._files.ContainsKey(fileName))
                {
                    if (file.Parent is not null)
                    {
                        throw new Exception("The parent of adding file is in other folder.");
                    }
                    else
                    {
                        findFolders.AddFile(file);
                    }
                }
                else
                    throw new Exception($"File: {path}/{fileName} is exist.");
            }
            else
            {
                throw new Exception($"Path: {path} is invalid.");
            }
        }

        public void AddFolder(KartStorageFolder folder)
        {
            if (_folders.ContainsKey(folder.Name))
                throw new Exception($"Folder: {folder.Name} is exist.");
            else
            {
                if (folder._parent is not null)
                    throw new Exception("The parent of a folder you want to add is not null.");
                else
                {
                    if (_removedFolders.Contains(folder))
                    {
                        if (folder._sourceRhoFolder is not null && _sourceRhoFolder is not null)
                            _sourceRhoFolder.AddFolder(folder._sourceRhoFolder);
                        if (folder._sourceRho5Folder is not null && _sourceRho5Folder is not null)
                            _sourceRho5Folder.AddFolder(folder._sourceRho5Folder);
                        _removedFolders.Remove(folder);
                    }
                    else
                        _addedFolders.Add(folder);
                    _folders.Add(folder.Name, folder);
                    folder._parent = this;
                    folder._prevCounterInitialized = false;
                    folder.Name = folder._name;
                }
            }
        }

        public void AddFolder(string path, KartStorageFolder folder)
        {
            string[] splittedPath = path.Split('/');
            KartStorageFolder findFolders = this;
            List<string> folderNameList = new List<string>();
            for (int i = 0; i < splittedPath.Length; i++)
            {
                string folderName = splittedPath[i];
                folderNameList.Add(folderName);
                if (!findFolders._folders.ContainsKey(folderName))
                    throw new Exception($"Folder: {string.Join('/', folderNameList.ToArray())} can not be found.");
                else
                {
                    findFolders = findFolders._folders[folderName];
                }
            }
            if (splittedPath.Length > 1)
            {
                string folderName = folder.Name;
                if (!findFolders._folders.ContainsKey(folderName))
                    findFolders.AddFolder(folder);
                else
                    throw new Exception($"Folder: {path}/{folderName} is exist.");
            }
            else
            {
                throw new Exception($"Path: {path} is invalid.");
            }
        }

        public bool ContainsFolder(string path)
        {
            string[] splittedPath = path.Split('/');
            KartStorageFolder findFolder = this;
            for (int i = 0; i < splittedPath.Length; i++)
            {
                string folderName = splittedPath[i];
                if (findFolder._folders.ContainsKey(folderName))
                    findFolder = findFolder._folders[folderName];
                else
                    return false;
            }
            return splittedPath.Length > 0;
        }

        public bool ContainsFile(string path)
        {
            string[] splittedPath = path.Split('/');
            KartStorageFolder findFolder = this;
            for (int i = 0; i < splittedPath.Length - 1; i++)
            {
                string folderName = splittedPath[i];
                if (findFolder._folders.ContainsKey(folderName))
                    findFolder = findFolder._folders[folderName];
                else
                    return false;
            }
            if (splittedPath.Length > 0)
                return findFolder._files.ContainsKey(splittedPath[^1]);
            else
                return false;
        }

        public bool RemoveFile(string fileFullName)
        {
            string[] splittedPath = fileFullName.Split('/');
            KartStorageFolder findFolder = this;
            for (int i = 0; i < splittedPath.Length - 1; i++)
            {
                string folderName = splittedPath[i];
                if (findFolder._folders.ContainsKey(folderName))
                    findFolder = findFolder._folders[folderName];
                else
                    return false;
            }
            if (splittedPath.Length > 0 && findFolder._files.ContainsKey(splittedPath[^1]))
            {
                findFolder.RemoveFile(findFolder._files[splittedPath[^1]]);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveFile(KartStorageFile fileToDelete)
        {
            if (_files.ContainsKey(fileToDelete.Name) && _files[fileToDelete.Name] == fileToDelete)
            {
                if (_addedFiles.Contains(fileToDelete))
                    _addedFiles.Remove(fileToDelete);
                else
                {
                    if (fileToDelete._sourceFile is RhoFile rhoFile && _sourceRhoFolder is not null)
                        _sourceRhoFolder.RemoveFile(rhoFile);
                    else if (fileToDelete._sourceFile is Rho5File rho5File && _sourceRho5Folder is not null)
                        _sourceRho5Folder.RemoveFile(rho5File);
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
            string[] splittedPath = folderFullName.Split('/');
            KartStorageFolder findFolder = this;
            for (int i = 0; i < splittedPath.Length - 1; i++)
            {
                string folderName = splittedPath[i];
                if (findFolder._folders.ContainsKey(folderName))
                    findFolder = findFolder._folders[folderName];
                else
                    return false;
            }
            if (splittedPath.Length > 0 && findFolder._folders.ContainsKey(splittedPath[^1]))
            {
                findFolder.RemoveFolder(findFolder._folders[splittedPath[^1]]);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveFolder(KartStorageFolder folderToDelete)
        {
            if (_folders.ContainsKey(folderToDelete.Name) && _folders[folderToDelete.Name] == folderToDelete)
            {
                if (_addedFolders.Contains(folderToDelete))
                    _addedFolders.Remove(folderToDelete);
                else
                {
                    if (folderToDelete._sourceRhoFolder is not null && _sourceRhoFolder is not null)
                        _sourceRhoFolder.RemoveFolder(folderToDelete._sourceRhoFolder);
                    if (folderToDelete._sourceRho5Folder is not null && _sourceRho5Folder is not null)
                        _sourceRho5Folder.RemoveFolder(folderToDelete._sourceRho5Folder);
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
            foreach (KartStorageFolder folder in _folders.Values)
                RemoveFolder(folder);
            foreach (KartStorageFile file in _files.Values)
                RemoveFile(file);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                foreach (KartStorageFolder folder in _folders.Values)
                    folder.Dispose();
                foreach (KartStorageFile file in _files.Values)
                    file.Dispose();
                foreach (KartStorageFolder folder in _removedFolders)
                    folder.Dispose();
                foreach (KartStorageFile file in _removedFiles)
                    file.Dispose();
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
            return $"KartStorageFolder:{FullName}";
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
            if (file is KartStorageFile KartStorageFile)
                AddFile(KartStorageFile);
            else
                throw new Exception();
        }

        void IModifiableRhoFolder.AddFile(string path, IModifiableRhoFile file)
        {
            if (file is KartStorageFile KartStorageFile)
                AddFile(path, KartStorageFile);
            else
                throw new Exception();
        }

        void IModifiableRhoFolder.AddFolder(IModifiableRhoFolder folder)
        {
            if (folder is KartStorageFolder KartStorageFolder)
                AddFolder(KartStorageFolder);
            else
                throw new Exception();
        }

        void IModifiableRhoFolder.AddFolder(string path, IModifiableRhoFolder folder)
        {
            if (folder is KartStorageFolder KartStorageFolder)
                AddFolder(path, KartStorageFolder);
            else
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

        #endregion
    }
}