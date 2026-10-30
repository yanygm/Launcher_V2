using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 好友/私聊/公会聊天 TCP 服务。
    ///
    /// 原实现的两个硬伤：
    ///   1. 读到长度前缀却完全不用，把一次 Read 的字节当成完整包 → TCP 粘包/拆包必错；
    ///   2. 同一 NetworkStream 上并发 BeginWrite，帧会交错。
    /// 现在：按 4 字节长度前缀做累积重组；发送串行化（聊天流量小，同步写足够且简单）。
    /// </summary>
    public class MsgrServer
    {
        private const int HeaderSize = 4;
        private const int MaxPacket = 64 * 1024;

        private TcpListener _tcpListener;
        private readonly int _listenPort;
        private readonly string _serverName;
        private volatile bool _isRunning;
        private readonly object _lockObj = new object();

        public static ConcurrentDictionary<string, ClientState> Clients = new ConcurrentDictionary<string, ClientState>(StringComparer.Ordinal);
        public static ConcurrentDictionary<uint, List<string>> rooms = new ConcurrentDictionary<uint, List<string>>();
        public static uint roomID = 1;

        public MsgrServer(string serverName, int listenPort)
        {
            _serverName = serverName;
            _listenPort = listenPort;
            _isRunning = false;
        }

        public void Start()
        {
            lock (_lockObj)
            {
                if (_isRunning)
                {
                    return;
                }
                try
                {
                    _tcpListener = new TcpListener(IPAddress.Any, _listenPort);
                    _tcpListener.Start();
                    _isRunning = true;
                    Log.Info($"[{_serverName}] 已启动，监听端口 {_listenPort}");
                    BeginAcceptClient();
                }
                catch (SocketException ex)
                {
                    Log.Error($"[{_serverName}] 启动失败（端口可能被占用）: {ex.Message}");
                    _isRunning = false;
                }
                catch (Exception ex)
                {
                    Log.Error($"[{_serverName}] 启动异常", ex);
                    _isRunning = false;
                }
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
                Log.Info($"[{_serverName}] 已停止");
            }
        }

        private void BeginAcceptClient()
        {
            if (!_isRunning || _tcpListener == null)
                return;
            try
            {
                _tcpListener.BeginAcceptTcpClient(EndAcceptClient, null);
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
            if (!_isRunning || _tcpListener == null)
                return;

            TcpClient client = null;
            try
            {
                client = _tcpListener.EndAcceptTcpClient(ar);
                try
                {
                    client.NoDelay = true;
                }
                catch
                {
                }
                var clientState = new ClientState { Client = client, Buffer = new byte[4096] };
                Log.Debug($"[{_serverName}] 客户端连接：{client.Client.RemoteEndPoint}");
                BeginReceive(clientState);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex)
            {
                Log.Warn($"[{_serverName}] 接受连接异常: {ex.SocketErrorCode}");
            }
            catch (Exception ex)
            {
                Log.Warn($"[{_serverName}] 接受连接异常", ex);
                client?.Close();
            }
            finally
            {
                if (_isRunning)
                {
                    BeginAcceptClient();
                }
            }
        }

        private void BeginReceive(ClientState clientState)
        {
            if (!_isRunning || clientState.Client == null || !clientState.Client.Connected)
                return;
            try
            {
                NetworkStream stream = clientState.Client.GetStream();
                stream.BeginRead(clientState.Buffer, 0, clientState.Buffer.Length, EndReceive, clientState);
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Log.Debug($"[{_serverName}] 接收异常: {ex.Message}");
                    CloseClient(clientState);
                }
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
            var clientState = ar.AsyncState as ClientState;
            if (clientState == null || clientState.Client == null)
                return;

            int bytesRead;
            try
            {
                bytesRead = clientState.Client.GetStream().EndRead(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Debug($"[{_serverName}] 读取结束异常: {ex.Message}");
                CloseClient(clientState);
                return;
            }

            if (bytesRead <= 0)
            {
                CloseClient(clientState);
                return;
            }

            Metrics.Inc(ref Metrics.ChatPackets);
            Metrics.Inc(ref Metrics.TcpBytesIn, bytesRead);

            lock (clientState.PendingLock)
            {
                clientState.Pending.AddRange(new ArraySegment<byte>(clientState.Buffer, 0, bytesRead));
            }

            if (!ProcessPending(clientState))
            {
                CloseClient(clientState);
                return;
            }

            if (_isRunning)
            {
                BeginReceive(clientState);
            }
        }

        /// <summary>按长度前缀重组并逐包分发。返回 false 表示流已不可用。</summary>
        private bool ProcessPending(ClientState clientState)
        {
            while (true)
            {
                byte[] packet = null;
                lock (clientState.PendingLock)
                {
                    List<byte> pending = clientState.Pending;
                    if (pending.Count < HeaderSize)
                    {
                        return true;
                    }
                    int declared = pending[0] | (pending[1] << 8) | (pending[2] << 16) | (pending[3] << 24);
                    if (declared <= 0 || declared > MaxPacket)
                    {
                        Log.Warn($"[{_serverName}] 非法包长 {declared}，断开连接");
                        return false;
                    }
                    if (pending.Count < HeaderSize + declared)
                    {
                        return true; // 半包，继续等
                    }
                    packet = new byte[HeaderSize + declared];
                    pending.CopyTo(0, packet, 0, packet.Length);
                    pending.RemoveRange(0, packet.Length);
                }

                try
                {
                    HandlePacket(clientState, packet);
                }
                catch (Exception ex)
                {
                    // 单个聊天包的解析错误不应断开连接
                    Metrics.Inc(ref Metrics.HandlerErrors);
                    Log.Warn($"[{_serverName}] 处理聊天包异常", ex);
                }
            }
        }

        private void HandlePacket(ClientState clientState, byte[] packet)
        {
            using InPacket inPacket = new InPacket(packet);
            inPacket.ReadInt(); // 长度前缀
            var packetValue = (PacketName)inPacket.ReadUInt();

            // 聊天包前 4 字节是长度前缀，包名在偏移 8 处，必须显式传入（否则显示成 0x00000004）
            Log.Packet("M", "chat", packet, packet.Length, (uint)packetValue);

            if (PacketDispatcher.Dispatch(typeof(MsgrServer), packetValue, inPacket, packet, clientState, this))
                return;

            switch (packetValue)
            {
                case PacketName.PqEnterChatServer:
                {
                    inPacket.ReadUInt();
                    inPacket.ReadUInt();
                    string nickname = inPacket.ReadString();
                    if (string.IsNullOrEmpty(nickname))
                    {
                        return;
                    }
                    Clients.AddOrUpdate(nickname, clientState, (key, oldState) =>
                    {
                        if (!ReferenceEquals(oldState, clientState))
                        {
                            try
                            {
                                oldState.Client?.Close();
                            }
                            catch
                            {
                            }
                        }
                        return clientState;
                    });
                    break;
                }
                case PacketName.PqInitInviteMsgrChat:
                {
                    uint accountID1 = inPacket.ReadUInt();
                    uint accountID2 = inPacket.ReadUInt();
                    string nickname1 = inPacket.ReadString();
                    string nickname2 = inPacket.ReadString();
                    uint id = Interlocked.Increment(ref roomID);
                    using (OutPacket outPacket = new OutPacket("PrInitInviteMsgrChat"))
                    {
                        outPacket.WriteUInt(accountID1);
                        outPacket.WriteUInt(accountID2);
                        outPacket.WriteString(nickname1);
                        outPacket.WriteString(nickname2);
                        outPacket.WriteUInt(id);
                        outPacket.WriteInt(0);
                        byte[] payload = outPacket.ToArray();
                        if (Clients.TryGetValue(nickname1, out var c1))
                        {
                            Send(c1, payload);
                        }
                        if (Clients.TryGetValue(nickname2, out var c2))
                        {
                            Send(c2, payload);
                        }
                        rooms.TryAdd(id, new List<string> { nickname1, nickname2 });
                    }
                    break;
                }
                case PacketName.PqMsgrChat:
                {
                    uint id = inPacket.ReadUInt();
                    string nickname = inPacket.ReadString();
                    string message = inPacket.ReadString();
                    if (!rooms.TryGetValue(id, out List<string> members))
                    {
                        Log.Debug($"[{_serverName}] 聊天室 {id} 不存在");
                        return;
                    }
                    using (OutPacket outPacket = new OutPacket("PrMsgrChat"))
                    {
                        outPacket.WriteUInt(id);
                        outPacket.WriteUInt(ClientManager.GetUserNO(nickname));
                        outPacket.WriteString(nickname);
                        outPacket.WriteString(message ?? "");
                        outPacket.WriteInt(0);
                        Broadcast(members, outPacket.ToArray());
                    }
                    break;
                }
                case PacketName.PqLeaveMsgrChat:
                {
                    uint userNO = inPacket.ReadUInt();
                    uint id = inPacket.ReadUInt();
                    string nickname = ClientManager.GetNickname(userNO);
                    if (string.IsNullOrEmpty(nickname))
                        return;
                    if (!rooms.TryGetValue(id, out List<string> members))
                        return;
                    members.Remove(nickname);
                    if (members.Count == 0)
                    {
                        rooms.TryRemove(id, out _);
                        return;
                    }
                    using (OutPacket outPacket = new OutPacket("PrLeaveMsgrChat"))
                    {
                        outPacket.WriteUInt(userNO);
                        outPacket.WriteUInt(id);
                        Broadcast(members, outPacket.ToArray());
                    }
                    break;
                }
                case PacketName.PqGuildChat:
                {
                    string nickname = inPacket.ReadString();
                    string message = inPacket.ReadString();
                    using (OutPacket outPacket = new OutPacket("PrGuildChat"))
                    {
                        outPacket.WriteString(nickname);
                        outPacket.WriteString(message ?? "");
                        byte[] payload = outPacket.ToArray();
                        foreach (var client in Clients.Values)
                        {
                            Send(client, payload);
                        }
                    }
                    break;
                }
                default:
                    Log.Debug($"[{_serverName}] 未处理的包 {packetValue}");
                    break;
            }
        }

        private void Broadcast(List<string> members, byte[] payload)
        {
            foreach (string member in members.ToArray())
            {
                if (Clients.TryGetValue(member, out ClientState client))
                {
                    Send(client, payload);
                }
            }
        }

        /// <summary>串行写：同一连接上的帧不会交错。body 不含 4 字节长度前缀。</summary>
        public bool Send(ClientState clientState, byte[] body)
        {
            if (body == null)
            {
                return false;
            }
            byte[] framed = new byte[HeaderSize + body.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(body.Length), 0, framed, 0, HeaderSize);
            Buffer.BlockCopy(body, 0, framed, HeaderSize, body.Length);
            return SendRaw(clientState, framed);
        }

        /// <summary>发送已经带长度前缀的完整帧。</summary>
        private bool SendRaw(ClientState clientState, byte[] framed)
        {
            if (!_isRunning || clientState?.Client == null || !clientState.Client.Connected || framed == null)
            {
                return false;
            }

            lock (clientState.SendLock)
            {
                try
                {
                    NetworkStream stream = clientState.Client.GetStream();
                    stream.Write(framed, 0, framed.Length);
                    Metrics.Inc(ref Metrics.TcpBytesOut, framed.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Debug($"[{_serverName}] 发送失败: {ex.Message}");
                    CloseClient(clientState);
                    return false;
                }
            }
        }

        /// <summary>供 PacketDispatcher 之外的旧代码调用。</summary>
        public bool Send(ClientState clientState, OutPacket outPacket)
        {
            return outPacket != null && Send(clientState, outPacket.ToArray());
        }

        private void CloseClient(ClientState clientState)
        {
            try
            {
                clientState.Client?.Close();
                var disconnected = Clients.Where(kv => ReferenceEquals(kv.Value, clientState)).Select(kv => kv.Key).ToList();
                foreach (string nickname in disconnected)
                {
                    Clients.TryRemove(nickname, out _);
                    foreach (var room in rooms.Values)
                    {
                        room.Remove(nickname);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[{_serverName}] 关闭客户端异常: {ex.Message}");
            }
        }

        /// <summary>客户端状态（含重组缓冲与发送锁）。</summary>
        public class ClientState
        {
            public TcpClient Client { get; set; }
            public byte[] Buffer { get; set; }

            internal readonly object SendLock = new object();
            internal readonly object PendingLock = new object();
            internal readonly List<byte> Pending = new List<byte>(4096);
        }
    }
}
