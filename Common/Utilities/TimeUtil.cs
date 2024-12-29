using System;
using System.Xml;

namespace KartNew.Utilities
{
	public class TimeUtil
	{
		private static ushort[] _yearTable;

		private static ushort[] _monthTable;

		private static ushort[] _monthLeapYearTable;

		static TimeUtil()
		{
			TimeUtil._yearTable = new ushort[] { 0, 365, 730, 1095, 1460, 1826, 2191, 2556, 2921, 3287, 3652, 4017, 4382, 4748, 5113, 5478, 5843, 6209, 6574, 6939, 7304, 7670, 8035, 8400, 8765, 9131, 9496, 9861, 10226, 10592, 10957, 11322, 11687, 12053, 12418, 12783, 13148, 13514, 13879, 14244, 14609, 14975, 15340, 15705, 16070, 16436, 16801, 17166, 17531, 17897, 18262, 18627, 18992, 19358, 19723, 20088, 20453, 20819, 21184, 21549, 21914, 22280, 22645, 23010, 23375, 23741, 24106, 24471, 24836, 25202, 25567, 25932, 26297, 26663, 27028, 27393, 27758, 28124, 28489, 28854, 29219, 29585, 29950, 30315, 30680, 31046, 31411, 31776, 32141, 32507, 32872, 33237, 33602, 33968, 34333, 34698, 35063, 35429, 35794, 36159, 36524, 36890, 37255, 37620, 37985, 38351, 38716, 39081, 39446, 39812, 40177, 40542, 40907, 41273, 41638, 42003, 42368, 42734, 43099, 43464, 43829, 44195, 44560, 44925, 45290, 45656, 46021, 46386, 46751, 47117, 47482, 47847, 48212, 48578, 48943, 49308, 49673, 50039, 50404, 50769, 51134, 51500, 51865, 52230, 52595, 52961, 53326, 53691, 54056, 54422, 54787, 55152, 55517, 55883, 56248, 56613, 56978, 57344, 57709, 58074, 58439, 58805, 59170, 59535, 59900, 60266, 60631, 60996, 61361, 61727, 62092, 62457, 62822, 63188, 63553, 63918, 64283, 64649, 65014, 65379, 65535 };
			TimeUtil._monthTable = new ushort[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
			TimeUtil._monthLeapYearTable = new ushort[] { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };
		}

		public TimeUtil()
		{
		}

		public static int GetDays(DateTime time)
		{
			int year = time.Year - 1900;
			int month = time.Month - 1;
			return TimeUtil._yearTable[year] + (TimeUtil.IsLeapYear(time.Year) ? TimeUtil._monthLeapYearTable[month] : TimeUtil._monthTable[month]) + time.Day;
		}

		public static int GetMonth(ref int date, bool leapYear)
		{
			bool flag;
			bool flag1;
			int num = 0;
			if (!leapYear)
			{
				while (true)
				{
					if (num >= 12)
					{
						flag = false;
					}
					else
					{
						flag = (TimeUtil._monthTable[num] >= date ? true : date > TimeUtil._monthTable[num + 1]);
					}
					if (!flag)
					{
						break;
					}
					num++;
				}
				date -= TimeUtil._monthTable[num];
			}
			else
			{
				while (true)
				{
					if (num >= 12)
					{
						flag1 = false;
					}
					else
					{
						flag1 = (TimeUtil._monthLeapYearTable[num] >= date ? true : date > TimeUtil._monthLeapYearTable[num + 1]);
					}
					if (!flag1)
					{
						break;
					}
					num++;
				}
				date -= TimeUtil._monthLeapYearTable[num];
			}
			return num;
		}

		public static int GetYear(ref int date)
		{
			int num;
			int num1 = 0;
			int num2 = 180;
			int num3 = 0;
			while (true)
			{
				if (num1 < num2)
				{
					num3 = (num1 + num2) / 2;
					if (TimeUtil._yearTable[num3] > date)
					{
						num2 = (num1 + num2) / 2;
					}
					else if (TimeUtil._yearTable[num3 + 1] <= date)
					{
						num1 = num3 + 1;
					}
					else
					{
						date -= TimeUtil._yearTable[num3];
						num = num3;
						break;
					}
				}
				else
				{
					date += 157;
					num = 179;
					break;
				}
			}
			return num;
		}

		public static bool IsInYear(string period, DateTime time)
		{
			string[] strArrays = period.Split(new char[] { '~' });
			DateTime dateTime = (strArrays[0] == "*" ? DateTime.MinValue : XmlConvert.ToDateTime(strArrays[0], XmlDateTimeSerializationMode.RoundtripKind));
			return (dateTime >= time ? false : time < (strArrays[1] == "*" ? DateTime.MaxValue : XmlConvert.ToDateTime(strArrays[1], XmlDateTimeSerializationMode.RoundtripKind)));
		}

		public static bool IsLeapYear(int year)
		{
			bool flag;
			if (year % 4 != 0)
			{
				flag = false;
			}
			else
			{
				flag = (year % 100 != 0 ? true : year % 400 == 0);
			}
			return flag;
		}
	}
}