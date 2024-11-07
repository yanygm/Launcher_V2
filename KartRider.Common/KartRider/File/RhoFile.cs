using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KartLibrary.IO;

namespace KartLibrary.File;

public class RhoFile : IRhoFile, IDisposable, IModifiableRhoFile
{
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

    public RhoFolder? Parent => _parentFolder;

    IRhoFolder? IRhoFile.Parent => Parent;

    IModifiableRhoFolder? IModifiableRhoFile.Parent => Parent;

    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
            _extNum = null;
            _dataIndexBase = null;
            Match match = new Regex("^(.*)\\..*").Match(_name);
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
        get
        {
            if (Parent == null)
            {
                return _name;
            }

            return Parent.FullName + "/" + _name;
        }
    }

    public string NameWithoutExt => _nameWithoutExt;

    public int Size => _dataSource?.Size ?? 0;

    public IDataSource? DataSource
    {
        internal get
        {
            return _dataSource;
        }
        set
        {
            _dataSource = value;
        }
    }

    public RhoFileProperty FileEncryptionProperty
    {
        get
        {
            return _fileProperty;
        }
        set
        {
            _fileProperty = value;
        }
    }

    public bool HasDataSource => _dataSource != null;

    internal bool IsModified
    {
        get
        {
            if (!(_originalName != _name))
            {
                return _originalSource != _dataSource;
            }

            return true;
        }
    }

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

    public Stream CreateStream()
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        return _dataSource.CreateStream();
    }

    public void WriteTo(Stream stream)
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        _dataSource.WriteTo(stream);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        await _dataSource.WriteToAsync(stream, cancellationToken);
    }

    public void WriteTo(byte[] array, int offset, int count)
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        WriteTo(array, offset, count);
    }

    public async Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        await WriteToAsync(array, offset, count, cancellationToken);
    }

    public byte[] GetBytes()
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        return _dataSource.GetBytes();
    }

    public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        return await _dataSource.GetBytesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _parentFolder = null;
        _dataSource?.Dispose();
    }

    public override string ToString()
    {
        return "RhoFile:" + FullName;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
        }
    }

    internal uint getExtNum()
    {
        uint? extNum = _extNum;
        if (!extNum.HasValue)
        {
            string[] array = _name.Split('.');
            if (array.Length != 0)
            {
                string s = array[^1];
                byte[] bytes = Encoding.ASCII.GetBytes(s);
                byte[] array2 = new byte[4];
                Array.Copy(bytes, array2, Math.Min(4, bytes.Length));
                _extNum = BitConverter.ToUInt32(array2);
            }
            else
            {
                _extNum = 0u;
            }
        }

        return _extNum.Value;
    }

    internal uint getDataIndex(uint folderDataIndex)
    {
        uint? dataIndexBase = _dataIndexBase;
        if (!dataIndexBase.HasValue)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(_nameWithoutExt);
            uint num = Adler.Adler32(0u, bytes, 0, bytes.Length);
            uint extNum = getExtNum();
            _dataIndexBase = num + extNum;
        }

        if (folderDataIndex == uint.MaxValue)
        {
            folderDataIndex = 0u;
        }

        return _dataIndexBase.Value + folderDataIndex;
    }

    internal void appliedChanges()
    {
        _originalName = _name;
        _originalSource = _dataSource;
    }
}