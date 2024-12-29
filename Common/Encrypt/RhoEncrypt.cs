using System;

namespace KartLibrary.Encrypt;

public static class RhoEncrypt
{
    public static byte[] DecryptData(uint Key, byte[] Data)
    {
        byte[] array = RhoKey.ExtendKey(Key);
        byte[] array2 = new byte[Data.Length];
        for (int i = 0; i < Data.Length; i++)
        {
            array2[i] = (byte)(Data[i] ^ array[i & 0x3F]);
        }

        return array2;
    }

    public static void DecryptData(uint Key, byte[] Data, int Offset, int Length)
    {
        if (Offset + Length > Data.Length)
        {
            throw new Exception("Over range.");
        }

        byte[] array = RhoKey.ExtendKey(Key);
        for (int i = 0; i < Length; i++)
        {
            int num = i + Offset;
            Data[num] ^= array[num & 0x3F];
        }
    }

    public unsafe static byte[] DecryptHeaderInfo(byte[] Data, uint Key)
    {
        uint num = Key;
        uint num2 = 0u;
        byte[] array = new byte[Data.Length];
        fixed (byte* ptr = array)
        {
            fixed (byte* ptr3 = Data)
            {
                uint* ptr2 = (uint*)ptr;
                uint* ptr4 = (uint*)ptr3;
                for (int i = 0; i < Data.Length >> 2; i++)
                {
                    uint vector = RhoKey.GetVector(num);
                    uint num3 = ptr4[i];
                    num3 ^= vector;
                    num2 += (ptr2[i] = num3 ^ num2);
                    num++;
                }
            }
        }

        return array;
    }

    public static byte[] DecryptBlockInfoOld(byte[] Data, byte[] key)
    {
        if (Data.Length != 32)
        {
            throw new NotSupportedException("Exception: the length of Data is not 32 bytes.");
        }

        byte[] array = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            array[i] = (byte)(key[i] ^ Data[i]);
        }

        return array;
    }

    public static byte[] EncryptData(uint Key, byte[] Data)
    {
        byte[] array = RhoKey.ExtendKey(Key);
        byte[] array2 = new byte[Data.Length];
        for (int i = 0; i < Data.Length; i++)
        {
            array2[i] = (byte)(Data[i] ^ array[i & 0x3F]);
        }

        return array2;
    }

    public static void EncryptData(uint Key, byte[] Data, int Offset, int Length)
    {
        if (Offset + Length > Data.Length)
        {
            throw new Exception("Over range.");
        }

        byte[] array = RhoKey.ExtendKey(Key);
        for (int i = 0; i < Length; i++)
        {
            int num = i + Offset;
            Data[num] ^= array[num & 0x3F];
        }
    }

    public unsafe static byte[] EncryptHeaderInfo(byte[] Data, uint Key)
    {
        uint num = Key;
        uint num2 = 0u;
        byte[] array = new byte[Data.Length];
        fixed (byte* ptr = array)
        {
            fixed (byte* ptr3 = Data)
            {
                uint* ptr2 = (uint*)ptr;
                uint* ptr4 = (uint*)ptr3;
                for (int i = 0; i < Data.Length >> 2; i++)
                {
                    uint vector = RhoKey.GetVector(num);
                    uint num3 = ptr4[i];
                    uint num4 = ptr4[i];
                    num4 ^= vector;
                    num4 ^= num2;
                    ptr2[i] = num4;
                    num2 += num3;
                    num++;
                }
            }
        }

        return array;
    }

    public static byte[] EncryptBlockInfoOld(byte[] Data, byte[] key)
    {
        if (Data.Length != 32)
        {
            throw new NotSupportedException("Exception: the length of Data is not 32 bytes.");
        }

        byte[] array = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            array[i] = (byte)(key[i] ^ Data[i]);
        }

        return array;
    }
}