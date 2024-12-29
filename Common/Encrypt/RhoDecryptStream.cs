using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace KartLibrary.Encrypt;

public class RhoDecryptStream : Stream
{
    private long _basePosition;

    private long _position;

    private byte[] extendedKey = new byte[0];

    private byte[] buffer = new byte[64];

    private int bufferLength;

    private int bufferRead;

    public Stream BaseStream { get; init; }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => BaseStream.Length;

    public DecryptStreamSeekMode SeekMode { get; set; }

    public long BasePosition
    {
        get
        {
            return _basePosition;
        }
        set
        {
            if (SeekMode == DecryptStreamSeekMode.KeepBasePosition)
            {
                _basePosition = value;
            }
        }
    }

    public override long Position
    {
        get
        {
            return _position + bufferRead;
        }
        set
        {
            BaseStream.Position = (_position = value);
            bufferLength = (bufferLength = 64);
        }
    }

    public RhoDecryptStream(Stream baseStream, uint Key, DecryptStreamSeekMode seekMode)
    {
        if (!baseStream.CanRead)
        {
            throw new ArgumentException("baseStream is not a readable stream.");
        }

        extendedKey = RhoKey.ExtendKey(Key);
        SeekMode = seekMode;
        BaseStream = baseStream;
    }

    public override void Flush()
    {
        BaseStream.Flush();
    }

    public unsafe override int Read(byte[] writeArr, int offset, int count)
    {
        int num = Math.Min(count, (int)(Length - Position));
        if (num >= writeArr.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &writeArr[offset])
        {
            fixed (byte* ptr2 = buffer)
            {
                if (num < 0)
                {
                    throw new EndOfStreamException();
                }

                if (Sse2.IsSupported)
                {
                    int num2 = 0;
                    int num3 = num;
                    while (num3 > 0)
                    {
                        if (bufferRead >= bufferLength)
                        {
                            updateBuffer();
                        }

                        int num4 = Math.Min(Math.Min(bufferLength - bufferRead, num3), 16);
                        if (num3 < 16)
                        {
                            for (int i = 0; i < num4; i++)
                            {
                                ptr[num2 + i] = buffer[bufferRead + i];
                            }

                            num2 += num4;
                            num3 -= num4;
                            continue;
                        }

                        int num5 = bufferRead & -16;
                        int num6 = bufferRead & 0xF;
                        Vector128<byte> vector = Sse2.LoadVector128(ptr2 + num5);
                        if (num6 != 0)
                        {
                            vector = Sse2.ShiftRightLogical128BitLane(vector, (byte)num6);
                        }

                        Sse2.Store(ptr + num2, vector);
                        num2 += num4;
                        bufferRead += num4;
                        num3 -= num4;
                    }
                }
                else
                {
                    for (int j = 0; j < num; j++)
                    {
                        if (bufferRead >= bufferLength)
                        {
                            updateBuffer();
                        }

                        ptr[j] = buffer[bufferRead++];
                    }
                }
            }
        }

        return num;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (SeekMode == DecryptStreamSeekMode.KeepBasePosition)
        {
            long num = 0L;
            long num2 = 0L;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    num = offset;
                    break;
                case SeekOrigin.Current:
                    num = Position + offset;
                    break;
                case SeekOrigin.End:
                    num = Length + offset;
                    break;
            }

            if (num < BasePosition)
            {
                throw new ArgumentOutOfRangeException("New offset is smaller than base position.");
            }

            num2 = offset - ((offset - BasePosition) & 0x3F);
            BaseStream.Seek(num2, SeekOrigin.Begin);
            updateBuffer();
            bufferRead = (int)(num2 - offset);
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
        bufferLength = (int)Math.Min(64L, BaseStream.Length - BaseStream.Position);
        _position = BaseStream.Position;
        BaseStream.Read(buffer, 0, bufferLength);
        if (Avx2.IsSupported)
        {
            fixed (byte* ptr = extendedKey)
            {
                fixed (byte* ptr2 = buffer)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector256<byte> right = Avx.LoadVector256(ptr + (i << 5));
                        Vector256<byte> left = Avx.LoadVector256(ptr2 + (i << 5));
                        left = Avx2.Xor(left, right);
                        Avx.Store(ptr2 + (i << 5), left);
                    }
                }
            }
        }
        else if (Sse2.IsSupported)
        {
            fixed (byte* ptr3 = extendedKey)
            {
                fixed (byte* ptr4 = buffer)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Vector128<byte> right2 = Sse2.LoadVector128(ptr3 + (j << 4));
                        Vector128<byte> left2 = Sse2.LoadVector128(ptr4 + (j << 4));
                        left2 = Sse2.Xor(left2, right2);
                        Sse2.Store(ptr4 + (j << 4), left2);
                    }
                }
            }
        }
        else
        {
            for (int k = 0; k < bufferLength; k++)
            {
                buffer[k] ^= extendedKey[k];
            }
        }

        bufferRead = 0;
    }
}