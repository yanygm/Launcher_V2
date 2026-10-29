using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;

namespace KartLibrary.File.Lpk
{
    /// <summary>
    /// P3563 LPK v4 解码 / 提取逻辑（C# 移植自 Kartrider-lpk-unpacker 的 lpk_v4.py）。
    /// 仅支持已观察到的版本 4、flags 6/7、零分段列表。
    /// </summary>
    public class LpkException : Exception
    {
        public LpkException(string message) : base(message) { }
    }

    /// <summary>
    /// 单个 LPK 文件里的一条记录。
    /// </summary>
    public class LpkRecord
    {
        public string Path;
        public uint Flags;
        public uint Blocks;
        public uint Dsize;
        public uint Csize;
        public string Md5;           // 32 字符小写十六进制
    }

    /// <summary>
    /// 单个 LPK 文件解码后的索引。
    /// </summary>
    public class LpkIndex
    {
        public string FileName;
        public int FileCount;
        public uint HeaderChecksum;
        public long DataBegin;
        public List<LpkRecord> Records = new List<LpkRecord>();
    }

    public static class LpkV4
    {
        public const string DEFAULT_MIXING = "NhucnUGGZIHvxdga";

        // 32 字节掩码常量
        private static readonly byte[] MASK =
            new byte[] {
                0x7b, 0x2f, 0x35, 0x48, 0x43, 0x95, 0x02, 0xb9,
                0xae, 0x91, 0xa6, 0xe1, 0xd8, 0xd6, 0x24, 0xb4,
                0x33, 0x10, 0x1d, 0x3d, 0xc1, 0xbb, 0xc6, 0xf4,
                0xa5, 0xfe, 0xb3, 0x69, 0x6b, 0x56, 0xe4, 0x75
            };

        #region ChaCha20 (原始变体: 64 位计数器 + 64 位 nonce)

        /// <summary>
        /// 原始 ChaCha20（djb）：32 字节密钥，64 位计数器（state[12..13]），64 位 nonce（state[14..15]）。
        /// </summary>
        private sealed class ChaCha20
        {
            private const int ROUNDS = 10; // 20 轮 = 10 次双轮
            private readonly uint[] _state = new uint[16];
            private readonly uint[] _working = new uint[16];
            private ulong _counter;

            public ChaCha20(byte[] key, ulong counter, byte[] nonce8)
            {
                _state[0] = 0x61707865u;
                _state[1] = 0x3320646eu;
                _state[2] = 0x79622d32u;
                _state[3] = 0x6b206574u;
                for (int i = 0; i < 8; i++)
                    _state[4 + i] = BitConverter.ToUInt32(key, i * 4);
                _counter = counter;
                // state[14], state[15] 为 64 位 nonce
                _state[14] = BitConverter.ToUInt32(nonce8, 0);
                _state[15] = BitConverter.ToUInt32(nonce8, 4);
            }

            public void Process(byte[] input, int inOff, int len, byte[] output, int outOff)
            {
                byte[] ks = new byte[64];
                int pos = 0;
                while (pos < len)
                {
                    _state[12] = (uint)(_counter & 0xFFFFFFFFu);
                    _state[13] = (uint)(_counter >> 32);
                    Array.Copy(_state, _working, 16);
                    for (int r = 0; r < ROUNDS; r++)
                    {
                        QuarterRound(0, 4, 8, 12);
                        QuarterRound(1, 5, 9, 13);
                        QuarterRound(2, 6, 10, 14);
                        QuarterRound(3, 7, 11, 15);
                        QuarterRound(0, 5, 10, 15);
                        QuarterRound(1, 6, 11, 12);
                        QuarterRound(2, 7, 8, 13);
                        QuarterRound(3, 4, 9, 14);
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        uint val = _working[i] + _state[i];
                        ks[i * 4] = (byte)val;
                        ks[i * 4 + 1] = (byte)(val >> 8);
                        ks[i * 4 + 2] = (byte)(val >> 16);
                        ks[i * 4 + 3] = (byte)(val >> 24);
                    }
                    int n = Math.Min(64, len - pos);
                    for (int i = 0; i < n; i++)
                        output[outOff + pos + i] = (byte)(input[inOff + pos + i] ^ ks[i]);
                    pos += 64;
                    _counter++;
                }
            }

            private void QuarterRound(int a, int b, int c, int d)
            {
                uint[] x = _working;
                x[a] += x[b]; x[d] ^= x[a]; x[d] = Rotl(x[d], 16);
                x[c] += x[d]; x[b] ^= x[c]; x[b] = Rotl(x[b], 12);
                x[a] += x[b]; x[d] ^= x[a]; x[d] = Rotl(x[d], 8);
                x[c] += x[d]; x[b] ^= x[c]; x[b] = Rotl(x[b], 7);
            }

