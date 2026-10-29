using System;
using System.Text;

namespace KartRider.IO.Packet
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
			byte[] array = this.ToArray();
			for (int i = 0; i < (int)array.Length; i++)
			{
				stringBuilder.AppendFormat("{0:X2} ", array[i]);
			}
			return stringBuilder.ToString();
		}
	}
}