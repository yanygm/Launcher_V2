using KartRider;
using Profile;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace KartRider.Common.Network;

public class ClientGroup
{
    public string ClientID { get; set; }

    public uint RIV { get; set; }

    public uint SIV { get; set; }
}

public static class ClientManager
{
    // 线程安全的集合，存储所有客户端会话（键：客户端唯一标识，值：会话对象）
    private static readonly ConcurrentDictionary<string, SessionGroup> _clientSessions = new ConcurrentDictionary<string, SessionGroup>();

    public static List<ClientGroup> ClientGroups = new List<ClientGroup>();

    // 添加客户端会话
    public static void AddClient(SessionGroup session)
    {
        IPEndPoint clientEndPoint = session.Client.Socket.RemoteEndPoint as IPEndPoint;
        if (clientEndPoint == null) return;

        string clientId = GetClientId(clientEndPoint);
        _clientSessions.TryAdd(clientId, session);

        ClientGroup clientGroup1 = new ClientGroup { ClientID = clientId, RIV = 0, SIV = 0 };
        ClientGroups.Add(clientGroup1);

        uint IV = GameSupport.PcFirstMessage();
        var clientGroup2 = ClientManager.ClientGroups.FirstOrDefault(cg => cg.ClientID == clientId);
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
            var clientGroupToRemove = ClientGroups.FirstOrDefault(cg => cg.ClientID == clientId);
            if (clientGroupToRemove != null)
            {
                ClientGroups.Remove(clientGroupToRemove);
            }
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
}
