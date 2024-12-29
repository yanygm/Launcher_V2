using System;
using System.IO;

namespace KartLibrary.Encrypt;

public class Rho5EncryptStream : Stream
{
    private byte[] _encryptBuffer = new byte[64];

    private byte[] _dataBuffer = new byte[64];

    private byte[] _outDataBuffer = new byte[64];

    private int _bufStartBase;

    private int _bufFlushPos;

    private int _bufferSize = 64;

    private uint _partialEncNum;

    private int _bufPos = 64;

    private bool _inited;

    public Stream BaseStream { get; set; }

    private Rho5KeyProvider KeyProvider { get; }

    public override bool CanRead => false;

    public override bool CanSeek => BaseStream.CanSeek;

    public override bool CanWrite => BaseStream.CanRead;

    public override long Length => BaseStream.Length + (_bufPos - _bufFlushPos);

    public override long Position
    {
        get
        {
            return _bufStartBase + _bufPos;
        }
        set
        {
            BaseStream.Position = value;
            _bufPos = (_bufStartBase = 64);
        }
    }

    public Rho5EncryptStream(Stream BaseStream, byte[] Key)
    {
        this.BaseStream = BaseStream;
        KeyProvider = new Rho5KeyProvider();
        KeyProvider.InitFromKey(Key);
        _inited = true;
        _bufFlushPos = _bufferSize;
        _bufPos = _bufferSize;
        _bufStartBase = 0;
    }

    public Rho5EncryptStream(Stream BaseStream, string fileName, string mixingData)
    {
        this.BaseStream = BaseStream;
        KeyProvider = new Rho5KeyProvider();
        KeyProvider.InitHeaderKey(fileName, mixingData);
        _inited = true;
        _bufFlushPos = _bufferSize;
        _bufPos = _bufferSize;
        _bufStartBase = 0;
    }

    public Rho5EncryptStream(Stream BaseStream)
    {
        this.BaseStream = BaseStream;
        KeyProvider = new Rho5KeyProvider();
        _inited = false;
    }

    public override void Flush()
    {
        flushDataBuffer();
        BaseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        flushDataBuffer();
        BaseStream.Seek(offset, origin);
        long position = BaseStream.Position;
        _bufPos = 0;
        _bufFlushPos = 0;
        _bufStartBase = (int)position;
        return position;
    }

    public override void SetLength(long value)
    {
        BaseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (offset + count > buffer.Length)
        {
            throw new ArgumentException("");
        }

        int num = count;
        int num2 = offset;
        while (num > 0)
        {
            if (_bufPos >= _bufferSize)
            {
                flushDataBuffer();
            }

            int num3 = Math.Min(num, _bufferSize - _bufPos);
            Array.Copy(buffer, num2, _dataBuffer, _bufPos, num3);
            _bufPos += num3;
            num -= num3;
            num2 += num3;
        }
    }

    private unsafe void updatesEncryptBuffer()
    {
        fixed (byte* ptr = _encryptBuffer)
        {
            uint* ptr2 = (uint*)ptr;
            for (int i = 0; i < _bufferSize >> 2; i++)
            {
                ptr2[i] = KeyProvider.GetNextSubNum();
            }
        }
    }

    private unsafe void flushDataBuffer()
    {
        int num = _bufFlushPos >> 2;
        int num2 = _bufPos + 3 >> 2;
        int count = _bufPos - _bufFlushPos;
        fixed (byte* ptr = _dataBuffer)
        {
            fixed (byte* ptr3 = _encryptBuffer)
            {
                fixed (byte* ptr5 = _outDataBuffer)
                {
                    uint* ptr2 = (uint*)ptr;
                    uint* ptr4 = (uint*)ptr3;
                    uint* ptr6 = (uint*)ptr5;
                    if (((uint)_bufPos & 3u) != 0)
                    {
                        ptr2[num2 - 1] &= (uint)(-1 >>> (4 - (_bufPos & 3) << 3));
                    }

                    for (int i = num; i < num2; i++)
                    {
                        ptr6[i] = ptr2[i] + ptr4[i];
                    }

                    BaseStream.Write(_outDataBuffer, _bufFlushPos, count);
                }
            }
        }

        if (_bufPos >= _bufferSize)
        {
            updatesEncryptBuffer();
            _bufPos = 0;
            _bufFlushPos = 0;
            _bufStartBase = (int)BaseStream.Length;
        }
        else
        {
            _bufFlushPos = _bufPos;
        }
    }

    public void SetToHeaderKey(string fileName, string mixingData)
    {
        KeyProvider.InitHeaderKey(fileName, mixingData);
        _bufFlushPos = _bufferSize;
        _bufPos = _bufferSize;
    }

    public void SetToFilesInfoKey(string fileName, string mixingData)
    {
        KeyProvider.InitFilesInfoKey(fileName, mixingData);
        _bufFlushPos = _bufferSize;
        _bufPos = _bufferSize;
    }

    public void SetKey(byte[] key)
    {
        KeyProvider.InitFromKey(key);
        _bufFlushPos = _bufferSize;
        _bufPos = _bufferSize;
    }

    ~Rho5EncryptStream()
    {
        _encryptBuffer = null;
    }
}