            private static uint Rotl(uint v, int n) => (v << n) | (v >> (32 - n));
        }

        #endregion

        #region NativeReadStream（匹配原生缓冲读取顺序与精确排空的计数器重置）

        private sealed class NativeReadStream
        {
            private readonly Stream _stream;
            private readonly byte[] _key;
            private long _rawPosition;
            private long _position;
            private byte[] _buffer;
            private ulong _counter;

            public NativeReadStream(Stream stream, byte[] key, long start)
            {
                _stream = stream;
                _key = key;
                _rawPosition = start;
            }

            public long RawPosition => _rawPosition;
            public long Position => _position;

            public byte[] Read(int size)
            {
                if (!(0 < size && size <= (1 << 20)))
                    throw new LpkException("bad read size: " + size);
                int requested = size;
                var ms = new MemoryStream();
                while (size > 0)
                {
                    if (_buffer == null || _buffer.Length == 0)
                    {
                        int length = ((size + 63) / 64) * 64;
                        byte[] raw = new byte[length];
                        _stream.Seek(_rawPosition, SeekOrigin.Begin);
                        ReadExact(_stream, raw, length);
                        byte[] dec = new byte[length];
                        var chacha = new ChaCha20(_key, _counter, new byte[8]);
                        chacha.Process(raw, 0, length, dec, 0);
                        _buffer = dec;
                        _counter += (ulong)(length / 64);
                        _rawPosition += length;
                    }
                    int bufLen = _buffer.Length;
                    int take = Math.Min(size, bufLen);
                    ms.Write(_buffer, 0, take);
                    if (take == bufLen)
                        _buffer = null;
                    else
                    {
                        byte[] nb = new byte[bufLen - take];
                        Array.Copy(_buffer, take, nb, 0, bufLen - take);
                        _buffer = nb;
                    }
                    size -= take;
                    if (take == bufLen && size == 0)
                        _counter = 0;
                }
                _position += requested;
                return ms.ToArray();
            }

            public uint U32()
            {
                byte[] b = Read(4);
                return BitConverter.ToUInt32(b, 0);
            }
        }

        #endregion

        #region 公共 API

        /// <summary>
        /// 解码一个 LPK 文件，返回索引（记录路径、标志、大小、MD5 与数据区起点）。
        /// </summary>
        public static LpkIndex Decode(Stream stream, string fileName, string mixing)
        {
            long fileLength = stream.Length;
            string name = fileName.ToLowerInvariant();
            int nameSum = 0;
            foreach (char c in name)
                nameSum += (int)c;
            int h = nameSum % 312 + 30;

            stream.Seek(0, SeekOrigin.Begin);
            int firstByte = stream.ReadByte();
            if (firstByte < 0)
                throw new LpkException("empty file");

            stream.Seek(h, SeekOrigin.Begin);
            byte[] b5 = new byte[5];
            ReadExact(stream, b5, 5);
            // Python: disk[0] >> 1 & 255 ^ disk[h] == 4  (运算符优先级使其等价于 val ^ (disk[h]==4))
            // disk[0] 以有符号字节解读（struct.unpack('b', ...)）。
            sbyte firstByteSigned = (sbyte)firstByte;
            int val = (firstByteSigned >> 1) & 0xFF;
            bool diskHIs4 = (b5[0] == 4);
            if (val == (diskHIs4 ? 1 : 0))
                throw new LpkException("Invalid LPK data (header tag mismatch)");

            // raw_length = u32(h+1) ^ (signed(disk[0]) >> 1 & 0xFFFFFFFF)
            uint rawLength = BitConverter.ToUInt32(b5, 1) ^ (uint)(firstByteSigned >> 1);
            if (!(rawLength > 0 && rawLength <= 1024))
                throw new LpkException("Invalid LPK data (rawLength=" + rawLength + ")");

            int headerEnd = h + 5 + 2 * (int)rawLength;
            string text = name + mixing;

            byte[] headerKey = new byte[32];
            for (int i = 0; i < 32; i++)
                headerKey[i] = (byte)(((text[i % text.Length] + i) & 0xFF) ^ MASK[i]);

            var header = new NativeReadStream(stream, headerKey, headerEnd);
            uint checksum = header.U32();
            uint count = header.U32();
            if (!(count > 0 && count < 100000))
                throw new LpkException("Invalid LPK data (count=" + count + ")");

            int start = headerEnd + 8 + ((nameSum * 3) % 212) + 64;

            byte[] key = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                int idx = text.Length - 1 - (i % text.Length);
                key[i] = (byte)((((int)text[idx] * (2 + i % 3)) + i) & 0xFF ^ MASK[i]);
            }

