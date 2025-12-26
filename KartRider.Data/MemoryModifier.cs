using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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

    public void LaunchAndModifyMemory(string kartRiderDirectory)
    {
        if (File.Exists(Path.Combine(kartRiderDirectory, "25登录器.exe")))
        {
            Process process25 = null;
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("25登录器.exe")
                {
                    WorkingDirectory = Path.GetFullPath(kartRiderDirectory),
                    UseShellExecute = true,
                    Verb = "runas" // 请求管理员权限（内存修改可能需要）
                };

                process25 = Process.Start(startInfo);

                Thread.Sleep(500);

                ModifySpecificMemory(process25.Id, new IntPtr(0x004A1896), new byte[] { 0x31, 0x35, 0x38, 0x2E, 0x32, 0x34, 0x37, 0x2E, 0x32, 0x32, 0x30, 0x2E, 0x38, 0x37, 0x00 });
                ModifySpecificMemory(process25.Id, new IntPtr(0x004A19DB), new byte[] { 0x61, 0x64, 0x64, 0x72, 0x3D, 0x22, 0x31, 0x32, 0x37, 0x2E, 0x30, 0x2E, 0x30, 0x2E, 0x31, 0x3A, 0x33, 0x39, 0x33, 0x31, 0x32, 0x22, 0x2F, 0x3E });
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"UAC取消或权限不足: {ex.Message}");
            }
            finally
            {
                process25?.Dispose(); // 释放进程资源（不影响目标进程运行）
            }
        }
        else
        {
            DataPacket packet = new DataPacket
            {
                Nickname = ProfileService.SettingConfig.Name,
                TimeTicks = MultyPlayer.GetUpTime()
            };

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

                // 2. 等待进程初始化（根据实际情况调整等待时间，确保进程加载完成）
                Thread.Sleep(1000); // 等待1秒（可根据需要延长）

                // 3. 查找并修改内存

                // 修改指定位置的内存值
                // 地址009C610E改为byte 120
                ModifySpecificMemory(process.Id, new IntPtr(0x009C610E), (byte)120);
                // 地址011F1C64改为单浮点10000
                ModifySpecificMemory(process.Id, new IntPtr(0x011F1C64), 10000f);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"UAC取消或权限不足: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"操作失败: {ex.Message}");
            }
            finally
            {
                process?.Dispose(); // 释放进程资源（不影响目标进程运行）
            }
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
        {
            Console.WriteLine("查找和替换的字节长度必须一致");
            return false;
        }

        IntPtr hProcess = OpenProcess(PROCESS_ACCESS_FLAGS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("无法打开进程, 可能权限不足");
            return false;
        }

        try
        {
            IntPtr address = IntPtr.Zero;
            bool modified = false; // 跟踪是否至少修改了一个匹配

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
                        // 在当前页中搜索所有特征码匹配
                        int index = 0;
                        while (index < bytesRead - searchBytes.Length + 1)
                        {
                            index = FindBytes(buffer, searchBytes, index);
                            if (index == -1)
                                break;

                            // 计算实际内存地址
                            IntPtr targetAddress = IntPtr.Add(mbi.BaseAddress, index);
                            Console.WriteLine($"找到特征码, 地址: 0x{targetAddress:X}");

                            // 修改内存
                            if (WriteProcessMemory(hProcess, targetAddress, replaceBytes, replaceBytes.Length, out int bytesWritten) && bytesWritten == replaceBytes.Length)
                            {
                                modified = true;
                            }
                            else
                            {
                                Console.WriteLine("写入内存失败, 可能没有写入权限");
                            }

                            // 移动到下一个可能的匹配位置（避免重叠匹配）
                            index += searchBytes.Length;
                        }
                    }
                }

                // 移动到下一个内存页
                address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);
            }

            return modified; // 返回是否至少修改了一个匹配
        }
        finally
        {
            CloseHandle(hProcess); // 释放进程句柄
        }
    }

    /// <summary>
    /// 在字节数组中从指定位置开始查找目标序列
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
