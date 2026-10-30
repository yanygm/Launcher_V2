using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace KartRider.Logging
{
    /// <summary>
    /// 最近 N 个网络包的环形缓冲。
    ///
    /// 目标：既能在需要时看到"对局里发了什么"，又不在热路径上做任何格式化。
    /// 每次收/发包只做一次定长内存拷贝（最多 192 字节）+ 写几个字段，
    /// 不分配、不拼字符串、不碰 IO；要查看时再通过 /packets 或 --dump-packets 输出。
    ///
    /// 容量 16384 条，按 8 人房每秒约 500 包计算，约保留最近 30 秒。
    /// </summary>
    public static class PacketRing
    {
        public const int Capacity = 16384;
        public const int MaxData = 192;

        private struct Record
        {
            public long Tick;
            public uint Opcode;
            public ushort Len;
            public ushort StoredLen;
            public byte Dir;        // 0=C(客户端发来) 1=S(服务端发出) 2=U(UDP)
            public string Nick;     // 直接引用会话里的昵称字符串，不复制
            public byte[] Data;     // 复用的定长块
        }

        private static readonly Record[] Records = new Record[Capacity];
        private static long _writeIndex;
        private static long _total;
        private static readonly object InitLock = new object();
        private static bool _initialized;

        public static long Total => Interlocked.Read(ref _total);

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }
            lock (InitLock)
            {
                if (_initialized)
                {
                    return;
                }
                for (int i = 0; i < Capacity; i++)
                {
                    Records[i].Data = new byte[MaxData];
                }
                _initialized = true;
            }
        }

        /// <summary>热路径调用：只拷贝，不格式化。</summary>
        public static void Add(byte direction, uint opcode, string nickname, byte[] data, int length)
        {
            EnsureInitialized();
            long index = Interlocked.Increment(ref _writeIndex) - 1;
            ref Record rec = ref Records[index % Capacity];
            rec.Tick = Environment.TickCount64;
            rec.Opcode = opcode;
            rec.Len = (ushort)Math.Min(length, ushort.MaxValue);
            rec.StoredLen = (ushort)Math.Min(length, MaxData);
            rec.Dir = direction;
            rec.Nick = nickname;
            if (data != null && rec.StoredLen > 0)
            {
                Buffer.BlockCopy(data, 0, rec.Data, 0, rec.StoredLen);
            }
            Interlocked.Increment(ref _total);
        }

        /// <summary>把字节转成十六进制，每 4 个字符（2 字节）空一格。</summary>
        public static string GroupHex(byte[] data, int offset, int count)
        {
            if (data == null || count <= 0)
            {
                return "";
            }
            int n = Math.Min(count, data.Length - offset);
            if (n <= 0)
            {
                return "";
            }
            string hex = Convert.ToHexString(data, offset, n);
            var sb = new StringBuilder(hex.Length + hex.Length / 4);
            for (int i = 0; i < hex.Length; i += 4)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(hex, i, Math.Min(4, hex.Length - i));
            }
            return sb.ToString();
        }

        /// <summary>按时间顺序取最近 count 条（可选按包名/方向过滤），此时才做字符串格式化。</summary>
        public static List<string> Snapshot(int count, string filter = null, string direction = null)
        {
            EnsureInitialized();
            long total = Interlocked.Read(ref _writeIndex);
            int take = (int)Math.Min(Math.Max(count, 1), Math.Min(total, Capacity));
            var lines = new List<string>(take);
            long start = total - take;
            string dirFilter = direction?.Trim();
            DateTime now = DateTime.Now;
            long currentTick = Environment.TickCount64;

            for (long i = start; i < total; i++)
            {
                ref Record rec = ref Records[i % Capacity];
                if (rec.Tick == 0)
                {
                    continue;
                }
                string opName = DescribeOpcode(rec.Opcode);
                if (!string.IsNullOrEmpty(filter)
                    && opName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                string dir = rec.Dir == 0 ? "C" : rec.Dir == 1 ? "S" : rec.Dir == 2 ? "U" : "M";
                if (!string.IsNullOrEmpty(dirFilter)
                    && !string.Equals(dir, dirFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 记录里存的是 TickCount64，按差值换算回墙钟时间
                DateTime stamp = now.AddMilliseconds(rec.Tick - currentTick);

                var sb = new StringBuilder(160);
                sb.Append('[').Append(dir).Append(']');
                sb.Append('[').Append(stamp.ToString("yyyy-MM-dd HH:mm:ss")).Append(']');
                sb.Append('[').Append(rec.Nick ?? "-").Append("] ");
                sb.Append(opName).Append(": ");
                sb.Append(GroupHex(rec.Data, 0, rec.StoredLen));
                if (rec.StoredLen < rec.Len)
                {
                    sb.Append(" …");
                }
                lines.Add(sb.ToString());
            }
            return lines;
        }

        public static void Clear()
        {
            EnsureInitialized();
            for (int i = 0; i < Capacity; i++)
            {
                Records[i].Tick = 0;
            }
            Interlocked.Exchange(ref _writeIndex, 0);
            Interlocked.Exchange(ref _total, 0);
        }

        private static string DescribeOpcode(uint opcode)
        {
            try
            {
                return Enum.GetName(typeof(KartRider_PacketName.PacketName), opcode) ?? "0x" + opcode.ToString("X8");
            }
            catch
            {
                return "0x" + opcode.ToString("X8");
            }
        }
    }
}
