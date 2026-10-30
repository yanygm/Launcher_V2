using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using KartRider.Common.Security;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider_PacketName;
using Profile;

namespace KartRider
{
    /// <summary>
    /// UDP 游戏数据中继。
    ///
    /// 关键修改：
    ///   1. 每包不再读玩家档案 + 解析 "ip:port" 字符串（原来每个 UDP 包都要做一次）；
    ///   2. 收包长度校验（&lt;8 直接丢弃），不再靠异常兜底；
    ///   3. 发送缓冲复用、IV 改成计数器派生（不再每包 new Random）；
    ///   4. 转发时不再为了读一个 tick 把刚拼好的包重新 ToArray 一遍；
    ///   5. 增大 SO_RCVBUF/SO_SNDBUF，降低突发丢包。
    /// </summary>
    public class UdpServer
    {
        private const int HeaderSize = 8; // 4B IV + 4B checksum

        private UdpClient _udpClient;
        private readonly int _listenPort;
        private readonly string _serverName;
        private volatile bool _isRunning;
        private readonly object _lockObj = new object();

        private static readonly ConcurrentDictionary<string, UdpPeer> udpClients = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, long> udpLastActivity = new(StringComparer.Ordinal);

        private byte[] _sendScratch = new byte[2048];
        private static long _ivCounter = Environment.TickCount64;

        private sealed class UdpPeer
        {
            public IPEndPoint EndPoint;
            public uint Hash;
            public bool P2P;
            public string ClientId;
            public long LastSeen;
        }

        public UdpServer(string serverName, int listenPort)
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
                    _udpClient = new UdpClient(_listenPort);
                    try
                    {
                        _udpClient.Client.ReceiveBufferSize = 256 * 1024;
                        _udpClient.Client.SendBufferSize = 256 * 1024;
                    }
                    catch
                    {
                    }

                    // 关闭 Windows 下 ICMP Port Unreachable → ConnectionReset 的行为
                    const int SIO_UDP_CONNRESET = -1744830452;
                    try
                    {
                        _udpClient.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
                    }
                    catch
                    {
                    }

