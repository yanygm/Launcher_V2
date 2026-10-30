using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Profile;
using KartRider.Logging;

namespace KartRider
{
    public partial class Launcher : Form
    {
        public string kartRiderDirectory;
        public static string KartRider;
        public static string pinFile;
        public static string pinFileBak;

        public Launcher()
        {
            RestorePinFile();
            this.InitializeComponent();
        }

        /// <summary>
        /// 恢复备份的 PIN 文件
        /// </summary>
        /// <returns>是否恢复成功</returns>
        private bool RestorePinFile()
        {
            if (string.IsNullOrEmpty(pinFileBak) || string.IsNullOrEmpty(pinFile))
                return false;

            if (!File.Exists(pinFileBak))
            {
                Log.Info($"[PIN] 备份文件不存在，无法恢复");
                return false;
            }

            try
            {
                if (File.Exists(pinFile))
                    File.Delete(pinFile);
                File.Move(pinFileBak, pinFile);
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"[PIN] 恢复 PIN 文件失败: {ex.Message}");
                return false;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            RestorePinFile();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ClientVersion.Text = ProfileService.SettingConfig.ClientVersion.ToString();
            Log.Info($"ClientVersion: {ClientVersion.Text}");
            Log.Info($"程序编译时间: {CompileTime.Time}");
            VersionLabel.Text = CompileTime.Time;
            Log.Info($"Process: {KartRider}");
            string[] UserPaths = Directory.GetDirectories(FileName.ProfileDir);
            foreach (string userPath in UserPaths)
            {
                string UserName = Path.GetFileName(userPath);
                ClientManager.GetUserNO(UserName);
            }
        }

        private void Start_Button_Click(object sender, EventArgs e)
        {
            // if (Process.GetProcessesByName("KartRider").Length != 0)
            // {
            //     LauncherSystem.MessageBoxType1();
            // }
            // else
            // {
            var thread = new Thread(() =>
            {
                try
                {
                    LaunchGame();
                }
                catch (Exception ex)
                {
                    Log.Info($"启动游戏失败: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "GameLauncherThread"
            };
            thread.Start();
            // }
        }

        /// <summary>
        /// 启动游戏的核心逻辑
        /// </summary>
        private void LaunchGame()
        {
            if (string.IsNullOrEmpty(pinFile))
            {
                Log.Info($"[PIN] 文件不存在，无法启动游戏");
                return;
            }

            var Game = new StartGame();
            Game.Start(kartRiderDirectory, pinFile, pinFileBak);
        }

        private void Setting_Button_Click(object sender, EventArgs e)
        {
            Program.SettingDlg = new Setting();
            Program.SettingDlg.ShowDialog();
        }

        private void GitHub_Click(object sender, EventArgs e)
        {
            string url = "https://yanygm.github.io/Launcher_V2/";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Info($"错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 标签点击事件处理器（同步包装）
        /// </summary>
        private async void label_Client_Click(object sender, EventArgs e)
        {
            try
            {
                await label_Client_ClickAsync(sender, e);
            }
            catch (Exception ex)
            {
                Log.Info($"检查更新时出错: {ex.Message}");
                MessageBox.Show($"检查更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 异步执行检查更新逻辑
        /// </summary>
        private async Task label_Client_ClickAsync(object sender, EventArgs e)
        {
            var data = await global::KartRider.Update.GetUpdateAsync();
            if (data == null)
            {
                MessageBox.Show("获取游戏版本失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 弹出“是否”确认框
            DialogResult result = MessageBox.Show(
                $"当前版本为：P{ClientVersion.Text}\n最新版本为：{data.version}\n是否需要更新？",
                "确认操作",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            // 根据用户选择执行对应逻辑
            if (result == DialogResult.Yes)
            {
                RestorePinFile();
                LauncherSystem.CheckGame(kartRiderDirectory);
            }
        }

        private void button_ToggleTerminal_Click(object sender, EventArgs e)
        {
            Program.isVisible = !Program.isVisible;
            // consoleHandle 在 AllocConsole 之前是 0，取不到时现取一次，避免按钮"点了没反应"
            IntPtr handle = Program.consoleHandle != IntPtr.Zero ? Program.consoleHandle : Program.GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                Program.ShowWindow(handle, Program.isVisible ? Program.SW_SHOW : Program.SW_HIDE);
            }
            ProfileService.SettingConfig.Console = Program.isVisible;
            ProfileService.SaveSettings();
        }

        private void ConsoleLogger_Click(object sender, EventArgs e)
        {
            CachedConsoleWriter.SaveToFile();
            CachedConsoleWriter.cachedWriter.ClearCache();
        }
    }
}