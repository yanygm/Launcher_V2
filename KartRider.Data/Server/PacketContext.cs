using KartRider.IO.Packet;
using KartRider_PacketName;
using System;

namespace KartRider
{
    public class PacketContext
    {
        public Type ServerType { get; internal set; }

        public PacketName PacketName { get; internal set; }

        public InPacket Packet { get; internal set; }

        public byte[] RawData { get; internal set; }

        public object ClientInfo { get; internal set; }

        public object Server { get; internal set; }

        public bool Handled { get; set; }
    }
}
