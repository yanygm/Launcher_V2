using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KartRider
{
    public class RouterListener
    {
        public static string sIP;

        public static ushort port;

        public static IPEndPoint client;

        public static System.Net.IPEndPoint CurrentUDPServer { get; set; }

        public static TcpListener Listener { get; private set; }

        public static SessionGroup MySession { get; set; }

        static RouterListener()
        {
            RouterListener.sIP = LanIpGetter.GetLocalLanIp();
            RouterListener.port = 39312;
        }

        public static int[] DataTime()
        {
            DateTime dt = DateTime.Now;
            DateTime time = new DateTime(1900, 1, 1, 0, 0, 0);
            TimeSpan t = dt.Subtract(time);
            double totalSeconds = dt.TimeOfDay.TotalSeconds / 4;
            int Month = (dt.Year - 1900) * 12;
            int MonthCount = Month + dt.Month;
            double tempResult = (double)MonthCount / 2;
            int oddMonthCount;
            if (tempResult % 1 != 0)
            {
                oddMonthCount = (int)tempResult + 1;
            }
            else
            {
                oddMonthCount = (int)tempResult;
            }
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

                if (File.Exists(Launcher.pinFileBak))
                {
                    File.Delete(Launcher.pinFile);
                    File.Move(Launcher.pinFileBak, Launcher.pinFile);
                }
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
            if (RouterListener.Listener == null || RouterListener.CurrentUDPServer == null)
            {
                RouterListener.Listener = new TcpListener(IPAddress.Any, RouterListener.port);
                RouterListener.CurrentUDPServer = new System.Net.IPEndPoint(IPAddress.Any, 39311);
            }
            if (!RouterListener.Listener.Server.IsBound)
            {
                Console.WriteLine("Load server IP: {0}:{1}", RouterListener.sIP, RouterListener.port);
                RouterListener.Listener.Start();
                RouterListener.Listener.BeginAcceptSocket(OnAcceptSocket, RouterListener.Listener);
            }
            else
            {
                RouterListener.Listener.BeginAcceptSocket(OnAcceptSocket, RouterListener.Listener);
            }
        }
    }
}
