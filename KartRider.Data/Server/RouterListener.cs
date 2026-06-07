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

            if (RouterListener.Listener == null)
            {
                RouterListener.Listener = new TcpListener(IPAddress.Any, ProfileService.SettingConfig.ServerPort);
            }
            if (!RouterListener.Listener.Server.IsBound)
            {
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
            if (Listener != null)
            {
                try
                {
                    Listener.Stop();
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