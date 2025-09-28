using System;
using System.Windows.Forms;

namespace KartRider
{
	public static class LauncherSystem
	{
		public static void MessageBoxType1()
		{
			MessageBox.Show("跑跑卡丁车已经运行了！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public static void MessageBoxType2()
		{
			MessageBox.Show("已经有一个启动器在运行了！\n不可以同时运行多个启动器！\n点击确认退出程序", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Environment.Exit(1);
		}

		public static void MessageBoxType3()
		{
			MessageBox.Show("找不到KartRider.exe或KartRider.pin文件！\n点击确认退出程序", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Environment.Exit(1);
		}

		public static void MessageBoxType4()
		{
			MessageBox.Show("读取游戏Data内文件失败！\n请检查游戏是否完整，或尝试重新安装游戏！\n点击确认退出程序", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Environment.Exit(1);
		}
	}
}
