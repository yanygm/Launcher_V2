using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public class ByteArrayDataSource : IDataSource, IDisposable
{
    private byte[] _arr;

    private bool _disposed;

    public bool Locked
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public int Size => _arr.Length;

    public ByteArrayDataSource(byte[] sourceArray)
    {
        _arr = sourceArray;
        _disposed = false;
    }

    public Stream CreateStream()
    {
        return new MemoryStream(_arr, writable: false);
    }

    public void WriteTo(Stream stream)
    {
        if (!stream.CanWrite)
        {
            throw new Exception("stream is not writeable.");
        }

        stream.Write(_arr, 0, _arr.Length);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!stream.CanWrite)
        {
            throw new Exception("stream is not writeable.");
        }

        await stream.WriteAsync(_arr, 0, _arr.Length, cancellationToken);
    }

    public void WriteTo(byte[] buffer, int offset, int count)
    {
        if (buffer.Length - offset < count)
        {
            throw new Exception("buffer size is less than count.");
        }

        if (count > _arr.Length)
        {
            throw new Exception("count is greater than array size.");
        }

        Array.Copy(_arr, 0, buffer, offset, count);
    }

    public async Task WriteToAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (buffer.Length - offset < count)
        {
            throw new Exception("buffer size is less than count.");
        }

        if (count > _arr.Length)
        {
            throw new Exception("count is greater than array size.");
        }

        Array.Copy(_arr, 0, buffer, offset, count);
    }

    public byte[] GetBytes()
    {
        byte[] array = new byte[_arr.Length];
        Array.Copy(_arr, array, _arr.Length);
        return array;
    }

    public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        byte[] array = new byte[_arr.Length];
        Array.Copy(_arr, array, _arr.Length);
        return array;
    }

    public void Dispose()
    {
        _arr = null;
        _disposed = true;
    }
}