using ExcData;
using KartRider.Common.Network;
using Profile;
using RiderData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace KartRider;

public class ClientGroup
{
    public string Nickname { get; set; }

    public uint RIV { get; set; }

    public uint SIV { get; set; }
}

public static class ClientManager
{
    // 线程安全的集合，存储所有客户端会话（键：客户端唯一标识，值：会话对象）
    private static readonly ConcurrentDictionary<string, SessionGroup> _clientSessions = new ConcurrentDictionary<string, SessionGroup>();
    public static ConcurrentDictionary<string, ClientGroup> ClientGroups = new ConcurrentDictionary<string, ClientGroup>();

    // 添加客户端会话
    public static void AddClient(SessionGroup session)
    {
        IPEndPoint clientEndPoint = session.Client.Socket.RemoteEndPoint as IPEndPoint;
        if (clientEndPoint == null) return;

        string clientId = GetClientId(clientEndPoint);
        _clientSessions.TryAdd(clientId, session);

        ClientGroup clientGroup1 = new ClientGroup { Nickname = "", RIV = 0, SIV = 0 };
        ClientGroups.TryAdd(clientId, clientGroup1);

        uint IV = GameSupport.PcFirstMessage(session);
        var clientGroup2 = ClientGroups[clientId];
        clientGroup2.RIV = IV;
        clientGroup2.SIV = IV;
        Console.WriteLine($"客户端 {clientId} 已连接，当前在线数：{_clientSessions.Count}");
    }

    // 移除客户端会话（客户端断开时调用）
    public static void RemoveClient(Socket clientSocket)
    {
        IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
        if (clientEndPoint == null) return;

        string clientId = GetClientId(clientEndPoint);
        if (_clientSessions.TryRemove(clientId, out _))
        {
            var ClientGroup = ClientManager.ClientGroups[clientId];
            int roomId = RoomManager.TryGetRoomId(ClientGroup.Nickname);
            int slotId = RoomManager.GetPlayerSlotId(roomId, ClientGroup.Nickname);
            if (slotId != -1)
            {
                RoomManager.RemovePlayer(roomId, (byte)slotId);
            }
            ClientGroups.TryRemove(clientId, out _);
            Console.WriteLine($"客户端 {clientId} 已断开，当前在线数：{_clientSessions.Count}");
        }
    }

    // 获取所有在线客户端
    public static ConcurrentDictionary<string, SessionGroup> GetAllClients()
    {
        return _clientSessions;
    }

    // 生成客户端唯一标识（IP:端口）
    public static string GetClientId(IPEndPoint endPoint)
    {
        return $"{endPoint.Address}:{endPoint.Port}";
    }

    public static IPEndPoint ClientToIPEndPoint(string input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        // 分割 IP 和端口（取最后一个冒号作为分隔点，避免 IPV6 地址包含冒号的情况）
        int colonIndex = input.LastIndexOf(':');
        if (colonIndex == -1)
            return null;

        // 提取 IP 地址部分
        string ipPart = input.Substring(0, colonIndex);
        // 提取端口部分
        string portPart = input.Substring(colonIndex + 1);

        // 验证并解析 IP 地址
        if (!IPAddress.TryParse(ipPart, out IPAddress ipAddress))
            return null;

        // 验证并转换端口号（端口范围：0-65535）
        if (!int.TryParse(portPart, out int port) || port < 0 || port > 65535)
            return null;

        // 创建并返回 IPEndPoint
        return new IPEndPoint(ipAddress, port);
    }

    // 查询ClientGroups中是否存在指定Nickname的ClientGroup
    public static bool HasClientWithNickname(string nickname)
    {
        var online = ClientGroups.Values.Any(cg => cg.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        if (online)
        {
            var clientId = ClientGroups.FirstOrDefault(cg => cg.Value.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)).Key;
            if (_clientSessions.ContainsKey(clientId))
            {
                var Client = _clientSessions[clientId].Client;
                if (Client.Socket.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}
