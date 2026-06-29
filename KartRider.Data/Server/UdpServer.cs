using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using KartRider.Common.Security;
using KartRider.IO.Packet;
using KartRider_PacketName;
using Profile;

namespace KartRider
{
    /// <summary>
    /// UDP服务端封装类
    /// </summary>
    public class UdpServer
    {
        // UDP核心通信对象
        private UdpClient _udpClient;
        // 监听端口
        private readonly int _listenPort;
        // 服务端名称（日志区分多实例）
        private readonly string _serverName;
        // 线程取消标识（替代CancellationToken）
        private volatile bool _isRunning;
        // 同步锁（防止重复启动/停止）
        private readonly object _lockObj = new object();
        private static ConcurrentDictionary<string, (IPEndPoint, uint, bool)> udpClients = new ConcurrentDictionary<string, (IPEndPoint, uint, bool)>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serverName">服务端名称（日志区分）</param>
        /// <param name="listenPort">监听端口（唯一）</param>
        public UdpServer(string serverName, int listenPort)
        {
            _serverName = serverName;
            _listenPort = listenPort;
            _isRunning = false;
        }

        /// <summary>
        /// 启动UDP服务端
        /// </summary>
        public void Start()
        {
            lock (_lockObj)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"[{_serverName}] 服务端已启动，无需重复启动");
                    return;
                }

                try
                {
                    // 初始化UDP客户端并绑定端口
                    _udpClient = new UdpClient(_listenPort);

                    // 禁用 UDP Socket 的 ConnectionReset 错误（Windows 特有）
                    // 当向未监听端口发送 UDP 时，远程返回 ICMP Port Unreachable，
                    // 系统将其映射为 ConnectionReset 并在 EndReceive 时抛出，
                    // 同时丢弃所有已缓冲的正常数据包。此设置彻底关闭该行为。
                    const int SIO_UDP_CONNRESET = -1744830452;
                    _udpClient.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);

                    _isRunning = true;

                    Console.WriteLine($"[{_serverName}] 服务端启动成功，监听端口：{_listenPort}");
                    Console.WriteLine($"[{_serverName}] 等待客户端数据...\n");

