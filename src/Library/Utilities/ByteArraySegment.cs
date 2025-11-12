namespace Launcher.Library.Utilities
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
                return mBuffer;
            }
            set
            {
                mBuffer = value;
            }
        }

        public bool Encrypted
        {
            get
            {
                return mEncrypted;
            }
        }

        public int Length
        {
            get
            {
                return mLength;
            }
        }

        public int Start
        {
            get
            {
                return mStart;
            }
        }

        public ByteArraySegment(byte[] pBuffer, bool pEncrypted)
        {
            mBuffer = pBuffer;
            mLength = mBuffer.Length;
            mEncrypted = pEncrypted;
        }

        public ByteArraySegment(byte[] pBuffer, int pStart, int pLength)
        {
            mBuffer = pBuffer;
            mStart = pStart;
            mLength = pLength;
        }

        public bool Advance(int pLength)
        {
            mStart += pLength;
            mLength -= pLength;
            return mLength > 0 ? false : true;
        }
    }
}