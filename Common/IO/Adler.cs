using System;

namespace KartLibrary.IO;

public static class Adler
{
    public const uint AdlerModulo = 65521u;

    public static uint Adler32(uint adler, byte[] buffer, int offset, int count)
    {
        if (buffer.Length < offset + count)
        {
            throw new Exception("buffer is small.");
        }

        uint num = adler & 0xFFFFu;
        uint num2 = (adler >> 16) & 0xFFFFu;
        for (int i = 0; i < count; i++)
        {
            num = (num + buffer[offset + i]) % 65521;
            num2 = (num2 + num) % 65521;
        }

        return (num2 << 16) | num;
    }

    public static uint Adler32Combine(uint prevChksum, byte[] buffer, int offset, int count)
    {
        uint num = prevChksum & 0xFFFFu;
        uint num2 = (prevChksum >> 16) & 0xFFFFu;
        for (int i = 0; i < count; i++)
        {
            num = (num + buffer[offset + i]) % 65521;
            num2 = (num2 + num) % 65521;
        }

        return (num2 << 16) | num;
    }
}