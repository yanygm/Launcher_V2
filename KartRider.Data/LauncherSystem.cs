using System;
using System.Windows.Forms;

namespace KartRider
{
	public static class LauncherSystem
	{
		public static void MessageBoxType1()
		{
			MessageBox.Show("跑跑卡丁车正在运行，请结束跑跑卡丁车后再退出该程序！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void MessageBoxType2()
		{
			MessageBox.Show("跑跑卡丁车正在运行，请结束跑跑卡丁车后再退出该程序！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void MessageBoxType3()
		{
			MessageBox.Show(Launcher.KartRider + " 或 " + Launcher.pinFile + " 找不到文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
			Environment.Exit(1);
		}
	}
}