                    _isRunning = true;
                    Log.Info($"[{_serverName}] UDP 已启动，端口 {_listenPort}");
                    BeginReceive();
                }
                catch (SocketException ex)
                {
                    Log.Error($"[{_serverName}] UDP 启动失败（端口可能被占用）: {ex.Message}");
                    _isRunning = false;
                }
                catch (Exception ex)
                {
                    Log.Error($"[{_serverName}] UDP 启动异常", ex);
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
                    _udpClient?.Close();
                }
                catch (Exception ex)
                {
                    Log.Warn($"[{_serverName}] UDP 停止异常: {ex.Message}");
                }
                finally
                {
                    _udpClient?.Dispose();
                    _udpClient = null;
                }
                Log.Info($"[{_serverName}] UDP 已停止");
            }
        }

        private void BeginReceive()
        {
            if (!_isRunning || _udpClient == null) return;
            try
            {
                _udpClient.BeginReceive(EndReceive, null);
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Log.Warn($"[{_serverName}] UDP 接收异常: {ex.Message}");
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(1000);
                        BeginReceive();
                    });
                }
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
            if (!_isRunning || _udpClient == null) return;

            IPEndPoint clientEP = null;
            byte[] receiveBuffer = null;
            try
            {
                receiveBuffer = _udpClient.EndReceive(ar, ref clientEP);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                // ICMP 端口不可达，非致命
            }
            catch (SocketException ex)
            {
                if (_isRunning)
                {
                    Log.Warn($"[{_serverName}] UDP 接收失败: {ex.Message} ({ex.SocketErrorCode})");
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Log.Warn($"[{_serverName}] UDP 接收异常", ex);
                }
            }
            finally
            {
                if (_isRunning)
                {
                    BeginReceive();
                }
            }

            if (receiveBuffer == null || receiveBuffer.Length < HeaderSize)
            {
                Metrics.Inc(ref Metrics.ProtocolErrors);
                return;
            }

            Metrics.Inc(ref Metrics.UdpPacketsIn);
            Metrics.Inc(ref Metrics.UdpBytesIn, receiveBuffer.Length);

            try
            {
                HandleDatagram(receiveBuffer, clientEP);
            }
            catch (Exception ex)
            {
                Metrics.Inc(ref Metrics.HandlerErrors);
                Log.Warn($"[{_serverName}] 处理 UDP 数据异常", ex);
            }
        }

        private void HandleDatagram(byte[] receiveBuffer, IPEndPoint clientEP)
        {
            uint iv = BitConverter.ToUInt32(receiveBuffer, 0);
            int payloadLength = receiveBuffer.Length - HeaderSize;
            byte[] packetData = new byte[payloadLength];
            Buffer.BlockCopy(receiveBuffer, 4, packetData, 0, payloadLength);
            KRPacketCrypto.HashDecrypt(packetData, (uint)payloadLength, iv);

            using InPacket p = new InPacket(packetData);
            uint accountID = p.ReadUInt();
            uint hash = p.ReadUInt();
            var packetValue = (PacketName)p.ReadUInt();

            string nickname = "";
            ClientManager.UserNOToNickname.TryGetValue(accountID, out nickname);

            if (!string.IsNullOrEmpty(nickname))
            {
                // UDP 载荷结构：[accountID][hash][packetName]...，包名在第 9 字节起，必须显式传入
                Log.Packet("U", nickname, packetData, packetData.Length, (uint)packetValue);
            }

            bool p2p = false;
            if (!string.IsNullOrEmpty(nickname))
            {
                UdpPeer peer = GetOrCreatePeer(nickname, clientEP, hash);
                peer.LastSeen = Environment.TickCount64;
                udpLastActivity[nickname] = peer.LastSeen;
                p2p = peer.P2P;
            }

            if (PacketDispatcher.Dispatch(typeof(UdpServer), packetValue, p, receiveBuffer, clientEP, this))
            {
                return;
            }

            switch (packetValue)
            {
                case PacketName.PqUdpEcho:
                {
                    using OutPacket outPacket = new OutPacket();
                    outPacket.WriteUInt(accountID);
                    outPacket.WriteUInt(hash);
                    outPacket.WriteInt((int)PacketName.PrUdpEcho);
                    outPacket.WriteInt(p.ReadInt());
                    outPacket.WriteInt(p.ReadInt());
                    BeginSend(outPacket, clientEP);
                    break;
                }
                case PacketName.PqUdpTimeSync:
                {
                    int clientTick = p.ReadInt();
                    using OutPacket outPacket = new OutPacket();
                    outPacket.WriteUInt(accountID);
                    outPacket.WriteUInt(hash);
                    outPacket.WriteInt((int)PacketName.PrUdpTimeSync);
                    outPacket.WriteInt(clientTick);
                    outPacket.WriteUInt(MultyPlayer.ConvertTick());
                    bool success = BeginSend(outPacket, clientEP);

                    if (!string.IsNullOrEmpty(nickname))
                    {
                        int roomId = RoomManager.TryGetRoomId(nickname);
                        var room = RoomManager.GetRoom(roomId);
                        if (room?.Ready != null && room.Ready.ContainsKey(nickname) && room.Ready[nickname] == false)
                        {
                            room.Ready[nickname] = success;
                        }
                    }
                    break;
                }
                case PacketName.GameSlotPacket:
                {
                    int roomId = RoomManager.TryGetRoomId(nickname);
                    var room = RoomManager.GetRoom(roomId);
                    byte[] data = p.ReadBytes(p.Available);
                    if (room != null)
                    {
                        // tick 位于转发包偏移 32 处（acc 4 + hash 4 + name 4 + 后续数据前 20 字节）
                        uint tick = data.Length >= 24 ? BitConverter.ToUInt32(data, 20) : 0;
                        foreach (RoomMember member in room._slots)
                        {
                            if (member is Player player && player.Nickname != nickname)
                            {
                                RelayTo(player, member, data, packetValue, hash, p2p, tick);
                            }
                        }
                        foreach (RoomMember member in room.ObIDs)
                        {
                            if (member is Player player)
                            {
                                RelayTo(player, member, data, packetValue, hash, p2p, tick);
                            }
                        }
                    }
                    break;
                }
                case PacketName.RoomSlotPacket:
                {
                    string owner = MyRoomData.GetRoomOwnerByNickname(nickname);
                    byte[] data = p.ReadBytes(p.Available);
                    if (!string.IsNullOrEmpty(owner))
                    {
                        foreach (string member in MyRoomData.GetRoomPlayers(owner))
                        {
                            if (string.IsNullOrEmpty(member) || string.Equals(member, nickname, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var target = GetUdp(member);
                            if (p2p && target.Item3)
                                continue;
                            if (target.Item1 == null)
                                continue;

                            uint pHash = target.Item2 == 0 ? hash : target.Item2;
                            using OutPacket outPacket = new OutPacket();
                            outPacket.WriteUInt(ClientManager.GetUserNO(member));
                            outPacket.WriteUInt(pHash);
                            outPacket.WriteUInt((uint)packetValue);
                            outPacket.WriteBytes(data);
                            BeginSend(outPacket, target.Item1);
                        }
                    }
                    break;
                }
                default:
                    Log.Debug($"[{_serverName}] 未知 UDP 包 {packetValue}");
                    break;
            }
        }

        private void RelayTo(Player player, RoomMember member, byte[] data, PacketName packetValue, uint senderHash, bool senderP2P, uint tick)
        {
            var target = GetUdp(player.Nickname);
            if (target.Item1 == null)
            {
                return;
            }
            if (senderP2P && target.Item3)
            {
                return; // 双方都直连时服务端不再重复中继
            }

            uint pHash = target.Item2 == 0 ? senderHash : target.Item2;
            using OutPacket outPacket = new OutPacket();
            outPacket.WriteUInt(ClientManager.GetUserNO(player.Nickname));
            outPacket.WriteUInt(pHash);
            outPacket.WriteUInt((uint)packetValue);
            outPacket.WriteBytes(data);

            if (packetValue == PacketName.GameKartQuadPacket || packetValue == PacketName.GameKartPacket)
            {
                if (tick != 0 && tick < player.LastPacketReceived && tick > MultyPlayer.ConvertTick())
                {
                    Log.Debug($"[{player.Nickname}] 丢包率过高，丢弃数据包");
                    return;
                }
                if (tick != 0)
                {
                    player.LastPacketReceived = tick;
                }
            }

            BeginSend(outPacket, target.Item1);
        }

        private UdpPeer GetOrCreatePeer(string nickname, IPEndPoint clientEP, uint hash)
        {
            if (udpClients.TryGetValue(nickname, out UdpPeer peer))
            {
                peer.EndPoint = clientEP;
                peer.Hash = hash;
                return peer;
            }

            var created = new UdpPeer
            {
                EndPoint = clientEP,
                Hash = hash,
                P2P = false,
                LastSeen = Environment.TickCount64
            };

            try
            {
                ProfileConfig config = ProfileService.GetProfileConfig(nickname);
                if (config?.Rider != null)
                {
                    IPEndPoint client = ClientManager.ClientToIPEndPoint(config.Rider.ClientId);
                    if (client != null)
                    {
                        created.ClientId = config.Rider.ClientId;
                        created.P2P = clientEP.Equals(new IPEndPoint(client.Address, config.Rider.UdpPort));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[{_serverName}] 解析 {nickname} 的 UDP 端点失败: {ex.Message}");
            }

            return udpClients.GetOrAdd(nickname, created);
        }

        public bool BeginSend(OutPacket outPacket, IPEndPoint endPoint)
        {
            if (outPacket == null)
            {
                return false;
            }
            byte[] buffer = outPacket.ToArray();
            return BeginSend(buffer, endPoint);
        }

        public bool BeginSend(byte[] buffer, IPEndPoint endPoint)
        {
            if (buffer == null || endPoint == null || (IPAddress.Any.Equals(endPoint.Address) && endPoint.Port == 0))
            {
                return false;
            }

            try
            {
                int total = buffer.Length + HeaderSize;
                if (_sendScratch.Length < total)
                {
                    _sendScratch = new byte[Math.Max(total, _sendScratch.Length * 2)];
                }
                byte[] frame = _sendScratch;

                uint siv = NextIv();
                uint digest = KRPacketCrypto.HashEncrypt(buffer, (uint)buffer.Length, siv);

                Buffer.BlockCopy(BitConverter.GetBytes(siv), 0, frame, 0, 4);
                Buffer.BlockCopy(buffer, 0, frame, 4, buffer.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(siv ^ digest ^ 1329075907u), 0, frame, total - 4, 4);

                UdpClient client = _udpClient;
                if (client == null)
                {
                    return false;
                }
                int sent = client.Send(frame, total, endPoint);
                Metrics.Inc(ref Metrics.UdpPacketsOut);
                Metrics.Inc(ref Metrics.UdpBytesOut, total);
                if (sent == total)
                {
                    return true;
                }
                Log.Warn($"[{_serverName}] UDP 发送不完整 {sent}/{total}");
                return false;
            }
            catch (SocketException ex)
            {
                Log.Debug($"[{_serverName}] UDP 发送失败: {ex.Message} ({ex.SocketErrorCode})");
                return false;
            }
            catch (Exception ex)
            {
                Log.Warn($"[{_serverName}] UDP 发送异常", ex);
                return false;
            }
        }

        private static uint NextIv()
        {
            // 计数器派生：零分配、同 tick 内也不会重复
            long n = Interlocked.Increment(ref _ivCounter);
            uint v = (uint)(n * 2654435761L);
            return v == 0 ? 1u : v;
        }

        public static (IPEndPoint, uint, bool) GetUdp(string nickname)
        {
            if (!string.IsNullOrEmpty(nickname) && udpClients.TryGetValue(nickname, out UdpPeer peer) && peer.EndPoint != null)
            {
                return (peer.EndPoint, peer.Hash, peer.P2P);
            }

            var profile = string.IsNullOrEmpty(nickname) ? null : ProfileService.GetProfileConfig(nickname);
            if (profile?.Rider == null)
            {
                return (null, 0, false);
            }
            IPEndPoint client = ClientManager.ClientToIPEndPoint(profile.Rider.ClientId);
            if (client == null)
            {
                return (null, 0, false);
            }
            return (new IPEndPoint(client.Address, profile.Rider.UdpPort), 0, false);
        }

        public static long GetUdpIdle(string nickname, long now)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return long.MaxValue;
            }
            if (udpLastActivity.TryGetValue(nickname, out long last))
            {
                return now - last;
            }
            return long.MaxValue;
        }

        public static void RemoveClient(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            udpClients.TryRemove(nickname, out _);
            udpLastActivity.TryRemove(nickname, out _);
        }

        public static void OnNicknameChanged(string oldNickname, string newNickname)
        {
            if (string.IsNullOrEmpty(oldNickname) || string.IsNullOrEmpty(newNickname) || oldNickname == newNickname)
                return;

            if (udpClients.TryRemove(oldNickname, out UdpPeer peer))
            {
                udpClients[newNickname] = peer;
            }
            if (udpLastActivity.TryRemove(oldNickname, out long lastActivity))
            {
                udpLastActivity[newNickname] = lastActivity;
            }
        }

        public static int PeerCount => udpClients.Count;
    }
}
