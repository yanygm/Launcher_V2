using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public class ByteArrayDataSource : IDataSource
    {
        private byte[] _arr;
        private bool _disposed;
        public bool Locked => throw new NotImplementedException();

        public int Size => _arr.Length;

        public ByteArrayDataSource(byte[] sourceArray)
        {
            _arr = sourceArray;
            _disposed = false;
        }

        public Stream CreateStream()
        {
            return new MemoryStream(_arr, false);
        }

        public void WriteTo(Stream stream)
        {
            if (!stream.CanWrite)
                throw new Exception("stream is not writeable.");
            stream.Write(_arr, 0, _arr.Length);
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanWrite)
                throw new Exception("stream is not writeable.");
            await stream.WriteAsync(_arr, 0, _arr.Length, cancellationToken);
        }

        public void WriteTo(byte[] buffer, int offset, int count)
        {
            if ((buffer.Length - offset) < count)
                throw new Exception("buffer size is less than count.");
            if (count > _arr.Length)
                throw new Exception("count is greater than array size.");
            Array.Copy(_arr, 0, buffer, offset, count);
        }

        public async Task WriteToAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if ((buffer.Length - offset) < count)
                throw new Exception("buffer size is less than count.");
            if (count > _arr.Length)
                throw new Exception("count is greater than array size.");
            Array.Copy(_arr, 0, buffer, offset, count);
        }

        public byte[] GetBytes()
        {
            byte[] output = new byte[_arr.Length];
            Array.Copy(_arr, output, _arr.Length);
            return output;
        }

        public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default)
        {
            byte[] output = new byte[_arr.Length];
            Array.Copy(_arr, output, _arr.Length);
            return output;
        }

        public void Dispose()
        {
            _arr = null;
            _disposed = true;
        }
    }
}