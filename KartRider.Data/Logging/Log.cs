using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace KartRider.Logging
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        None = 5
    }

    /// <summary>
    /// 进程级日志器。
    /// 设计目标（针对原实现的两个致命问题）：
    ///   1. 任何路径都不得无界缓存文本 —— 内存中只保留固定容量的环形缓冲（用于崩溃转储）。
    ///   2. 热路径（每包）默认不产生任何字符串 —— PacketTrace 默认关闭，调用方须先判 IsPacketTraceEnabled。
    /// </summary>
    public static class Log
    {
        private static readonly object Gate = new object();
        private static readonly Queue<string> Tail = new Queue<string>(1024);
        private const int TailCapacity = 512;

        private static volatile LogLevel _minLevel = LogLevel.Info;
        private static volatile bool _enabled = true;
        private static volatile bool _fileEnabled;
        private static volatile bool _packetTrace;
        private static volatile bool _console = true;
        private static volatile bool _initialized;

        // 包跟踪（实时文本）与环形缓冲（零格式化）
        private static volatile bool _ringEnabled = true;
        private static volatile bool _liveTrace;
        private static string _traceFilter = "";
        private static int _traceSample = 1;
        private static bool _traceToConsole;
        private static int _traceMaxPerSecond = 200;
        private static long _traceSeen;
        private static long _traceWindowStart;
        private static int _traceInWindow;
        private static readonly object TraceGate = new object();
        private static volatile bool _showUdp = true;
        private static StreamWriter _traceWriter;
        private static long _traceBytes;

        private static string _dir;
        private static string _filePath;
        private static StreamWriter _writer;
        private static long _writtenBytes;
        private const long MaxFileBytes = 8L * 1024 * 1024;
        private const int MaxFiles = 5;

        public static LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

        public static bool IsPacketTraceEnabled => _packetTrace && _enabled;

        /// <summary>日志总开关；关闭后不写控制台、不写文件、不做包记录。</summary>
        public static bool Enabled => _enabled;

        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>是否写日志文件；关闭时只输出到控制台。</summary>
        public static void SetFileEnabled(bool enabled)
        {
            _fileEnabled = enabled;
        }

        /// <summary>环形缓冲是否开启（默认开）。</summary>
        public static bool IsPacketRingEnabled => _ringEnabled && _enabled;

        public static void Init(string directory, LogLevel minLevel = LogLevel.Info, bool packetTrace = false, bool console = true, bool announce = true)
        {
            lock (Gate)
            {
                try
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                }
                catch
                {
                }
                _writer = null;

                _dir = directory;
                _minLevel = minLevel;
                _packetTrace = packetTrace;
                _console = console;
                try
                {
                    if (string.IsNullOrEmpty(_dir))
                    {
                        _dir = AppContext.BaseDirectory;
                    }
                    // 未开启文件日志时不要创建 Logs 目录和空日志文件
                    if (_fileEnabled)
                    {
                        Directory.CreateDirectory(_dir);
                        OpenWriter();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[Log] 初始化失败: " + ex.Message);
                    _writer = null;
                }
                _initialized = true;
            }
            if (announce)
            {
                Info("[Log] 日志已启动，级别=" + minLevel + "，包跟踪=" + packetTrace + "，目录=" + _dir);
            }
        }

        private static void OpenWriter()
        {
            if (string.IsNullOrEmpty(_dir))
            {
                return;
            }
            _filePath = Path.Combine(_dir, "launcher.log");
            _writtenBytes = 0;
            var fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 8192, FileOptions.None);
            _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
        }

        private static void RotateIfNeeded()
        {
            if (_writer == null || _writtenBytes < MaxFileBytes)
            {
                return;
            }
            try
            {
                _writer.Dispose();
                for (int i = MaxFiles - 1; i >= 1; i--)
                {
                    string from = Path.Combine(_dir, i == 1 ? "launcher.log" : $"launcher.{i - 1}.log");
                    string to = Path.Combine(_dir, $"launcher.{i}.log");
                    if (File.Exists(from))
                    {
                        File.Copy(from, to, true);
                    }
                }
                _writtenBytes = 0;
                OpenWriter();
            }
            catch
            {
                // 轮转失败不影响主流程，继续写原文件
            }
        }

        private static void Write(LogLevel level, string message, Exception ex)
        {
            if (!_enabled)
            {
                return;
            }
            if (level < _minLevel)
            {
                return;
            }
            string line = $"{DateTime.Now:HH:mm:ss.fff} [{level,-5}] {message}";
            if (ex != null)
            {
                line += Environment.NewLine + ex;
            }

            lock (Gate)
            {
                if (_console)
                {
                    Console.WriteLine(line);
                }
                if (_writer != null && _fileEnabled)
                {
                    try
                    {
                        _writer.WriteLine(line);
                        _writtenBytes += line.Length + 2;
                        RotateIfNeeded();
                    }
                    catch
                    {
                        _writer = null; // 写盘失败后不再重试，避免异常风暴
                    }
                }
                if (level >= LogLevel.Warn)
                {
                    if (Tail.Count >= TailCapacity)
                    {
                        Tail.Dequeue();
                    }
                    Tail.Enqueue(line);
                }
            }
        }

        public static void Trace(string message) => Write(LogLevel.Trace, message, null);
        public static void Debug(string message) => Write(LogLevel.Debug, message, null);
        public static void Info(string message) => Write(LogLevel.Info, message, null);
        public static void Warn(string message, Exception ex = null) => Write(LogLevel.Warn, message, ex);
        public static void Error(string message, Exception ex = null) => Write(LogLevel.Error, message, ex);

        /// <summary>
        /// 每包跟踪：仅在 IsPacketTraceEnabled 为 true 时由调用方创建字符串。
        /// </summary>
        public static void Packet(string direction, string nickname, byte[] data, int length, uint? opcodeOverride = null)
        {
            if (!_enabled || data == null || length <= 0)
            {
                return;
            }

            // 方向：C=客户端→服务端，S=服务端→客户端，U=UDP 对局通道，M=聊天服
            byte dir = direction == "C" ? (byte)0
                     : direction == "U" ? (byte)2
                     : direction == "M" ? (byte)3
                     : (byte)1;

            // UDP 载荷开头是 accountID，不是包名哈希，必须由调用方传入真实包名
            uint opcode = opcodeOverride ?? (length >= 4 ? BitConverter.ToUInt32(data, 0) : 0u);

            // 1) 环形缓冲：只做一次定长拷贝，零格式化 —— 用于事后查看（/packets）
            if (_ringEnabled)
            {
                PacketRing.Add(dir, opcode, nickname, data, length);
            }

            // 2) 实时文本跟踪：默认关闭，开启后可按包名过滤 + 采样 + 限速
            if (!_liveTrace)
            {
                return;
            }
            if (dir == 2 && !_showUdp)
            {
                return;
            }

            // 客户端上行包(C)一律全量落盘：过滤/采样/限速只作用于服务端(S)/UDP(U)/聊天服(M)。
            // 原因：判断“客户端到底有没有发这个包”是负向证据，任何抽样或限速都会把证据抹掉。
            bool isClientPacket = dir == 0;
            string name = DescribeOpcode(opcode);
            if (!isClientPacket)
            {
                if (!string.IsNullOrEmpty(_traceFilter)
                    && name.IndexOf(_traceFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }
                if (_traceSample > 1 && (Interlocked.Increment(ref _traceSeen) % _traceSample) != 0)
                {
                    return;
                }
                if (!AcquireTraceToken())
                {
                    return;
                }
            }

            string dirText = dir == 0 ? "C" : dir == 2 ? "U" : dir == 3 ? "M" : "S";
            string line = $"[{dirText}][{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{nickname ?? "-"}] "
                          + $"{name}: {PacketRing.GroupHex(data, 0, Math.Min(length, 512))}";
            // 控制台是当前唯一的落盘通道（ConsoleLogger 退出时保存），C 包强制打印，
            // 否则 PacketTraceConsole=false 时客户端包就彻底消失了。
            WriteTrace(line, forceConsole: isClientPacket);
        }

        private static bool AcquireTraceToken()
        {
            long now = Environment.TickCount64;
            lock (Gate)
            {
                if (now - _traceWindowStart >= 1000)
                {
                    _traceWindowStart = now;
                    _traceInWindow = 0;
                }
                if (_traceInWindow >= _traceMaxPerSecond)
                {
                    return false;
                }
                _traceInWindow++;
                return true;
            }
        }

        private static void WriteTrace(string line, bool forceConsole = false)
        {
            try
            {
                if (_traceToConsole || forceConsole)
                {
                    Console.WriteLine(line);
                }
            }
            catch
            {
            }

            if (!_fileEnabled)
            {
                return; // 只输出控制台，不落盘
            }

            lock (TraceGate)
            {
                try
                {
                    if (_traceWriter == null)
                    {
                        Directory.CreateDirectory(_dir ?? AppContext.BaseDirectory);
                        string path = Path.Combine(_dir ?? AppContext.BaseDirectory, "packets.log");
                        _traceWriter = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 16384), new UTF8Encoding(false))
                        {
                            // 崩溃前也要能看到最后几个包
                            AutoFlush = true
                        };
                        _traceBytes = 0;
                    }
                    _traceWriter.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {line}");
                    _traceBytes += line.Length + 16;
                    if (_traceBytes > 32L * 1024 * 1024)
                    {
                        _traceWriter.Flush();
                        _traceWriter.Dispose();
                        _traceWriter = null; // 下次写入时重新打开（追加），文件过大时人工清理
                    }
                }
                catch
                {
                    _traceWriter = null;
                }
            }
        }

        /// <summary>
        /// 配置包跟踪。
        /// </summary>
        /// <param name="liveTrace">实时文本输出（默认关，开销最大）</param>
        /// <param name="filter">包名包含该子串才输出；空=全部</param>
        /// <param name="sample">每 N 个包取 1 个</param>
        /// <param name="toConsole">是否同时打印到控制台（高码率时很慢，建议只写文件）</param>
        /// <param name="ringEnabled">环形缓冲（默认开，热路径只有一次拷贝）</param>
        /// <param name="maxPerSecond">实时输出每秒上限</param>
        public static void ConfigurePacketTrace(bool liveTrace, string filter, int sample, bool toConsole, bool ringEnabled, int maxPerSecond = 500, bool showUdp = true)
        {
            _liveTrace = liveTrace;
            _traceFilter = filter ?? "";
            _traceSample = Math.Max(1, sample);
            _traceToConsole = toConsole;
            _ringEnabled = ringEnabled;
            _traceMaxPerSecond = Math.Max(1, maxPerSecond);
            _showUdp = showUdp;

            if (liveTrace)
            {
                Info($"[Log] 包输出已开启：S/U/M 过滤='{(_traceFilter == "" ? "全部" : _traceFilter)}'，采样 1/{_traceSample}，"
                     + $"每秒上限 {_traceMaxPerSecond}，控制台={(toConsole ? "是" : "否")}；C（客户端上行）全量记录，不采样、不过滤、不限速");
                Info("[Log] 格式: [方向][时间][昵称] 包名: 十六进制(每4字符空格)；未知包名显示为 uint 十进制值；"
                     + "C=客户端→服务端(全量), S=服务端→客户端, U=UDP对局, M=聊天服" + (showUdp ? "" : "（U 已隐藏）"));
                if (!toConsole)
                {
                    Info("[Log] 提示：当前 PacketTraceConsole=false，包只写文件不打印到控制台；要看控制台请设为 true，或用 --trace 启动");
                }
            }
            else
            {
                Info("[Log] 包输出: 关（需要时把 PacketTrace 设为 true，或用 --trace 启动；/packets 可回看最近包）");
            }
            if (ringEnabled)
            {
                Info("[Log] 包环形缓冲: 开（最近 " + PacketRing.Capacity + " 条，GET /packets?n=200 查看）");
            }
        }

        private static string DescribeOpcode(uint opcode)
        {
            try
            {
                return Enum.GetName(typeof(KartRider_PacketName.PacketName), opcode) ?? opcode.ToString();
            }
            catch
            {
                return opcode.ToString();
            }
        }

        /// <summary>
        /// 崩溃时导出最近的 Warn/Error 上下文（不含每包日志，容量固定）。
        /// </summary>
        public static string DumpRecent(int maxLines = 200)
        {
            lock (Gate)
            {
                var sb = new StringBuilder();
                int skip = Math.Max(0, Tail.Count - maxLines);
                int i = 0;
                foreach (string line in Tail)
                {
                    if (i++ >= skip)
                    {
                        sb.AppendLine(line);
                    }
                }
                return sb.ToString();
            }
        }

        public static string CurrentFile => _filePath;

        public static void Shutdown()
        {
            lock (Gate)
            {
                try
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                }
                catch
                {
                }
                _writer = null;
            }
            lock (TraceGate)
            {
                try
                {
                    _traceWriter?.Flush();
                    _traceWriter?.Dispose();
                }
                catch
                {
                }
                _traceWriter = null;
            }
        }
    }
}
