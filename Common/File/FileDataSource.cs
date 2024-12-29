using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public class FileDataSource : IDataSource, IDisposable
{
    private string _fileName;

    private int _size;

    private bool _disposed;

    public bool Locked => false;

    public int Size => _size;

    public FileDataSource(string fileName)
    {
        if (!System.IO.File.Exists(fileName))
        {
            throw new FileNotFoundException("file not found", fileName);
        }

        _fileName = fileName;
        using (FileStream fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
        {
            _size = (int)fileStream.Length;
        }

        _disposed = false;
    }

    public Stream CreateStream()
    {
        return new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public void WriteTo(Stream stream)
    {
        using FileStream fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
        fileStream.CopyTo(stream);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
    {
        using FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
        await tmpFileStream.CopyToAsync(stream, cancellationToken);
    }

    public void WriteTo(byte[] buffer, int offset, int count)
    {
        using FileStream fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
        fileStream.Read(buffer, offset, count);
    }

    public async Task WriteToAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
    {
        using FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
        await tmpFileStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public byte[] GetBytes()
    {
        using FileStream fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
        byte[] array = new byte[_size];
        fileStream.Read(array);
        return array;
    }

    public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        byte[] output = new byte[_size];
        await WriteToAsync(output, 0, output.Length);
        return output;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}