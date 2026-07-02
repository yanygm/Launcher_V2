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
using System.Threading.Tasks;

namespace KartRider;

public static class ClientManager
{
    // 线程安全的集合，存储所有客户端会话（键：客户端唯一标识，值：会话对象）
    private static readonly ConcurrentDictionary<string, SessionGroup> _clientSessions = new ConcurrentDictionary<string, SessionGroup>();
    public static ConcurrentDictionary<string, uint> NicknameToUserNO = new ConcurrentDictionary<string, uint>();
    public static ConcurrentDictionary<uint, string> UserNOToNickname = new ConcurrentDictionary<uint, string>();
    private static readonly object SyncLock = new object();
    private static uint UserNO = 1;

    // 添加客户端会话
    public static void AddClient(SessionGroup session)
    {
        IPEndPoint clientEndPoint = session.Client.Socket.RemoteEndPoint as IPEndPoint;
        if (clientEndPoint == null) return;

        string clientId = GetClientId(clientEndPoint);
        _clientSessions.TryAdd(clientId, session);

        uint IV = GameSupport.PcFirstMessageAsync(session);
        session.Client.RIV = IV;
        session.Client.SIV = IV;
        Console.WriteLine($"客户端 {clientId} 已连接，当前在线数：{_clientSessions.Count}");
    }

    // 移除客户端会话（客户端断开时调用）
    public static void RemoveClient(Socket clientSocket)
    {
        IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
        if (clientEndPoint == null) return;

        string clientId = GetClientId(clientEndPoint);
        _clientSessions.TryGetValue(clientId, out var client);
        RandomTrack.ClearUsedTracks(client.Client.Nickname);
        if (!string.IsNullOrEmpty(client.Client.Nickname))
        {
            int roomId = RoomManager.TryGetRoomId(client.Client.Nickname);
            int slotId = RoomManager.GetPlayerSlotId(roomId, client.Client.Nickname);
            if (slotId != -1)
            {
                RoomManager.RemovePlayer(roomId, (byte)slotId, client.Client.Nickname);
            }
            MyRoomData.TryLeaveMyRoom(client.Client.Nickname);
        }
        if (_clientSessions.TryRemove(clientId, out _))
        {
            Console.WriteLine($"客户端 {clientId} 已断开，当前在线数：{_clientSessions.Count}");
        }
    }

    // 获取所有在线玩家
    public static List<string> GetOnlinePlayers()
    {
        List<string> OnlinePlayers = new List<string>();
        foreach (var session in _clientSessions.Values)
        {
            if (!string.IsNullOrEmpty(session.Client.Nickname))
            {
                OnlinePlayers.Add(session.Client.Nickname);
            }
        }
        return OnlinePlayers;
    }

    public static ICollection<SessionGroup> GetClients()
    {
        return _clientSessions.Values;
    }

    public static SessionGroup GetParent(string nickname)
    {
        var session = _clientSessions.Values.FirstOrDefault(x => x.Client.Nickname == nickname);
        if (session != null)
        {
            return session;
        }
        return null;
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
        var session = _clientSessions.Values.FirstOrDefault(x => x.Client.Nickname == nickname);
        if (session != null)
        {
            var Client = session.Client;
            try
            {
                // 使用 Poll 检测真实连接状态（1000微秒 = 1毫秒）
                // 如果 socket 不可读且不可写，说明连接已断开或有问题
                bool isReadable = Client.Socket.Poll(1000, SelectMode.SelectRead);
                bool hasData = Client.Socket.Available > 0;
                bool isConnected = isReadable && !hasData ? false : Client.Socket.Connected;
                return isConnected;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public static uint GetUserNO(string Nickname)
    {
        // 1. 先检查昵称是否已存在（忽略大小写，可选）
        if (NicknameToUserNO.TryGetValue(Nickname, out uint existingUserNO))
        {
            // 存在则直接返回原有UserNO，不分配新编号
            return existingUserNO;
        }

        // 2. 不存在则分配新UserNO
        uint newUserNO = UserNO++; // 仅自增一次

        // 3. 双向绑定：昵称→编号、编号→昵称
        NicknameToUserNO.TryAdd(Nickname, newUserNO);
        UserNOToNickname.TryAdd(newUserNO, Nickname);

        return newUserNO;
    }

    public static string GetNickname(uint UserNO)
    {
        if (UserNOToNickname.TryGetValue(UserNO, out string nickname))
        {
            return nickname;
        }
        return null;
    }

    public static void UpdateNickname(string oldNickname, string newNickname)
    {
        // 锁定保证原子更新（多线程必备，两个字典要一起修改）
        lock (SyncLock)
        {
            // 1. 取出原有账号
            if (!NicknameToUserNO.TryGetValue(oldNickname, out uint userNO))
                return; // 找不到原昵称，直接退出

            // 2. 冲突校验：新昵称已被别人占用则禁止改名
            if (NicknameToUserNO.ContainsKey(newNickname))
            {
                // 昵称已存在，改名失败，直接return
                return;
            }

            // 3. 删除旧昵称映射
            NicknameToUserNO.TryRemove(oldNickname, out _);
            // 绑定新昵称→账号
            NicknameToUserNO[newNickname] = userNO;

            // 4. 更新账号→昵称（直接下标覆盖，必然生效）
            UserNOToNickname[userNO] = newNickname;
        }
    }
}