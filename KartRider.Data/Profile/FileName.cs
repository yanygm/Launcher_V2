using ExcData;
using KartRider;
using KartRider.Logging;
using Microsoft.Win32.SafeHandles;
using RiderData;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

namespace Profile
{
    public class fileName
    {
        public string NicknameDir;
        public string config_path;
        public string ItemPresetsConfig;
        public string NewKart_LoadFile;
        public string NewItem_LoadFile;
        public string Favorite_LoadFile;
        public string Locked_LoadFile;
        public string FavoriteTrack_LoadFile;
        public string TuneData_LoadFile;
        public string PlantData_LoadFile;
        public string LevelData_LoadFile;
        public string PartsData_LoadFile;
        public string Parts12Data_LoadFile;
        public string Level12Data_LoadFile;
        public string Competitive_LoadFile;
        public string TrainingMission_LoadFile;
        public string GiveGift_LoadFile;
        public string RewardBox_LoadFile;
        public string Bingo_LoadFile;
    }

    public static class FileName
    {
        public static string appDir = AppDomain.CurrentDomain.BaseDirectory; // 应用程序所在目录
        public static string KartRider = Path.GetFullPath(Path.Combine(appDir, @"KartRider.exe"));
        public static string pinFile = Path.GetFullPath(Path.Combine(appDir, @"KartRider.pin"));
        public static string ProfileDir = Path.GetFullPath(Path.Combine(appDir, @"Profile"));
        public static string Load_Settings = Path.GetFullPath(Path.Combine(ProfileDir, @"Settings.json"));
        public static string Load_TrackRank = Path.GetFullPath(Path.Combine(appDir, @"TrackRank"));
        public static string ModelMax_LoadFile = Path.GetFullPath(Path.Combine(ProfileDir, @"ModelMax.xml"));
        public static string Coupon = Path.GetFullPath(Path.Combine(ProfileDir, @"Coupon.json"));

        /// <summary>
        /// 昵称 → 路径集合。原实现是普通 Dictionary，被多个会话线程并发 Load/Remove/索引，
        /// 会撕裂或抛 InvalidOperationException；改为并发字典后 GetOrAdd 是原子的。
        /// </summary>
        public static ConcurrentDictionary<string, fileName> FileNames { get; } =
            new ConcurrentDictionary<string, fileName>(StringComparer.Ordinal);

