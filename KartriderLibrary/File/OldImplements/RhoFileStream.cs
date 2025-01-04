using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Encrypt;
using System.IO;
using Ionic.Zlib;

namespace KartLibrary.File
{
    public class RhoFileStream : Stream
    {
        private Rho _baseRho;
        private RhoFileInfo _baseFile;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _baseFile.FileSize;

        public override long Position { get; set; } = 0;

        private Stream _baseStream;

        private RhoDecryptStream _baseDecryptStream;

        private RhoDataInfo _baseBlockInfo;

        private RhoDataInfo _nextBlockInfo;

        public RhoFileStream(Rho rho, string path)
        {
            RhoFileInfo rhoFileInfo = rho.GetFile(path);
            if (rhoFileInfo == null)
                throw new FileNotFoundException($"File: {path} cannot be found in this rho file.", path);
            _baseFile = rhoFileInfo;
            _baseRho = rhoFileInfo.BaseRho;
            if (_baseRho == null)
                throw new InvalidOperationException("Rho has been disposed.");
            _baseBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex);
            _baseStream = _baseRho.baseStream;
            _baseStream.Seek(_baseBlockInfo.Offset, SeekOrigin.Begin);
            if (_baseBlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
            {
                _baseDecryptStream = new RhoDecryptStream(_baseRho.baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.KeepBasePosition);
                _nextBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex + 1);
            }
            else
            {
                if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.Compressed) != 0)
                {
                    _baseStream = new ZlibStream(_baseStream, CompressionMode.Decompress);
                }
                if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.FullEncrypted) != 0)
                {
                    _baseStream = new RhoDecryptStream(_baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.ResetBasePosition);
                }
            }
        }

        public RhoFileStream(RhoFileInfo rhoFileInfo)
        {
            if (rhoFileInfo == null)
                throw new ArgumentNullException("fileInfo is null.");
            _baseFile = rhoFileInfo;
            _baseRho = rhoFileInfo.BaseRho;
            if (_baseRho == null)
                throw new ArgumentException("The base rho file has been disposed.");
            _baseBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex);
            _baseStream = _baseRho.baseStream;
            _baseStream.Seek(_baseBlockInfo.Offset, SeekOrigin.Begin);
            if (_baseBlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
            {
                _baseDecryptStream = new RhoDecryptStream(_baseRho.baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.KeepBasePosition);
                _nextBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex + 1);
            }
            else
            {
                if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.Compressed) != 0)
                {
                    _baseStream = new ZlibStream(_baseStream, CompressionMode.Decompress);
                }
                if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.FullEncrypted) != 0)
                {
                    _baseStream = new RhoDecryptStream(_baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.ResetBasePosition);
                }
            }
        }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readLen = (int)Math.Min(count, Length - Position);
            if (_baseBlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
            {
                int _readCount = 0;
                if (Position < _baseBlockInfo.DataSize)
                {
                    int encryptLen = (int)Math.Min(_baseBlockInfo.DataSize - Position, readLen);
                    _readCount = _baseDecryptStream.Read(buffer, offset, encryptLen);
                    if (encryptLen < readLen)
                    {
                        if (_nextBlockInfo is null)
                            throw new Exception("next block is not found.");
                        _baseStream.Seek(_nextBlockInfo.Offset, SeekOrigin.Begin);
                        _readCount += _baseStream.Read(buffer, offset + _readCount, readLen - encryptLen);
                    }
                    Position += _readCount;
                }
                else
                {
                    if (_nextBlockInfo is null)
                        throw new Exception("next block is not found.");
                    _baseStream.Seek(_nextBlockInfo.Offset, SeekOrigin.Begin);
                    _readCount = _baseStream.Read(buffer, offset, readLen);
                }
                return _readCount;
            }
            return _baseStream.Read(buffer, offset, readLen);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}