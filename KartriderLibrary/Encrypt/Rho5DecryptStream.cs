using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace KartLibrary.Encrypt
{
    public class Rho5DecryptStream : Stream
    {
        public Stream BaseStream { get; set; }
        private Rho5KeyProvider KeyProvider { get; }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => BaseStream.Length;

        public override long Position { get => bufStartPos + bufPos; set { BaseStream.Position = value; bufPos = bufStartPos = 64; } }

        private byte[] Buffer = new byte[64];

        private int bufStartPos = 0;

        private int bufferCount = 64;

        private int bufPos = 64;

        private bool Inited = false;

        public Rho5DecryptStream(Stream BaseStream, byte[] Key)
        {
            this.BaseStream = BaseStream;
            KeyProvider = new Rho5KeyProvider();
            KeyProvider.InitFromKey(Key);
            Inited = true;
            bufPos = bufStartPos = 64;
        }

        public Rho5DecryptStream(Stream BaseStream, string fileName, string anotherData)
        {
            this.BaseStream = BaseStream;
            KeyProvider = new Rho5KeyProvider();
            KeyProvider.InitHeaderKey(fileName, anotherData);
            Inited = true;
            bufPos = bufStartPos = 64;
        }

        public Rho5DecryptStream(Stream BaseStream)
        {
            this.BaseStream = BaseStream;
            KeyProvider = new Rho5KeyProvider();
            Inited = false;
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = count;
            int writePos = offset;
            while (readCount > 0)
            {
                if (bufPos >= bufferCount)
                {
                    bool result = refreshBuffer();
                    if (!result)
                        return count - readCount;
                }
                int copyLen = Math.Min(bufferCount - bufPos, readCount);
                Array.Copy(Buffer, bufPos, buffer, writePos, copyLen);
                bufPos += copyLen;
                writePos += copyLen;
                readCount -= copyLen;
            }
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            BaseStream.Seek(offset, origin);
            long newOffset = BaseStream.Position;
            bufferCount = 64;
            bufPos = 64;
            return newOffset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private unsafe bool refreshBuffer()
        {
            bufStartPos = (int)BaseStream.Position;
            int readLen = BaseStream.Read(Buffer, 0, 64);
            if (readLen <= 0)
                return false;
            int count = (readLen + 3) >> 2;
            fixed (byte* p = Buffer)
            {
                uint* ptr = (uint*)p;
                for (int i = 0; i < count; i++)
                {
                    uint sub_num = KeyProvider.GetNextSubNum();
                    ptr[i] -= sub_num;
                }
            }
            bufPos = 0;
            bufferCount = readLen;
            return true;
        }

        public void SetToHeaderKey(string fileName, string anotherData)
        {
            KeyProvider.InitHeaderKey(fileName, anotherData);
        }

        public void SetToFilesInfoKey(string fileName, string anotherData)
        {
            KeyProvider.InitFilesInfoKey(fileName, anotherData);
        }

        ~Rho5DecryptStream()
        {
            Buffer = null;
        }

    }
}