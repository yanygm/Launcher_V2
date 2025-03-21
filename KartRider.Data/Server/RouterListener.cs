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

		public static int port;

		public static string forceConnect;

		public static IPEndPoint client;

		public static System.Net.IPEndPoint CurrentUDPServer { get; set; }

		public static string ForceConnect { get; set; }

		public static TcpListener Listener { get; private set; }

		public static SessionGroup MySession { get; set; }

		static RouterListener()
		{
			string str = "0.0.0.0";
			RouterListener.sIP = str;
			int str1 = 39312;
			RouterListener.port = str1;
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
				RouterListener.forceConnect = RouterListener.sIP;
				if (RouterListener.ForceConnect != "")
				{
					RouterListener.forceConnect = RouterListener.ForceConnect;
				}
				RouterListener.MySession = new SessionGroup(clientSocket, null);
				IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
				RouterListener.client = clientEndPoint;
				Console.WriteLine("Client: " + RouterListener.client.Address.ToString() + ":" + RouterListener.client.Port.ToString());
				if (File.Exists(Program.LauncherDlg.kartRiderDirectory + "KartRider-bak.pin"))
				{
					File.Delete(Program.LauncherDlg.kartRiderDirectory + "KartRider.pin");
					File.Move(Program.LauncherDlg.kartRiderDirectory + "KartRider-bak.pin", Program.LauncherDlg.kartRiderDirectory + "KartRider.pin");
				}
				GameSupport.PcFirstMessage();
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
				RouterListener.Listener = new TcpListener(IPAddress.Parse(RouterListener.sIP), RouterListener.port);
				RouterListener.CurrentUDPServer = new System.Net.IPEndPoint(IPAddress.Parse(RouterListener.sIP), 39311);
			}
			if (!RouterListener.Listener.Server.IsBound)
			{
				Console.WriteLine("Load server IP: {0}:{1}", RouterListener.sIP, RouterListener.port);
				RouterListener.ForceConnect = "";
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
