using System;
using System.CodeDom;
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
        public static IPAddress sIP {  get; set; }

        public static System.Net.IPEndPoint CurrentUDPServer { get; set; }

        public static TcpListener Listener { get; private set; }

        public static SessionGroup MySession { get; set; }

        public static int[] DataTime()
        {
            DateTime dt = DateTime.Now;
            DateTime time = new DateTime(1900, 1, 1, 0, 0, 0);
            TimeSpan t = dt.Subtract(time);
            double totalSeconds = dt.TimeOfDay.TotalSeconds / 4;
            int MonthCount = (dt.Year - 1900) * 12 + dt.Month;
            int oddMonthCount = (MonthCount + 1) / 2;
            return new int[] { t.Days, (int)totalSeconds, oddMonthCount };
        }

        public static void OnAcceptSocket(IAsyncResult ar)
        {
            try
            {
                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket clientSocket = RouterListener.Listener.EndAcceptSocket(ar);

                // 创建客户端会话（自动开始接收消息）
                RouterListener.MySession = new SessionGroup(clientSocket, null);

                // 将会话添加到管理类
                ClientManager.AddClient(RouterListener.MySession);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常：{ex.Message}");
            }
            finally
            {
                RouterListener.Listener.BeginAcceptSocket(new AsyncCallback(RouterListener.OnAcceptSocket), null);
            }
        }

        public static void Start()
        {
            // 示例：启动两个独立的UDP服务端
            var server1 = new UdpServer("服务端1", 39311);
            var server2 = new UdpServer("服务端2", 39312);

            // 启动服务端
            server1.Start();
            server2.Start();

            if (RouterListener.Listener == null)
            {
                RouterListener.Listener = new TcpListener(IPAddress.Any, ProfileService.SettingConfig.ServerPort);
            }
            if (!RouterListener.Listener.Server.IsBound)
            {
                var RouterIPList = LanIpGetter.GetAllLocalLanIps();
                foreach (var ip in RouterIPList)
                {
                    Console.WriteLine("Load server IP: {0}:{1}", ip, ProfileService.SettingConfig.ServerPort);
                }
                RouterListener.Listener.Start();
                RouterListener.Listener.BeginAcceptSocket(OnAcceptSocket, RouterListener.Listener);
            }
            else
            {
                RouterListener.Listener.BeginAcceptSocket(OnAcceptSocket, RouterListener.Listener);
            }
        }

        public static byte[] Connect()
        {
            foreach (var server in ProfileService.SettingConfig.ServerList)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(server.IP, server.Port);
                if (socket.Connected)
                {
                    // 接收服务器回传的数据
                    byte[] buffer = new byte[1024];
                    int bytesRead = socket.Receive(buffer);
                    byte[] response = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, response, 0, bytesRead);
                    socket.Close();
                    return response;
                }
            }
            return null;
        }
    }
}