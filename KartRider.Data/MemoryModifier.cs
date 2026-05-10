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
            foreach (PINFile.AuthMethod authMethod in val.AuthMethods)
            {
                Console.WriteLine("Changing IP Addr to local... {0}", authMethod.Name);
                authMethod.LoginServers?.Clear();
                authMethod.LoginServers?.Add(new PINFile.IPEndPoint
                {
                    IP = ProfileService.SettingConfig.ServerIP,
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

            // 3. 立即修改内存（无需等待连接）
            // 星标赛道数量50改为120
            ModifyMemory(processId, new byte[] { 0x83, 0xFA, 0x32 }, new byte[] { 0x83, 0xFA, 0x78 });
            // 赛道模型边界大小2000改为10000单浮点
            ModifyMemory(processId, new byte[] { 0x00, 0x00, 0xFA, 0x44 }, new byte[] { 0x00, 0x40, 0x1C, 0x46 });

            // 4. 启动后台线程持续检测 TCP 连接
            string serverIP = ProfileService.SettingConfig.ServerIP;
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
    /// 在目标进程中查找特征码并修改
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <param name="searchBytes">要查找的字节序列</param>
    /// <param name="replaceBytes">要替换的字节序列</param>
    /// <returns>是否修改成功</returns>
    private bool ModifyMemory(int processId, byte[] searchBytes, byte[] replaceBytes)
    {
        if (searchBytes.Length != replaceBytes.Length)
            Console.WriteLine("查找和替换的字节长度必须一致");

        IntPtr hProcess = OpenProcess(PROCESS_ACCESS_FLAGS, false, processId);
        if (hProcess == IntPtr.Zero)
            Console.WriteLine("无法打开进程, 可能权限不足");

        try
        {
            IntPtr address = IntPtr.Zero;
            while (true)
            {
                // 枚举进程内存页
                if (VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION mbi, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()) == IntPtr.Zero)
                    break;

                // 只处理可读写的私有内存页（避免系统内存或只读内存）
                if (mbi.State == 0x1000 && // MEM_COMMIT（已提交的内存）
                    (mbi.Protect == 0x04 || mbi.Protect == 0x08 || mbi.Protect == 0x10 || // PAGE_READWRITE, PAGE_WRITECOPY, PAGE_EXECUTE_READWRITE
                     mbi.Protect == 0x80 || mbi.Protect == 0x40)) // PAGE_EXECUTE_WRITECOPY, PAGE_READWRITE
                {
                    // 读取当前内存页数据
                    byte[] buffer = new byte[(int)mbi.RegionSize];
                    if (ReadProcessMemory(hProcess, mbi.BaseAddress, buffer, buffer.Length, out int bytesRead) && bytesRead > 0)
                    {
                        // 在当前页中搜索特征码
                        int index = FindBytes(buffer, searchBytes);
                        if (index != -1)
                        {
                            // 计算实际内存地址
                            IntPtr targetAddress = IntPtr.Add(mbi.BaseAddress, index);
                            Console.WriteLine($"找到特征码, 地址: 0x{targetAddress:X}");

                            // 修改内存
                            if (WriteProcessMemory(hProcess, targetAddress, replaceBytes, replaceBytes.Length, out int bytesWritten) && bytesWritten == replaceBytes.Length)
                            {
                                return true;
                            }
                            else
                            {
                                Console.WriteLine("写入内存失败, 可能没有写入权限");
                            }
                        }
                    }
                }

                // 移动到下一个内存页
                address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);
            }

            return false; // 未找到特征码
        }
        finally
        {
            CloseHandle(hProcess); // 释放进程句柄
        }
    }

    /// <summary>
    /// 在字节数组中查找目标序列
    /// </summary>
    private int FindBytes(byte[] buffer, byte[] searchBytes)
    {
        for (int i = 0; i <= buffer.Length - searchBytes.Length; i++)
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
