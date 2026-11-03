using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Profile;
using KartRider.Logging;

namespace KartRider
{
    public class RouterListener
    {
        public static string ServerIP = "";

        public static TCPServer TCPServer = new TCPServer("TCP", ProfileService.SettingConfig.ServerPort);

        public static SessionGroup MySession
        {
            get => TCPServer.LastSession;
            set => TCPServer.LastSession = value;
        }

        public static List<string> RouterIPList = new List<string>();

        public static MsgrServer MsgrServer = new MsgrServer("MsgrServer", (ushort)(ProfileService.SettingConfig.ServerPort + 2));

        public static UdpServer UDPServer = new UdpServer("UDP", ProfileService.SettingConfig.ServerPort);

        public static UdpServer P2PServer = new UdpServer("P2P", (ushort)(ProfileService.SettingConfig.ServerPort + 1));

        public static int DataTime()
        {
            DateTime dt = DateTime.Now;
            // DateTime time = new DateTime(1900, 1, 1, 0, 0, 0);
            // TimeSpan t = dt.Subtract(time);
            // double totalSeconds = dt.TimeOfDay.TotalSeconds / 4;
            int MonthCount = (dt.Year - 1900) * 12 + dt.Month;
            int oddMonthCount = (MonthCount + 1) / 2;
            return oddMonthCount;
        }

        public static void Start()
        {
            // 启动服务端
            UDPServer.Start();
            P2PServer.Start();
            MsgrServer.Start();

            // 仅在 TCP 监听未运行时才需要重建监听与 IP 列表
            if (!TCPServer.IsRunning)
            {
                // 先收集 IP 列表（在创建监听之前完成，避免中间异常导致
                // 监听处于"已创建但未 Start"状态）
                RouterIPList = LanIpGetter.GetAllLocalLanIps();
                RouterIPList.Add("127.0.0.1");
                foreach (var IP in RouterIPList)
                {
                    Log.Info($"Load Server IP: {IP}:{ProfileService.SettingConfig.ServerPort}");
                }

                // 后台异步获取公网 IP，避免 .Result 同步阻塞服务器启动
                // （GetCountryAsync 若网络不可达可能耗时数秒~数十秒，期间玩家将无法连接）
                StartPublicIpLookup();

                // 端口绑定失败会抛出 SocketException，由调用方提示用户
                TCPServer.ListenPort = ProfileService.SettingConfig.ServerPort;
                TCPServer.Start();
            }

            if (LanIpGetter.CheckHasPublicIpv6())
            {
                TinyMapper.Start();
            }

            // 启动心跳/掉线检测监控，及时清理假死连接（幽灵玩家）
            HeartbeatMonitor.Start();
        }

        /// <summary>
        /// 停止服务端所有监听（释放端口）
        /// </summary>
        public static void Stop()
        {
            // 停止 UDP 服务
            UDPServer.Stop();
            P2PServer.Stop();

            // 停止 TCP 消息服务
            MsgrServer.Stop();

            // 停止 TCP 监听器
            TCPServer.Stop();

            // 停止 TinyMapper 端口转发
            TinyMapper.Stop();

            // 停止心跳/掉线检测监控
            HeartbeatMonitor.Stop();

            Log.Info("[RouterListener] 所有服务已停止");
        }

        private static void StartPublicIpLookup()
        {
            Task.Run(async () =>
            {
                try
                {
                    var ipInfo = await Update.GetCountryAsync();
                    string ip = ipInfo?.Ip ?? "";
                    if (!string.IsNullOrEmpty(ip))
                    {
                        lock (RouterIPList)
                        {
                            if (!RouterIPList.Contains(ip))
                            {
                                RouterIPList.Add(ip);
                            }
                        }
                        RouterListener.ServerIP = ip;
                        Log.Info($"Load Server IP: {ip}:{ProfileService.SettingConfig.ServerPort}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"获取公网 IP 失败: {ex.Message}");
                }
            });
        }
    }
}