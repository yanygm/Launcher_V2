namespace Launcher.Library.IO
{
    public sealed class PacketReadException : Exception
    {
        public PacketReadException(string message) : base(message)
        {
        }
    }
}