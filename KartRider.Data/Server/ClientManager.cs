using ExcData;
using KartRider.Common.Network;
using KartRider.Logging;
using Profile;
using RiderData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace KartRider;

public static class ClientManager
{
    // 线程安全的集合，存储所有客户端会话（键：客户端唯一标识 IP:端口，值：会话对象）
    private static readonly ConcurrentDictionary<string, SessionGroup> _clientSessions = new(StringComparer.Ordinal);

    // 昵称 → 会话索引。原实现每次收包都用 ConcurrentDictionary.Values.FirstOrDefault 线性扫描
    // （Values 会锁住所有桶并复制一份），玩家越多越慢；这里改成 O(1) 查找。
    private static readonly ConcurrentDictionary<string, SessionGroup> _byNickname = new(StringComparer.Ordinal);

    public static ConcurrentDictionary<string, uint> NicknameToUserNO = new(StringComparer.Ordinal);
    public static ConcurrentDictionary<uint, string> UserNOToNickname = new();
    private static readonly object SyncLock = new();
    private static uint UserNO = 1;

    /// <summary>新连接建立时触发（StartGame 用它替代 netstat 轮询）。</summary>
    public static event Action<SessionGroup> ClientConnected;

    private static TaskCompletionSource<SessionGroup> _pendingConnect;

    /// <summary>
    /// 等待下一个客户端连接（StartGame 用）。超时返回 null。
    /// </summary>
    public static Task<SessionGroup> WaitForClientAsync(TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<SessionGroup>(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref _pendingConnect, tcs);

        if (timeout > TimeSpan.Zero)
        {
            var timer = new Timer(_ =>
            {
                if (ReferenceEquals(Volatile.Read(ref _pendingConnect), tcs))
                {
                    tcs.TrySetResult(null);
                }
            }, null, timeout, Timeout.InfiniteTimeSpan);
            tcs.Task.ContinueWith(_ => timer.Dispose(), TaskScheduler.Default);
        }

        return tcs.Task;
    }

    public static void AddClient(SessionGroup session)
    {
        if (session?.Client == null)
        {
            return;
        }

        IPEndPoint clientEndPoint = session.Client.Socket.RemoteEndPoint as IPEndPoint;
        if (clientEndPoint == null) return;

        string clientId = GetClientId(clientEndPoint);
        _clientSessions.TryAdd(clientId, session);

        session.Client.OnDisconnected += s => RemoveClient(s.Socket);

        uint IV = GameSupport.PcFirstMessageAsync(session);
        session.Client.RIV = IV;
        session.Client.SIV = IV;
        Log.Info($"客户端 {clientId} 已连接，当前在线数：{_clientSessions.Count}");

        try
        {
            ClientConnected?.Invoke(session);
        }
        catch (Exception ex)
        {
            Log.Warn("[ClientManager] ClientConnected 回调异常", ex);
        }

        Volatile.Read(ref _pendingConnect)?.TrySetResult(session);
    }

    /// <summary>维护 昵称 → 会话 索引。Session.Nickname 变更时调用。</summary>
    public static void BindNickname(SessionGroup group, string oldNickname, string newNickname)
    {
        if (group == null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(oldNickname)
            && _byNickname.TryGetValue(oldNickname, out SessionGroup oldGroup)
            && ReferenceEquals(oldGroup, group))
        {
            _byNickname.TryRemove(oldNickname, out _);
        }
        if (!string.IsNullOrEmpty(newNickname))
        {
            _byNickname[newNickname] = group;
        }
    }

