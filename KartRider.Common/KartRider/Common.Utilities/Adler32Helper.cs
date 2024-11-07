using System;
using System.Text;

namespace KartRider.Common.Utilities
{
	public class Adler32Helper
	{
		public Adler32Helper()
		{
		}

		public static uint GenerateAdler32(byte[] str, uint a1 = 0)
		{
			uint num;
			int num1 = 0;
			uint length = (uint)str.Length;
			uint num2 = a1 >> 16;
			uint num3 = (ushort)a1;
			if ((int)str.Length == 1)
			{
				int num4 = (int)(str[0] + num3);
				if (num4 >= 65521)
				{
					num4 -= 65521;
				}
				int num5 = (int)((ulong)num4 + (ulong)num2);
				if (num5 >= 65521)
				{
					num5 -= 65521;
				}
				num = (uint)(num4 | num5 << 16);
			}
			else if (str == null)
			{
				num = 1;
			}
			else if ((int)str.Length < 16)
			{
				if (str.Length != 0)
				{
					do
					{
						int num6 = num1;
						num1 = num6 + 1;
						num3 += str[num6];
						num2 += num3;
						length--;
					}
					while (length != 0);
				}
				if (num3 >= 65521)
				{
					num3 -= 65521;
				}
				num = num3 | num2 % 65521 << 16;
			}
			else
			{
				if ((int)str.Length >= 5552)
				{
					uint length1 = (uint)((int)str.Length / 5552);
					do
					{
						length -= 5552;
						int num7 = 347;
						do
						{
							int num8 = (int)(str[num1] + num3);
							int num9 = (int)(num8 + num2);
							int num10 = str[num1 + 1] + num8;
							int num11 = num10 + num9;
							int num12 = str[num1 + 2] + num10;
							int num13 = num12 + num11;
							int num14 = str[num1 + 3] + num12;
							int num15 = num14 + num13;
							int num16 = str[num1 + 4] + num14;
							int num17 = num16 + num15;
							int num18 = str[num1 + 5] + num16;
							int num19 = num18 + num17;
							int num20 = str[num1 + 6] + num18;
							int num21 = num20 + num19;
							int num22 = str[num1 + 7] + num20;
							int num23 = num22 + num21;
							int num24 = str[num1 + 8] + num22;
							int num25 = num24 + num23;
							int num26 = str[num1 + 9] + num24;
							int num27 = num26 + num25;
							int num28 = str[num1 + 10] + num26;
							int num29 = num28 + num27;
							int num30 = str[num1 + 11] + num28;
							int num31 = num30 + num29;
							int num32 = str[num1 + 12] + num30;
							int num33 = num32 + num31;
							int num34 = str[num1 + 13] + num32;
							int num35 = num34 + num33;
							int num36 = str[num1 + 14] + num34;
							int num37 = num36 + num35;
							num3 = (uint)(str[num1 + 15] + num36);
							num2 = (uint)(num3 + num37);
							num1 += 16;
							num7--;
						}
						while (num7 != 0);
						num3 %= 65521;
						length1--;
						num2 %= 65521;
					}
					while (length1 != 0);
				}
				if (length != 0)
				{
					if (length >= 16)
					{
						uint num38 = length >> 4;
						do
						{
							int num39 = (int)(str[num1] + num3);
							int num40 = (int)(num39 + num2);
							int num41 = str[num1 + 1] + num39;
							int num42 = num41 + num40;
							int num43 = str[num1 + 2] + num41;
							int num44 = num43 + num42;
							int num45 = str[num1 + 3] + num43;
							int num46 = num45 + num44;
							int num47 = str[num1 + 4] + num45;
							int num48 = num47 + num46;
							int num49 = str[num1 + 5] + num47;
							int num50 = num49 + num48;
							int num51 = str[num1 + 6] + num49;
							int num52 = num51 + num50;
							int num53 = str[num1 + 7] + num51;
							int num54 = num53 + num52;
							int num55 = str[num1 + 8] + num53;
							int num56 = num55 + num54;
							int num57 = str[num1 + 9] + num55;
							int num58 = num57 + num56;
							int num59 = str[num1 + 10] + num57;
							int num60 = num59 + num58;
							int num61 = str[num1 + 11] + num59;
							int num62 = num61 + num60;
							int num63 = str[num1 + 12] + num61;
							int num64 = num63 + num62;
							int num65 = str[num1 + 13] + num63;
							int num66 = num65 + num64;
							int num67 = str[num1 + 14] + num65;
							int num68 = num67 + num66;
							num3 = (uint)(str[num1 + 15] + num67);
							length -= 16;
							num2 = (uint)(num3 + num68);
							num1 += 16;
							num38--;
						}
						while (num38 != 0);
					}
					while (length != 0)
					{
						int num69 = num1;
						num1 = num69 + 1;
						num3 += str[num69];
						num2 += num3;
						length--;
					}
					num3 %= 65521;
					num2 %= 65521;
				}
				num = num3 | num2 << 16;
			}
			return num;
		}

		public static uint GenerateAdler32_ASCII(string str, uint a1 = 0)
		{
			uint num = Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes(str), a1);
			return num;
		}

		public static uint GenerateAdler32_UNICODE(string str, uint a1 = 0)
		{
			uint num = Adler32Helper.GenerateAdler32(Encoding.Unicode.GetBytes(str), a1);
			return num;
		}

		public static int GenerateSimpleAdler32(byte[] bytes)
		{
			uint num = 1;
			uint num1 = 0;
			byte[] numArray = bytes;
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				num = (num + numArray[i]) % 65521;
				num1 = (num1 + num) % 65521;
			}
			return (int)((num1 << 16) + num);
		}
	}
}