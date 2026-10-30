using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;

namespace KartLibrary.File.Lpk
{
    /// <summary>
    /// P3563 LPK v4 解码 / 提取 / 打包逻辑（C# 移植自 Kartrider-lpk-unpacker 的 lpk_v4.py）。
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
        public byte[] Md5Raw;        // 解码时保留的原始 16 字节摘要（避免 Extract 反复解析十六进制）
        public uint TagLow;          // 原生打包器写入的 8 字节 tag（算法未知，不参与解密）
        public uint TagHigh;
        public uint Csum;            // 记录校验值
        public uint Extra;           // 未使用（已观察到的文件均为 0）
        public uint Sections;        // 分段列表数量（已观察到的文件均为 0）
    }

    /// <summary>
    /// 原包中无法重新计算、但想原样保留的头部信息（头部校验值、记录的 tag、记录顺序与压缩决策）。
    /// 用于“解包 → 修改 → 打包”时尽量与原包保持一致。
    /// </summary>
    public sealed class LpkMetadata
    {
        public uint HeaderChecksum;
        public int RawLength;
        public long DataBegin;
        public string SourcePath;                  // 原包路径（用于复用未改动文件的原始密文）
        public string Mixing = LpkV4.DEFAULT_MIXING; // 由 head + mid 反推出的格式串（重打包时必须沿用）
        public byte[] Head = Array.Empty<byte>();  // [0, h) 的原始字节
        public byte[] Mark = Array.Empty<byte>();  // [h, h + 5) 的原始字节
        public byte[] Mid = Array.Empty<byte>();   // [h + 5, headerEnd) 的原始字节
        public byte[] FileTail = Array.Empty<byte>(); // 数据区之后的尾部补齐字节
        public List<LpkRecord> Records = new List<LpkRecord>();
        public int ReusedCount;                    // 打包时复用了原包密文的记录数（统计用）
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
        /// <summary>实际使用的格式串（调用方未指定时由 head + mid 反推）。</summary>
        public string Mixing = LpkV4.DEFAULT_MIXING;
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
            /// <summary>预读窗口大小。记录表是顺序读取的，一次读入可把 Seek 次数从“每 64 字节一次”降到“每窗口一次”。</summary>
            private const int WindowSize = 1 << 18;

            private readonly Stream _stream;
            private readonly byte[] _key;
            private long _rawPosition;
            private long _position;
            private byte[] _buffer = Array.Empty<byte>();
            private int _bufferOffset;
            private ulong _counter;

            private byte[] _window = Array.Empty<byte>();
            private long _windowStart;
            private int _windowLength;

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
                byte[] output = new byte[size];
                int written = 0;
                while (size > 0)
                {
                    if (_bufferOffset >= _buffer.Length)
                    {
                        int length = ((size + 63) / 64) * 64;
                        byte[] raw = TakeRaw(length);
                        byte[] dec = new byte[length];
                        new ChaCha20(_key, _counter, new byte[8]).Process(raw, 0, length, dec, 0);
                        _buffer = dec;
                        _bufferOffset = 0;
                        _counter += (ulong)(length / 64);
                        _rawPosition += length;
                    }
                    int take = Math.Min(size, _buffer.Length - _bufferOffset);
                    Buffer.BlockCopy(_buffer, _bufferOffset, output, written, take);
                    _bufferOffset += take;
                    written += take;
                    size -= take;
                    // 缓冲被精确排空时计数器归零（与原生读取行为一致）
                    if (_bufferOffset == _buffer.Length && size == 0)
                        _counter = 0;
                }
                _position += requested;
                return output;
            }

            private byte[] TakeRaw(int length)
            {
                if (_windowLength == 0
                    || _rawPosition < _windowStart
                    || _rawPosition + length > _windowStart + _windowLength)
                    FillWindow(length);
                byte[] raw = new byte[length];
                Buffer.BlockCopy(_window, (int)(_rawPosition - _windowStart), raw, 0, length);
                return raw;
            }

            private void FillWindow(int minLength)
            {
                long available = _stream.Length - _rawPosition;
                if (available < minLength)
                    throw new LpkException("unexpected EOF");
                int want = (int)Math.Min(Math.Max(minLength, WindowSize), available);
                byte[] window = new byte[want];
                _stream.Seek(_rawPosition, SeekOrigin.Begin);
                ReadExact(_stream, window, want);
                _window = window;
                _windowStart = _rawPosition;
                _windowLength = want;
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
        public static LpkIndex Decode(Stream stream, string fileName, string mixing = null)
        {
            long fileLength = stream.Length;
            string name = fileName.ToLowerInvariant();
            int nameSum = 0;
            foreach (char c in name)
                nameSum += (int)c;
            int h = nameSum % 312 + 30;

            if (h + 5 > fileLength)
                throw new LpkException("empty file");

            // 头部的 head / mark / mid 三块一次读齐：既用于校验，也用于反推 mixing。
            stream.Seek(0, SeekOrigin.Begin);
            byte[] head = new byte[h];
            ReadExact(stream, head, head.Length);
            byte[] b5 = new byte[5];
            ReadExact(stream, b5, b5.Length);

            // 原生判定（0x1400e0e1c: xor esi,edx / cmp sil,4）：
            //   (disk[h] ^ ((sbyte)disk[0] >> 1)) & 0xFF == 4
            // disk[0] 以有符号字节解读（struct.unpack('b', ...)）。
            sbyte firstByteSigned = (sbyte)head[0];
            if (((b5[0] ^ (byte)(firstByteSigned >> 1)) & 0xFF) != 4)
                throw new LpkException("Invalid LPK data (header tag mismatch)");

            // raw_length = u32(h+1) ^ (signed(disk[0]) >> 1 & 0xFFFFFFFF)
            uint rawLength = BitConverter.ToUInt32(b5, 1) ^ (uint)(firstByteSigned >> 1);
            if (!(rawLength > 0 && rawLength <= 1024))
                throw new LpkException("Invalid LPK data (rawLength=" + rawLength + ")");

            int headerEnd = h + 5 + 2 * (int)rawLength;
            byte[] mid = new byte[2 * (int)rawLength];
            ReadExact(stream, mid, mid.Length);

            // 官方包把 mixing 以 head + mid 的形式存在包内；调用方未指定时按原生规则反推。
            if (string.IsNullOrEmpty(mixing))
                mixing = DeriveMixing(head, b5, mid);

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
                    Md5 = ToHex(digest),
                    Md5Raw = digest,
                    TagLow = tagLow,
                    TagHigh = tagHigh,
                    Csum = csum,
                    Extra = extra,
                    Sections = sections
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
                Mixing = mixing,
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
            byte[] md5 = record.Md5Raw ?? FromHex(record.Md5);
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

        #region 打包（编码）

        /// <summary>
        /// 待写入 LPK 的文件条目。<see cref="Data"/> 与 <see cref="SourcePath"/> 至少提供一个，
        /// 提供 <see cref="SourcePath"/> 时按需读取，避免一次性把所有文件读进内存。
        /// </summary>
        public sealed class LpkEntry
        {
            /// <summary>包内路径，使用 '/' 分隔（例如 "etc_/aiCampaign@cn.xml"）。</summary>
            public string Path;

            /// <summary>文件内容。</summary>
            public byte[] Data;

            /// <summary>本地文件路径（当 <see cref="Data"/> 为空时使用）。</summary>
            public string SourcePath;

            internal byte[] Load()
            {
                if (Data != null)
                    return Data;
                if (!string.IsNullOrEmpty(SourcePath))
                    return System.IO.File.ReadAllBytes(SourcePath);
                return Array.Empty<byte>();
            }
        }

        // 与原生打包器一致的头部标记字节
        private const byte HEAD_TAG_BYTE = 0xA7;
        private const byte HEAD_MARK_BYTE = 0xD7;
        private const int HEAD_RAW_LENGTH = 16;
        private const uint HEAD_LENGTH_MASK = 0xFFFFFFD3u; // (uint)((sbyte)HEAD_TAG_BYTE >> 1)

        /// <summary>
        /// 将一组文件编码为 LPK v4 分包（与 <see cref="Decode"/> 对称）。
        /// 记录表与头部按原生读取顺序分块做 ChaCha20 加密，数据区各文件按 1024 字节对齐。
        /// </summary>
        /// <param name="metadata">
        /// 可选。原包的头部信息。提供时会沿用原包的头部校验值、记录顺序、每记录 tag 与压缩决策，
        /// 使“解包 → 修改 → 打包”的结果与原包保持一致（这些值无法从数据重新计算）。
        /// </param>
        /// <param name="mixing">
        /// 可选。整包的格式串（默认 <see cref="DEFAULT_MIXING"/>）。为空时依次取
        /// <paramref name="metadata"/> 的 <see cref="LpkMetadata.Mixing"/>、再退回默认值。
        /// </param>
        public static void Encode(string outputPath, IList<LpkEntry> entries, LpkMetadata metadata = null, string mixing = null)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            string fileName = System.IO.Path.GetFileName(outputPath);
            if (string.IsNullOrEmpty(fileName))
                throw new LpkException("输出路径无效: " + outputPath);

            string name = fileName.ToLowerInvariant();
            int nameSum = 0;
            foreach (char c in name)
                nameSum += c;

            // mixing 是整包的根密钥：头部块、记录表、每个文件的密文都由它派生，
            // 同时它又以 head + mid 的形式存在包内。重打包必须沿用原包的 mixing，
            // 否则三处密钥全部对不上。
            if (string.IsNullOrEmpty(mixing))
                mixing = metadata != null && !string.IsNullOrEmpty(metadata.Mixing)
                    ? metadata.Mixing
                    : DEFAULT_MIXING;

            string text = name + mixing;
            var (digits, nonce16) = Parameters(mixing);

            var metaRecords = new Dictionary<string, LpkRecord>(StringComparer.Ordinal);
            if (metadata != null)
            {
                metadata.ReusedCount = 0;
                foreach (LpkRecord record in metadata.Records)
                    metaRecords[record.Path] = record;
                if (metaRecords.Count > 0)
                    entries = ReorderByMetadata(entries, metadata);
            }

            // 记录表长度只取决于路径，先算出来以确定数据区起点
            long tableLength = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                LpkEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Path))
                    throw new LpkException("记录路径不能为空");
                int plen = Encoding.Unicode.GetByteCount(entry.Path) / 2;
                if (plen <= 0 || plen > 4096)
                    throw new LpkException("记录路径长度非法: " + entry.Path);
                tableLength += 4 + (long)plen * 2 + 4 * 7 + 16 + 8;
            }

            int h = nameSum % 312 + 30;
            int rawLength = metadata != null && metadata.RawLength > 0 ? metadata.RawLength : HEAD_RAW_LENGTH;
            int headerEnd = h + 5 + 2 * rawLength;
            long tableStart = headerEnd + 8 + ((nameSum * 3) % 212) + 64;
            long tablePaddedEnd = tableStart + ((tableLength + 63) / 64) * 64;
            long dataBegin = ((tablePaddedEnd + 1023) / 1024) * 1024;

            var records = new List<LpkRecord>(entries.Count);
            var csumList = new List<uint>(entries.Count);
            var tagList = new List<byte[]>(entries.Count);
            var junk = new FillRandom(nameSum);
            long blocks = 0;
            long dataEnd = dataBegin;

            // 原包流：内容未改动的记录直接复用原来的密文，使重打包结果与官方包逐字节一致。
            // 原包通常就是输出文件本身，所以先写同目录临时文件，成功后再替换，避免截断正在读取的原包。
            FileStream template = null;
            if (metadata != null && !string.IsNullOrEmpty(metadata.SourcePath) && System.IO.File.Exists(metadata.SourcePath))
                template = new FileStream(metadata.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);

            string tempPath = outputPath + ".tmp";
            bool committed = false;
            try
            {
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
            {
                // 1) 头部区（0 ~ tableStart），表头之后回填
                byte[] head = BuildRegion(metadata != null ? metadata.Head : null, h, junk, HEAD_TAG_BYTE);
                fs.Write(head, 0, head.Length);

                byte[] mark = new byte[5];
                if (metadata != null && metadata.Mark.Length == 5)
                    mark = (byte[])metadata.Mark.Clone();
                else
                {
                    mark[0] = HEAD_MARK_BYTE;
                    WriteLE(mark, 1, (uint)rawLength ^ HEAD_LENGTH_MASK);
                }
                fs.Write(mark, 0, mark.Length);

                // mid 不是随机填充，它是 mixing 的密文：
                //   mid_u16[i] = mixing_u16[i] ^ ((sbyte)head[i % h] >> 1) ^ 0x4B
                // 复用到原包 mid 时 mixing 保持不变；否则必须按当前 head 反算，
                // 否则解包端（含官方客户端）反推出的 mixing 是乱码，三把密钥全错。
                byte[] mid = (metadata != null && metadata.Mid.Length == 2 * rawLength)
                    ? (byte[])metadata.Mid.Clone()
                    : BuildMid(head, rawLength, mixing);
                fs.Write(mid, 0, mid.Length);
                long headerStart = fs.Position;               // == headerEnd
                long gapLength = tableStart - headerStart;
                if (template != null && headerStart + gapLength <= template.Length)
                {
                    // 头部加密块 + 对齐区：直接沿用原包字节（对齐区是随机填充，无法重算）
                    byte[] gap = ReadBytes(template, headerStart, (int)gapLength);
                    fs.Write(gap, 0, gap.Length);
                }
                else
                {
                    WriteJunk(fs, gapLength, junk);
                }

                // 2) 数据区基础层：从记录表末尾开始整体复制原包字节，保留表后对齐填充与
                //    文件之间的填充字节，随后每个文件的密文会覆盖到对应位置
                //    （未改动文件因此与原包逐字节一致）。
                if (template != null && template.Length > tablePaddedEnd)
                {
                    long remain = template.Length - tablePaddedEnd;
                    byte[] buffer = new byte[1 << 20];
                    template.Seek(tablePaddedEnd, SeekOrigin.Begin);
                    fs.Seek(tablePaddedEnd, SeekOrigin.Begin);
                    while (remain > 0)
                    {
                        int want = (int)Math.Min(buffer.Length, remain);
                        int got = template.Read(buffer, 0, want);
                        if (got <= 0)
                            break;
                        fs.Write(buffer, 0, got);
                        remain -= got;
                    }
                }

                // 3) 数据区：逐个文件写入（每个文件按 1024 字节块对齐）
                for (int i = 0; i < entries.Count; i++)
                {
                    byte[] data = entries[i].Load();
                    metaRecords.TryGetValue(entries[i].Path, out LpkRecord source);
                    string md5Hex = ToHex(MD5.HashData(data));

                    uint flags;
                    byte[] reused = null;   // 原包中该文件的密文（内容未改动时直接复用）
                    byte[] stored = null;   // 重新编码时的存储明文

                    if (template != null && source != null && source.Dsize == data.Length
                        && string.Equals(source.Md5, md5Hex, StringComparison.Ordinal))
                    {
                        flags = source.Flags;
                        reused = ReadBytes(template, metadata.DataBegin + (long)source.Blocks * 1024, (int)source.Csize);
                        metadata.ReusedCount++;
                    }
                    else
                    {
                        // 原生打包器（以及客户端）对所有重编码文件一律使用 zlib：即便压缩后更大也照写，
                        // 与官方包/参考工具的 flags=7 保持一致，避免客户端走 flags=6 分支。
                        flags = 7;
                        byte[] compressed = ZlibDeflate(data);
                        stored = compressed.Length <= 1024 ? PadTo64(compressed) : compressed;
                    }

                    var record = new LpkRecord
                    {
                        Path = entries[i].Path,
                        Flags = flags,
                        Blocks = (uint)blocks,
                        Dsize = (uint)data.Length,
                        Csize = (uint)(reused != null ? reused.Length : stored.Length),
                        Md5 = md5Hex
                    };

                    uint sumDigest = 0;
                    foreach (byte b in FromHex(record.Md5))
                        sumDigest += b;
                    uint csum = (record.Flags + record.Blocks + record.Dsize + record.Csize + sumDigest) & 0xFFFFFFFFu;

                    fs.Seek(dataBegin + (long)record.Blocks * 1024, SeekOrigin.Begin);
                    byte[] encrypted = reused ?? EncryptStored(record, stored, digits, nonce16);
                    fs.Write(encrypted, 0, encrypted.Length);

                    // tag 由原生打包器写成：tagLow = adler0(原始数据)，tagHigh = adler0(写入的密文)
                    record.TagLow = Adler0(data);
                    record.TagHigh = reused != null ? source.TagHigh : Adler0(encrypted);
                    tagList.Add(CombineTag(record.TagLow, record.TagHigh));

                    records.Add(record);
                    csumList.Add(csum);

                    dataEnd = dataBegin + (long)record.Blocks * 1024 + record.Csize;
                    blocks = (blocks * 1024 + record.Csize + 1023) / 1024;
                }

                // 4) 回填头部加密块
                byte[] headerKey = new byte[32];
                for (int i = 0; i < 32; i++)
                    headerKey[i] = (byte)(((text[i % text.Length] + i) & 0xFF) ^ MASK[i]);

                byte[] headerPlain = new byte[64];
                if (template != null)
                {
                    // 沿用原包头部块中未使用的 56 字节，保持与原包一致
                    byte[] templateCipher = ReadBytes(template, headerStart, 64);
                    new ChaCha20(headerKey, 0, new byte[8]).Process(templateCipher, 0, 64, headerPlain, 0);
                }
                else
                {
                    // 官方包的头部块后 56 字节全为 0，新建包时照此写入
                }

                // 头部校验值（原生算法的还原结果）：
                //   checksum = mark[0] + u32(mark[1..5]) + count + 4 + Σ u16(mid[])  (32 位回绕)
                // 其中 mark 为 h 处的 5 字节，mid 为 mark 之后 2*rawLength 字节。
                uint headerChecksum = ComputeHeaderChecksum(mark, mid, records.Count);
                WriteLE(headerPlain, 0, headerChecksum);
                WriteLE(headerPlain, 4, (uint)records.Count);

                byte[] headerCipher = new byte[64];
                new ChaCha20(headerKey, 0, new byte[8]).Process(headerPlain, 0, 64, headerCipher, 0);
                fs.Seek(headerStart, SeekOrigin.Begin);
                fs.Write(headerCipher, 0, headerCipher.Length);

                // 5) 记录表（按原生读取顺序分块加密）
                byte[] tableKey = new byte[32];
                for (int i = 0; i < 32; i++)
                {
                    int idx = text.Length - 1 - (i % text.Length);
                    tableKey[i] = (byte)((((int)text[idx] * (2 + i % 3)) + i) & 0xFF ^ MASK[i]);
                }

                var tablePlain = new List<byte>((int)tableLength + 64);
                var readSizes = new List<int>(records.Count * 12);
                for (int i = 0; i < records.Count; i++)
                {
                    LpkRecord record = records[i];
                    byte[] digest = FromHex(record.Md5);
                    byte[] pathBytes = Encoding.Unicode.GetBytes(record.Path);

                    AppendU32(tablePlain, (uint)(pathBytes.Length / 2));
                    readSizes.Add(4);
                    tablePlain.AddRange(pathBytes);
                    readSizes.Add(pathBytes.Length);

                    AppendU32(tablePlain, csumList[i]);
                    readSizes.Add(4);
                    AppendU32(tablePlain, record.Flags);
                    readSizes.Add(4);
                    AppendU32(tablePlain, record.Blocks);
                    readSizes.Add(4);
                    AppendU32(tablePlain, record.Dsize);
                    readSizes.Add(4);
                    AppendU32(tablePlain, record.Csize);
                    readSizes.Add(4);
                    AppendU32(tablePlain, 0u); // extra
                    readSizes.Add(4);
                    AppendU32(tablePlain, 0u); // sections
                    readSizes.Add(4);

                    tablePlain.AddRange(digest);
                    readSizes.Add(16);

                    // 原生打包器的 8 字节 tag 算法未知（不参与解密）：有原包信息时沿用，否则生成稳定的伪随机值
                    tablePlain.AddRange(tagList[i]);
                    readSizes.Add(4);
                    readSizes.Add(4);
                }
                int tablePadCount = 0;
                while (tablePlain.Count % 64 != 0)
                {
                    tablePlain.Add(junk.NextByte());
                    tablePadCount++;
                }

                byte[] tableCipher = EncryptReadPattern(tablePlain.ToArray(), tableKey, readSizes);
                fs.Seek(tableStart, SeekOrigin.Begin);
                fs.Write(tableCipher, 0, tableCipher.Length);

                // 记录表末尾的补齐字节由原生打包器随机填充，直接沿用原包密文，保证逐字节一致
                if (template != null && tablePadCount > 0)
                {
                    long tableEnd = tableStart + tableCipher.Length;
                    long copyFrom = tableEnd - tablePadCount;
                    if (copyFrom >= 0 && tableEnd <= template.Length)
                    {
                        byte[] padCipher = ReadBytes(template, copyFrom, tablePadCount);
                        fs.Seek(copyFrom, SeekOrigin.Begin);
                        fs.Write(padCipher, 0, padCipher.Length);
                    }
                }

                // 6) 尾部补齐到相对数据区起点的 1024 字节边界（与原包一致），补齐内容取头部字节
                long fileLength = dataBegin + ((dataEnd - dataBegin + 1023) / 1024) * 1024;
                if (fileLength > dataEnd)
                {
                    byte[] tail = new byte[fileLength - dataEnd];
                    if (metadata != null && metadata.FileTail.Length == tail.Length)
                    {
                        Array.Copy(metadata.FileTail, tail, tail.Length);
                    }
                    else
                    {
                        for (int i = 0; i < tail.Length; i++)
                            tail[i] = head[i % head.Length];
                    }
                    fs.Seek(dataEnd, SeekOrigin.Begin);
                    fs.Write(tail, 0, tail.Length);
                }
                fs.SetLength(fileLength);
            }

            // 先释放原包句柄，再覆盖目标文件（Windows 不允许覆盖仍被打开的文件）
            template?.Dispose();
            template = null;
            System.IO.File.Move(tempPath, outputPath, true);
            committed = true;
            }
            finally
            {
                template?.Dispose();
                if (!committed)
                {
                    try
                    {
                        if (System.IO.File.Exists(tempPath))
                            System.IO.File.Delete(tempPath);
                    }
                    catch
                    {
                        // 清理失败不影响异常抛出
                    }
                }
            }
        }

        private static byte[] ReadBytes(Stream stream, long offset, int length)
        {
            byte[] buffer = new byte[length];
            stream.Seek(offset, SeekOrigin.Begin);
            ReadExact(stream, buffer, length);
            return buffer;
        }

        /// <summary>
        /// 读取包内的 head / mark / mid 三块（不解析记录表）。
        /// </summary>
        private static (byte[] Head, byte[] Mark, byte[] Mid) ReadHeaderRegions(FileStream fs, int h)
        {
            fs.Seek(0, SeekOrigin.Begin);
            byte[] head = new byte[h];
            ReadExact(fs, head, head.Length);
            byte[] mark = new byte[5];
            ReadExact(fs, mark, mark.Length);
            uint rawLength = BitConverter.ToUInt32(mark, 1) ^ (uint)((sbyte)head[0] >> 1);
            if (rawLength == 0 || rawLength > 1024)
                throw new LpkException("Invalid LPK data (rawLength=" + rawLength + ")");
            byte[] mid = new byte[2 * (int)rawLength];
            ReadExact(fs, mid, mid.Length);
            return (head, mark, mid);
        }

        /// <summary>
        /// 反推一个已有 LPK 的 mixing（解包入口用；只读 head + mid，不解析记录表）。
        /// </summary>
        public static string ReadMixing(string lpkPath)
        {
            string fileName = System.IO.Path.GetFileName(lpkPath);
            if (string.IsNullOrEmpty(fileName))
                throw new LpkException("LPK 路径无效: " + lpkPath);

            int nameSum = 0;
            foreach (char c in fileName.ToLowerInvariant())
                nameSum += c;
            int h = nameSum % 312 + 30;

            using var fs = new FileStream(lpkPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var (head, mark, mid) = ReadHeaderRegions(fs, h);
            return DeriveMixing(head, mark, mid);
        }

        /// <summary>
        /// 读取一个已有 LPK 的头部信息（校验值、tag、记录顺序），用于重新打包时沿用。
        /// </summary>
        public static LpkMetadata ReadMetadata(string lpkPath, string mixing = null)
        {
            string fileName = System.IO.Path.GetFileName(lpkPath);
            if (string.IsNullOrEmpty(fileName))
                throw new LpkException("LPK 路径无效: " + lpkPath);

            string name = fileName.ToLowerInvariant();
            int nameSum = 0;
            foreach (char c in name)
                nameSum += c;
            int h = nameSum % 312 + 30;

            using var fs = new FileStream(lpkPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var (head, mark, mid) = ReadHeaderRegions(fs, h);
            int rawLength = mid.Length / 2;

            // 官方包内 mixing 就藏在 head + mid 里；调用方未指定时按原生规则反推，
            // 这样重打包才能沿用原包密钥（非默认 mixing 的包也不会被误读成默认值）。
            if (string.IsNullOrEmpty(mixing))
                mixing = DeriveMixing(head, mark, mid);

            fs.Seek(0, SeekOrigin.Begin);
            LpkIndex index = Decode(fs, fileName, mixing);

            long dataEnd = index.DataBegin;
            foreach (LpkRecord record in index.Records)
                dataEnd = Math.Max(dataEnd, index.DataBegin + (long)record.Blocks * 1024 + record.Csize);
            byte[] fileTail = Array.Empty<byte>();
            long fileLength = fs.Length;
            if (fileLength > dataEnd && fileLength - dataEnd < 1024 * 1024)
                fileTail = ReadBytes(fs, dataEnd, (int)(fileLength - dataEnd));

            return new LpkMetadata
            {
                HeaderChecksum = index.HeaderChecksum,
                RawLength = (int)rawLength,
                DataBegin = index.DataBegin,
                SourcePath = lpkPath,
                Mixing = mixing,
                Head = head,
                Mark = mark,
                Mid = mid,
                FileTail = fileTail,
                Records = index.Records
            };
        }

        /// <summary>按原包记录顺序排列，原包中不存在的（新增）文件排在最后。</summary>
        private static List<LpkEntry> ReorderByMetadata(IList<LpkEntry> entries, LpkMetadata metadata)
        {
            var byPath = new Dictionary<string, LpkEntry>(StringComparer.Ordinal);
            foreach (LpkEntry entry in entries)
                if (entry != null && !string.IsNullOrEmpty(entry.Path))
                    byPath[entry.Path] = entry;

            var ordered = new List<LpkEntry>(entries.Count);
            foreach (LpkRecord record in metadata.Records)
                if (byPath.TryGetValue(record.Path, out LpkEntry entry))
                {
                    ordered.Add(entry);
                    byPath.Remove(record.Path);
                }

            foreach (LpkEntry entry in entries)
                if (entry != null && byPath.ContainsKey(entry.Path ?? string.Empty))
                {
                    ordered.Add(entry);
                    byPath.Remove(entry.Path);
                }
            return ordered;
        }

        /// <summary>
        /// 由 head + mid 反推 mixing（与 <see cref="BuildMid"/> 互逆）：
        ///   mixing_u16[i] = mid_u16[i] ^ ((sbyte)head[i % h] >> 1) ^ 0x4B
        /// </summary>
        private static string DeriveMixing(byte[] head, byte[] mark, byte[] mid)
        {
            uint rawLength = BitConverter.ToUInt32(mark, 1) ^ (uint)((sbyte)head[0] >> 1);
            int units = (int)rawLength;
            var sb = new StringBuilder(units);
            for (int i = 0; i < units; i++)
            {
                int pad = (sbyte)head[i % head.Length] >> 1;
                ushort u = BitConverter.ToUInt16(mid, i * 2);
                sb.Append((char)((u ^ pad ^ 0x4B) & 0xFFFF));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 按 mixing 与 head 反算 mid 区（写入端），head 必须先定稿。
        /// mixing 短于 rawLength 时循环使用，与原生一致。
        /// </summary>
        private static byte[] BuildMid(byte[] head, int rawLength, string mixing)
        {
            byte[] mu = Encoding.Unicode.GetBytes(mixing);
            int units = Math.Max(1, mu.Length / 2);
            byte[] mid = new byte[2 * rawLength];
            for (int i = 0; i < rawLength; i++)
            {
                int pad = (sbyte)head[i % head.Length] >> 1;
                ushort m = BitConverter.ToUInt16(mu, (i % units) * 2);
                ushort v = (ushort)((m ^ pad ^ 0x4B) & 0xFFFF);
                mid[i * 2] = (byte)v;
                mid[i * 2 + 1] = (byte)(v >> 8);
            }
            return mid;
        }

        /// <summary>优先复用原包字节，否则用确定性伪随机填充。</summary>
        private static byte[] BuildRegion(byte[] source, int length, FillRandom junk, byte? firstByte)
        {
            byte[] region;
            if (source != null && source.Length == length)
                region = (byte[])source.Clone();
            else
            {
                region = new byte[length];
                for (int i = 0; i < region.Length; i++)
                    region[i] = junk.NextByte();
                if (firstByte.HasValue)
                    region[0] = firstByte.Value;
            }
            return region;
        }

        private static byte[] CombineTag(uint low, uint high)
        {
            byte[] tag = new byte[8];
            Buffer.BlockCopy(BitConverter.GetBytes(low), 0, tag, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(high), 0, tag, 4, 4);
            return tag;
        }

        /// <summary>
        /// 按原生读取顺序分块加密：读取端每次按“还需字节数向上取整到 64”装载一块并用递增计数器解密，
        /// 因此这里先模拟同样的装载顺序，再对每个块做 ChaCha20 加密。
        /// </summary>
        private static byte[] EncryptReadPattern(byte[] plain, byte[] key, List<int> readSizes)
        {
            var blocks = new List<(int Offset, int Length, ulong Counter)>();
            int consumed = 0;
            int blockRemain = 0;
            ulong counter = 0;
            foreach (int size in readSizes)
            {
                int remaining = size;
                while (remaining > 0)
                {
                    if (blockRemain == 0)
                    {
                        int length = ((remaining + 63) / 64) * 64;
                        blocks.Add((consumed, length, counter));
                        counter += (ulong)(length / 64);
                        blockRemain = length;
                    }
                    int take = Math.Min(remaining, blockRemain);
                    blockRemain -= take;
                    consumed += take;
                    remaining -= take;
                    if (blockRemain == 0 && remaining == 0)
                        counter = 0;
                }
            }

            int total = 0;
            foreach (var block in blocks)
                total = Math.Max(total, block.Offset + block.Length);
            if (plain.Length < total)
                Array.Resize(ref plain, total);

            // 相邻且计数器连续的块合并成一次处理：省掉每块的 ChaCha20 状态初始化与 keystream 分配
            // （官方 10 MB 分包的表区有 3000+ 块，合并后只剩寥寥几次）。
            byte[] cipher = new byte[total];
            int i = 0;
            while (i < blocks.Count)
            {
                int j = i;
                while (j + 1 < blocks.Count
                       && blocks[j + 1].Offset == blocks[j].Offset + blocks[j].Length
                       && blocks[j + 1].Counter == blocks[j].Counter + (ulong)(blocks[j].Length / 64))
                    j++;
                int runOffset = blocks[i].Offset;
                int runLength = blocks[j].Offset + blocks[j].Length - runOffset;
                new ChaCha20(key, blocks[i].Counter, new byte[8]).Process(plain, runOffset, runLength, cipher, runOffset);
                i = j + 1;
            }
            return cipher;
        }

        /// <summary>
        /// 加密单个文件的前 1024 字节（与 <see cref="Extract"/> 对称）。
        /// </summary>
        private static byte[] EncryptStored(LpkRecord record, byte[] stored, int[] digits, byte[] nonce16)
        {
            byte[] outp = (byte[])stored.Clone();
            int length = Math.Min(1024, stored.Length);
            if (length <= 0)
                return outp;

            byte[] key = ComputeExtractKey(record, digits);
            ulong counter = BitConverter.ToUInt64(nonce16, 0);
            byte[] nonce8 = new byte[8];
            Array.Copy(nonce16, 8, nonce8, 0, 8);
            new ChaCha20(key, counter, nonce8).Process(stored, 0, length, outp, 0);
            return outp;
        }

        private static byte[] ZlibDeflate(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var zlib = new ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal, true))
                zlib.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        private static byte[] PadTo64(byte[] data)
        {
            int size = ((data.Length + 63) / 64) * 64;
            if (size == data.Length)
                return data;
            byte[] padded = new byte[size];
            Array.Copy(data, padded, data.Length);
            return padded;
        }

        /// <summary>
        /// 与原生打包器一致的 Adler-32 变体：累加器初值为 0（不是标准实现的 1），结果按 (b &lt;&lt; 16) | a 返回。
        /// 原生格式用它来写记录的 tagLow（原始数据）与 tagHigh（写入的密文）。
        /// </summary>
        private static uint Adler0(byte[] data)
        {
            uint a = 0, b = 0;
            foreach (byte x in data)
            {
                a = (a + x) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        private static void AppendU32(List<byte> buffer, uint value)
        {
            buffer.Add((byte)value);
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 24));
        }

        private static void WriteJunk(Stream stream, long count, FillRandom random)
        {
            byte[] buffer = new byte[4096];
            while (count > 0)
            {
                int size = (int)Math.Min(buffer.Length, count);
                for (int i = 0; i < size; i++)
                    buffer[i] = random.NextByte();
                stream.Write(buffer, 0, size);
                count -= size;
            }
        }

        /// <summary>
        /// 确定性的填充字节生成器（原生文件里这些区域为未初始化内存，内容不影响读取）。
        /// </summary>
        private sealed class FillRandom
        {
            private uint _state;

            public FillRandom(int seed)
            {
                _state = (uint)seed * 2654435761u ^ 0x2545F491u;
                if (_state == 0)
                    _state = 1;
            }

            public byte NextByte()
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return (byte)(_state >> 16);
            }
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

        /// <summary>
        /// 头部校验值（原生加载端会校验，不匹配会被判为无效包）：
        ///   checksum = mark[0] + u32(mark[1..5]) + count + 4 + Σ u16(mid[0..2*rawLength])，32 位回绕。
        /// 该公式由参考实现的原生代码还原，并已用官方包验证一致。
        /// </summary>
        private static uint ComputeHeaderChecksum(byte[] mark, byte[] mid, int count)
        {
            uint markWord = (uint)(mark[1] | (mark[2] << 8) | (mark[3] << 16) | (mark[4] << 24));
            uint sumUnits = 0;
            for (int i = 0; i + 1 < mid.Length; i += 2)
                sumUnits += (uint)(mid[i] | (mid[i + 1] << 8));

            return unchecked(mark[0] + markWord + (uint)count + 4u + sumUnits);
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
