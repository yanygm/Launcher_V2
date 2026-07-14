using System;
using System.CodeDom;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using KartRider.Common.Network;
using Profile;

namespace KartRider
{
    public class RouterListener
    {
        public static string ServerIP = "";

        public static TcpListener Listener { get; private set; }

        public static SessionGroup MySession { get; set; }

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

        public static void OnAcceptSocket(IAsyncResult ar)
        {
            try
            {
                if (RouterListener.Listener == null)
                    return;

                Socket clientSocket = RouterListener.Listener.EndAcceptSocket(ar);

                // 创建客户端会话（自动开始接收消息）
                RouterListener.MySession = new SessionGroup(clientSocket, null);

                // 将会话添加到管理类
                ClientManager.AddClient(RouterListener.MySession);
            }
            catch (ObjectDisposedException)
            {
                // Listener 已停止/释放，忽略
            }
            catch (NullReferenceException)
            {
                // Listener 已被 Stop() 置为 null，忽略
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常：{ex.Message}");
            }
            finally
            {
                if (RouterListener.Listener != null)
                {
                    try
                    {
                        RouterListener.Listener.BeginAcceptSocket(new AsyncCallback(RouterListener.OnAcceptSocket), null);
                    }
                    catch (ObjectDisposedException) { }
                    catch (NullReferenceException) { }
                }
            }
        }

        public static void Start()
        {
            // 启动服务端
            UDPServer.Start();
            P2PServer.Start();
            MsgrServer.Start();

            // 仅在 Listener 为 null 或未绑定时才需要创建/启动
            if (RouterListener.Listener == null || !RouterListener.Listener.Server.IsBound)
            {
                // 先收集 IP 列表（在创建 Listener 之前完成，避免中间异常导致
                // Listener 处于"已创建但未 Start"状态，进而在 Stop() 时触发
                // .NET 8 的 "Not listening" InvalidOperationException）
                RouterIPList = new List<string>();
                RouterIPList = LanIpGetter.GetAllLocalLanIps();
                RouterIPList.Add("127.0.0.1");
                var ipInfo = Task.Run(async () => await Update.GetCountryAsync()).Result;
                string ip = ipInfo?.Ip ?? "";
                if (!string.IsNullOrEmpty(ip))
                {
                    RouterIPList.Add(ip);
                }
                foreach (var IP in RouterIPList)
                {
                    Console.WriteLine("Load Server IP: {0}:{1}", IP, ProfileService.SettingConfig.ServerPort);
                }

                // 创建 Listener 后立即 Start，避免中间代码抛异常导致不一致状态
                RouterListener.Listener = new TcpListener(IPAddress.Any, ProfileService.SettingConfig.ServerPort);
                RouterListener.Listener.Start();
                RouterListener.Listener.BeginAcceptSocket(OnAcceptSocket, RouterListener.Listener);
            }
            else
            {
                RouterListener.Listener.BeginAcceptSocket(OnAcceptSocket, RouterListener.Listener);
            }
            if (LanIpGetter.CheckHasPublicIpv6())
            {
                TinyMapper.Start();
            }
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
            // 注意：.NET 8 中 TcpListener.Stop() 在未 Start 时会抛出
            // "Not listening. You must call the Start method before calling this method."
            // 使用 Server.Close() 直接关闭底层 Socket 避免此异常。
            if (Listener != null)
            {
                try
                {
                    if (Listener.Server != null)
                    {
                        Listener.Server.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RouterListener] 停止 TCP 监听器异常: {ex.Message}");
                }
                Listener = null;
            }

            // 停止 TinyMapper 端口转发
            TinyMapper.Stop();

            Console.WriteLine("[RouterListener] 所有服务已停止");
        }
    }
}