using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;

namespace KartLibrary.Encrypt
{
    public class Rho5EncryptStream : Stream
    {
        public Stream BaseStream { get; set; }
        private Rho5KeyProvider KeyProvider { get; }

        public override bool CanRead => false;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanRead;

        public override long Length => BaseStream.Length + (_bufPos - _bufFlushPos);

        public override long Position { get => _bufStartBase + _bufPos; set { BaseStream.Position = value; _bufPos = _bufStartBase = 64; } }

        private byte[] _encryptBuffer = new byte[64];

        private byte[] _dataBuffer = new byte[64];

        private byte[] _outDataBuffer = new byte[64];

        private int _bufStartBase = 0;

        private int _bufFlushPos = 0;

        private int _bufferSize = 64;

        private uint _partialEncNum = 0;

        private int _bufPos = 64;

        private bool _inited = false;

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
            long newOffset = BaseStream.Position;
            _bufPos = 0;
            _bufFlushPos = 0;
            _bufStartBase = (int)newOffset;
            return newOffset;
        }

        public override void SetLength(long value)
        {
            //if(value < BaseStream.Length)
            //    BaseStream.SetLength(value);
            //else if(value > BaseStream.Length)
            //{
            //    BaseStream.Seek(0, SeekOrigin.End);
            //    int fillLen = (int)(value - Length);
            //    ulong rndNum = (ulong)Environment.TickCount64;
            //    while(fillLen >= 0x08)
            //    {
            //        rndNum += 0xD751EBEB91;
            //        rndNum = BitOperations.RotateRight(rndNum, (int)(rndNum & 0x3F));
            //        rndNum *= 0x63E6E248A1;
            //        Write(BitConverter.GetBytes(rndNum));
            //        fillLen -= 0x08;
            //    }
            //    while (fillLen > 0)
            //    {
            //        rndNum += 0xD7194B89D7;
            //        rndNum = BitOperations.RotateLeft(rndNum, (int)(rndNum & 0x3F));
            //        rndNum *= 0x59799BF0CF;
            //        Write(BitConverter.GetBytes(rndNum), (int)(rndNum & 7), 1);
            //        fillLen -= 0x1;
            //    }
            //    Flush();
            //}
            BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if ((offset + count) > buffer.Length)
                throw new ArgumentException("");
            int writeCount = count;
            int bufferPos = offset;
            while (writeCount > 0)
            {
                if (_bufPos >= _bufferSize)
                {
                    flushDataBuffer();
                }
                int copyLen = Math.Min(writeCount, _bufferSize - _bufPos);
                Array.Copy(buffer, bufferPos, _dataBuffer, _bufPos, copyLen);
                _bufPos += copyLen;
                writeCount -= copyLen;
                bufferPos += copyLen;
            }
        }

        private unsafe void updatesEncryptBuffer()
        {
            fixed (byte* ptr = _encryptBuffer)
            {
                uint* uPtr = (uint*)ptr;
                for (int i = 0; i < (_bufferSize >> 2); i++)
                {
                    uPtr[i] = KeyProvider.GetNextSubNum();
                }
            }
        }

        private unsafe void flushDataBuffer()
        {
            int encBegin = (_bufFlushPos) >> 2;
            int encEnd = (_bufPos + 0x03) >> 2;
            int count = _bufPos - _bufFlushPos;
            fixed (byte* inPtr = _dataBuffer, encPtr = _encryptBuffer, outPtr = _outDataBuffer)
            {
                uint* inUPtr = (uint*)inPtr;
                uint* encUPtr = (uint*)encPtr;
                uint* outUPtr = (uint*)outPtr;
                if ((_bufPos & 0x03) != 0)
                    inUPtr[encEnd - 1] = inUPtr[encEnd - 1] & (0xFFFFFFFF >> ((4 - (_bufPos & 0x03)) << 3));
                for (int i = encBegin; i < encEnd; i++)
                {
                    outUPtr[i] = inUPtr[i] + encUPtr[i];
                }
                BaseStream.Write(_outDataBuffer, _bufFlushPos, count);
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
}