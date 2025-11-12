namespace Launcher.Library.File
{
    public interface IRhoFile : IDisposable
    {
        IRhoFolder? Parent { get; }

        string Name { get; }

        string FullName { get; }

        int Size { get; }

        bool HasDataSource { get; }

        Stream CreateStream();

        void WriteTo(Stream stream);

        Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default);

        void WriteTo(byte[] array, int offset, int count);

        Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default);

        byte[] GetBytes();

        Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default);
    }
}