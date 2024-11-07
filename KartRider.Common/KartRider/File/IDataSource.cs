using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public interface IDataSource : IDisposable
{
    bool Locked { get; }

    int Size { get; }

    Stream CreateStream();

    void WriteTo(Stream stream);

    Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

    void WriteTo(byte[] array, int offset, int count);

    Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default(CancellationToken));

    byte[] GetBytes();

    Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken));
}