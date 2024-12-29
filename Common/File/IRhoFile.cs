using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public interface IRhoFile : IDisposable
{
    IRhoFolder? Parent { get; }

    string Name { get; }

    string FullName { get; }

    int Size { get; }

    bool HasDataSource { get; }

    Stream CreateStream();

    void WriteTo(Stream stream);

    Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

    void WriteTo(byte[] array, int offset, int count);

    Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default(CancellationToken));

    byte[] GetBytes();

    Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default(CancellationToken));
}