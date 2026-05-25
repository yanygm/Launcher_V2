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

class MemoryModifier
{
    // 导入Windows API（内存操作所需）
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out int lpNumberOfBytesRead
    );

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int nSize,
        out int lpNumberOfBytesWritten
    );

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern IntPtr VirtualQueryEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer,
        uint dwLength
    );

    // 内存区域信息结构体（用于枚举内存页）
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    // 进程内存操作权限（读取+写入+查询内存信息）
    private const uint PROCESS_ACCESS_FLAGS = 0x0010 | 0x0020 | 0x0008; // PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION

    // PIN 文件路径
    private string _pinFile;
    private string _pinFileBak;
    private string _kartRiderDirectory;

    public void LaunchAndModifyMemory(string kartRiderDirectory, string pinFile, string pinFileBak)
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
                UseShellExecute = true,
                Verb = "runas" // 请求管理员权限（内存修改可能需要）
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
                while (!pinRestored && checkCount < 120) // 最多检测2分钟 (120 * 1秒)
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

                            // 修改内存
                            // 星标赛道数量50改为120
                            ModifyMemory(processId, new byte[] { 0x64, 0x00, 0x00, 0x00, 0xF7, 0xF9, 0x83, 0xFA, 0x32 },
                            new byte[] { 0x64, 0x00, 0x00, 0x00, 0xF7, 0xF9, 0x83, 0xFA, 0x78 });
                            // 赛道模型边界大小2000改为10000单浮点
                            ModifyMemory(processId, new byte[] { 0x00, 0x00, 0x98, 0x41, 0x00, 0x00, 0xFA, 0x44 },
                            new byte[] { 0x00, 0x00, 0x98, 0x41, 0x00, 0x40, 0x1C, 0x46 });
                            // 修改贴图 1 2 4 8 16 32 64 128 256 512 1024 的限制
                            ModifyMemory(processId, new byte[] { 0x8B, 0x44, 0x24, 0x04, 0x83, 0xF8, 0x01, 0x74, 0x3D, 0x83, 0xF8, 0x02, 0x74, 0x38, 0x83, 0xF8, 0x04, 0x74, 0x33, 0x83, 0xF8, 0x08, 0x74, 0x2E, 0x83, 0xF8, 0x10, 0x74, 0x29, 0x83, 0xF8, 0x20, 0x74, 0x24, 0x83, 0xF8, 0x40, 0x74, 0x1F },
                            new byte[] { 0xB0, 0x01, 0xC3, 0x90 });
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
                    Console.WriteLine("[TCP检测] 超过2分钟未检测到连接，停止检测");
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
    /// 处理 PIN 文件：备份、修改服务器地址、写入
    /// </summary>
    /// <returns>是否处理成功</returns>
    private bool ModifyPinFile()
    {
        try
        {
            // 检查 PIN 文件是否存在
            if (string.IsNullOrEmpty(_pinFile) || !File.Exists(_pinFile))
            {
                Console.WriteLine("[PIN] PIN 文件不存在，尝试从备份恢复...");
                
                if (!string.IsNullOrEmpty(_pinFileBak) && File.Exists(_pinFileBak))
                {
                    File.Copy(_pinFileBak, _pinFile, overwrite: true);
                    Console.WriteLine("[PIN] 已从备份恢复 PIN 文件");
                }
                else
                {
                    Console.WriteLine("[PIN] 备份文件也不存在，无法恢复");
                    return false;
                }
            }

            // 备份当前 PIN 文件
            if (!string.IsNullOrEmpty(_pinFileBak))
            {
                Console.WriteLine($"[PIN] 备份 PIN 文件到 {_pinFileBak}");
                File.Copy(_pinFile, _pinFileBak, overwrite: true);
            }

            // 读取 PIN 文件
            PINFile pinFileObj = new PINFile(_pinFile);

            // 修改服务器地址
            if (pinFileObj.AuthMethods != null)
            {
                foreach (PINFile.AuthMethod authMethod in pinFileObj.AuthMethods)
                {
                    Console.WriteLine("[PIN] 修改服务器地址: {0}", authMethod.Name);
                    authMethod.LoginServers?.Clear();
                    authMethod.LoginServers?.Add(new PINFile.IPEndPoint
                    {
                        IP = ProfileService.SettingConfig.ServerIP,
                        Port = ProfileService.SettingConfig.ServerPort
                    });
                }
            }

            // 处理 NGS 设置
            if (!ProfileService.SettingConfig.NgsOn && pinFileObj.BmlObjects != null)
            {
                foreach (BmlObject bml in pinFileObj.BmlObjects)
                {
                    if (bml.Name == "extra" && bml.SubObjects != null)
                    {
                        for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                        {
                            if (bml.SubObjects[i].Item1 == "NgsOn")
                            {
                                Console.WriteLine("[PIN] 移除 NgsOn 配置");
                                bml.SubObjects.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }

            // 写入修改后的 PIN 文件
            File.WriteAllBytes(_pinFile, pinFileObj.GetEncryptedData());
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PIN] 处理 PIN 文件失败: {ex.Message}");
            return false;
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
    /// 等待 TCP 连接建立（带超时）
    /// </summary>
    private bool WaitForTcpConnection(int processId, string serverIP, int serverPort, int timeoutMs)
    {
        int elapsed = 0;
        int interval = 1000; // 每秒检测一次

        while (elapsed < timeoutMs)
        {
            if (CheckTcpConnection(processId, serverIP, serverPort))
            {
                return true;
            }
            Thread.Sleep(interval);
            elapsed += interval;
        }

        return false;
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

    /// <summary>
    /// 在目标进程中查找特征码并修改所有匹配项
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <param name="searchBytes">要查找的字节序列</param>
    /// <param name="replaceBytes">要替换的字节序列</param>
    /// <returns>修改成功的数量</returns>
    private int ModifyMemory(int processId, byte[] searchBytes, byte[] replaceBytes)
    {
        // 如果替换字节长度小于搜索字节，用 0x90 (NOP) 补充
        if (replaceBytes.Length < searchBytes.Length)
        {
            byte[] paddedReplace = new byte[searchBytes.Length];
            Array.Copy(replaceBytes, paddedReplace, replaceBytes.Length);
            for (int i = replaceBytes.Length; i < searchBytes.Length; i++)
            {
                paddedReplace[i] = 0x90; // NOP 指令
            }
            replaceBytes = paddedReplace;
        }
        else if (replaceBytes.Length > searchBytes.Length)
        {
            return 0;
        }

        IntPtr hProcess = OpenProcess(PROCESS_ACCESS_FLAGS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("无法打开进程, 可能权限不足");
            return 0;
        }

        int modifiedCount = 0;
        int scanCount = 0;
        const int maxScanCount = 10000; // 最大扫描页数限制，防止异常
        try
        {
            IntPtr address = IntPtr.Zero;
            long maxAddress = 0x7FFFFFFF;

            while (address.ToInt64() < maxAddress && scanCount < maxScanCount)
            {
                scanCount++;

                // 枚举进程内存页
                int queryResult = VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION mbi, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()).ToInt32();
                if (queryResult == 0)
                    break;

                // 安全检查：确保 BaseAddress 有效
                if (mbi.BaseAddress == IntPtr.Zero && address != IntPtr.Zero)
                {
                    break;
                }

                // 同步 address 与 BaseAddress
                if (mbi.BaseAddress != address)
                {
                    address = mbi.BaseAddress;
                }

                long regionSize = mbi.RegionSize.ToInt64();
                if (regionSize <= 0)
                {
                    address = IntPtr.Add(address, 0x1000);
                    continue;
                }

                // 只处理可读写的私有内存页
                if (mbi.State == 0x1000 &&
                    (mbi.Protect == 0x04 || mbi.Protect == 0x08 || mbi.Protect == 0x10 ||
                     mbi.Protect == 0x80 || mbi.Protect == 0x40))
                {
                    // 限制单页读取大小，防止过大内存分配
                    int readSize = (int)Math.Min(regionSize, 64 * 1024 * 1024); // 最大64MB
                    byte[] buffer = new byte[readSize];

                    if (ReadProcessMemory(hProcess, mbi.BaseAddress, buffer, readSize, out int bytesRead) && bytesRead > 0)
                    {
                        int searchStart = 0;
                        int foundIndex;

                        while ((foundIndex = FindBytes(buffer, searchBytes, searchStart)) != -1)
                        {
                            IntPtr targetAddress = IntPtr.Add(mbi.BaseAddress, foundIndex);
                            Console.WriteLine($"找到特征码, 地址: 0x{targetAddress:X}");

                            if (WriteProcessMemory(hProcess, targetAddress, replaceBytes, replaceBytes.Length, out int bytesWritten) && bytesWritten == replaceBytes.Length)
                            {
                                modifiedCount++;
                            }
                            else
                            {
                                Console.WriteLine("写入内存失败");
                            }

                            // 更新buffer
                            for (int k = 0; k < replaceBytes.Length && foundIndex + k < buffer.Length; k++)
                            {
                                buffer[foundIndex + k] = replaceBytes[k];
                            }
                            searchStart = foundIndex + searchBytes.Length;
                        }
                    }
                }

                // 移动到下一个内存页
                address = IntPtr.Add(mbi.BaseAddress, (int)regionSize);
            }

            Console.WriteLine($"共修改了 {modifiedCount} 处特征码 (扫描了 {scanCount} 页)");
            return modifiedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"修改内存异常: {ex.Message}");
            return modifiedCount;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    /// <summary>
    /// 在字节数组中查找目标序列（从指定位置开始）
    /// </summary>
    private int FindBytes(byte[] buffer, byte[] searchBytes, int startIndex = 0)
    {
        for (int i = startIndex; i <= buffer.Length - searchBytes.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < searchBytes.Length; j++)
            {
                if (buffer[i + j] != searchBytes[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 修改目标进程中指定位置的内存值
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <param name="address">内存地址</param>
    /// <param name="newValue">要写入的新值</param>
    /// <returns>是否修改成功</returns>
    public bool ModifySpecificMemory(int processId, IntPtr address, object newValue)
    {
        byte[] bytesToWrite;

        // 根据newValue的类型将其转换为字节数组
        if (newValue is byte)
        {
            bytesToWrite = new byte[] { (byte)newValue };
        }
        else if (newValue is short)
        {
            bytesToWrite = BitConverter.GetBytes((short)newValue);
        }
        else if (newValue is int)
        {
            bytesToWrite = BitConverter.GetBytes((int)newValue);
        }
        else if (newValue is long)
        {
            bytesToWrite = BitConverter.GetBytes((long)newValue);
        }
        else if (newValue is float)
        {
            bytesToWrite = BitConverter.GetBytes((float)newValue);
        }
        else if (newValue is double)
        {
            bytesToWrite = BitConverter.GetBytes((double)newValue);
        }
        else if (newValue is byte[])
        {
            bytesToWrite = (byte[])newValue;
        }
        else
        {
            Console.WriteLine("不支持的数据类型");
            return false;
        }

        // 打开目标进程
        IntPtr hProcess = OpenProcess(PROCESS_ACCESS_FLAGS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("无法打开进程, 可能权限不足");
            return false;
        }

        try
        {
            // 写入内存
            if (WriteProcessMemory(hProcess, address, bytesToWrite, bytesToWrite.Length, out int bytesWritten) && bytesWritten == bytesToWrite.Length)
            {
                Console.WriteLine($"成功修改地址 0x{address:X} 的内存值为 {newValue}");
                return true;
            }
            else
            {
                Console.WriteLine($"写入内存地址 0x{address:X} 失败, 可能没有写入权限");
                return false;
            }
        }
        finally
        {
            CloseHandle(hProcess); // 释放进程句柄
        }
    }
}