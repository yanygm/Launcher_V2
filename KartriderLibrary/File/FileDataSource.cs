using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public class FileDataSource : IDataSource
    {
        private string _fileName;
        private int _size;
        private bool _disposed;

        public bool Locked => false;

        public int Size => _size;

        public FileDataSource(string fileName)
        {
            if (!System.IO.File.Exists(fileName))
                throw new FileNotFoundException("file not found", fileName);
            _fileName = fileName;
            using (FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                _size = (int)tmpFileStream.Length;
            }
            _disposed = false;
        }

        public Stream CreateStream()
        {
            return new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void WriteTo(Stream stream)
        {
            using (FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                tmpFileStream.CopyTo(stream);
            }
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using (FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                await tmpFileStream.CopyToAsync(stream, cancellationToken);
            }
        }

        public void WriteTo(byte[] buffer, int offset, int count)
        {
            using (FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                tmpFileStream.Read(buffer, offset, count);
            }
        }

        public async Task WriteToAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            using (FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                await tmpFileStream.ReadAsync(buffer, offset, count, cancellationToken);
            }
        }

        public byte[] GetBytes()
        {
            using (FileStream tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] output = new byte[_size];
                tmpFileStream.Read(output);
                return output;
            }
        }

        public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default)
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
}