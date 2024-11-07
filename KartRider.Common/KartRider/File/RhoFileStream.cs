using System;
using System.IO;
using System.IO.Compression;
using KartLibrary.Encrypt;

namespace KartLibrary.File;

public class RhoFileStream : Stream
{
    private Rho _baseRho;

    private RhoFileInfo _baseFile;

    private Stream _baseStream;

    private RhoDecryptStream _baseDecryptStream;

    private RhoDataInfo _baseBlockInfo;

    private RhoDataInfo _nextBlockInfo;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _baseFile.FileSize;

    public override long Position { get; set; }

    public RhoFileStream(Rho rho, string path)
    {
        RhoFileInfo file = rho.GetFile(path);
        if (file == null)
        {
            throw new FileNotFoundException("File: " + path + " cannot be found in this rho file.", path);
        }

        _baseFile = file;
        _baseRho = file.BaseRho;
        if (_baseRho == null)
        {
            throw new InvalidOperationException("Rho has been disposed.");
        }

        _baseBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex);
        _baseStream = _baseRho.baseStream;
        _baseStream.Seek(_baseBlockInfo.Offset, SeekOrigin.Begin);
        if (_baseBlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
        {
            _baseDecryptStream = new RhoDecryptStream(_baseRho.baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.KeepBasePosition);
            _nextBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex + 1);
            return;
        }

        if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.Compressed) != 0)
        {
            _baseStream = new ZLibStream(_baseStream, CompressionMode.Decompress);
        }

        if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.FullEncrypted) != 0)
        {
            _baseStream = new RhoDecryptStream(_baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.ResetBasePosition);
        }
    }

    public RhoFileStream(RhoFileInfo rhoFileInfo)
    {
        if (rhoFileInfo == null)
        {
            throw new ArgumentNullException("fileInfo is null.");
        }

        _baseFile = rhoFileInfo;
        _baseRho = rhoFileInfo.BaseRho;
        if (_baseRho == null)
        {
            throw new ArgumentException("The base rho file has been disposed.");
        }

        _baseBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex);
        _baseStream = _baseRho.baseStream;
        _baseStream.Seek(_baseBlockInfo.Offset, SeekOrigin.Begin);
        if (_baseBlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
        {
            _baseDecryptStream = new RhoDecryptStream(_baseRho.baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.KeepBasePosition);
            _nextBlockInfo = _baseRho.GetBlockInfo(_baseFile.FileBlockIndex + 1);
            return;
        }

        if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.Compressed) != 0)
        {
            _baseStream = new ZLibStream(_baseStream, CompressionMode.Decompress);
        }

        if ((_baseBlockInfo.BlockProperty & RhoBlockProperty.FullEncrypted) != 0)
        {
            _baseStream = new RhoDecryptStream(_baseStream, RhoKey.GetDataKey(_baseRho.GetFileKey(), _baseFile), DecryptStreamSeekMode.ResetBasePosition);
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int num = (int)Math.Min(count, Length - Position);
        if (_baseBlockInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
        {
            int num2 = 0;
            if (Position < _baseBlockInfo.DataSize)
            {
                int num3 = (int)Math.Min(_baseBlockInfo.DataSize - Position, num);
                num2 = _baseDecryptStream.Read(buffer, offset, num3);
                if (num3 < num)
                {
                    if (_nextBlockInfo == null)
                    {
                        throw new Exception("next block is not found.");
                    }

                    _baseStream.Seek(_nextBlockInfo.Offset, SeekOrigin.Begin);
                    num2 += _baseStream.Read(buffer, offset + num2, num - num3);
                }

                Position += num2;
            }
            else
            {
                if (_nextBlockInfo == null)
                {
                    throw new Exception("next block is not found.");
                }

                _baseStream.Seek(_nextBlockInfo.Offset, SeekOrigin.Begin);
                num2 = _baseStream.Read(buffer, offset, num);
            }

            return num2;
        }

        return _baseStream.Read(buffer, offset, num);
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