using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.IO
{
    public static class Adler
    {
        public const uint AdlerModulo = 65521;
        public static uint Adler32(uint adler, byte[] buffer, int offset, int count)
        {
            if (buffer.Length < (offset + count))
                throw new Exception("buffer is small.");
            uint a = adler & 0xFFFFu;
            uint b = (adler >> 16) & 0xFFFFu;
            for(int i = 0; i < count; i++)
            {
                a = (a + buffer[offset + i]) % AdlerModulo;
                b = (b + a) % AdlerModulo;
            }
            return (b << 16) | a;
        }

        public static uint Adler32Combine(uint prevChksum, byte[] buffer, int offset, int count)
        {
            uint a = prevChksum & 0xFFFFu;
            uint b = (prevChksum >> 16) & 0xFFFFu;
            for (int i = 0; i < count; i++)
            {
                a = (a + buffer[offset + i]) % AdlerModulo;
                b = (b + a) % AdlerModulo;
            }
            return (b << 16) | a;
        }
    }
}
