using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public class KartStorageFile : IRhoFile, IDisposable, IModifiableRhoFile
{
    internal KartStorageFolder? _parentFolder;

    internal IModifiableRhoFile? _sourceFile;

    private string _name;

    private string _nameWithoutExt;

    private string _fullname;

    private IDataSource? _dataSource;

    private bool _disposed;

    public KartStorageFolder? Parent => _parentFolder;

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
            if (_sourceFile != null)
            {
                _sourceFile.Name = value;
            }

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

    public int Size
    {
        get
        {
            if (_sourceFile == null)
            {
                return _dataSource?.Size ?? 0;
            }

            return _sourceFile.Size;
        }
    }

    public bool HasDataSource
    {
        get
        {
            if (_sourceFile == null)
            {
                return _dataSource != null;
            }

            return _sourceFile.HasDataSource;
        }
    }

    public IDataSource? DataSource
    {
        set
        {
            if (_sourceFile != null)
            {
                _sourceFile.DataSource = value;
            }

            _dataSource = value;
        }
    }

    public KartStorageFile()
    {
        _parentFolder = null;
        _name = "";
        _fullname = "";
        _nameWithoutExt = "";
        _dataSource = null;
        _sourceFile = null;
        _disposed = false;
    }

    internal KartStorageFile(IModifiableRhoFile sourceFile)
        : this()
    {
        _sourceFile = sourceFile;
        Name = sourceFile.Name;
    }

    public Stream CreateStream()
    {
        if (_sourceFile != null)
        {
            return _sourceFile.CreateStream();
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        return _dataSource.CreateStream();
    }

    public void WriteTo(Stream stream)
    {
        if (_sourceFile != null)
        {
            _sourceFile.WriteTo(stream);
            return;
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        _dataSource.WriteTo(stream);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_sourceFile != null)
        {
            await _sourceFile.WriteToAsync(stream, cancellationToken);
            return;
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        await _dataSource.WriteToAsync(stream, cancellationToken);
    }

    public void WriteTo(byte[] array, int offset, int count)
    {
        if (_sourceFile != null)
        {
            _sourceFile.WriteTo(array, offset, count);
            return;
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        _dataSource.WriteTo(array, offset, count);
    }

    public async Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_sourceFile != null)
        {
            await _sourceFile.WriteToAsync(array, offset, count, cancellationToken);
            return;
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        await _dataSource.WriteToAsync(array, offset, count, cancellationToken);
    }

    public byte[] GetBytes()
    {
        if (_sourceFile != null)
        {
            return _sourceFile.GetBytes();
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        return _dataSource.GetBytes();
    }

    public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_sourceFile != null)
        {
            return await _sourceFile.GetBytesAsync(cancellationToken);
        }

        if (_dataSource == null)
        {
            throw new InvalidOperationException("There are no any data source.");
        }

        return await _dataSource.GetBytesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _parentFolder = null;
        if (_sourceFile == null)
        {
            _dataSource?.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
        }
    }

    public override string ToString()
    {
        return "KartStorageFile:" + FullName;
    }
}