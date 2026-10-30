using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 轻量端口转发：IPv6 [::] ↔ IPv4 127.0.0.1（服务端/客户端两个方向）。
    ///
    /// 原实现的 UDP 转发是"每个数据报 new UdpClient() + 无限等待回包"，
    /// 每个包一个句柄、对端不回就永久泄漏。现在改成"每个来源端点一条长连 socket"，
    /// 空闲 2 分钟回收，回包按来源端点回送，RTT 与乱序都恢复正常。
    /// </summary>
    public static class TinyMapper
    {
        private static CancellationTokenSource _cts;
        private static readonly List<IDisposable> _listeners = new();
        private static readonly ConcurrentDictionary<string, UdpPeer> _udpPeers = new(StringComparer.Ordinal);
        private static Timer _evictTimer;
        private static readonly object InitLock = new();

        private const int UdpIdleMs = 120_000;

        private sealed class UdpPeer
        {
            public UdpClient Socket;
            public IPEndPoint Source;
            public UdpClient Local;
            public long LastSeen;
            public CancellationTokenSource Cts;
        }

        public static void Start()
        {
            lock (InitLock)
            {
                Stop();
                _cts = new CancellationTokenSource();
                CancellationToken ct = _cts.Token;

                IPAddress localIPv6 = IPAddress.IPv6Any;
                IPAddress remoteIPv4 = IPAddress.Parse("127.0.0.1");
                int port = ProfileService.SettingConfig.ServerPort;

                Log.Info("[TinyMapper] 启动 IPv6→IPv4 端口转发");
                AddTcpForward(localIPv6, remoteIPv4, port, ct);
                AddUdpForward(localIPv6, remoteIPv4, port, ct);
                AddUdpForward(localIPv6, remoteIPv4, port + 1, ct);
                AddTcpForward(localIPv6, remoteIPv4, port + 2, ct);
                StartEvictTimer();
            }
        }

        public static void ClientStart()
        {
            lock (InitLock)
            {
                Stop();
                _cts = new CancellationTokenSource();
                CancellationToken ct = _cts.Token;

                IPAddress localIPv4 = IPAddress.Parse("127.0.0.1");
                IPAddress remoteIPv6 = IPAddress.Parse(ProfileService.SettingConfig.ServerIP);
                int port = ProfileService.SettingConfig.ServerPort;

                Log.Info("[TinyMapper] 启动 IPv4→IPv6 端口转发");
                AddTcpForward(localIPv4, remoteIPv6, port, ct);
                AddUdpForward(localIPv4, remoteIPv6, port, ct);
                AddUdpForward(localIPv4, remoteIPv6, port + 1, ct);
                AddTcpForward(localIPv4, remoteIPv6, port + 2, ct);
                StartEvictTimer();
            }
        }

        public static void Stop()
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            lock (_listeners)
            {
                foreach (IDisposable listener in _listeners)
                {
                    try { listener?.Dispose(); } catch { }
                }
                _listeners.Clear();
            }

            foreach (var pair in _udpPeers)
            {
                DisposePeer(pair.Value);
            }
            _udpPeers.Clear();

            _evictTimer?.Dispose();
            _evictTimer = null;
            Log.Debug("[TinyMapper] 所有端口转发已停止");
        }

        private static void StartEvictTimer()
        {
            _evictTimer?.Dispose();
            _evictTimer = new Timer(_ => EvictIdlePeers(), null, 30_000, 30_000);
        }

        private static void EvictIdlePeers()
        {
            long now = Environment.TickCount64;
            foreach (var pair in _udpPeers)
            {
                if (now - pair.Value.LastSeen > UdpIdleMs && _udpPeers.TryRemove(pair.Key, out UdpPeer peer))
                {
                    DisposePeer(peer);
                }
            }
        }

        private static void DisposePeer(UdpPeer peer)
        {
            try { peer.Cts?.Cancel(); } catch { }
            try { peer.Socket?.Dispose(); } catch { }
            try { peer.Cts?.Dispose(); } catch { }
        }

        #region 启动转发
        private static void AddTcpForward(IPAddress localIP, IPAddress remoteIP, int port, CancellationToken ct)
        {
            var localEp = new IPEndPoint(localIP, port);
            var remoteEp = new IPEndPoint(remoteIP, port);
            Log.Info($"[TinyMapper][TCP] {localEp} → {remoteEp}");

            var listener = new TcpListener(localEp);
            listener.Start();
            lock (_listeners) _listeners.Add(listener);
            _ = RunTcpForward(listener, remoteEp, ct);
        }

        private static void AddUdpForward(IPAddress localIP, IPAddress remoteIP, int port, CancellationToken ct)
        {
            var localEp = new IPEndPoint(localIP, port);
            var remoteEp = new IPEndPoint(remoteIP, port);
            Log.Info($"[TinyMapper][UDP] {localEp} → {remoteEp}");

            var udp = new UdpClient(localEp);
            try
            {
                udp.Client.ReceiveBufferSize = 256 * 1024;
                udp.Client.SendBufferSize = 256 * 1024;
            }
            catch
            {
            }
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
                    TcpClient client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    _ = HandleTcp(client, remoteEp, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Log.Debug($"[TinyMapper] TCP 接受结束: {ex.Message}");
            }
        }

        private static async Task HandleTcp(TcpClient client, IPEndPoint remoteEp, CancellationToken ct)
        {
            TcpClient remote = null;
            try
            {
                client.NoDelay = true;
                remote = new TcpClient(remoteEp.AddressFamily) { NoDelay = true };
                await remote.ConnectAsync(remoteEp.Address, remoteEp.Port, ct).ConfigureAwait(false);

                NetworkStream cs = client.GetStream();
                NetworkStream rs = remote.GetStream();

                await Task.WhenAny(
                    CopyAsync(cs, rs, ct),
                    CopyAsync(rs, cs, ct)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Debug($"[TinyMapper] TCP 转发中断: {ex.Message}");
            }
            finally
            {
                try { client.Close(); } catch { }
                try { remote?.Close(); } catch { }
            }
        }

        private static async Task CopyAsync(NetworkStream from, NetworkStream to, CancellationToken ct)
        {
            byte[] buffer = new byte[8192];
            try
            {
                int read;
                while ((read = await from.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                {
                    await to.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Debug($"[TinyMapper] TCP 拷贝结束: {ex.Message}");
            }
        }
        #endregion

        #region UDP 转发
        private static async Task RunUdpForward(UdpClient local, IPEndPoint remoteEp, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    UdpReceiveResult req = await local.ReceiveAsync(ct).ConfigureAwait(false);
                    UdpPeer peer = GetOrCreatePeer(local, req.RemoteEndPoint, remoteEp, ct);
                    if (peer == null)
                    {
                        continue;
                    }
                    peer.LastSeen = Environment.TickCount64;
                    try
                    {
                        await peer.Socket.SendAsync(req.Buffer, req.Buffer.Length, remoteEp).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"[TinyMapper] UDP 转发失败: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Log.Debug($"[TinyMapper] UDP 循环结束: {ex.Message}");
            }
        }

        private static UdpPeer GetOrCreatePeer(UdpClient local, IPEndPoint source, IPEndPoint remoteEp, CancellationToken ct)
        {
            string key = source.ToString() + ">" + remoteEp;
            if (_udpPeers.TryGetValue(key, out UdpPeer existing))
            {
                return existing;
            }

            try
            {
                var socket = new UdpClient(remoteEp.AddressFamily);
                try
                {
                    socket.Client.ReceiveBufferSize = 256 * 1024;
                }
                catch
                {
                }

                var peer = new UdpPeer
                {
                    Socket = socket,
                    Source = source,
                    Local = local,
                    LastSeen = Environment.TickCount64,
                    Cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                };
                UdpPeer registered = _udpPeers.GetOrAdd(key, peer);
                if (!ReferenceEquals(registered, peer))
                {
                    // 并发创建：保留已存在的通道，销毁刚建的多余 socket
                    DisposePeer(peer);
                    return registered;
                }
                _ = PumpReplies(peer);
                return peer;
            }
            catch (Exception ex)
            {
                Log.Warn($"[TinyMapper] 创建 UDP 转发通道失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>把远端回包送回原始来源端点（同一个来源复用同一条 socket，保证端口与顺序）。</summary>
        private static async Task PumpReplies(UdpPeer peer)
        {
            try
            {
                CancellationToken ct = peer.Cts.Token;
                while (!ct.IsCancellationRequested)
                {
                    UdpReceiveResult res = await peer.Socket.ReceiveAsync(ct).ConfigureAwait(false);
                    peer.LastSeen = Environment.TickCount64;
                    try
                    {
                        await peer.Local.SendAsync(res.Buffer, res.Buffer.Length, peer.Source).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"[TinyMapper] UDP 回送失败: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Log.Debug($"[TinyMapper] UDP 回送循环结束: {ex.Message}");
            }
        }
        #endregion
    }
}