                    // 开始异步接收数据（APM模式）
                    BeginReceive();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[{_serverName}] 启动失败：{ex.Message}（端口可能被占用）");
                    _isRunning = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_serverName}] 启动异常：{ex.Message}");
                    _isRunning = false;
                }
            }
        }

        /// <summary>
        /// 停止UDP服务端
        /// </summary>
        public void Stop()
        {
            lock (_lockObj)
            {
                if (!_isRunning)
                {
                    Console.WriteLine($"[{_serverName}] 服务端未启动，无需停止");
                    return;
                }

                _isRunning = false;

                try
                {
                    // 关闭UDP客户端（终止异步接收）
                    _udpClient?.Close();
                    Console.WriteLine($"[{_serverName}] 服务端停止成功");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_serverName}] 停止异常：{ex.Message}");
                }
                finally
                {
                    // 释放资源
                    _udpClient?.Dispose();
                    _udpClient = null;
                }
            }
        }

        /// <summary>
        /// 异步接收数据（APM模式：BeginReceive）
        /// </summary>
        private void BeginReceive()
        {
            if (!_isRunning || _udpClient == null) return;

            try
            {
                // 启动异步接收，完成后回调EndReceive
                _udpClient.BeginReceive(EndReceive, null);
            }
            catch (Exception ex)
            {
                if (_isRunning) // 仅在运行中时打印异常（停止时的异常忽略）
                {
                    Console.WriteLine($"[{_serverName}] 接收数据异常：{ex.Message}");
                    // 延迟重试接收（避免异常循环）
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(1000);
                        BeginReceive();
                    });
                }
            }
        }

        /// <summary>
        /// 异步接收完成回调（APM模式：EndReceive）
        /// </summary>
        /// <param name="ar">异步操作结果</param>
        private void EndReceive(IAsyncResult ar)
        {
            if (!_isRunning || _udpClient == null) return;

            IPEndPoint clientEP = null;
            byte[] receiveBuffer = null;

            try
            {
                // 结束异步接收，获取数据和客户端地址
                receiveBuffer = _udpClient.EndReceive(ar, ref clientEP);

                try
                {
                    // 解析数据
                    if (clientEP != null)
                    {
                        uint iv = BitConverter.ToUInt32(receiveBuffer, 0);
                        uint otherChecksum = BitConverter.ToUInt32(receiveBuffer, receiveBuffer.Length - 4);
                        byte[] packetData = new byte[receiveBuffer.Length - (4 + 4)];
                        Buffer.BlockCopy(receiveBuffer, 4, packetData, 0, packetData.Length);
                        KRPacketCrypto.HashDecrypt(packetData, (uint)packetData.Length, iv);
                        InPacket p = new InPacket(packetData);
                        uint accountID = p.ReadUInt();
                        uint hash = p.ReadUInt();
                        uint packetName = p.ReadUInt();
                        var packetValue = (PacketName)packetName;

                        string nickname = "";
                        ClientManager.UserNOToNickname.TryGetValue(accountID, out nickname);
                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        bool p2p = false;
                        if (!string.IsNullOrEmpty(nickname))
                        {
                            var playerConfig = ProfileService.GetProfileConfig(nickname);
                            IPEndPoint client = ClientManager.ClientToIPEndPoint(playerConfig.Rider.ClientId);
                            var clientudp = new IPEndPoint(client.Address, playerConfig.Rider.UdpPort);
                            p2p = (clientEP == clientudp);
                            udpClients.AddOrUpdate(nickname, (clientEP, hash, p2p), (key, oldValue) => (clientEP, hash, p2p));
                            // Console.WriteLine($"[UDP][{currentTime}][{nickname}] {packetValue}" + ": " + BitConverter.ToString(packetData).Replace("-", " "));
                        }

                        if (PacketDispatcher.Dispatch(typeof(UdpServer), packetValue, p, receiveBuffer, clientEP, this))
                            return;

                        if (packetValue == PacketName.PqUdpEcho)
                        {
                            OutPacket outPacket = new OutPacket();
                            outPacket.WriteUInt(accountID);
                            outPacket.WriteUInt(hash);
                            outPacket.WriteInt((int)PacketName.PrUdpEcho);

                            outPacket.WriteInt(p.ReadInt());
                            outPacket.WriteInt(p.ReadInt());
                            BeginSend(outPacket, clientEP);
                        }
                        else if (packetValue == PacketName.PqUdpTimeSync)
                        {
                            OutPacket outPacket = new OutPacket();
                            outPacket.WriteUInt(accountID);
                            outPacket.WriteUInt(hash);
                            outPacket.WriteInt((int)PacketName.PrUdpTimeSync);

                            outPacket.WriteInt(p.ReadInt());
                            outPacket.WriteUInt(MultyPlayer.ConvertTick());
                            bool success = BeginSend(outPacket, clientEP);

                            if (!string.IsNullOrEmpty(nickname))
                            {
                                int roomId = RoomManager.TryGetRoomId(nickname);
                                var room = RoomManager.GetRoom(roomId);
                                if (room != null)
                                {
                                    if (room.Ready != null)
                                    {
                                        if (room.Ready.ContainsKey(nickname) && room.Ready[nickname] == false)
                                        {
                                            room.Ready[nickname] = success;
                                        }
                                    }
                                }
                            }
                        }
                        else if (packetValue == PacketName.GameSlotPacket)
                        {
                            int roomId = RoomManager.TryGetRoomId(nickname);
                            var room = RoomManager.GetRoom(roomId);
                            byte[] data = p.ReadBytes(p.Available);
                            if (room != null)
                            {
                                foreach (RoomMember member in room._slots)
                                {
                                    if (member is Player player && player.Nickname != nickname)
                                    {
                                        var targetUdp = GetUdp(player.Nickname);
                                        // 发送方和目标方都是 P2P 直连时跳过，避免重复发送
                                        if (p2p && targetUdp.Item3)
                                            continue;

                                        var udp = targetUdp.Item1;
                                        var pHash = targetUdp.Item2 == 0 ? hash : targetUdp.Item2;

                                        OutPacket outPacket = new OutPacket();
                                        outPacket.WriteUInt(ClientManager.GetUserNO(player.Nickname));
                                        outPacket.WriteUInt(pHash);
                                        outPacket.WriteUInt(packetName);
                                        outPacket.WriteBytes(data);

                                        UdpBoardCast(player, udp, outPacket);
                                    }
                                }
                                foreach (RoomMember member in room.ObIDs)
                                {
                                    if (member is Player player)
                                    {
                                        var targetUdp = GetUdp(player.Nickname);
                                        // 发送方和目标方都是 P2P 直连时跳过，避免重复发送
                                        if (p2p && targetUdp.Item3)
                                            continue;

                                        var udp = targetUdp.Item1;
                                        var pHash = targetUdp.Item2 == 0 ? hash : targetUdp.Item2;

                                        OutPacket outPacket = new OutPacket();
                                        outPacket.WriteUInt(ClientManager.GetUserNO(player.Nickname));
                                        outPacket.WriteUInt(pHash);
                                        outPacket.WriteUInt(packetName);
                                        outPacket.WriteBytes(data);

                                        UdpBoardCast(player, udp, outPacket);
                                    }
                                }
                            }
                        }
                        else if (packetValue == PacketName.RoomSlotPacket)
                        {
                            string owner = MyRoomData.GetRoomOwnerByNickname(nickname);
                            byte[] data = p.ReadBytes(p.Available);
                            if (!string.IsNullOrEmpty(owner))
                            {
                                var members = MyRoomData.GetRoomPlayers(owner);
                                foreach (var member in members)
                                {
                                    if (string.IsNullOrEmpty(member) || string.Equals(member, nickname, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    var targetUdp = GetUdp(member);
                                    // 发送方和目标方都是 P2P 直连时跳过，避免重复发送
                                    if (p2p && targetUdp.Item3)
                                        continue;

                                    var udp = targetUdp.Item1;
                                    var pHash = targetUdp.Item2 == 0 ? hash : targetUdp.Item2;

                                    OutPacket outPacket = new OutPacket();
                                    outPacket.WriteUInt(ClientManager.GetUserNO(member));
                                    outPacket.WriteUInt(pHash);
                                    outPacket.WriteUInt(packetName);
                                    outPacket.WriteBytes(data);

                                    bool success = BeginSend(outPacket, udp);
                                    if (success)
                                    {
                                        // Console.WriteLine($"[{udp}][{currentTime}][{nickname}] {packetValue}: {BitConverter.ToString(outPacket.ToArray()).Replace("-", " ")}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Unknown Packet on UDP : {packetValue}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_serverName}] 处理数据异常：{ex.Message}");
                }
            }
            catch (ObjectDisposedException)
            {
                // 服务端停止时触发，忽略
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                // UDP Socket 收到 ICMP Port Unreachable，非致命错误，静默忽略并继续接收
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[{_serverName}] 处理数据异常：{ex.Message}，错误码：{ex.SocketErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_serverName}] 处理数据异常：{ex.Message}");
            }
            finally
            {
                // 持续接收下一个数据包（核心：循环异步接收）
                if (_isRunning)
                {
                    BeginReceive();
                }
            }
        }

        public bool BeginSend(OutPacket outPacket, IPEndPoint endPoint)
        {
            byte[] buffer = outPacket.ToArray();
            try
            {
                if (endPoint == null || IPAddress.Any.Equals(endPoint.Address) && endPoint.Port == 0)
                {
                    // 无效端点（0.0.0.0:0），跳过发送，避免触发 ICMP Port Unreachable
                    return false;
                }

                byte[] data = new byte[buffer.Length + 8];

                uint siv = (uint)(new Random((int)DateTime.Now.Ticks).Next());
                uint newHash = KRPacketCrypto.HashEncrypt(buffer, (uint)buffer.Length, siv);
                Buffer.BlockCopy(BitConverter.GetBytes(siv), 0, data, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((uint)(siv ^ newHash ^ 1329075907U)), 0, data, data.Length - 4, 4);
                Buffer.BlockCopy(buffer, 0, data, 4, buffer.Length);

                int sentBytes = _udpClient.Send(data, data.Length, endPoint);
                if (sentBytes == data.Length)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"发送失败（部分发送）：{sentBytes} / {data.Length}");
                    return false;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"发送失败（网络错误）：{ex.Message}，错误码：{ex.SocketErrorCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败：{ex.Message}");
                return false;
            }
        }

        public static (IPEndPoint, uint, bool) GetUdp(string nickname)
        {
            if (udpClients.TryGetValue(nickname, out var value))
            {
                return (value.Item1, value.Item2, value.Item3);
            }
            else
            {
                var profile = ProfileService.GetProfileConfig(nickname);
                IPEndPoint client = ClientManager.ClientToIPEndPoint(profile.Rider.ClientId);
                var udpIP = new IPEndPoint(client.Address, profile.Rider.UdpPort);
                return (udpIP, 0, false);
            }
        }

        public void UdpBoardCast(Player player, IPEndPoint udp, OutPacket outPacket)
        {
            InPacket iPacket = new InPacket(outPacket.ToArray());
            var data1 = iPacket.ReadBytes(32);
            var packetName = iPacket.ReadUInt();
            var packetValue = (PacketName)packetName;
            var tick = iPacket.ReadUInt();
            // var data2 = iPacket.ReadBytes(iPacket.Available);
            if (packetValue == PacketName.GameKartQuadPacket || packetValue == PacketName.GameKartPacket)
            {
                if (tick < player.LastPacketReceived && tick > MultyPlayer.ConvertTick())
                {
                    Console.WriteLine($"[{player.Nickname}] 丢包率过高，丢弃数据包");
                    return;
                }
                player.LastPacketReceived = tick;
            }
            bool success = BeginSend(outPacket, udp);
            if (success)
            {
                // Console.WriteLine($"[{udp}][{currentTime}][{nickname}] {packetValue}: {BitConverter.ToString(outPacket.ToArray()).Replace("-", " ")}");
            }
        }
    }
}