            var table = new NativeReadStream(stream, key, start);
            var records = new List<LpkRecord>();
            for (int n = 0; n < count; n++)
            {
                uint plen = table.U32();
                if (!(plen > 0 && plen <= 4096))
                    throw new LpkException("Invalid LPK data (plen=" + plen + ")");
                byte[] pathBytes = table.Read((int)plen * 2);
                string path = Encoding.Unicode.GetString(pathBytes);

                uint csum = table.U32();
                uint flags = table.U32();
                uint blocks = table.U32();
                uint dsize = table.U32();
                uint csize = table.U32();
                uint extra = table.U32();
                uint sections = table.U32();

                if (!(flags == 6 || flags == 7))
                    throw new LpkException("Unsupported flags: " + flags);
                if (sections != 0)
                    throw new LpkException("Unsupported sections: " + sections);

                byte[] digest = table.Read(16);
                uint tagLow = table.U32();
                uint tagHigh = table.U32();

                int sumDigest = 0;
                foreach (byte b in digest)
                    sumDigest += b;
                uint expectedCsum = (flags + blocks + dsize + csize + (uint)sumDigest) & 0xFFFFFFFFu;
                if (csum != expectedCsum)
                    throw new LpkException("Checksum mismatch for " + path);

                records.Add(new LpkRecord
                {
                    Path = path,
                    Flags = flags,
                    Blocks = blocks,
                    Dsize = dsize,
                    Csize = csize,
                    Md5 = ToHex(digest)
                });
            }

            long dataBegin = ((table.RawPosition + 1023) / 1024) * 1024;
            foreach (var r in records)
            {
                if ((long)dataBegin + (long)r.Blocks * 1024 + r.Csize > fileLength)
                    throw new LpkException("Invalid LPK data (data overflow) for " + r.Path);
            }

            return new LpkIndex
            {
                FileName = name,
                FileCount = (int)count,
                HeaderChecksum = checksum,
                DataBegin = dataBegin,
                Records = records
            };
        }

        /// <summary>
        /// 从已解码的记录中提取并校验（解密 + 解压 + 长度/MD5）一个文件。
        /// </summary>
        public static byte[] Extract(Stream stream, LpkRecord record, long dataBegin, int[] digits, byte[] nonce16)
        {
            if (!(record.Flags == 6 || record.Flags == 7))
                throw new LpkException("Invalid LPK data (flags)");

            long start = dataBegin + (long)record.Blocks * 1024;
            if (start + record.Csize > stream.Length)
                throw new LpkException("Invalid LPK data (data overflow) for " + record.Path);

            stream.Seek(start, SeekOrigin.Begin);
            byte[] stored = new byte[record.Csize];
            ReadExact(stream, stored, (int)record.Csize);

            int length = Math.Min(1024, stored.Length);
            ulong counter = BitConverter.ToUInt64(nonce16, 0);
            byte[] nonce64 = new byte[8];
            Array.Copy(nonce16, 8, nonce64, 0, 8);

            byte[] key = ComputeExtractKey(record, digits);
            byte[] clear = new byte[stored.Length];
            var chacha = new ChaCha20(key, counter, nonce64);
            chacha.Process(stored, 0, length, clear, 0);
            if (length < stored.Length)
                Array.Copy(stored, length, clear, length, stored.Length - length);

            byte[] output;
            if ((record.Flags & 1) != 0)
            {
                output = ZlibInflate(clear, out int consumed, out bool eof);
                if (!eof)
                    throw new LpkException("incomplete zlib for " + record.Path);
                int trailing = clear.Length - consumed;
                if (trailing > 0)
                {
                    int used = clear.Length - trailing;
                    if (!(clear.Length <= 1024 && ((used + 63) / 64 * 64) == clear.Length))
                        throw new LpkException("unexpected zlib trailing bytes for " + record.Path);
                }
            }
            else
            {
                int expected = (record.Dsize <= 1024)
                    ? ((int)record.Dsize + 63) / 64 * 64
                    : (int)record.Dsize;
                if (clear.Length != expected)
                    throw new LpkException("unexpected uncompressed storage length for " + record.Path);
                output = new byte[record.Dsize];
                Array.Copy(clear, 0, output, 0, (int)record.Dsize);
            }

            if (output.Length != record.Dsize)
                throw new LpkException("decoded size mismatch for " + record.Path);
            CheckMd5(output, record.Md5);
            return output;
        }

