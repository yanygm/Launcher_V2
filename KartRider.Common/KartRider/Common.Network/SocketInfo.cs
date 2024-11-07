using System;
using System.Net.Sockets;

namespace KartRider.Common.Network
{
	public class SocketInfo
	{
		public readonly System.Net.Sockets.Socket Socket;

		public bool NoEncryption;

		public SocketInfo.StateEnum State;

		public byte[] DataBuffer;

		public int Index;

		public SocketInfo(System.Net.Sockets.Socket socket, short headerLength) : this(socket, headerLength, false)
		{
		}

		public SocketInfo(System.Net.Sockets.Socket socket, short headerLength, bool noEncryption)
		{
			this.Socket = socket;
			this.State = SocketInfo.StateEnum.Header;
			this.NoEncryption = noEncryption;
			this.DataBuffer = new byte[headerLength];
			this.Index = 0;
		}

		SocketInfo()
		{
			this.DataBuffer = null;
		}

		public enum StateEnum
		{
			Header,
			Content
		}
	}
}