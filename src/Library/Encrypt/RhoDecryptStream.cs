using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Launcher.Library.Encrypt
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

        // 通用向量加载方法（替代Sse2.LoadVector128）
        private static unsafe Vector128<T> LoadVector128<T>(void* address) where T : unmanaged
        {
            // 将void*显式转换为T*类型指针
            T* typedPtr = (T*)address;

            // 使用类型化指针加载向量
            return Vector128.Load(typedPtr);
        }

        // 通用向量存储方法（替代Sse2.Store）
        private static unsafe void StoreVector128<T>(void* address, Vector128<T> vector) where T : unmanaged
        {
            // 将void*转换为类型化指针T*
            T* typedPtr = (T*)address;

            // 向量实例的Store方法：将向量数据写入指针指向的内存
            vector.Store(typedPtr);
        }

        /// <summary>
        /// 模拟Sse2.ShiftRightLogical128BitLane的功能：按64位lane向右逻辑移位
        /// </summary>
        /// <param name="vector">128位输入向量</param>
        /// <param name="shiftCount">移位计数（0或1，单位：64位lane）</param>
        /// <returns>移位后的128位向量</returns>
        private static Vector128<byte> ShiftRightLogical128BitLane(Vector128<byte> vector, byte shiftCount)
        {
            // 仅支持移位0或1个lane（SSE2指令的限制）
            shiftCount = (byte)(shiftCount & 0x1); // 确保移位值为0或1

            if (shiftCount == 0)
                return vector; // 移位0：返回原向量

            // 移位1个lane（8字节）：[lane0, lane1] → [lane1, 0]
            // 1. 提取原向量的高64位（lane1）
            Vector64<byte> highLane = vector.GetUpper();

            // 2. 创建新的低64位（原高64位）和高64位（全0）
            Vector64<byte> newLowLane = highLane;
            Vector64<byte> newHighLane = Vector64<byte>.Zero;

            // 3. 组合新的128位向量
            return Vector128<byte>.Zero.WithLower(newLowLane).WithUpper(newHighLane);
        }

        public override unsafe int Read(byte[] writeArr, int offset, int count)
        {
            int readLen = Math.Min(count, (int)(Length - Position));
            if (readLen >= writeArr.Length)
                throw new IndexOutOfRangeException();
            fixed (byte* writePtr = &writeArr[offset], bufPtr = buffer)
            {
                if (readLen < 0)
                    throw new EndOfStreamException();
                if (Vector128.IsHardwareAccelerated) //Sse2.IsSupported
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
                            int bIndex = bufferRead & ~0xF;
                            int nIndex = bufferRead & 0xF;
                            Vector128<byte> bufVec = Vector128<byte>.Zero;
                            if (Sse2.IsSupported)
                            {
                                bufVec = Sse2.LoadVector128(bufPtr + bIndex);
                                if (nIndex != 0)
                                    bufVec = Sse2.ShiftRightLogical128BitLane(bufVec, (byte)nIndex);
                                Sse2.Store(writePtr + writePos, bufVec);
                            }
                            else
                            {
                                bufVec = LoadVector128<byte>(bufPtr + bIndex);
                                if (nIndex != 0)
                                    bufVec = ShiftRightLogical128BitLane(bufVec, (byte)nIndex);
                                StoreVector128(writePtr + writePos, bufVec);
                            }
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
                        newOffset = Position + offset;
                        break;
                    case SeekOrigin.End:
                        newOffset = Length + offset;
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

        // private unsafe void updateBuffer()
        // {
        //     bufferLength = (int)Math.Min(64, BaseStream.Length - BaseStream.Position);
        //     _position = BaseStream.Position;
        //     BaseStream.Read(buffer, 0, bufferLength);
        //     if (Avx2.IsSupported)
        //     {
        //         fixed (byte* keyPtr = extendedKey, bufPtr = buffer)
        //         {
        //             for (int i = 0; i < 2; i++)
        //             {
        //                 Vector256<byte> keyVec = Avx.LoadVector256(keyPtr + (i << 5));
        //                 Vector256<byte> bufVec = Avx.LoadVector256(bufPtr + (i << 5));
        //                 bufVec = Avx2.Xor(bufVec, keyVec);
        //                 Avx.Store(bufPtr + (i << 5), bufVec);
        //             }
        //         }
        //     }
        //     else if (Sse2.IsSupported)
        //     {
        //         fixed (byte* keyPtr = extendedKey, bufPtr = buffer)
        //         {
        //             for (int i = 0; i < 4; i++)
        //             {
        //                 Vector128<byte> keyVec = Sse2.LoadVector128(keyPtr + (i << 4));
        //                 Vector128<byte> bufVec = Sse2.LoadVector128(bufPtr + (i << 4));
        //                 bufVec = Sse2.Xor(bufVec, keyVec);
        //                 Sse2.Store(bufPtr + (i << 4), bufVec);
        //             }
        //         }
        //     }
        //     else
        //     {
        //         for (int i = 0; i < bufferLength; i++)
        //         {
        //             buffer[i] ^= extendedKey[i];
        //         }
        //     }
        //     bufferRead = 0;
        // }

        private unsafe void updateBuffer()
        {
            bufferLength = (int)Math.Min(64, BaseStream.Length - BaseStream.Position);
            _position = BaseStream.Position;
            BaseStream.Read(buffer, 0, bufferLength);

            // 使用通用向量API，自动适配当前架构
            if (Vector256.IsHardwareAccelerated)
            {
                // 优先使用256位向量（如AVX2或ARM NEON的256位扩展）
                fixed (byte* keyPtr = extendedKey, bufPtr = buffer)
                {
                    int vectorSize = Vector256<byte>.Count; // 通常为32字节
                    int iterations = bufferLength / vectorSize;

                    for (int i = 0; i < iterations; i++)
                    {
                        Vector256<byte> keyVec = Vector256.Load(keyPtr + (i * vectorSize));
                        Vector256<byte> bufVec = Vector256.Load(bufPtr + (i * vectorSize));
                        bufVec = Vector256.Xor(bufVec, keyVec);
                        bufVec.Store(bufPtr + (i * vectorSize));
                    }

                    // 处理剩余字节（不足一个向量长度的部分）
                    for (int i = iterations * vectorSize; i < bufferLength; i++)
                    {
                        buffer[i] ^= extendedKey[i];
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // 使用128位向量（如SSE2或ARM NEON）
                fixed (byte* keyPtr = extendedKey, bufPtr = buffer)
                {
                    int vectorSize = Vector128<byte>.Count; // 通常为16字节
                    int iterations = bufferLength / vectorSize;

                    for (int i = 0; i < iterations; i++)
                    {
                        Vector128<byte> keyVec = Vector128.Load(keyPtr + (i * vectorSize));
                        Vector128<byte> bufVec = Vector128.Load(bufPtr + (i * vectorSize));
                        bufVec = Vector128.Xor(bufVec, keyVec);
                        bufVec.Store(bufPtr + (i * vectorSize));
                    }

                    // 处理剩余字节
                    for (int i = iterations * vectorSize; i < bufferLength; i++)
                    {
                        buffer[i] ^= extendedKey[i];
                    }
                }
            }
            else
            {
                // 无向量加速时使用纯软件实现
                for (int i = 0; i < bufferLength; i++)
                {
                    buffer[i] ^= extendedKey[i];
                }
            }

            bufferRead = 0;
        }
    }
}