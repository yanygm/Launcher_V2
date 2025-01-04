using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.IO;

namespace KartLibrary.Encrypt
{
    public class RhoDecryptStream : Stream
    {
        public Stream BaseStream { get; init; }
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => BaseStream.Length;

        public DecryptStreamSeekMode SeekMode { get; set; }

        private long _basePosition = 0;

        public long BasePosition
        {
            get => _basePosition;

            set
            {
                if (SeekMode == DecryptStreamSeekMode.KeepBasePosition)
                    _basePosition = value;
            }
        }

        private long _position = 0;

        public override long Position
        {
            get => _position + bufferRead;
            set
            {
                BaseStream.Position = _position = value;
                bufferLength = bufferLength = 64;
            }
        }

        private byte[] extendedKey = new byte[0];

        private byte[] buffer = new byte[64];

        private int bufferLength = 0;

        private int bufferRead = 0;

        public RhoDecryptStream(Stream baseStream, uint Key, DecryptStreamSeekMode seekMode)
        {
            if (!baseStream.CanRead)
                throw new ArgumentException("baseStream is not a readable stream.");
            extendedKey = RhoKey.ExtendKey(Key);
            SeekMode = seekMode;
            BaseStream = baseStream;
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override unsafe int Read(byte[] writeArr, int offset, int count)
        {
            int readLen = Math.Min(count, (int)(this.Length - this.Position));
            if (readLen >= writeArr.Length)
                throw new IndexOutOfRangeException();
            fixed (byte* writePtr = &writeArr[offset], bufPtr = buffer)
            {
                if (readLen < 0)
                    throw new EndOfStreamException();
                if (Sse2.IsSupported)
                {
                    int writePos = 0, reqCpy = readLen;
                    while (reqCpy > 0)
                    {
                        if (bufferRead >= bufferLength)
                            updateBuffer();
                        int cpyLen = Math.Min(Math.Min(bufferLength - bufferRead, reqCpy), 16);
                        if (reqCpy < 16)
                        {
                            for (int i = 0; i < cpyLen; i++)
                                writePtr[writePos + i] = buffer[bufferRead + i];
                            writePos += cpyLen;
                            reqCpy -= cpyLen;
                        }
                        else
                        {
                            int bIndex = bufferRead & ~(0xF);
                            int nIndex = bufferRead & 0xF;
                            Vector128<byte> bufVec = Sse2.LoadVector128(bufPtr + bIndex);
                            if (nIndex != 0)
                                bufVec = Sse2.ShiftRightLogical128BitLane(bufVec, (byte)nIndex);
                            Sse2.Store(writePtr + writePos, bufVec);
                            writePos += cpyLen;
                            bufferRead += cpyLen;
                            reqCpy -= cpyLen;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < readLen; i++)
                    {
                        if (bufferRead >= bufferLength)
                            updateBuffer();
                        writePtr[i] = buffer[bufferRead++];
                    }
                }
            }
            return readLen;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (SeekMode == DecryptStreamSeekMode.KeepBasePosition)
            {
                long newOffset = 0, bufferPosition = 0;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        newOffset = offset;
                        break;
                    case SeekOrigin.Current:
                        newOffset = this.Position + offset;
                        break;
                    case SeekOrigin.End:
                        newOffset = this.Length + offset;
                        break;
                }
                if (newOffset < BasePosition)
                    throw new ArgumentOutOfRangeException("New offset is smaller than base position.");
                bufferPosition = offset - ((offset - BasePosition) & 63);
                BaseStream.Seek(bufferPosition, SeekOrigin.Begin);
                updateBuffer();
                bufferRead = (int)(bufferPosition - offset);
            }
            else if (SeekMode == DecryptStreamSeekMode.ResetBasePosition)
            {
                BaseStream.Seek(offset, origin);
                updateBuffer();
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void SetBasePosition(long basePos)
        {
            BasePosition = basePos;
        }

        private unsafe void updateBuffer()
        {
            bufferLength = (int)Math.Min(64, BaseStream.Length - BaseStream.Position);
            _position = BaseStream.Position;
            BaseStream.Read(buffer, 0, bufferLength);
            if (Avx2.IsSupported)
            {
                fixed (byte* keyPtr = extendedKey, bufPtr = buffer)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector256<byte> keyVec = Avx.LoadVector256(keyPtr + (i << 5));
                        Vector256<byte> bufVec = Avx.LoadVector256(bufPtr + (i << 5));
                        bufVec = Avx2.Xor(bufVec, keyVec);
                        Avx.Store(bufPtr + (i << 5), bufVec);
                    }
                }
            }
            else if (Sse2.IsSupported)
            {
                fixed (byte* keyPtr = extendedKey, bufPtr = buffer)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector128<byte> keyVec = Sse2.LoadVector128(keyPtr + (i << 4));
                        Vector128<byte> bufVec = Sse2.LoadVector128(bufPtr + (i << 4));
                        bufVec = Sse2.Xor(bufVec, keyVec);
                        Sse2.Store(bufPtr + (i << 4), bufVec);
                    }
                }
            }
            else
            {
                for (int i = 0; i < bufferLength; i++)
                {
                    buffer[i] ^= extendedKey[i];
                }
            }
            bufferRead = 0;
        }
    }
}