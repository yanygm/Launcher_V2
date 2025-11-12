using KartNew.Utilities;
using KartRider.Common.Utilities;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace KartRider.IO.Packet
{
	public class OutPacket : PacketBase, IDisposable
	{
		private MemoryStream m_stream;

		private bool m_disposed;

		public bool Disposed
		{
			get
			{
				return this.m_disposed;
			}
		}

		public override int Length
		{
			get
			{
				return (int)this.m_stream.Position;
			}
		}

		public override int Position
		{
			get
			{
				return (int)this.m_stream.Position;
			}
			set
			{
				this.m_stream.Position = (long)value;
			}
		}

		public OutPacket(int size = 64)
		{
			this.m_stream = new MemoryStream(size);
			this.m_disposed = false;
		}

		public OutPacket(string rttiVal) : this(64)
		{
			this.WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes(rttiVal), 0));
		}

		private void Append(long value, int byteCount)
		{
			for (int i = 0; i < byteCount; i++)
			{
				this.m_stream.WriteByte((byte)value);
				value >>= 8;
			}
		}

		public new void Dispose()
		{
			this.m_disposed = true;
			if (this.m_stream != null)
			{
				this.m_stream.Dispose();
			}
			this.m_stream = null;
		}

		private void ThrowIfDisposed()
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
		}

		public override byte[] ToArray()
		{
			this.ThrowIfDisposed();
			return this.m_stream.ToArray();
		}

		public void WriteBool(bool value)
		{
			this.ThrowIfDisposed();
			this.WriteByte((byte)((value ? 1 : 0)));
		}

		public void WriteByte(byte value = 0)
		{
			this.ThrowIfDisposed();
			this.m_stream.WriteByte(value);
		}

		public void WriteBytes(params byte[] value)
		{
			this.ThrowIfDisposed();
			this.m_stream.Write(value, 0, (int)value.Length);
		}

		public void WriteEncByte(byte value)
		{
			this.WriteBytes(CryptoConstants.encryptBytes(new byte[] { value }));
		}

		public void WriteEncFloat(float value)
		{
			this.WriteBytes(CryptoConstants.encryptBytes(BitConverter.GetBytes(value)));
		}

		public void WriteEncInt(int value)
		{
			this.WriteBytes(CryptoConstants.encryptBytes(BitConverter.GetBytes(value)));
		}

		public void WriteEncUInt(uint value)
		{
			this.WriteBytes(CryptoConstants.encryptBytes(BitConverter.GetBytes(value)));
		}

		public void WriteEndPoint(IPEndPoint endpoint)
		{
			if (endpoint != null)
			{
				this.WriteEndPoint(endpoint.Address, (ushort)endpoint.Port);
			}
			else
			{
				this.WriteInt(0);
				this.WriteUShort(0);
			}
		}

		public void WriteEndPoint(IPAddress ip, ushort port)
		{
			this.WriteBytes(ip.GetAddressBytes());
			this.WriteUShort(port);
		}

		public void WriteFloat(float value = 0f)
		{
			this.WriteBytes(BitConverter.GetBytes(value));
		}

		public void WriteHexString(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			value = value.Replace(" ", "");
			for (int i = 0; i < value.Length; i += 2)
			{
				this.WriteByte(byte.Parse(value.Substring(i, 2), NumberStyles.HexNumber));
			}
		}

		public void WriteInt(int value = 0)
		{
			this.ThrowIfDisposed();
			this.Append((long)value, 4);
		}

		public void WriteLong(long value = 0L)
		{
			this.ThrowIfDisposed();
			this.Append(value, 8);
		}

		public void WriteSByte(sbyte value = 0)
		{
			this.WriteByte((byte)value);
		}

		public void WriteShort(short value = 0)
		{
			this.ThrowIfDisposed();
			this.Append((long)value, 2);
		}

		public void WriteString(string value, bool ascii = false)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.WriteInt(value.Length);
			this.WriteString(value, value.Length, ascii);
		}

		public void WriteString(string value, int length, bool ascii = false)
		{
			int i;
			if ((value == null || length < 0 ? true : length > value.Length))
			{
				throw new ArgumentNullException("value");
			}
			if (ascii)
			{
				byte[] bytes = Encoding.ASCII.GetBytes(value);
				for (i = 0; i < value.Length & i < length; i++)
				{
					this.WriteByte(bytes[i]);
				}
			}
			else
			{
				byte[] bytes = Encoding.Unicode.GetBytes(value);
				for (i = 0; i < value.Length & i < length; i++)
				{
					int num = i * 2;
					this.WriteByte(bytes[num]);
					this.WriteByte(bytes[num + 1]);
				}
				while (i < length)
				{
					this.WriteShort(0);
					i++;
				}
			}
		}

		public void WriteTime(DateTime time)
		{
			this.WriteTime((time == DateTime.MinValue ? (long)-1 : time.Ticks));
		}

		public void WriteTime(long ticks)
		{
			if (ticks != (long)-1)
			{
				DateTime dateTime = new DateTime(ticks);
				this.WriteShort((short)(TimeUtil.GetDays(dateTime) - 1));
				this.WriteShort((short)(dateTime.Second / 4 + dateTime.Minute * 15 + dateTime.Hour * 900));
			}
			else
			{
				this.WriteShort(-1);
				this.WriteShort(0);
			}
		}

		public void WriteUInt(uint value = 0)
		{
			this.WriteInt((int)value);
		}

		public void WriteULong(ulong value = 0L)
		{
			this.WriteLong((long)value);
		}

		public void WriteUShort(ushort value = 0)
		{
			this.WriteShort((short)value);
		}
	}
}