        /// <summary>
        /// 确保 Profile 数据目录已创建，并把该目录（含已存在的昵称子目录）设置为「大小写敏感」。
        ///
        /// Windows 默认大小写不敏感，昵称 abc / ABC 会落到同一个 Profile\abc 目录里互相覆盖；
        /// 打开 Win10 1803+ 的目录级开关后，之后新建的子目录/文件会继承该标志。
        /// 进程内只做一次，失败只记日志、不影响启动。
        /// </summary>
        public static string EnsureProfileDir()
        {
            lock (ProfileDirLock)
            {
                if (profileDirPrepared)
                {
                    return ProfileDir;
                }
                try
                {
                    if (!Directory.Exists(ProfileDir))
                    {
                        Directory.CreateDirectory(ProfileDir);
                        Log.Info($"[Profile] 已创建数据目录: {ProfileDir}");
                    }

                    bool enabled = EnableCaseSensitivity(ProfileDir);
                    int subDirs = 0;
                    if (enabled)
                    {
                        // 开关只对「之后新建」的子目录生效，老账号目录要补一遍
                        foreach (string dir in Directory.GetDirectories(ProfileDir))
                        {
                            if (EnableCaseSensitivity(dir)) subDirs++;
                        }
                        if (subDirs > 0)
                        {
                            Log.Info($"[Profile] 已为 {subDirs} 个昵称目录开启大小写敏感");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[Profile] 准备数据目录失败: {ex.Message}");
                }
                profileDirPrepared = true;
                return ProfileDir;
            }
        }

        /// <summary>
        /// 把目录设置为大小写敏感（FILE_CASE_SENSITIVE_INFORMATION）。
        /// 已经是敏感状态时直接返回 true；目录不存在或系统不支持时返回 false。
        /// </summary>
        public static bool EnableCaseSensitivity(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

            SafeFileHandle handle = CreateFileW(
                directory,
                GenericRead | GenericWrite,
                FileShareRead | FileShareWrite | FileShareDelete,
                IntPtr.Zero,
                OpenExisting,
                FileFlagBackupSemantics, // 打开目录必须带这个标志
                IntPtr.Zero);
            if (handle.IsInvalid)
            {
                Log.Warn($"[Profile] 无法打开目录以设置大小写敏感: {directory}（{new Win32Exception(Marshal.GetLastWin32Error()).Message}）");
                return false;
            }

            using (handle)
            {
                uint flags = QueryCaseSensitiveFlags(handle);
                if ((flags & FileCsFlagCaseSensitiveDir) != 0)
                {
                    return true; // 已经是大小写敏感
                }

                uint value = flags | FileCsFlagCaseSensitiveDir;
                int setResult = SetCaseSensitiveFlags(handle, ref value);
                if (setResult == 0)
                {
                    Log.Info($"[Profile] 目录已设为大小写敏感: {directory}");
                    return true;
                }

                // 0xC0000003 STATUS_INVALID_INFO_CLASS：系统不支持（Win10 1803 之前）
                Log.Warn($"[Profile] 设置目录大小写敏感失败 NTSTATUS=0x{setResult:X8}: {directory}");
                return false;
            }
        }

        private static uint QueryCaseSensitiveFlags(SafeFileHandle handle)
        {
            uint flags = 0;
            if (NtQueryInformationFile(handle, out _, ref flags, sizeof(uint), FileCaseSensitiveInformationForceAccessCheck) == 0)
            {
                return flags;
            }
            flags = 0;
            NtQueryInformationFile(handle, out _, ref flags, sizeof(uint), FileCaseSensitiveInformation);
            return flags;
        }

        /// <summary>
        /// 新版 Windows 只接受 71（带访问检查），老版本只认 51，两个都试一遍。
        /// </summary>
        private static int SetCaseSensitiveFlags(SafeFileHandle handle, ref uint value)
        {
            int result = NtSetInformationFile(handle, out _, ref value, sizeof(uint), FileCaseSensitiveInformationForceAccessCheck);
            if (result == 0 || result != StatusInvalidInfoClass)
            {
                return result;
            }
            return NtSetInformationFile(handle, out _, ref value, sizeof(uint), FileCaseSensitiveInformation);
        }

        private static readonly object ProfileDirLock = new object();
        private static bool profileDirPrepared;

        private const int FileCaseSensitiveInformation = 51;                      // FILE_CASE_SENSITIVE_INFORMATION
        private const int FileCaseSensitiveInformationForceAccessCheck = 71;      // 新版 Windows 的写法
        private const int StatusInvalidInfoClass = unchecked((int)0xC0000003);    // STATUS_INVALID_INFO_CLASS
        private const uint FileCsFlagCaseSensitiveDir = 0x1;                      // FILE_CS_FLAG_CASE_SENSITIVE_DIR
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint FileShareDelete = 0x00000004;
        private const uint OpenExisting = 3;
        private const uint FileFlagBackupSemantics = 0x02000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct IoStatusBlock
        {
            public IntPtr Status;
            public IntPtr Information;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationFile(
            SafeFileHandle fileHandle,
            out IoStatusBlock ioStatusBlock,
            ref uint fileInformation,
            uint fileInformationLength,
            int fileInformationClass);

        [DllImport("ntdll.dll")]
        private static extern int NtSetInformationFile(
            SafeFileHandle fileHandle,
            out IoStatusBlock ioStatusBlock,
            ref uint fileInformation,
            uint fileInformationLength,
            int fileInformationClass);

        public static fileName Load(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return null;
            }
            return FileNames.GetOrAdd(nickname, Create);
        }

        private static fileName Create(string nickname)
        {
            var filename = new fileName();
            filename.NicknameDir = Path.GetFullPath(Path.Combine(ProfileDir, nickname));
            filename.config_path = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Launcher.json"));
            filename.ItemPresetsConfig = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"ItemPresetsConfig.json"));
            filename.Favorite_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Favorite.json"));
            filename.Locked_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Locked.json"));
            filename.FavoriteTrack_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"FavoriteTrack.json"));
            filename.NewKart_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"NewKart.json"));
            filename.NewItem_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"NewItem.json"));
            filename.TuneData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"TuneData.json"));
            filename.PlantData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"PlantData.json"));
            filename.LevelData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"LevelData.json"));
            filename.PartsData_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"PartsData.json"));
            filename.Parts12Data_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Parts12Data.json"));
            filename.Level12Data_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Level12Data.json"));
            filename.Competitive_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Competitive.json"));
            filename.TrainingMission_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"TrainingMission.json"));
            filename.GiveGift_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"GiveGift.json"));
            filename.RewardBox_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"RewardBox.json"));
            filename.Bingo_LoadFile = Path.GetFullPath(Path.Combine(filename.NicknameDir, @"Bingo.json"));

            try
            {
                // 先保证 Profile 目录存在且大小写敏感，昵称目录才会继承该标志
                EnsureProfileDir();
                if (!Directory.Exists(filename.NicknameDir))
                {
                    Directory.CreateDirectory(filename.NicknameDir);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FileName] 创建目录失败 {nickname}: {ex.Message}");
            }

            return filename;
        }

        /// <summary>
        /// 清理会话结束后不再需要的昵称缓存（避免静态字典无界增长）。
        /// </summary>
        public static void Forget(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            FileNames.TryRemove(nickname, out _);
        }
    }
}
