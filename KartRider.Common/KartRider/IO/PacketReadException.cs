using System;

namespace KartRider.IO.Packet
{
	public sealed class PacketReadException : Exception
	{
		public PacketReadException(string message) : base(message)
		{
		}
	}
}