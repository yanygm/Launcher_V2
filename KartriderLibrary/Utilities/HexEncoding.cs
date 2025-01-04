using System;
using System.Globalization;
using System.Text;

namespace KartRider.Common.Utilities
{
	public class HexEncoding
	{
		public HexEncoding()
		{
		}

		public static byte[] GetBytes(string pHexString)
		{
			string empty = string.Empty;
			for (int i = 0; i < pHexString.Length; i++)
			{
				char chr = pHexString[i];
				if (HexEncoding.IsHexDigit(chr))
				{
					empty = string.Concat(empty, chr.ToString());
				}
			}
			if (empty.Length % 2 != 0)
			{
				empty = empty.Substring(0, empty.Length - 1);
			}
			byte[] num = new byte[empty.Length / 2];
			int num1 = 0;
			for (int j = 0; j < (int)num.Length; j++)
			{
				num[j] = HexEncoding.HexToByte(new string(new char[] { empty[num1], empty[num1 + 1] }));
				num1 += 2;
			}
			return num;
		}

		public static string GetString(byte[] pArray)
		{
			StringBuilder stringBuilder = new StringBuilder();
			byte[] numArray = pArray;
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				byte num = numArray[i];
				stringBuilder.Append(num.ToString("X2")).Append(" ");
			}
			return stringBuilder.ToString();
		}

		private static byte HexToByte(string pHex)
		{
			if ((pHex.Length > 2 ? true : pHex.Length <= 0))
			{
				throw new ArgumentException("hex must be 1 or 2 characters in length");
			}
			return byte.Parse(pHex, NumberStyles.HexNumber);
		}

		public static bool IsHexDigit(char pChar)
		{
			bool flag;
			int num = Convert.ToInt32('A');
			int num1 = Convert.ToInt32('0');
			pChar = char.ToUpper(pChar);
			int num2 = Convert.ToInt32(pChar);
			if ((num2 < num ? true : num2 >= num + 6))
			{
				flag = ((num2 < num1 ? true : num2 >= num1 + 10) ? false : true);
			}
			else
			{
				flag = true;
			}
			return flag;
		}

		public static string ToStringFromAscii(byte[] pBytes)
		{
			char[] chrArray = new char[(int)pBytes.Length];
			for (int i = 0; i < (int)pBytes.Length; i++)
			{
				if ((pBytes[i] >= 32 ? true : pBytes[i] < 0))
				{
					chrArray[i] = (char)(pBytes[i] & 255);
				}
				else
				{
					chrArray[i] = '.';
				}
			}
			return new string(chrArray);
		}
	}
}