        /// <summary>
        /// 计算解密每条记录所用的 32 字节 ChaCha20 密钥（与 lpk_v4.py 的 extract 一致）。
        /// </summary>
        private static byte[] ComputeExtractKey(LpkRecord record, int[] digits)
        {
            byte[] md5 = FromHex(record.Md5);
            byte[] pathUtf16 = Encoding.Unicode.GetBytes(record.Path);
            ushort[] units = new ushort[pathUtf16.Length / 2];
            Buffer.BlockCopy(pathUtf16, 0, units, 0, pathUtf16.Length);

            byte[] key = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                int idx = (digits[(i + 2) % digits.Length] + i) & 15;
                int value = ((digits[(i + 1) % digits.Length] + i) % 5) + md5[idx] + (digits[i % digits.Length] & 1);
                int ki = (((value & 0xFF) * (units[i % units.Length] & 0xFF)) + i) & 0xFF;
                key[i] = (byte)(ki ^ MASK[i]);
            }
            return key;
        }

        /// <summary>
        /// 由格式字符串恢复 P3563 v4 的参数：返回 (十进制位数列表, 16 字节 nonce)。
        /// </summary>
        public static (int[] digits, byte[] nonce) Parameters(string mixing)
        {
            uint value = 2166136261;
            foreach (char c in mixing)
                value = (value ^ (uint)c) * 16777619u & 0xFFFFFFFFu;

            byte[] source = new byte[12];
            WriteLE(source, 0, value);
            WriteLE(source, 4, value >> 1);
            WriteLE(source, 8, (value >> 1) ^ 108u);

            byte[] mask = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                int m = ((17 * i) ^ (8 * i)) + ((i >> 2) ^ (90 + 43 * i)) - (i % 11);
                mask[i] = (byte)(m & 0xFF);
            }

            byte[] nonce12 = new byte[12];
            for (int i = 0; i < 12; i++)
                nonce12[i] = (byte)(source[i] ^ mask[i]);

            string s = value.ToString();
            int[] digits = new int[s.Length];
            for (int i = 0; i < s.Length; i++)
                digits[i] = s[i] - '0';

            // nonce = nonce12[8:12] + bytes(4) + nonce12[:8]
            byte[] nonce16 = new byte[16];
            Array.Copy(nonce12, 8, nonce16, 0, 4);
            Array.Copy(nonce12, 0, nonce16, 8, 8);
            return (digits, nonce16);
        }

        #endregion

        #region 辅助方法

        private static void WriteLE(byte[] buf, int offset, uint v)
        {
            buf[offset] = (byte)v;
            buf[offset + 1] = (byte)(v >> 8);
            buf[offset + 2] = (byte)(v >> 16);
            buf[offset + 3] = (byte)(v >> 24);
        }

        private static void ReadExact(Stream stream, byte[] buf, int count)
        {
            int off = 0;
            while (off < count)
            {
                int r = stream.Read(buf, off, count - off);
                if (r == 0)
                    throw new LpkException("unexpected EOF");
                off += r;
            }
        }

        private static byte[] ZlibInflate(byte[] data, out int consumed, out bool eof)
        {
            var codec = new ZlibCodec();
            codec.InitializeInflate();
            codec.InputBuffer = data;
            codec.NextIn = 0;
            codec.AvailableBytesIn = data.Length;
            var ms = new MemoryStream();
            byte[] outBuf = new byte[16384];
            eof = false;
            int guard = 0;
            while (true)
            {
                codec.OutputBuffer = outBuf;
                codec.NextOut = 0;
                codec.AvailableBytesOut = outBuf.Length;
                int rc = codec.Inflate(FlushType.None);
                int written = outBuf.Length - codec.AvailableBytesOut;
                if (written > 0)
                    ms.Write(outBuf, 0, written);
                if (rc == ZlibConstants.Z_STREAM_END) { eof = true; break; }
                if (rc != ZlibConstants.Z_OK)
                    throw new LpkException("zlib inflate error: " + rc);
                if (codec.AvailableBytesIn == 0) break;
                if (++guard > 1_000_000)
                    throw new LpkException("zlib inflate loop guard");
            }
            consumed = data.Length - codec.AvailableBytesIn;
            return ms.ToArray();
        }

        private static void CheckMd5(byte[] data, string expectedHex)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(data);
            if (ToHex(hash) != expectedHex)
                throw new LpkException("MD5 mismatch");
        }

        private static string ToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static byte[] FromHex(string hex)
        {
            byte[] outp = new byte[hex.Length / 2];
            for (int i = 0; i < outp.Length; i++)
                outp[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return outp;
        }

        #endregion
    }
}
