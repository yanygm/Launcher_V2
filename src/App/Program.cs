using KartRider;
using Launcher.App.Constant;
using Launcher.App.Forms;
using Launcher.App.Logger;
using Launcher.App.Profile;
using Launcher.App.Utility;
using Launcher.Library.Constant;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Launcher.App
{
    internal static class Program
    {
        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public const int SW_HIDE = 0;

        /// <summary>
        /// 显示窗口
        /// </summary>
        public const int SW_SHOW = 5;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern nint GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindowVisible(nint hWnd);

        /// <summary>
        /// 目标Encoding
        /// </summary>
        public static Encoding targetEncoding = Encoding.UTF8;

        /// <summary>
        /// 控制台窗口的句柄
        /// </summary>
        public static IntPtr consoleHandle;

        /// <summary>
        /// 游戏所在的目录
        /// </summary>
        public static string RootDirectory = FileName.AppDir;

        /// <summary>
        /// 当前系统架构 小写字符串 目前仅有 x64 x86 arm64
        /// </summary>
        public static string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower();

        // ----- Form Dialogs -----
        public static MainForm LauncherDlg;
        public static GetKart GetKartDlg;
        public static Options OptionsDlg;

        [STAThread]
        private static async Task Main(string[] args)
        {
            // 分配控制台
            AllocConsole();
            consoleHandle = Process.GetCurrentProcess().MainWindowHandle;

            // 初始化 Windows 窗体应用程序
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 保存原始输出流
            var originalOut = Console.Out;

            // 创建缓存编写器并替换控制台输出
            CachedConsoleWriter.cachedWriter = new CachedConsoleWriter(originalOut);
            Console.SetOut(CachedConsoleWriter.cachedWriter);

            // 初始化自适应编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SetAdaptiveConsoleEncoding();

            if (args != null && args.Length > 1)
            {
                // remove the 1st arg (which is the path to the program)
                // then process the last args
                Utils.ProcessPack(new ArraySegment<string>(args, 1, args.Length - 1).ToArray());
                return;
            }

            // auto hide console window if not in debug mode
            if (!ProfileService.ProfileConfig.ServerSetting.ConsoleVisibility)
                ShowWindow(consoleHandle, SW_HIDE);

            // check & create app directory
            if (!Directory.Exists(FileName.ProfileDir))
                Directory.CreateDirectory(FileName.ProfileDir);
            if (!Directory.Exists(FileName.LogDir))
                Directory.CreateDirectory(FileName.LogDir);

            if (!ProfileService.Load()) // New User
                Utils.MsgWelcome();

            if (Constants.DBG)
                Console.Write("[DEBUG] ");
            Console.WriteLine($"中国跑跑卡丁车单机启动器 {Constants.Version} for {architecture}" +
                $" - built on {File.GetLastWriteTime(Process.GetCurrentProcess().MainModule.FileName):yyyy/MM/dd HH:mm:ss K}, #{ThisAssembly.Git.Commit}");
            string args_string = args != null && args.Length > 0 ? string.Join(' ', args) : "null";
            Console.WriteLine($"启动参数/开关: [{args_string}]");
            Utils.PrintDivLine();

            // get country code
            string countryCode = await Utils.GetCountryAsync();
            if (countryCode != "") // available country code
            {
                // change country code & write to file
                ProfileService.ProfileConfig.ServerSetting.CC = (CountryCode)Enum.Parse(typeof(CountryCode), countryCode);
                ProfileService.Save();
            }
            Console.WriteLine($"最后一次联网打开地区为: {ProfileService.ProfileConfig.ServerSetting.CC}");

            Utils.PrintDivLine();

            // auto check update
            if (ProfileService.ProfileConfig.ServerSetting.AutoUpdate)
            {
                new Updater().ShowDialog();
                Utils.PrintDivLine();
            }

            if (Process.GetProcessesByName("KartRider").Length != 0)
            {
                Utils.MsgKartIsRunning();
                return;
            }

            // find game directory
            if (Utils.CheckGameAvailability(FileName.AppDir))
            {
                // working directory
                RootDirectory = FileName.AppDir;
                Console.WriteLine("使用当前目录下的游戏.");
            }
            else if (Utils.CheckGameAvailability(FileName.TCGKartGamePath))
            {
                // TCGame registered directory
                RootDirectory = FileName.TCGKartGamePath;
                Console.WriteLine("使用TCGame注册的游戏目录下的游戏.");
            }
            else
            {
                // game not found
                Utils.MsgFileNotFound();
            }
            Console.WriteLine($"游戏目录: {RootDirectory}");
            Utils.PrintDivLine();

            // Open Main Form
            LauncherDlg = new();
            Application.Run(LauncherDlg);

            return;
        }

        public static void SetAdaptiveConsoleEncoding()
        {
            try
            {
                // 1. 检测操作系统类型
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                // 2. 优先尝试 UTF-8 (跨平台通用)
                targetEncoding = Encoding.UTF8;

                // 3. Windows 中文环境特殊处理 (部分终端默认 GBK)
                if (isWindows)
                {
                    // 获取系统默认区域语言
                    var systemCulture = CultureInfo.InstalledUICulture;
                    bool isChineseCulture = systemCulture.Name.StartsWith("zh-");

                    // 中文系统尝试使用 GBK (CP936), 避免 UTF-8 在部分老终端乱码
                    if (isChineseCulture)
                    {
                        try
                        {
                            targetEncoding = Encoding.GetEncoding(936); // 936 对应 GBK
                        }
                        catch (ArgumentException)
                        {
                            // 极少数情况系统不支持 GBK, 则 fallback 到 UTF-8
                            targetEncoding = Encoding.UTF8;
                        }
                    }
                }

                // 4. 应用编码设置 (输出/输入保持一致)
                Console.OutputEncoding = targetEncoding;
                Console.InputEncoding = targetEncoding;

                // 5. 验证编码是否生效 (可选)
                Console.WriteLine($"已适配编码: {targetEncoding.EncodingName}");
            }
            catch (Exception ex)
            {
                // 异常时使用系统默认编码作为最后保障
                Console.WriteLine($"编码设置失败, 使用默认编码: {ex.Message}");
            }
        }
    }
}
