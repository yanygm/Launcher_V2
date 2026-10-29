using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KartLibrary.File.Lpk
{
    /// <summary>
    /// 单个虚拟文件（对应一个 LPK 记录）。
    /// </summary>
    public class LpkFileInfo
    {
        public string FileName { get; set; }
        public string FullName { get; set; }

        internal Func<byte[]> DataLoader { get; set; }

        public byte[] GetData()
        {
            if (DataLoader == null)
                return null;
            return DataLoader();
        }
    }

    /// <summary>
    /// 虚拟文件夹（由 LPK 记录路径构建的目录树）。
    /// </summary>
    public class LpkFolderInfo
    {
        public string FolderName { get; set; }
        public string FullName { get; set; }

        internal Dictionary<string, LpkFolderInfo> SubFolders { get; } = new Dictionary<string, LpkFolderInfo>(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, LpkFileInfo> Files { get; } = new Dictionary<string, LpkFileInfo>(StringComparer.OrdinalIgnoreCase);

        public LpkFileInfo[] GetFilesInfo() => Files.Values.ToArray();
        public LpkFolderInfo[] GetFoldersInfo() => SubFolders.Values.ToArray();
    }

    /// <summary>
    /// 读取一个或多个 LPK v4 分包，构建虚拟目录树，并按 Rho.cs 所需的接口提供文件访问。
    /// 与 KartLibrary.File.PackFolderManager 接口兼容（OpenDataFolder / GetRootFolder / Reset），
    /// 但数据来源改为 lpk-unpacker。
    /// </summary>
    public class LpkFolderManager
    {
        private readonly LpkFolderInfo _root = new LpkFolderInfo { FolderName = "", FullName = "" };
        private readonly List<string> _allPaths = new List<string>();

        /// <summary>
        /// 打开一个 .lpk 文件，或一个包含 .lpk 文件的目录（自动扫描目录下所有 .lpk）。
        /// </summary>
        public void OpenDataFolder(string input)
        {
            string[] lpkFiles;
            if (System.IO.File.Exists(input) && input.EndsWith(".lpk", StringComparison.OrdinalIgnoreCase))
            {
                lpkFiles = new[] { input };
            }
            else if (System.IO.Directory.Exists(input))
            {
                lpkFiles = System.IO.Directory.GetFiles(input, "*.lpk", SearchOption.TopDirectoryOnly);
            }
            else
            {
                string dir = System.IO.Path.GetDirectoryName(input);
                if (string.IsNullOrEmpty(dir))
                    dir = ".";
                if (!System.IO.Directory.Exists(dir))
                    throw new LpkException("找不到 .lpk 文件所在目录: " + dir);
                lpkFiles = System.IO.Directory.GetFiles(dir, "*.lpk", SearchOption.TopDirectoryOnly);
            }

            if (lpkFiles.Length == 0)
                throw new LpkException("未找到任何 .lpk 文件");

            Array.Sort(lpkFiles, StringComparer.OrdinalIgnoreCase);

            foreach (string lpk in lpkFiles)
            {
                // mixing 是整包根密钥，官方包把它以 head + mid 的形式存在包内；
                // 每个分包单独反推，才能同时打开不同 mixing 的包。
                string mixing = LpkV4.ReadMixing(lpk);
                var (digits, nonce) = LpkV4.Parameters(mixing);

                using var fs = new FileStream(lpk, FileMode.Open, FileAccess.Read, FileShare.Read);
                LpkIndex index = LpkV4.Decode(fs, Path.GetFileName(lpk), mixing);
                foreach (var rec in index.Records)
                {
                    AddFile(rec.Path, lpk, rec, index.DataBegin, digits, nonce);
                }
            }
        }

        private void AddFile(string path, string lpkPath, LpkRecord rec, long dataBegin, int[] digits, byte[] nonce)
        {
            path = Normalize(path);
            string[] parts = path.Split('/');
            var cur = _root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string part = parts[i];
                if (!cur.SubFolders.TryGetValue(part, out var sub))
                {
                    sub = new LpkFolderInfo
                    {
                        FolderName = part,
                        FullName = cur.FullName == "" ? part : cur.FullName + "/" + part
                    };
                    cur.SubFolders[part] = sub;
                }
                cur = sub;
            }

            string fileName = parts[parts.Length - 1];
            if (cur.Files.ContainsKey(fileName))
                return; // 已存在则保留首个（不同分包出现相同路径时）

            var fi = new LpkFileInfo { FileName = fileName, FullName = path };
            string lp = lpkPath;
            LpkRecord r = rec;
            long db = dataBegin;
            fi.DataLoader = () =>
            {
                using var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    return LpkV4.Extract(fs, r, db, digits, nonce);
                }
                catch (LpkException)
                {
                    throw;
                }
            };
            cur.Files[fileName] = fi;
            _allPaths.Add(path);
        }

        public LpkFolderInfo GetRootFolder() => _root;

        public void Reset()
        {
            // 数据按需从磁盘读取，无需要释放的持久句柄。
        }

        /// <summary>
        /// 依据 LPK 内记录路径推断区服代码（kr / cn / tw），默认 cn。
        /// </summary>
        public string GetRegionCode()
        {
            foreach (var p in _allPaths)
            {
                var low = p.ToLowerInvariant();
                if (low.StartsWith("zeta/kr/")) return "kr";
                if (low.StartsWith("zeta/cn/")) return "cn";
                if (low.StartsWith("zeta/tw/")) return "tw";
            }
            return "cn";
        }

        private static string Normalize(string p)
        {
            p = p.Replace('\\', '/');
            while (p.StartsWith("/"))
                p = p.Substring(1);
            return p;
        }
    }
}
