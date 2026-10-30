using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace KartRider.Logging
{
    /// <summary>
    /// 轻量运行指标：全部为 Interlocked 计数器，读取无锁，不参与业务逻辑。
    /// 供 /metrics 与 /status 使用，也可在崩溃报告中输出。
    /// </summary>
    public static class Metrics
    {
        private static readonly long StartTicks = Stopwatch.GetTimestamp();

        public static long TcpPacketsIn;
        public static long TcpPacketsOut;
        public static long TcpBytesIn;
        public static long TcpBytesOut;
        public static long UdpPacketsIn;
        public static long UdpPacketsOut;
        public static long UdpBytesIn;
        public static long UdpBytesOut;
        public static long HandlerErrors;
        public static long ProtocolErrors;
        public static long Disconnects;
        public static long SlowClientDrops;
        public static long ChecksumMismatch;
        public static long ProfileCacheHits;
        public static long ProfileCacheMisses;
        public static long ProfileDiskReads;
        public static long ProfileDiskWrites;
        public static long ChatPackets;

        private static long _lastTcpIn;
        private static long _lastTcpOut;
        private static long _lastUdpIn;
        private static long _lastUdpOut;
        private static long _lastSampleTicks = Stopwatch.GetTimestamp();
        private static double _tcpInRate;
        private static double _tcpOutRate;
        private static double _udpInRate;
        private static double _udpOutRate;

        public static void Inc(ref long counter, long value = 1)
        {
            Interlocked.Add(ref counter, value);
        }

        private static Timer _sampler;

        /// <summary>启动每秒采样（把累计值折算成速率）。重复调用无副作用。</summary>
        public static void StartSampler()
        {
            if (_sampler != null)
            {
                return;
            }
            _sampler = new Timer(_ => SampleRates(), null, 1000, 1000);
        }

        public static TimeSpan Uptime
        {
            get
            {
                double seconds = (Stopwatch.GetTimestamp() - StartTicks) / (double)Stopwatch.Frequency;
                return TimeSpan.FromSeconds(seconds);
            }
        }

        /// <summary>
        /// 每秒调用一次，把累计值折算成速率（滑动窗口为两次调用之间的真实间隔）。
        /// </summary>
        public static void SampleRates()
        {
            long now = Stopwatch.GetTimestamp();
            long last = Interlocked.Exchange(ref _lastSampleTicks, now);
            double seconds = (now - last) / (double)Stopwatch.Frequency;
            if (seconds <= 0.01)
            {
                return;
            }
            long tcpIn = Interlocked.Read(ref TcpPacketsIn);
            long tcpOut = Interlocked.Read(ref TcpPacketsOut);
            long udpIn = Interlocked.Read(ref UdpPacketsIn);
            long udpOut = Interlocked.Read(ref UdpPacketsOut);

            _tcpInRate = (tcpIn - Interlocked.Exchange(ref _lastTcpIn, tcpIn)) / seconds;
            _tcpOutRate = (tcpOut - Interlocked.Exchange(ref _lastTcpOut, tcpOut)) / seconds;
            _udpInRate = (udpIn - Interlocked.Exchange(ref _lastUdpIn, udpIn)) / seconds;
            _udpOutRate = (udpOut - Interlocked.Exchange(ref _lastUdpOut, udpOut)) / seconds;
        }

        public static Dictionary<string, object> Snapshot()
        {
            var d = new Dictionary<string, object>
            {
                ["uptimeSeconds"] = (long)Uptime.TotalSeconds,
                ["tcpIn"] = Interlocked.Read(ref TcpPacketsIn),
                ["tcpOut"] = Interlocked.Read(ref TcpPacketsOut),
                ["tcpBytesIn"] = Interlocked.Read(ref TcpBytesIn),
                ["tcpBytesOut"] = Interlocked.Read(ref TcpBytesOut),
                ["udpIn"] = Interlocked.Read(ref UdpPacketsIn),
                ["udpOut"] = Interlocked.Read(ref UdpPacketsOut),
                ["udpBytesIn"] = Interlocked.Read(ref UdpBytesIn),
                ["udpBytesOut"] = Interlocked.Read(ref UdpBytesOut),
                ["tcpInPerSec"] = Math.Round(_tcpInRate, 1),
                ["tcpOutPerSec"] = Math.Round(_tcpOutRate, 1),
                ["udpInPerSec"] = Math.Round(_udpInRate, 1),
                ["udpOutPerSec"] = Math.Round(_udpOutRate, 1),
                ["handlerErrors"] = Interlocked.Read(ref HandlerErrors),
                ["protocolErrors"] = Interlocked.Read(ref ProtocolErrors),
                ["disconnects"] = Interlocked.Read(ref Disconnects),
                ["slowClientDrops"] = Interlocked.Read(ref SlowClientDrops),
                ["checksumMismatch"] = Interlocked.Read(ref ChecksumMismatch),
                ["profileCacheHits"] = Interlocked.Read(ref ProfileCacheHits),
                ["profileCacheMisses"] = Interlocked.Read(ref ProfileCacheMisses),
                ["profileDiskReads"] = Interlocked.Read(ref ProfileDiskReads),
                ["profileDiskWrites"] = Interlocked.Read(ref ProfileDiskWrites),
                ["chatPackets"] = Interlocked.Read(ref ChatPackets),
                ["workingSetMB"] = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 1),
                ["gcHeapMB"] = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 1)
            };
            return d;
        }
    }
}
