using System;

namespace KartRider.Common.Security
{
	public static class KRPacketCrypto
	{
		public static uint HashDecrypt(byte[] pData, uint nLength, uint nKey)
		{
			uint num = nKey ^ 347277256;
			uint num1 = nKey ^ 2361332396;
			uint num2 = nKey ^ 604215233;
			uint num3 = nKey ^ 4089260480;
			int num4 = 0;
			uint num5 = 0;
			int i = 0;
			for (i = 0; (ulong)i < (ulong)(nLength >> 4); i++)
			{
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4) ^ num), 0, pData, num4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 4) ^ num1), 0, pData, num4 + 4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 8) ^ num2), 0, pData, num4 + 8, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 12) ^ num3), 0, pData, num4 + 12, 4);
				num5 = num5 ^ BitConverter.ToUInt32(pData, num4 + 12) ^ BitConverter.ToUInt32(pData, num4 + 8) ^ BitConverter.ToUInt32(pData, num4 + 4) ^ BitConverter.ToUInt32(pData, num4);
				num4 += 16;
			}
			i *= 16;
			num4 = 0;
			byte[] bytes = BitConverter.GetBytes(num);
			byte[] numArray = BitConverter.GetBytes(num1);
			byte[] bytes1 = BitConverter.GetBytes(num2);
			byte[] numArray1 = BitConverter.GetBytes(num3);
			byte[] numArray2 = new byte[16];
			Buffer.BlockCopy(bytes, 0, numArray2, 0, 4);
			Buffer.BlockCopy(numArray, 0, numArray2, 4, 4);
			Buffer.BlockCopy(bytes1, 0, numArray2, 8, 4);
			Buffer.BlockCopy(numArray1, 0, numArray2, 12, 4);
			while ((ulong)i < (ulong)nLength)
			{
				ref byte numPointer = ref pData[i];
				numPointer = (byte)(numPointer ^ numArray2[num4]);
				num5 = (uint)(num5 ^ pData[i] << (num4 & 31));
				i++;
				num4++;
			}
			return num5;
		}

		public static uint HashEncrypt(byte[] pData, uint nLength, uint nKey)
		{
			uint num = nKey ^ 347277256;
			uint num1 = nKey ^ 2361332396;
			uint num2 = nKey ^ 604215233;
			uint num3 = nKey ^ 4089260480;
			int num4 = 0;
			uint num5 = 0;
			int i = 0;
			for (i = 0; (ulong)i < (ulong)(nLength >> 4); i++)
			{
				num5 = num5 ^ BitConverter.ToUInt32(pData, num4 + 12) ^ BitConverter.ToUInt32(pData, num4 + 8) ^ BitConverter.ToUInt32(pData, num4 + 4) ^ BitConverter.ToUInt32(pData, num4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4) ^ num), 0, pData, num4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 4) ^ num1), 0, pData, num4 + 4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 8) ^ num2), 0, pData, num4 + 8, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 12) ^ num3), 0, pData, num4 + 12, 4);
				num4 += 16;
			}
			i *= 16;
			num4 = 0;
			byte[] bytes = BitConverter.GetBytes(num);
			byte[] numArray = BitConverter.GetBytes(num1);
			byte[] bytes1 = BitConverter.GetBytes(num2);
			byte[] numArray1 = BitConverter.GetBytes(num3);
			byte[] numArray2 = new byte[16];
			Buffer.BlockCopy(bytes, 0, numArray2, 0, 4);
			Buffer.BlockCopy(numArray, 0, numArray2, 4, 4);
			Buffer.BlockCopy(bytes1, 0, numArray2, 8, 4);
			Buffer.BlockCopy(numArray1, 0, numArray2, 12, 4);
			while ((ulong)i < (ulong)nLength)
			{
				num5 = (uint)(num5 ^ pData[i] << (num4 & 31));
				ref byte numPointer = ref pData[i];
				numPointer = (byte)(numPointer ^ numArray2[num4]);
				i++;
				num4++;
			}
			return num5;
		}
	}
}