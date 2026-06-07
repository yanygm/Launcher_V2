using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
using KartRider.IO.Packet;
using Microsoft.Win32;
using Profile;
using LoggerLibrary;
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

            // 创建缓存编写器并替换控制台输出
            CachedConsoleWriter.cachedWriter = new CachedConsoleWriter(originalOut);
            Console.SetOut(CachedConsoleWriter.cachedWriter);

            // 初始化自适应编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SetAdaptiveConsoleEncoding();

            if (args != null && args.Length > 0)
            {
                RhoPacker.PackTool(args);
            }
            else
            {
                ProfileService.LoadSettings();
                // 检查更新
                if (ProfileService.SettingConfig.AutoUpdate)
                {
                    await Update.UpdateDataAsync();
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
                            Console.WriteLine($"启动路由器监听失败: {ex.Message}");
                        }
                    }

                    PatchManager.StartUpdateAsync(RootDirectory).Wait();

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
                Console.WriteLine($"已适配编码: {targetEncoding.EncodingName}");
            }
            catch (Exception ex)
            {
                // 异常时使用系统默认编码作为最后保障
                Console.WriteLine($"编码设置失败，使用默认编码: {ex.Message}");
            }
        }

        public static void Load_Data()
        {
            try
            {
                string localFilePath = FileName.ModelMax_LoadFile;
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 如果本地文件不存在，直接创建
                if (!File.Exists(localFilePath))
                {
                    File.WriteAllText(localFilePath, ModelMax.XmlContent);
                    Console.WriteLine($"ModelMax.xml已创建: {localFilePath}");
                }
                else
                {
                    // 加载本地和资源XML
                    XDocument localXml = XDocument.Load(localFilePath);
                    XDocument resourceXml = XDocument.Parse(ModelMax.XmlContent);
                    
                    int addedCount = 0;
                    
                    // 遍历资源中的所有 kart 节点
                    foreach (var resourceKart in resourceXml.Root!.Elements("kart"))
                    {
                        string? id = resourceKart.Attribute("id")?.Value;
                        string? name = resourceKart.Attribute("name")?.Value;
                        
                        if (id != null)
                        {
                            // 检查本地是否已存在该ID
                            bool exists = localXml.Root!.Elements("kart")
                                .Any(k => k.Attribute("id")?.Value == id);
                            
                            if (!exists)
                            {
                                // 复制 kart 元素（包含所有属性）
                                localXml.Root.Add(new XElement(resourceKart));
                                Console.WriteLine($"已添加 kart: {name ?? id}");
                                addedCount++;
                            }
                        }
                    }
                    
                    if (addedCount > 0)
                    {
                        localXml.Save(localFilePath, SaveOptions.None);
                        Console.WriteLine($"ModelMax.xml已更新，新增 {addedCount} 个kart");
                    }
                    else
                    {
                        Console.WriteLine("ModelMax.xml已是最新，无需更新");
                    }
                }

                SpecialKartConfig.SaveConfigToFile(FileName.SpecialKartConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载数据失败: {ex.Message}");
            }
        }
    }
}