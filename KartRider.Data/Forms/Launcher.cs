using ExcData;
using KartRider.Common.Data;
using KartRider.Common.Utilities;
using Launcher.Properties;
using LoggerLibrary;
using Profile;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace KartRider
{
    public partial class Launcher : Form
    {
        public static bool GetKart = true;
        public string kartRiderDirectory;
        public static string KartRider;
        public static string pinFile;
        public static string pinFileBak;

        public Launcher()
        {
            this.InitializeComponent();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(pinFileBak))
            {
                File.Delete(pinFile);
                File.Move(pinFileBak, pinFile);
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ProfileService.LoadSettings();
            PINFile val = new PINFile(pinFile);
            ProfileService.SettingConfig.ClientVersion = val.Header.MinorVersion;
            ProfileService.SettingConfig.LocaleID = val.Header.LocaleID;
            ProfileService.SettingConfig.nClientLoc = val.Header.Unk2;
            ProfileService.SaveSettings();
            ClientVersion.Text = val.Header.MinorVersion.ToString();
            Console.WriteLine($"ClientVersion: {val.Header.MinorVersion}");
            Console.WriteLine($"程序编译时间: {CompileTime.Time}");
            VersionLabel.Text = CompileTime.Time;
            Console.WriteLine("Process: {0}", KartRider);
            Load_Data();
            try
            {
                RouterListener.Start();
            }
            catch (Exception ex)
            {
                if (ex is System.Net.Sockets.SocketException)
                {
                    LauncherSystem.MessageBoxType2();
                }
            }
        }

        private void Start_Button_Click(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("KartRider").Length != 0)
            {
                LauncherSystem.MessageBoxType1();
            }
            else
            {
                (new Thread(() =>
                {
                    if (File.Exists(pinFileBak))
                    {
                        File.Delete(pinFile);
                        File.Move(pinFileBak, pinFile);
                    }
                    Console.WriteLine("Backing up old PinFile...");
                    Console.WriteLine(pinFile);
                    File.Copy(pinFile, pinFileBak);
                    PINFile val = new PINFile(pinFile);
                    foreach (PINFile.AuthMethod authMethod in val.AuthMethods)
                    {
                        Console.WriteLine("Changing IP Addr to local... {0}", authMethod.Name);
                        authMethod.LoginServers.Clear();
                        authMethod.LoginServers.Add(new PINFile.IPEndPoint
                        {
                            IP = ProfileService.SettingConfig.ServerIP,
                            Port = ProfileService.SettingConfig.ServerPort
                        });
                    }
                    if (!ProfileService.SettingConfig.NgsOn)
                    {
                        foreach (BmlObject bml in val.BmlObjects)
                        {
                            if (bml.Name == "extra")
                            {
                                for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                                {
                                    Console.WriteLine("Removing {0}", bml.SubObjects[i].Item1);
                                    if (bml.SubObjects[i].Item1 == "NgsOn")
                                    {
                                        bml.SubObjects.RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    File.WriteAllBytes(pinFile, val.GetEncryptedData());
                    var modifier = new MemoryModifier();
                    modifier.LaunchAndModifyMemory(kartRiderDirectory);
                })).Start();
            }
        }

        private void GetKart_Button_Click(object sender, EventArgs e)
        {
            if (Launcher.GetKart)
            {
                Program.GetKartDlg = new GetKart();
                Program.GetKartDlg.ShowDialog();
            }
        }

        private void Setting_Button_Click(object sender, EventArgs e)
        {
            Program.SettingDlg = new Setting();
            Program.SettingDlg.ShowDialog();
        }

        public void Load_Data()
        {
            string ModelMax = Resources.ModelMax;
            if (!File.Exists(FileName.ModelMax_LoadFile))
            {
                using (StreamWriter streamWriter = new StreamWriter(FileName.ModelMax_LoadFile, false))
                {
                    streamWriter.Write(ModelMax);
                }
            }
            XmlFileUpdater.XmlUpdater updater = new XmlFileUpdater.XmlUpdater();
            updater.UpdateLocalXmlWithResource(FileName.ModelMax_LoadFile, ModelMax);

            SpecialKartConfig.SaveConfigToFile(FileName.SpecialKartConfig);
            MultyPlayer.kartConfig = SpecialKartConfig.LoadConfigFromFile(FileName.SpecialKartConfig);
        }

        private void GitHub_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/yanygm/Launcher_V2/releases";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void KartInfo_Click(object sender, EventArgs e)
        {
            string url = "https://kartinfo.me/thread-9369-1-1.html";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void label_Client_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/brownsugar/popkart-client-archive/releases";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void button_ToggleTerminal_Click(object sender, EventArgs e)
        {
            Program.isVisible = !Program.isVisible;
            Program.ShowWindow(Program.consoleHandle, Program.isVisible ? Program.SW_SHOW : Program.SW_HIDE);
            using (StreamWriter streamWriter = new StreamWriter(FileName.Load_Console, false))
            {
                streamWriter.Write((Program.isVisible ? "1" : "0"));
            }
        }

        private void ConsoleLogger_Click(object sender, EventArgs e)
        {
            CachedConsoleWriter.SaveToFile();
            CachedConsoleWriter.cachedWriter.ClearCache();
        }
    }
}

