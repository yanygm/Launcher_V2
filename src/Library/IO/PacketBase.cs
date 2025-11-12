using System.Text;

namespace Launcher.Library.IO
{
    public abstract class PacketBase : IDisposable
    {
        public abstract int Length
        {
            get;
        }

        public abstract int Position
        {
            get;
            set;
        }

        protected PacketBase()
        {
        }

        public virtual void Dispose()
        {
        }

        public abstract byte[] ToArray();

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            byte[] array = ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                stringBuilder.AppendFormat("{0:X2} ", array[i]);
            }
            return stringBuilder.ToString();
        }
    }
}