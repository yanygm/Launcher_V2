using System;

namespace KartRider.Common.Utilities
{
	public sealed class ByteArraySegment
	{
		private byte[] mBuffer = null;

		private int mStart = 0;

		private int mLength = 0;

		private bool mEncrypted = true;

		public byte[] Buffer
		{
			get
			{
				return this.mBuffer;
			}
			set
			{
				this.mBuffer = value;
			}
		}

		public bool Encrypted
		{
			get
			{
				return this.mEncrypted;
			}
		}

		public int Length
		{
			get
			{
				return this.mLength;
			}
		}

		public int Start
		{
			get
			{
				return this.mStart;
			}
		}

		public ByteArraySegment(byte[] pBuffer, bool pEncrypted)
		{
			this.mBuffer = pBuffer;
			this.mLength = (int)this.mBuffer.Length;
			this.mEncrypted = pEncrypted;
		}

		public ByteArraySegment(byte[] pBuffer, int pStart, int pLength)
		{
			this.mBuffer = pBuffer;
			this.mStart = pStart;
			this.mLength = pLength;
		}

		public bool Advance(int pLength)
		{
			this.mStart += pLength;
			this.mLength -= pLength;
			return (this.mLength > 0 ? false : true);
		}
	}
}