using KartRider.IO.Packet;
using Microsoft.Win32;
using Profile;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Threading.Tasks;
using KartRider.Common.Data;
using KartRider.Common.Security;
using KartRider.Logging;

namespace KartRider
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public static bool isVisible = true;
        public static readonly IntPtr consoleHandle = GetConsoleWindow();

        public static Launcher LauncherDlg;
        public static Setting SettingDlg;
        public static bool SpeedPatch;
        public static bool PreventItem;
        public static Encoding targetEncoding = Encoding.UTF8;

        [STAThread]
        private static async Task Main(string[] args)
        {
            // 分配控制台
            AllocConsole();

            // 保存原始输出流
            var originalOut = Console.Out;

            // 创建缓存编写器并替换控制台输出（stdout + stderr 都拦截）
            CachedConsoleWriter.cachedWriter = new CachedConsoleWriter(originalOut);
            Console.SetOut(CachedConsoleWriter.cachedWriter);
            Console.SetError(CachedConsoleWriter.cachedWriter);

            // 崩溃转储只保留最近若干行（有界，不再无限增长）
            CachedConsoleWriter.cachedWriter = new CachedConsoleWriter(originalOut);
            Console.SetOut(CachedConsoleWriter.cachedWriter);
            Console.SetError(CachedConsoleWriter.cachedWriter);

            Log.Init(Path.Combine(FileName.appDir, "Logs"), LogLevel.Info, packetTrace: false, announce: false);
            // --no-kartrules：本次运行不使用客户端词条规则（二分定位用）
            bool noKartRules = args != null && args.Any(a => string.Equals(a, "--no-kartrules", StringComparison.OrdinalIgnoreCase));

            // 初始化自适应编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SetAdaptiveConsoleEncoding();

            // 自更新后由新实例启动：清理旧版本备份文件，随后继续正常主流程
            bool isUpdatedRestart = args != null && args.Length > 0 && string.Equals(args[0], "/updated", StringComparison.OrdinalIgnoreCase);
            if (isUpdatedRestart)
            {
                Update.CleanupOldVersion();
            }

            if (args != null && args.Length > 0 && !isUpdatedRestart)
            {
                RhoPacker.PackTool(args);
            }
            else
            {
                ProfileService.LoadSettings();
                ApplyLogSettings();
                ClientItemRules.SetEnabled(!noKartRules);
                // 检查更新：AutoUpdate 开启时静默更新，关闭时弹窗提示
                await Update.UpdateDataAsync(ProfileService.SettingConfig.AutoUpdate);
                string TCGame = "HKEY_CURRENT_USER\\Software\\TCGame\\kart";
                string RootDirectory = (string)Registry.GetValue(TCGame, "gamepath", null);
                if (File.Exists(FileName.pinFile) && File.Exists(FileName.KartRider))
                {
                    RootDirectory = FileName.appDir;
                }
                else if (!string.IsNullOrEmpty(RootDirectory) &&
                    File.Exists(Path.Combine(RootDirectory, @"KartRider.pin")) &&
                    File.Exists(Path.Combine(RootDirectory, @"KartRider.exe")) &&
                    File.Exists(Path.Combine(RootDirectory, @"Patcher.ex0")))
                {
                    RootDirectory = Path.GetFullPath(RootDirectory);
                }
                else
                {
                    LauncherSystem.MessageBoxType3(RootDirectory);
                    return;
                }
                if (!File.Exists(Path.Combine(RootDirectory, @"Patcher.exe")) && File.Exists(Path.Combine(RootDirectory, @"Patcher.ex0")))
                {
                    File.Copy(Path.Combine(RootDirectory, @"Patcher.ex0"), Path.Combine(RootDirectory, @"Patcher.exe"));
                }
                string KartRider = Path.GetFullPath(Path.Combine(RootDirectory, @"KartRider.exe"));
                string pinFile = Path.GetFullPath(Path.Combine(RootDirectory, @"KartRider.pin"));
                string pinFileBak = Path.GetFullPath(Path.Combine(RootDirectory, @"KartRider-bak.pin"));
                if (!string.IsNullOrEmpty(RootDirectory))
                {
                    if (File.Exists(pinFileBak))
                    {
                        File.Delete(pinFile);
                        File.Move(pinFileBak, pinFile);
                    }

                    Load_Data();

                    if (LanIpGetter.IsIPv6(ProfileService.SettingConfig.ServerIP))
                    {
                        // IPv6 远程服务器模式：仅启动端口转发（客户端 → IPv6 服务器）
                        // 不启动 RouterListener，避免端口冲突（SocketError 10048）
                        TinyMapper.ClientStart();
                    }
                    else
                    {
                        try
                        {
                            RouterListener.Start();
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            LauncherSystem.MessageBoxType2();
                        }
                        catch (Exception ex)
                        {
                            Log.Debug($"启动路由器监听失败: {ex.Message}");
                        }
                    }

                    PatchManager.StartUpdateAsync(RootDirectory).Wait();

                    Metrics.StartSampler();
                    if (ProfileService.SettingConfig.AdminPort > 0)
                    {
                        LocalAdminApi.Start(ProfileService.SettingConfig.AdminPort, ProfileService.SettingConfig.AdminToken);
                    }

                    PINFile val = new PINFile(pinFile);
                    ProfileService.SettingConfig.ClientVersion = val.Header.MinorVersion;
                    ProfileService.SettingConfig.LocaleID = val.Header.LocaleID;
                    ProfileService.SettingConfig.nClientLoc = val.Header.Unk2;
                    ProfileService.SaveSettings();

                    if (!ProfileService.SettingConfig.Console)
                    {
                        ShowWindow(consoleHandle, SW_HIDE);
                        isVisible = false;
                    }
                    if (ProfileService.SettingConfig.EnableMod)
                    {
                        // 初始化ModManager
                        ModManager.Initialize(FileName.appDir);
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Launcher StartLauncher = new Launcher();
                    Program.LauncherDlg = StartLauncher;
                    Program.LauncherDlg.kartRiderDirectory = RootDirectory;
                    Launcher.KartRider = KartRider;
                    Launcher.pinFile = pinFile;
                    Launcher.pinFileBak = pinFileBak;
                    Application.Run(StartLauncher);
                }
            }
        }

        public static void SetAdaptiveConsoleEncoding()
        {
            try
            {
                // 1. 检测操作系统类型
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                // 2. 优先尝试 UTF-8（跨平台通用）
                targetEncoding = Encoding.UTF8;

                // 3. Windows 中文环境特殊处理（部分终端默认 GBK）
                if (isWindows)
                {
                    try
                    {
                        // 注册表路径
                        string codePageRegPath = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Nls\\CodePage";

                        // 读取 OEMCP 值（返回 object 类型，需判断是否为 null）
                        object oemcpObj = Registry.GetValue(codePageRegPath, "OEMCP", null);

                        // 正确判断：是否读取到有效值，且能转换为 int
                        if (oemcpObj != null && int.TryParse(oemcpObj.ToString(), out int oemcp))
                        {
                            try
                            {
                                // 获取对应编码
                                targetEncoding = Encoding.GetEncoding(oemcp);
                            }
                            catch (ArgumentException)
                            {
                                // 编码不支持时回退到 UTF-8
                                targetEncoding = Encoding.UTF8;
                            }
                        }
                        else
                        {
                            // 未读取到 OEMCP 值
                            targetEncoding = Encoding.UTF8;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕获注册表读取异常（如权限不足）
                        targetEncoding = Encoding.UTF8;
                    }
                }
                // 4. 应用编码设置（输出/输入保持一致）
                Console.OutputEncoding = targetEncoding;
                Console.InputEncoding = targetEncoding;

                // 5. 验证编码是否生效（可选）
                Log.Info($"已适配编码: {targetEncoding.EncodingName}");
            }
            catch (Exception ex)
            {
                // 异常时使用系统默认编码作为最后保障
                Log.Info($"编码设置失败，使用默认编码: {ex.Message}");
            }
        }

        public static void Load_Data()
        {
            try
            {
                string localFilePath = FileName.ModelMax_LoadFile;

                string directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(localFilePath))
                {
                    File.WriteAllText(localFilePath, ModelMax.XmlContent);
                    Log.Info($"ModelMax.xml已创建: {localFilePath}");
                }
                else if (ProfileService.SettingConfig.ModelMaxAutoUpdate)
                {
                    XDocument localXml = XDocument.Load(localFilePath);
                    XDocument resourceXml = XDocument.Parse(ModelMax.XmlContent);

                    int addedCount = 0;

                    foreach (var resourceKart in resourceXml.Root!.Elements("kart"))
                    {
                        string? id = resourceKart.Attribute("id")?.Value;
                        string? name = resourceKart.Attribute("name")?.Value;

                        if (id != null)
                        {
                            bool exists = localXml.Root!.Elements("kart")
                                .Any(k => k.Attribute("id")?.Value == id);

                            if (!exists)
                            {
                                localXml.Root.Add(new XElement(resourceKart));
                                Log.Debug($"已添加 kart: {name ?? id}");
                                addedCount++;
                            }
                        }
                    }

                    if (addedCount > 0)
                    {
                        localXml.Save(localFilePath, SaveOptions.None);
                        Log.Info($"ModelMax.xml已更新，新增 {addedCount} 个kart");
                    }
                    else
                    {
                        Log.Debug("ModelMax.xml已是最新，无需更新");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("加载数据失败", ex);
            }
        }

        /// <summary>
        /// 把设置里的日志级别/包跟踪应用到日志器（设置变更后可再次调用）。
        /// </summary>
        public static void ApplyLogSettings()
        {
            Profile.Setting setting = ProfileService.SettingConfig ?? new Profile.Setting();
            LogLevel level = LogLevel.Info;
            if (!string.IsNullOrEmpty(setting.LogLevel))
            {
                Enum.TryParse(setting.LogLevel, true, out level);
            }
            Log.MinLevel = level;
            Log.SetEnabled(setting.LogEnabled);
            // 控制台窗口是否显示只由 Setting.Console 决定，与日志开关无关
            Log.Init(Path.Combine(FileName.appDir, "Logs"), level, setting.PacketTrace, console: true, announce: setting.LogEnabled);
            if (setting.LogEnabled)
            {
                Log.ConfigurePacketTrace(setting.PacketTrace, setting.PacketTraceFilter, setting.PacketTraceSample,
                                         setting.PacketTraceConsole, setting.PacketRing, 500, setting.PacketTraceShowUdp);
            }
        }
    }
}