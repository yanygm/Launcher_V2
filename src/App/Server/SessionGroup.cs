using System.Net.Sockets;

namespace Launcher.App.Server
{
    public class SessionGroup
    {
        public object m_lock = new object();

        public ClientSession Client
        {
            get;
            set;
        }

        public int TimeAttackStartTicks = 0;
        public int SendPlaneCount = 6;
        public int TotalSendPlaneCount = 6;
        public byte PlaneCheck1 = 0;

        public static uint LucciMax = 2000000;

        public static ushort usLocale = 3002;
        public static byte nClientLoc = 47;
        public static string Service = "cn";

        public static string Developer = "KartRider";

        public SessionGroup(Socket clientSocket, Socket serverSocket)
        {
            Client = new ClientSession(this, clientSocket);
        }
    }
}