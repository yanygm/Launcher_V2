using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using KartRider.Logging;

namespace KartRider
{
    /// <summary>
    /// 游戏主 TCP 服务：接受客户端连接并封装成 <see cref="SessionGroup"/> 交给 ClientManager。
    ///
    /// 启停模型与 MsgrServer / UdpServer 保持一致（实例 + _lockObj + _isRunning + Start/Stop）。
    /// 这里只负责「监听 / 接受连接」，连接建立后的加解密、粘包重组与包分发
    /// 仍由 SessionGroup / ClientSession / IPacketHandler 完成。
    /// </summary>
    public class TCPServer
    {
        private TcpListener _tcpListener;
        private int _listenPort;
        private readonly string _serverName;
        private volatile bool _isRunning;
        private readonly object _lockObj = new object();

        /// <summary>最近一次接受的会话（对齐原 RouterListener.MySession 语义）。</summary>
        public SessionGroup LastSession { get; set; }

        public TCPServer(string serverName, int listenPort)
        {
            _serverName = serverName;
            _listenPort = listenPort;
            _isRunning = false;
        }

        public bool IsRunning => _isRunning;

        /// <summary>监听端口；仅在未运行时允许修改，Start 时生效。</summary>
        public int ListenPort
        {
            get => _listenPort;
            set
            {
                lock (_lockObj)
                {
                    if (!_isRunning)
                    {
                        _listenPort = value;
                    }
                }
            }
        }

        /// <summary>
        /// 绑定并开始接受连接。
        /// 端口占用等绑定失败会抛出 SocketException，交由调用方提示用户。
        /// </summary>
        public void Start()
        {
            lock (_lockObj)
            {
                if (_isRunning)
                {
                    return;
                }

                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(IPAddress.Any, _listenPort);
                    listener.Start();
                }
                catch (SocketException ex)
                {
                    Log.Error($"[{_serverName}] 启动失败（端口可能被占用）: {ex.Message}");
                    CloseListener(listener);
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error($"[{_serverName}] 启动异常", ex);
                    CloseListener(listener);
                    throw;
                }

                _tcpListener = listener;
                _isRunning = true;
                Log.Info($"[{_serverName}] TCP 已启动，监听端口 {_listenPort}");
                BeginAcceptClient();
            }
        }

        public void Stop()
        {
            lock (_lockObj)
            {
                if (!_isRunning)
                {
                    return;
                }
                _isRunning = false;
                try
                {
                    _tcpListener?.Stop();
                }
                catch (Exception ex)
                {
                    Log.Warn($"[{_serverName}] 停止异常: {ex.Message}");
                }
                finally
                {
                    _tcpListener = null;
                }
                Log.Info($"[{_serverName}] TCP 已停止");
            }
        }

        private void BeginAcceptClient()
        {
            TcpListener listener = _tcpListener;
            if (!_isRunning || listener == null)
                return;
            try
            {
                listener.BeginAcceptSocket(EndAcceptClient, null);
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Log.Warn($"[{_serverName}] 接受连接异常: {ex.Message}");
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(1000);
                        BeginAcceptClient();
                    });
                }
            }
        }

        private void EndAcceptClient(IAsyncResult ar)
        {
            TcpListener listener = _tcpListener;
            if (!_isRunning || listener == null)
                return;

            try
            {
                Socket clientSocket = listener.EndAcceptSocket(ar);

                // 创建客户端会话（构造函数内即开始收包），加解密与包分发逻辑保持不变
                var session = new SessionGroup(clientSocket, null);
                LastSession = session;

                ClientManager.AddClient(session);
            }
            catch (ObjectDisposedException)
            {
                // 监听器已停止/释放，忽略
            }
            catch (Exception ex)
            {
                Log.Debug($"[{_serverName}] 接受连接异常: {ex.Message}");
            }
            finally
            {
                if (_isRunning)
                {
                    BeginAcceptClient();
                }
            }
        }

        private static void CloseListener(TcpListener listener)
        {
            if (listener == null)
                return;
            try
            {
                listener.Stop();
            }
            catch
            {
            }
        }
    }
}
