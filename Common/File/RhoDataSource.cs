using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public class RhoDataSource : IDataSource, IDisposable
{
    private bool _disposed;

    private RhoFileHandler _fileHandler;

    public bool Locked => false;

    public int Size => _fileHandler._size;

    internal RhoDataSource(RhoFileHandler fileHandler)
    {
        _disposed = false;
        _fileHandler = fileHandler;
    }

    public Stream CreateStream()
    {
        return new MemoryStream(_fileHandler.getData(), writable: false);
    }

    public void WriteTo(Stream stream)
    {
        if (!stream.CanWrite)
        {
            throw new Exception("This stream is not writeable");
        }

        byte[] data = _fileHandler.getData();
        stream.Write(data, 0, data.Length);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!stream.CanWrite)
        {
            throw new Exception("This stream is not writeable");
        }

        byte[] data = _fileHandler.getData();
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
    }

    public void WriteTo(byte[] buffer, int offset, int count)
    {
        if (buffer.Length - offset < count)
        {
            throw new IndexOutOfRangeException("given buffer is not enough to store the required data.");
        }

        if (count > _fileHandler._size)
        {
            throw new IndexOutOfRangeException("size is greater than file.");
        }

        Array.Copy(_fileHandler.getData(), 0, buffer, offset, count);
    }

    public async Task WriteToAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (buffer.Length - offset < count)
        {
            throw new IndexOutOfRangeException("given buffer is not enough to store the required data.");
        }

        if (count > _fileHandler._size)
        {
            throw new IndexOutOfRangeException("size is greater than file.");
        }

        Array.Copy(_fileHandler.getData(), 0, buffer, offset, count);
    }

    public byte[] GetBytes()
    {
        return _fileHandler.getData();
    }

    public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return _fileHandler.getData();
    }

    public void Dispose()
    {
        _disposed = true;
    }
}