using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 轻量端口转发器：将 IPv6 [::] 监听流量转发到本地 IPv4 127.0.0.1
    /// 调用 Start(port) 后自动启动以下转发通道：
    ///   port    → TCP + UDP 同时中转
    ///   port+1  → UDP 中转
    ///   port+2  → TCP 中转
    /// 调用 Stop() 结束所有中转
    /// </summary>
    public static class TinyMapper
    {
        private static CancellationTokenSource _cts;
        private static readonly List<IDisposable> _listeners = new();

        /// <summary>
        /// 启动 IPv6→IPv4 端口转发
        /// </summary>
        /// <param name="port">基础端口</param>
        public static void Start()
        {
            // 先停止已有中转
            Stop();

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var localIPv6 = IPAddress.IPv6Any;      // [::]
            var remoteIPv4 = IPAddress.Parse("127.0.0.1");
            var port = ProfileService.SettingConfig.ServerPort;

            Console.WriteLine($"[TinyMapper] 启动 IPv6→IPv4 端口转发");

            // === port: TCP ===
            AddTcpForward(localIPv6, remoteIPv4, port, ct);

            // === port: UDP ===
            AddUdpForward(localIPv6, remoteIPv4, port, ct);

            // === port + 1: UDP ===
            AddUdpForward(localIPv6, remoteIPv4, port + 1, ct);

            // === port + 2: TCP ===
            AddTcpForward(localIPv6, remoteIPv4, port + 2, ct);
        }

        public static void ClientStart()
        {
            // 先停止已有中转
            Stop();

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var localIPv4 = IPAddress.Parse("127.0.0.1");
            var remoteIPv6 = IPAddress.Parse(ProfileService.SettingConfig.ServerIP);
            var port = ProfileService.SettingConfig.ServerPort;

            Console.WriteLine($"[TinyMapper] 启动 IPv4→IPv6 端口转发");

            // === port: TCP ===
            AddTcpForward(localIPv4, remoteIPv6, port, ct);

            // === port: UDP ===
            AddUdpForward(localIPv4, remoteIPv6, port, ct);

            // === port + 1: UDP ===
            AddUdpForward(localIPv4, remoteIPv6, port + 1, ct);

            // === port + 2: TCP ===
            AddTcpForward(localIPv4, remoteIPv6, port + 2, ct);
        }

        /// <summary>
        /// 结束所有端口转发
        /// </summary>
        public static void Stop()
        {
            _cts?.Cancel();
            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    try { listener?.Dispose(); } catch { }
                }
                _listeners.Clear();
            }
            Console.WriteLine("[TinyMapper] 所有端口转发已停止");
        }

        #region 启动转发
        private static void AddTcpForward(IPAddress localIP, IPAddress remoteIP, int port, CancellationToken ct)
        {
            var localEp = new IPEndPoint(localIP, port);
            var remoteEp = new IPEndPoint(remoteIP, port);
            Console.WriteLine($"[TCP] {localEp} → {remoteEp}");

            var listener = new TcpListener(localEp);
            listener.Start(); // 先 Start，成功后再加入 _listeners 防止 Stop() 时对未启动 Listener 调用 Dispose 抛异常
            lock (_listeners) _listeners.Add(listener);
            _ = RunTcpForward(listener, remoteEp, ct);
        }

        private static void AddUdpForward(IPAddress localIP, IPAddress remoteIP, int port, CancellationToken ct)
        {
            var localEp = new IPEndPoint(localIP, port);
            var remoteEp = new IPEndPoint(remoteIP, port);
            Console.WriteLine($"[UDP] {localEp} → {remoteEp}");

            var udp = new UdpClient(localEp);
            lock (_listeners) _listeners.Add(udp);
            _ = RunUdpForward(udp, remoteEp, ct);
        }
        #endregion

        #region TCP 转发
        private static async Task RunTcpForward(TcpListener listener, IPEndPoint remoteEp, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct);
                    _ = HandleTcp(client, remoteEp);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch { }
        }

        private static async Task HandleTcp(TcpClient client, IPEndPoint remoteEp)
        {
            TcpClient remote = null;
            try
            {
                remote = new TcpClient(remoteEp.AddressFamily);
                await remote.ConnectAsync(remoteEp.Address, remoteEp.Port);

                var cs = client.GetStream();
                var rs = remote.GetStream();

                // 双向拷贝竞速：任意一方断开，立即关闭两边连接
                await Task.WhenAny(
                    CopyAsync(cs, rs),
                    CopyAsync(rs, cs)
                );
            }
            catch { }
            finally
            {
                // 关闭连接会中断另一端还在等待的 ReadAsync
                client.Close();
                remote?.Close();
            }
        }

        private static async Task CopyAsync(NetworkStream from, NetworkStream to)
        {
            var buf = new byte[8192];
            int r;
            while ((r = await from.ReadAsync(buf, 0, buf.Length)) > 0)
                await to.WriteAsync(buf, 0, r);
        }
        #endregion

        #region UDP 转发
        private static async Task RunUdpForward(UdpClient udp, IPEndPoint remoteEp, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var req = await udp.ReceiveAsync(ct);
                    _ = HandleUdp(udp, req, remoteEp);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch { }
        }

        private static async Task HandleUdp(UdpClient udp, UdpReceiveResult req, IPEndPoint remoteEp)
        {
            try
            {
                using var remoteUdp = new UdpClient(remoteEp.AddressFamily);
                await remoteUdp.SendAsync(req.Buffer, req.Buffer.Length, remoteEp);

                var res = await remoteUdp.ReceiveAsync();
                await udp.SendAsync(res.Buffer, res.Buffer.Length, req.RemoteEndPoint);
            }
            catch { }
        }
        #endregion
    }
}