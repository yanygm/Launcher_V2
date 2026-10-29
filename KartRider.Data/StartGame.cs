using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using KartRider.Common.Data;
using KartRider.IO.Packet;
using Profile;

namespace KartRider;

class StartGame
{
    // PIN 文件路径
    private string _pinFile;
    private string _pinFileBak;
    private string _kartRiderDirectory;

    public void Start(string kartRiderDirectory, string pinFile, string pinFileBak)
    {
        this._pinFile = pinFile;
        this._pinFileBak = pinFileBak;
        this._kartRiderDirectory = kartRiderDirectory;

        DataPacket packet = new DataPacket
        {
            Nickname = ProfileService.SettingConfig.Name,
            ClientVersion = ProfileService.SettingConfig.ClientVersion,
            CompileTime = CompileTime.Time,
        };

        try
        {
            RestorePinFile();
            File.Copy(pinFile, pinFileBak, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"备份 PIN 文件失败: {ex.Message}");
            return;
        }

        PINFile val;
        try
        {
            val = new PINFile(pinFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取 PIN 文件失败: {ex.Message}");
            return;
        }

        if (val.AuthMethods != null)
        {
            var ip = LanIpGetter.IsIPv6(ProfileService.SettingConfig.ServerIP) ? "127.0.0.1" : ProfileService.SettingConfig.ServerIP;
            foreach (PINFile.AuthMethod authMethod in val.AuthMethods)
            {
                authMethod.LoginServers?.Clear();
                authMethod.LoginServers?.Add(new PINFile.IPEndPoint
                {
                    IP = ip,
                    Port = ProfileService.SettingConfig.ServerPort
                });
            }
        }

        if (!ProfileService.SettingConfig.NgsOn && val.BmlObjects != null)
        {
            foreach (BmlObject bml in val.BmlObjects)
            {
                if (bml.Name == "extra" && bml.SubObjects != null)
                {
                    for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                    {
                        if (bml.SubObjects[i].Item1 == "NgsOn")
                        {
                            Console.WriteLine("Removing {0}", bml.SubObjects[i].Item1);
                            bml.SubObjects.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        try
        {
            File.WriteAllBytes(pinFile, val.GetEncryptedData());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"写入 PIN 文件失败: {ex.Message}");
            return;
        }

        Process process = null;
        try
        {
            // 1. 启动目标进程
            string passport = Base64Helper.Encode(JsonHelper.Serialize(packet));
            ProcessStartInfo startInfo = new ProcessStartInfo("KartRider.exe", $"TGC -region:3 -passport:{passport}")
            {
                WorkingDirectory = Path.GetFullPath(kartRiderDirectory),
                UseShellExecute = true
            };

            process = Process.Start(startInfo);
            Console.WriteLine($"进程已启动, ID: {process.Id}");

            // 保存进程 ID，避免后续访问已释放的 Process 对象
            int processId = process.Id;

            // 2. 等待进程初始化
            Thread.Sleep(2000);

            // 3. 启动后台线程持续检测 TCP 连接
            string serverIP = LanIpGetter.IsIPv6(ProfileService.SettingConfig.ServerIP) ? "127.0.0.1" : ProfileService.SettingConfig.ServerIP;
            int serverPort = ProfileService.SettingConfig.ServerPort;
            bool pinRestored = false;
            bool connectionEstablished = false;

            Thread detectThread = new Thread(() =>
            {
                int checkCount = 0;
                while (!pinRestored && checkCount < 30) // 最多检测30秒
                {
                    if (CheckTcpConnection(processId, serverIP, serverPort))
                    {
                        if (!connectionEstablished)
                        {
                            connectionEstablished = true;
                        }

                        // 连接成功，恢复 PIN 文件
                        if (RestorePinFile())
                        {
                            pinRestored = true;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        if (connectionEstablished)
                        {
                            // 之前连接过，现在断开了
                            connectionEstablished = false;
                        }
                    }

                    Thread.Sleep(1000);
                    checkCount++;
                }

                if (!pinRestored)
                {
                    Console.WriteLine("[TCP检测] 超过30秒未检测到连接，停止检测");
                }
            })
            {
                IsBackground = true,
                Name = "TcpDetectThread"
            };
            detectThread.Start();

            // 等待检测线程执行完毕
            detectThread.Join();
            Console.WriteLine("[TCP检测] 检测线程已结束");

            // 释放进程资源
            process?.Dispose();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.WriteLine($"UAC取消或权限不足: {ex.Message}");
            process?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"操作失败: {ex.Message}");
            process?.Dispose();
        }
    }

    /// <summary>
    /// 恢复备份的 PIN 文件
    /// </summary>
    /// <returns>是否恢复成功</returns>
    private bool RestorePinFile()
    {
        try
        {
            if (string.IsNullOrEmpty(_pinFileBak) || !File.Exists(_pinFileBak))
            {
                Console.WriteLine("[PIN] 备份文件不存在，无法恢复");
                return false;
            }

            if (File.Exists(_pinFile))
                File.Delete(_pinFile);

            File.Move(_pinFileBak, _pinFile);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PIN] 恢复 PIN 文件失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检测进程是否连接到指定 TCP 服务器
    /// </summary>
    /// <param name="processId">目标进程ID</param>
    /// <param name="serverIP">服务器IP</param>
    /// <param name="serverPort">服务器端口</param>
    /// <returns>是否已建立连接</returns>
    private bool CheckTcpConnection(int processId, string serverIP, int serverPort)
    {
        try
        {
            // 使用 netstat 命令查找该进程的 TCP 连接
            ProcessStartInfo psi = new ProcessStartInfo("netstat", $"-ano | findstr \"{processId}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process netstatProcess = Process.Start(psi))
            {
                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit();

                // 解析输出，查找到目标服务器的 ESTABLISHED 连接
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    // 格式: TCP    192.168.1.100:12345    127.0.0.1:8080    ESTABLISHED    12345
                    if (line.Contains("ESTABLISHED") && line.Contains($"{serverIP}:{serverPort}"))
                    {
                        return true;
                    }
                }
            }

            // 尝试更精确的查询方式
            psi = new ProcessStartInfo("netstat", "-ano")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process netstatProcess = Process.Start(psi))
            {
                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit();

                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.Contains($" {processId}") && 
                        line.Contains("ESTABLISHED") && 
                        line.Contains($"{serverIP}:{serverPort}"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP检测] 检测失败: {ex.Message}");
            return false;
        }
    }
}
