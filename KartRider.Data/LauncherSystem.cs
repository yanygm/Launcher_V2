using KartLibrary.File;
using KartRider.Common.Data;
using KartRider.IO.Packet;
using Profile;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

		public static void MessageBoxType3(string RootDirectory)
		{
			DialogResult result = MessageBox.Show(
				"找不到游戏文件！\n点击确认下载游戏文件到本程序目录，取消结束程序",
				"确认操作",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Question);
			
			if (result == DialogResult.OK)
			{
				// 使用本程序目录作为游戏目录进行下载
				string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (string.IsNullOrEmpty(RootDirectory))
				{
                    CheckGame(currentDirectory);
                }
				else
				{
                    CheckGame(RootDirectory);
                }
            }
			else
			{
				Environment.Exit(1);
			}
		}

		public static async Task CheckGameAsync(string kartRiderDirectory, string updateUrl = "")
		{
			// 强制显示终端窗口
            bool wasVisible = Program.isVisible;
            if (!Program.isVisible)
            {
                Program.isVisible = true;
                Program.ShowWindow(Program.consoleHandle, Program.SW_SHOW);
            }

			try
			{
				string filePath = JsonHelper.GetFilePath();
				if (string.IsNullOrEmpty(updateUrl))
				{
					var data = await Update.GetUpdateAsync().ConfigureAwait(false);
					if (data != null)
					{
						updateUrl = data.update_prefix;
					}
					else
					{
						MessageBox.Show("获取游戏版本失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				await new PatchManager().StartPatchAsync(updateUrl, kartRiderDirectory).ConfigureAwait(false);

				PINFile val = new PINFile(Path.GetFullPath(Path.Combine(kartRiderDirectory, @"KartRider.pin")));
				ProfileService.SettingConfig.ClientVersion = val.Header.MinorVersion;
				ProfileService.SettingConfig.LocaleID = val.Header.LocaleID;
				ProfileService.SettingConfig.nClientLoc = val.Header.Unk2;
				ProfileService.SaveSettings();
				// 更新完成后，根据设置恢复终端显示状态
				if (!wasVisible && !ProfileService.SettingConfig.Console)
				{
					Program.isVisible = false;
					Program.ShowWindow(Program.consoleHandle, Program.SW_HIDE);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"更新过程发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static void CheckGame(string kartRiderDirectory, string updateUrl = "", bool single = true)
		{
			Exception capturedException = null;

			// 在新线程中运行异步操作，避免阻塞 UI 线程
			var thread = new Thread(() =>
			{
				try
				{
					Console.WriteLine("[CheckGame] 启动更新线程");
					CheckGameAsync(kartRiderDirectory, updateUrl).GetAwaiter().GetResult();
					Console.WriteLine("[CheckGame] 更新线程完成");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[CheckGame] 更新线程异常: {ex.GetType().Name}: {ex.Message}");
					Console.WriteLine($"[CheckGame] 堆栈: {ex.StackTrace}");
					capturedException = ex;
				}
			})
			{
				IsBackground = false, // 改为前台线程，确保异常能被捕获
				Name = "GameUpdateThread"
			};

			Console.WriteLine("[CheckGame] 启动线程");
			thread.Start();
			Console.WriteLine("[CheckGame] 等待线程完成...");
			thread.Join(); // 等待线程完成
			Console.WriteLine("[CheckGame] 线程已结束");

			if (capturedException != null)
			{
				MessageBox.Show($"更新过程发生错误：{capturedException.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				if (single)
				{
					PatchManager.RhoDump(kartRiderDirectory);
				}
			}
		}
	}
}