using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KartLibrary.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;

namespace KartLibrary.File
{
    public class RhoFolder : IRhoFolder<RhoFolder, RhoFile>, IModifiableRhoFolder<RhoFolder, RhoFile>
    {
        #region Members
        private RhoFolder? _parent;
        private string _name;

        private Dictionary<string, RhoFile> _files;
        private Dictionary<string, RhoFolder> _folders;

        private uint _prevParentUpdatsCounter = 0xBAD_BEEFu;
        private uint _fullnameUpdatesCounter = 0x14325768u;
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
        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set
            {
                if (_isRootFolder)
                    throw new InvalidOperationException("cannot set the name of root folder.");
                _name = value;
                _folderDataIndex = null;
                Queue<RhoFolder> affectedFolders = new Queue<RhoFolder>();
                affectedFolders.Enqueue(this);
                while (affectedFolders.Count > 0)
                {
                    RhoFolder affectedFolder = affectedFolders.Dequeue();
                    affectedFolder._fullnameUpdatesCounter += 0x4B9AD755u;
                    foreach (RhoFolder folder in affectedFolder.Folders)
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

        public RhoFolder? Parent => _parent;

        IRhoFolder? IRhoFolder.Parent => _parent;

        IModifiableRhoFolder? IModifiableRhoFolder.Parent => _parent;

        public IReadOnlyCollection<RhoFile> Files => _files.Values;

        public IReadOnlyCollection<RhoFolder> Folders => _folders.Values;

        IReadOnlyCollection<IRhoFile> IRhoFolder.Files => Files;

        IReadOnlyCollection<IModifiableRhoFile> IModifiableRhoFolder.Files => Files;

        IReadOnlyCollection<IRhoFolder> IRhoFolder.Folders => Folders;

        IReadOnlyCollection<IModifiableRhoFolder> IModifiableRhoFolder.Folders => Folders;

        internal bool HasModified =>
            _addedFiles.Count > 0 ||
            _addedFolders.Count > 0 ||
            _removedFiles.Count > 0 ||
            _removedFolders.Count > 0 ||
            _originalName != _name ||
            checkIfFilesModified();
        #endregion

        #region Constructors
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

        internal RhoFolder(bool isRootFolder) : this()
        {
            _isRootFolder = true;
        }
        #endregion

        #region Methods
        public RhoFile? GetFile(string path)
        {
            string[] splittedPath = path.Split('/');
            RhoFolder findFolders = this;
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

        public RhoFolder? GetFolder(string path)
        {
            string[] splittedPath = path.Split('/');
            RhoFolder findFolder = this;
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

        public void AddFile(RhoFile file)
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
                        _removedFiles.Remove(file);
                    else
                        _addedFiles.Add(file);
                    _files.Add(file.Name, file);
                    file._parentFolder = this;
                }
            }
        }

        public void AddFile(string path, RhoFile file)
        {
            string[] splittedPath = path.Split('/');
            RhoFolder findFolders = this;
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

        public void AddFolder(RhoFolder folder)
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
                        _removedFolders.Remove(folder);
                    else
                        _addedFolders.Add(folder);
                    _folders.Add(folder.Name, folder);
                    folder._parent = this;
                    folder._prevCounterInitialized = false;
                    folder.Name = folder._name;
                }
            }

        }

        public void AddFolder(string path, RhoFolder folder)
        {
            string[] splittedPath = path.Split('/');
            RhoFolder findFolders = this;
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
            RhoFolder findFolder = this;
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
            RhoFolder findFolder = this;
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
            RhoFolder findFolder = this;
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

        public bool RemoveFile(RhoFile fileToDelete)
        {
            if (_files.ContainsKey(fileToDelete.Name) && _files[fileToDelete.Name] == fileToDelete)
            {
                if (_addedFiles.Contains(fileToDelete))
                    _addedFiles.Remove(fileToDelete);
                else
                    _removedFiles.Add(fileToDelete);
                _files.Remove(fileToDelete.Name);
                fileToDelete._parentFolder = null;
                return true;
            }
            return false;
        }

        public bool RemoveFolder(string folderFullName)
        {
            string[] splittedPath = folderFullName.Split('/');
            RhoFolder findFolder = this;
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

        public bool RemoveFolder(RhoFolder folderToDelete)
        {
            if (_folders.ContainsKey(folderToDelete.Name) && _folders[folderToDelete.Name] == folderToDelete)
            {
                if (_addedFolders.Contains(folderToDelete))
                    _addedFolders.Remove(folderToDelete);
                else
                    _removedFolders.Add(folderToDelete);
                _folders.Remove(folderToDelete.Name);
                folderToDelete._parent = null;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (RhoFolder folder in _folders.Values)
                _removedFolders.Add(folder);
            foreach (RhoFile file in _files.Values)
                _removedFiles.Add(file);
            _folders.Clear();
            _files.Clear();
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
                foreach (RhoFolder folder in _folders.Values)
                    folder.Dispose();
                foreach (RhoFile file in _files.Values)
                    file.Dispose();
                foreach (RhoFolder folder in _removedFolders)
                    folder.Dispose();
                foreach (RhoFile file in _removedFiles)
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
            return $"RhoFolder:{FullName}";
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
            if (file is RhoFile rhoFile)
                AddFile(rhoFile);
            else
                throw new Exception("");
        }

        void IModifiableRhoFolder.AddFile(string path, IModifiableRhoFile file)
        {
            if (file is RhoFile rhoFile)
                AddFile(path, rhoFile);
            else
                throw new Exception("");
        }

        void IModifiableRhoFolder.AddFolder(IModifiableRhoFolder folder)
        {
            if (folder is RhoFolder rhoFolder)
                AddFolder(rhoFolder);
            else
                throw new Exception("");
        }

        void IModifiableRhoFolder.AddFolder(string path, IModifiableRhoFolder folder)
        {
            if (folder is RhoFolder rhoFolder)
                AddFolder(path, rhoFolder);
            else
                throw new Exception("");
        }

        internal uint getFolderDataIndex()
        {
            if (_parent is not null && _prevParentUpdatsCounter != _parent._fullnameUpdatesCounter)
                _folderDataIndex = null;
            if (_folderDataIndex is null)
            {
                if (_name.Length == 0 && _parent is null)
                    return 0xFFFFFFFFu;
                string fullName = FullName;
                byte[] fullNameEncData = Encoding.Unicode.GetBytes(fullName);
                _folderDataIndex = Adler.Adler32(0, fullNameEncData, 0, fullNameEncData.Length);
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
            foreach (RhoFile file in _files.Values)
                if (file.IsModified)
                    return true;
            return false;
        }
        #endregion
    }
}