    public static void RemoveClient(Socket clientSocket)
    {
        try
        {
            if (clientSocket == null)
                return;

            IPEndPoint clientEndPoint;
            try
            {
                clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
            }
            catch
            {
                return;
            }
            if (clientEndPoint == null) return;

            string clientId = GetClientId(clientEndPoint);

            if (!_clientSessions.TryRemove(clientId, out var client) || client == null)
                return;

            string nickname = client.Client?.Nickname;

            // 换频道/断线重连时，客户端会先建新连接并完成登录，旧连接随后才关闭。
            // 此时索引里可能已经指向"新会话"，旧会话绝不能把它删掉——否则新会话的
            // GetParent(nickname) 会变成 null，后续房间/好友请求全部静默丢弃（表现为卡死）。
            bool ownsNickname = false;
            if (!string.IsNullOrEmpty(nickname)
                && _byNickname.TryGetValue(nickname, out SessionGroup current)
                && ReferenceEquals(current, client))
            {
                _byNickname.TryRemove(nickname, out _);
                ownsNickname = true;
            }

            if (ownsNickname)
            {
                try
                {
                    RandomTrack.ClearUsedTracks(nickname);
                    int roomId = RoomManager.TryGetRoomId(nickname);
                    int slotId = RoomManager.GetPlayerSlotId(roomId, nickname);
                    if (slotId != -1)
                    {
                        // 掉线一律立即移出槽位（不再保留 out 槽位）：
                        // 服务端自定义的 out 状态（6）客户端不认，保留只会占位且显示异常。
                        // 最后一个人退出时同样走这里，空房会被 RemoveRoom 销毁。
                        RoomManager.RemovePlayer(roomId, (byte)slotId, nickname);
                    }
                    MyRoomData.TryLeaveMyRoom(nickname);
                }
                catch (Exception ex)
                {
                    Log.Warn($"[ClientManager] 清理玩家 {nickname} 的房间数据异常：{ex.Message}");
                }
                finally
                {
                    // 无论房间清理结果如何，都要清理该玩家残留的 UDP / 缓存引用
                    UdpServer.RemoveClient(nickname);
                    Bingo.Unload(nickname);
                    CleanupSessionCaches(nickname);
                }
            }
            else if (!string.IsNullOrEmpty(nickname))
            {
                Log.Info($"[ClientManager] {nickname} 已由新会话接管，跳过旧会话 {clientId} 的清理");
            }

            Log.Info($"客户端 {clientId} 已断开，当前在线数：{_clientSessions.Count}");
        }
        catch (Exception ex)
        {
            Log.Warn($"[ClientManager] 移除客户端异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 清理"可按需重建"的会话级缓存（路径表、档案、Bingo 进度）。
    ///
    /// 注意：这里**故意不清除** NicknameToUserNO / UserNOToNickname。
    /// 客户端切换频道时会新开连接并用 UserNO 重新握手（PqChannelMovein），
    /// 这两个映射是账号级身份、必须跨连接存活；清掉会导致换频道后服务端
    /// 反查不到昵称、不回包，客户端表现为"整个卡死"。
    /// </summary>
    internal static void CleanupSessionCaches(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return;
        }
        FileName.Forget(nickname);
        ProfileService.Forget(nickname);
    }

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

    public static ICollection<SessionGroup> GetClients() => _clientSessions.Values;

    public static int OnlineCount => _clientSessions.Count;

    public static SessionGroup GetParent(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return null;
        }
        return _byNickname.TryGetValue(nickname, out SessionGroup group) ? group : null;
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

        int colonIndex = input.LastIndexOf(':');
        if (colonIndex == -1)
            return null;

        string ipPart = input.Substring(0, colonIndex);
        string portPart = input.Substring(colonIndex + 1);

        if (!IPAddress.TryParse(ipPart, out IPAddress ipAddress))
            return null;

        if (!int.TryParse(portPart, out int port) || port < 0 || port > 65535)
            return null;

        return new IPEndPoint(ipAddress, port);
    }

    // 查询是否存在指定昵称的客户端（用索引，避免全表扫描 + 1ms Poll）
    public static bool HasClientWithNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return false;
        }
        if (!_byNickname.TryGetValue(nickname, out SessionGroup group) || group?.Client?.Socket == null)
        {
            return false;
        }

        try
        {
            Socket socket = group.Client.Socket;
            // Poll(0) 只做一次非阻塞探测，不再阻塞 1ms
            bool readable = socket.Poll(0, SelectMode.SelectRead);
            bool hasData = socket.Available > 0;
            return readable && !hasData ? false : socket.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static uint GetUserNO(string Nickname)
    {
        if (string.IsNullOrEmpty(Nickname))
        {
            return 0;
        }

        lock (SyncLock)
        {
            if (NicknameToUserNO.TryGetValue(Nickname, out uint existingUserNO))
            {
                return existingUserNO;
            }

            // 原实现 UserNO++ 非原子，并发登录可能拿到同一个号
            uint newUserNO = UserNO++;
            NicknameToUserNO[Nickname] = newUserNO;
            UserNOToNickname[newUserNO] = Nickname;
            return newUserNO;
        }
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
        if (string.IsNullOrEmpty(oldNickname) || string.IsNullOrEmpty(newNickname))
        {
            return;
        }

        lock (SyncLock)
        {
            if (!NicknameToUserNO.TryGetValue(oldNickname, out uint userNO))
                return;

            if (NicknameToUserNO.ContainsKey(newNickname))
                return;

            NicknameToUserNO.TryRemove(oldNickname, out _);
            NicknameToUserNO[newNickname] = userNO;
            UserNOToNickname[userNO] = newNickname;
        }

        _byNickname.TryRemove(oldNickname, out SessionGroup group);
        if (group != null)
        {
            _byNickname[newNickname] = group;
        }
        ProfileService.OnNicknameChanged(oldNickname, newNickname);
    }
}
