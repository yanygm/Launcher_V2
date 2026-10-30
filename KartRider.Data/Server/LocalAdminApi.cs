using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 本地只读管理接口（默认 127.0.0.1:39314）。
    ///
    /// 用裸 TcpListener 实现最小 HTTP/1.1，避免 HttpListener 的 URL ACL 依赖，
    /// 也不需要管理员额外配置。只绑定回环地址、只支持 GET，不提供任何写操作。
    ///
    ///   GET /healthz   存活探针
    ///   GET /status    在线人数、房间数、关键计数、内存
    ///   GET /metrics   Prometheus 文本格式
    ///   GET /rooms     房间列表（id/人数/是否开局）
    /// </summary>
    public static class LocalAdminApi
    {
        private static TcpListener _listener;
        private static volatile bool _running;
        private static readonly object Gate = new();

        public static bool IsRunning => _running;

        public static void Start(int port, string token = null)
        {
            lock (Gate)
            {
                if (_running || port <= 0)
                {
                    return;
                }

                try
                {
                    _listener = new TcpListener(IPAddress.Loopback, port);
                    _listener.Start();
                    _running = true;
                    _ = AcceptLoop(token);
                    Log.Info($"[Admin] 本地管理接口已启动: http://127.0.0.1:{port}/status");
                }
                catch (Exception ex)
                {
                    Log.Warn($"[Admin] 管理接口启动失败（端口 {port}）: {ex.Message}");
                    _running = false;
                    _listener = null;
                }
            }
        }

        public static void Stop()
        {
            lock (Gate)
            {
                _running = false;
                try
                {
                    _listener?.Stop();
                }
                catch
                {
                }
                _listener = null;
            }
        }

        private static async System.Threading.Tasks.Task AcceptLoop(string token)
        {
            while (_running)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Log.Debug($"[Admin] 接受连接失败: {ex.Message}");
                    }
                    continue;
                }

                _ = HandleAsync(client, token);
            }
        }

        private static async System.Threading.Tasks.Task HandleAsync(TcpClient client, string token)
        {
            try
            {
                client.NoDelay = true;
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    client.ReceiveTimeout = 5000;
                    byte[] buffer = new byte[4096];
                    var request = new StringBuilder();
                    while (request.Length < 8192)
                    {
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        if (read <= 0)
                        {
                            return;
                        }
                        request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                        if (request.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                        {
                            break;
                        }
                    }

                    string head = request.ToString();
                    string[] lines = head.Split("\r\n");
                    string[] parts = lines[0].Split(' ');
                    if (parts.Length < 2)
                    {
                        await WriteAsync(stream, 400, "text/plain", "bad request").ConfigureAwait(false);
                        return;
                    }

                    string method = parts[0];
                    string path = parts[1];
                    string query = "";
                    int q = path.IndexOf('?');
                    if (q >= 0)
                    {
                        query = path.Substring(q + 1);
                        path = path.Substring(0, q);
                    }

                    if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        await WriteAsync(stream, 405, "text/plain", "only GET is allowed").ConfigureAwait(false);
                        return;
                    }

                    if (!string.IsNullOrEmpty(token) && !TokenMatches(query, lines, token))
                    {
                        await WriteAsync(stream, 401, "text/plain", "unauthorized").ConfigureAwait(false);
                        return;
                    }

                    switch (path.TrimEnd('/').ToLowerInvariant())
                    {
                        case "":
                        case "/healthz":
                            await WriteAsync(stream, 200, "text/plain", "ok").ConfigureAwait(false);
                            break;
                        case "/status":
                            await WriteAsync(stream, 200, "application/json", JsonSerializer.Serialize(BuildStatus())).ConfigureAwait(false);
                            break;
                        case "/metrics":
                            await WriteAsync(stream, 200, "text/plain; version=0.0.4", BuildMetrics()).ConfigureAwait(false);
                            break;
                        case "/rooms":
                            await WriteAsync(stream, 200, "application/json", JsonSerializer.Serialize(BuildRooms())).ConfigureAwait(false);
                            break;
                        case "/packets":
                        {
                            // 回看最近 N 个包：/packets?n=200&filter=GameSlot&dir=C
                            int count = ParseInt(GetQuery(query, "n"), 100);
                            string filter = GetQuery(query, "filter");
                            string dir = GetQuery(query, "dir");
                            var packetLines = PacketRing.Snapshot(Math.Clamp(count, 1, PacketRing.Capacity), filter, dir);
                            string header = $"# 最近 {packetLines.Count} 个包（总记录 {PacketRing.Total}，容量 {PacketRing.Capacity}）"
                                            + Environment.NewLine;
                            await WriteAsync(stream, 200, "text/plain", header + string.Join(Environment.NewLine, packetLines)).ConfigureAwait(false);
                            break;
                        }
                        default:
                            await WriteAsync(stream, 404, "text/plain", "not found").ConfigureAwait(false);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[Admin] 请求处理失败: {ex.Message}");
            }
        }

        private static string GetQuery(string query, string key)
        {
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }
            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0 && pair.Substring(0, eq) == key)
                {
                    return Uri.UnescapeDataString(pair.Substring(eq + 1));
                }
            }
            return null;
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, out int value) ? value : fallback;
        }

        private static bool TokenMatches(string query, string[] headerLines, string token)
        {
            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0 && pair.Substring(0, eq) == "token" && pair.Substring(eq + 1) == token)
                {
                    return true;
                }
            }
            foreach (string line in headerLines)
            {
                if (line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase)
                    && line.Substring("Authorization:".Length).Trim() == "Bearer " + token)
                {
                    return true;
                }
            }
            return false;
        }

        private static Dictionary<string, object> BuildStatus()
        {
            var status = Metrics.Snapshot();
            status["online"] = ClientManager.OnlineCount;
            status["rooms"] = RoomManager._rooms.Count;
            status["udpPeers"] = UdpServer.PeerCount;
            status["profilesCached"] = ProfileService.CachedProfiles;
            status["packetRingTotal"] = PacketRing.Total;
            status["packetRingCapacity"] = PacketRing.Capacity;
            status["players"] = ClientManager.GetOnlinePlayers();

            List<string> roomList = new();
            foreach (var pair in RoomManager._rooms.OrderBy(k => k.Key).Take(50))
            {
                roomList.Add($"{pair.Key}:{pair.Value.GetPlayerCount()}/{(pair.Value.Started ? "playing" : "waiting")}");
            }
            status["roomList"] = roomList;
            return status;
        }

        private static string BuildMetrics()
        {
            var sb = new StringBuilder();
            void Add(string name, object value, string help)
            {
                sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
                sb.Append("# TYPE ").Append(name).Append(" gauge\n");
                sb.Append(name).Append(' ').Append(value).Append('\n');
            }

            var s = Metrics.Snapshot();
            Add("kr_online_players", ClientManager.OnlineCount, "当前在线玩家数");
            Add("kr_rooms", RoomManager._rooms.Count, "房间数");
            Add("kr_uptime_seconds", s["uptimeSeconds"], "运行秒数（自进程启动）");
            Add("kr_tcp_packets_total", s["tcpIn"], "TCP 收包总数");
            Add("kr_tcp_packets_out_total", s["tcpOut"], "TCP 发包总数");
            Add("kr_udp_packets_total", s["udpIn"], "UDP 收包总数");
            Add("kr_udp_packets_out_total", s["udpOut"], "UDP 发包总数");
            Add("kr_tcp_in_per_sec", s["tcpInPerSec"], "TCP 收包速率");
            Add("kr_udp_in_per_sec", s["udpInPerSec"], "UDP 收包速率");
            Add("kr_handler_errors_total", s["handlerErrors"], "处理器异常次数");
            Add("kr_protocol_errors_total", s["protocolErrors"], "协议错误次数");
            Add("kr_checksum_mismatch_total", s["checksumMismatch"], "校验和不匹配次数");
            Add("kr_disconnects_total", s["disconnects"], "断开次数");
            Add("kr_slow_client_drops_total", s["slowClientDrops"], "慢客户端被断开次数");
            Add("kr_profile_cache_hits_total", s["profileCacheHits"], "档案缓存命中");
            Add("kr_profile_cache_misses_total", s["profileCacheMisses"], "档案缓存未命中");
            Add("kr_profile_disk_reads_total", s["profileDiskReads"], "档案磁盘读取次数");
            Add("kr_profile_disk_writes_total", s["profileDiskWrites"], "档案磁盘写入次数");
            Add("kr_memory_working_set_mb", s["workingSetMB"], "工作集（MB）");
            Add("kr_gc_heap_mb", s["gcHeapMB"], "GC 堆（MB）");
            return sb.ToString();
        }

        private static List<object> BuildRooms()
        {
            var list = new List<object>();
            foreach (var pair in RoomManager._rooms.OrderBy(k => k.Key))
            {
                var room = pair.Value;
                list.Add(new
                {
                    id = pair.Key,
                    name = room.RoomName,
                    players = room.GetPlayerCount(),
                    ai = room.GetAiCount(),
                    started = room.Started,
                    locked = room.Lock
                });
            }
            return list;
        }

        private static async System.Threading.Tasks.Task WriteAsync(NetworkStream stream, int code, string contentType, string body)
        {
            byte[] payload = Encoding.UTF8.GetBytes(body ?? "");
            string reason = code switch
            {
                200 => "OK",
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                405 => "Method Not Allowed",
                _ => "OK"
            };
            string head = $"HTTP/1.1 {code} {reason}\r\n"
                          + $"Content-Type: {contentType}; charset=utf-8\r\n"
                          + $"Content-Length: {payload.Length}\r\n"
                          + "Cache-Control: no-store\r\n"
                          + "Connection: close\r\n\r\n";
            byte[] headBytes = Encoding.ASCII.GetBytes(head);
            await stream.WriteAsync(headBytes, 0, headBytes.Length).ConfigureAwait(false);
            if (payload.Length > 0)
            {
                await stream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
            }
            await stream.FlushAsync().ConfigureAwait(false);
        }
    }
}
