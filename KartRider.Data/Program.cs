using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
using KartRider.IO.Packet;
using KartRider.Logging;
using Microsoft.Win32;
using Profile;
using LoggerLibrary;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Threading.Tasks;
using KartRider.Common.Data;
using KartRider.Common.Security;

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
        /// <summary>控制台窗口句柄：必须在 AllocConsole() 之后获取，静态初始化时还是 0。</summary>
        public static IntPtr consoleHandle;

        public static Launcher LauncherDlg;
        public static Setting SettingDlg;
        public static Encoding targetEncoding = Encoding.UTF8;

        private static Mutex _singleInstance;

        [STAThread]
        private static async Task Main(string[] args)
        {
            // ===== 无界面自检模式：不分配控制台、不加载 WinForms =====
            if (args != null && args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = SelfTest.RunAll(args.Length > 1 && args[1] == "--verbose");
                return;
            }

                        // ===== 单实例保护：避免两个启动器抢端口 / 互相干扰客户端 =====
            try
            {
                _singleInstance = new Mutex(true, @"Global\KartRiderLauncher_V2_Optimized", out bool createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("启动器已经在运行，请先关闭已有窗口（可在任务管理器结束 Launcher.exe）。",
                                    "重复启动", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch
            {
                // 拿不到互斥量时继续启动，不能因此挡住正常使用
            }

            // 分配控制台
            AllocConsole();
            consoleHandle = GetConsoleWindow();

            // 保存原始输出流
            var originalOut = Console.Out;

            // 崩溃转储只保留最近若干行（有界，不再无限增长）
            CachedConsoleWriter.cachedWriter = new CachedConsoleWriter(originalOut);
            Console.SetOut(CachedConsoleWriter.cachedWriter);
            Console.SetError(CachedConsoleWriter.cachedWriter);

            Log.Init(Path.Combine(FileName.appDir, "Logs"), LogLevel.Info, packetTrace: false, announce: false);

            // --no-kartrules：本次运行不使用客户端词条规则（二分定位用）
            bool noKartRules = args != null && args.Any(a => string.Equals(a, "--no-kartrules", StringComparison.OrdinalIgnoreCase));

            if (args != null && args.Length > 0 && !noKartRules)
            {
                RhoPacker.PackTool(args);
            }
            else
            {
                ProfileService.LoadSettings();
                ApplyLogSettings();
                ClientItemRules.SetEnabled(!noKartRules);

                // 打印构建标识，避免"跑的是哪个 exe"这种问题再次发生
                try
                {
                    string exePath = Application.ExecutablePath;
                    string sha = "";
                    try
                    {
                        sha = Update.CalculateSHA256(exePath);
                        if (sha.Length > 16)
                        {
                            sha = sha.Substring(0, 16) + "…";
                        }
                    }
                    catch
                    {
                    }
                    Log.Info($"[Build] Launcher_V2 优化版（编译 {CompileTime.Time}）| {exePath} | sha256:{sha}");
                }
                catch (Exception ex)
                {
                    Log.Debug("[Build] 打印构建标识失败: " + ex.Message);
                }

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

                    // 启动时把"词条→道具"生效表写进日志，便于与客户端仓库的文案逐条对照
        
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
                        catch (System.Net.Sockets.SocketException ex)
                        {
                            Log.Error("启动端口监听失败（可能端口被占用）", ex);
                            LauncherSystem.MessageBoxType2();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("启动路由器监听失败", ex);
                        }
                    }

                    PatchManager.StartUpdateAsync(RootDirectory).Wait();

                    Metrics.StartSampler();
                    if (ProfileService.SettingConfig.AdminPort > 0)
                    {
                        LocalAdminApi.Start(ProfileService.SettingConfig.AdminPort, ProfileService.SettingConfig.AdminToken);
                    }

                    PINFile val = new PINFile(pinFile);
                    // 只有客户端版本等字段真的变了才写盘：否则每次启动都会 File.Replace 一次，
                    // 把用户手改的 Settings.json 挤成 .bak（用户改的配置就"永远不生效"）
                    bool settingsChanged = ProfileService.SettingConfig.ClientVersion != val.Header.MinorVersion
                                           || ProfileService.SettingConfig.LocaleID != val.Header.LocaleID
                                           || ProfileService.SettingConfig.nClientLoc != val.Header.Unk2;
                    ProfileService.SettingConfig.ClientVersion = val.Header.MinorVersion;
                    ProfileService.SettingConfig.LocaleID = val.Header.LocaleID;
                    ProfileService.SettingConfig.nClientLoc = val.Header.Unk2;
                    if (settingsChanged)
                    {
                        ProfileService.SaveSettings();
                    }

                    if (!ProfileService.SettingConfig.Console)
                    {
                        ShowWindow(consoleHandle, SW_HIDE);
                        isVisible = false;
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

        /// <summary>
        /// 把设置里的日志级别/包跟踪应用到日志器（设置变更后可再次调用）。
        /// </summary>
        /// <summary>
        /// --kartrules &lt;item.kml&gt; [卡丁车 id]：解析客户端词条并打印结果，用于人工核对。
        /// </summary>
        private static int RunKartRules(string itemKmlPath, string kartIdText)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }

            Log.Init(Path.Combine(FileName.appDir, "Logs"), LogLevel.Info, packetTrace: false, announce: false);
            if (!File.Exists(itemKmlPath))
            {
                Console.WriteLine("[KartRules] 找不到文件: " + itemKmlPath);
                return 2;
            }

            ClientItemRules.Load(File.ReadAllBytes(itemKmlPath));
            ClientItemRules.LogSummary();

            if (int.TryParse(kartIdText, out int kartId) && ClientItemRules.TryGet(kartId, out var rule))
            {
                Console.WriteLine($"--- 卡丁车 {kartId} ---");
                Console.WriteLine("开局: " + string.Join(",", rule.StartItems));
                foreach (var r in rule.Convert)
                {
                    Console.WriteLine($"变更: {r.From} → {r.To} ({r.Probability}%)");
                }
                foreach (var r in rule.OnUse)
                {
                    Console.WriteLine($"使用后: {r.From} → {r.To} ({r.Probability}%)");
                }
                foreach (var r in rule.OnAttacked)
                {
                    Console.WriteLine($"被攻击: {r.From} → {r.To} ({r.Probability}%)");
                }
            }

            Log.Shutdown();
            return 0;
        }

        /// <summary>
        /// --tracetest：造几个假包走一遍"记录 + 控制台输出"链路，用于确认包输出功能本身是通的。
        /// </summary>
        private static int RunTraceTest()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }

            Console.WriteLine("=== 包输出链路自检（--tracetest）===");
            Log.Init(Path.Combine(FileName.appDir, "Logs"), LogLevel.Info, packetTrace: true);
            Log.ConfigurePacketTrace(liveTrace: true, filter: "", sample: 1, toConsole: true, ringEnabled: true, maxPerSecond: 100);

            byte[] payload = new byte[32];
            for (int i = 0; i < 5; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(PacketHash.PqServerTime), 0, payload, 0, 4);
                payload[4] = (byte)i;
                Log.Packet("C", "测试玩家", payload, payload.Length);
            }

            List<string> recent = PacketRing.Snapshot(5);
            Console.WriteLine($"--- 环形缓冲回看（{recent.Count} 条）---");
            foreach (string line in recent)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("=== 上面应有 5 条 [C] 输出 + 5 条环形缓冲记录，否则链路有问题 ===");
            Log.Shutdown();
            return 0;
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

        public static void SetAdaptiveConsoleEncoding()
        {
            try
            {
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                targetEncoding = Encoding.UTF8;

                if (isWindows)
                {
                    try
                    {
                        string codePageRegPath = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Nls\\CodePage";
                        object oemcpObj = Registry.GetValue(codePageRegPath, "OEMCP", null);
                        if (oemcpObj != null && int.TryParse(oemcpObj.ToString(), out int oemcp))
                        {
                            try
                            {
                                targetEncoding = Encoding.GetEncoding(oemcp);
                            }
                            catch (ArgumentException)
                            {
                                targetEncoding = Encoding.UTF8;
                            }
                        }
                        else
                        {
                            targetEncoding = Encoding.UTF8;
                        }
                    }
                    catch
                    {
                        targetEncoding = Encoding.UTF8;
                    }
                }

                Console.OutputEncoding = targetEncoding;
                Console.InputEncoding = targetEncoding;
                Console.WriteLine($"已适配编码: {targetEncoding.EncodingName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编码设置失败，使用默认编码: {ex.Message}");
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
    }
}
