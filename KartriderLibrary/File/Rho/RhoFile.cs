using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KartLibrary.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

namespace KartLibrary.File
{
    public class RhoFile : IRhoFile, IModifiableRhoFile
    {
        #region Members
        internal RhoFolder? _parentFolder;

        private string _name;
        private string _nameWithoutExt;
        private string _fullname;
        private uint? _extNum;
        private uint? _dataIndexBase;
        private RhoFileProperty _fileProperty;
        private IDataSource? _dataSource;

        private string _originalName;
        private IDataSource? _originalSource;

        private bool _disposed;
        #endregion

        #region Properties
        public RhoFolder? Parent => _parentFolder;

        IRhoFolder? IRhoFile.Parent => Parent;

        IModifiableRhoFolder? IModifiableRhoFile.Parent => Parent;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _extNum = null;
                _dataIndexBase = null;
                Regex fileNamePattern = new Regex(@"^(.*)\..*");
                Match match = fileNamePattern.Match(_name);
                if (match.Success)
                {
                    _nameWithoutExt = match.Groups[1].Value;
                }
                else
                {
                    _nameWithoutExt = _name;
                }
            }
        }

        public string FullName
        {
            get => Parent is not null ? $"{Parent.FullName}/{_name}" : _name;
        }

        public string NameWithoutExt => _nameWithoutExt;

        public int Size => _dataSource?.Size ?? 0;

        public IDataSource? DataSource
        {
            internal get => _dataSource;
            set => _dataSource = value;
        }

        public RhoFileProperty FileEncryptionProperty
        {
            get => _fileProperty;
            set => _fileProperty = value;
        }

        public bool HasDataSource => _dataSource is not null;

        internal bool IsModified => _originalName != _name || _originalSource != _dataSource;
        #endregion

        #region Constructors
        public RhoFile()
        {
            _parentFolder = null;
            _name = "";
            _nameWithoutExt = "";
            _fullname = "";
            _dataSource = null;
            _originalSource = null;
            _originalName = "";
        }
        #endregion

        #region Methods
        public Stream CreateStream()
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            return _dataSource.CreateStream();
        }

        public void WriteTo(Stream stream)
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            _dataSource.WriteTo(stream);
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            await _dataSource.WriteToAsync(stream, cancellationToken);
        }

        public void WriteTo(byte[] array, int offset, int count)
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            WriteTo(array, offset, count);
        }

        public async Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            await WriteToAsync(array, offset, count, cancellationToken);
        }

        public byte[] GetBytes()
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            return _dataSource.GetBytes();
        }

        public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default)
        {
            if (_dataSource is null)
                throw new InvalidOperationException("There are no any data source.");
            return await _dataSource.GetBytesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _parentFolder = null;
            _dataSource?.Dispose();
        }

        public override string ToString()
        {
            return $"RhoFile:{FullName}";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }
            }
        }

        internal uint getExtNum()
        {
            if (_extNum is null)
            {
                string[] spiltStrs = _name.Split('.');
                if (spiltStrs.Length > 0)
                {
                    string ext = spiltStrs[^1];
                    byte[] extEncData = Encoding.ASCII.GetBytes(ext);
                    byte[] extNumEncData = new byte[4];
                    Array.Copy(extEncData, extNumEncData, Math.Min(4, extEncData.Length));
                    _extNum = BitConverter.ToUInt32(extNumEncData);
                }
                else
                {
                    _extNum = 0;
                }
            }
            return _extNum.Value;
        }

        internal uint getDataIndex(uint folderDataIndex)
        {
            if (_dataIndexBase is null)
            {
                byte[] fileNameEncData = Encoding.Unicode.GetBytes(_nameWithoutExt);
                uint fileNameChksum = Adler.Adler32(0, fileNameEncData, 0, fileNameEncData.Length);
                uint extNum = getExtNum();
                _dataIndexBase = fileNameChksum + extNum;
            }
            if (folderDataIndex == 0xFFFFFFFFu)
                folderDataIndex = 0;
            return _dataIndexBase.Value + folderDataIndex;
        }

        internal void appliedChanges()
        {
            _originalName = _name;
            _originalSource = _dataSource;
        }
        #endregion
    }
}