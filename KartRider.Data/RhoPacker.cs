using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using KartLibrary.File.Lpk;

namespace KartRider;

/// <summary>
/// 拖放处理工具：
///   拖入 .lpk 文件 → 解包到与该文件同名的目录（DataPack0_00000.lpk → DataPack0_00000\）；
///   拖入目录      → 反向打包为同名 .lpk 文件（DataPack0_00000\ → DataPack0_00000.lpk）。
/// </summary>
internal static class RhoPacker
{
    public static void PackTool(string[] args)
    {
        if (args == null || args.Length == 0)
            return;

        int success = 0;
        int failed = 0;
        foreach (string arg in args)
        {
            try
            {
                if (File.Exists(arg))
                {
                    if (arg.EndsWith(".lpk", StringComparison.OrdinalIgnoreCase))
                    {
                        Unpack(arg);
                        success++;
                    }
                    else
                    {
                        Console.WriteLine($"[跳过] 不是 .lpk 文件：{arg}");
                        failed++;
                    }
                }
                else if (Directory.Exists(arg))
                {
                    Pack(arg);
                    success++;
                }
                else
                {
                    Console.WriteLine($"[跳过] 路径不存在：{arg}");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[失败] {arg}");
                Console.WriteLine($"       {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"全部完成：成功 {success} 项，失败 {failed} 项。");
        WaitForExit();
    }

    #region 解包：.lpk → 目录

    private static void Unpack(string lpkPath)
    {
        string fullPath = Path.GetFullPath(lpkPath);
        string baseDir = Path.GetDirectoryName(fullPath) ?? ".";
        string outputDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(fullPath));

        var manager = new LpkFolderManager();
        manager.OpenDataFolder(fullPath);
        LpkFolderInfo root = manager.GetRootFolder();

        Directory.CreateDirectory(outputDir);
        int count = 0;
        WriteFolder(root, outputDir, ref count);
        manager.Reset();

        Console.WriteLine($"[解包] {Path.GetFileName(fullPath)} → {outputDir}");
        Console.WriteLine($"       共导出 {count} 个文件");
    }

    private static void WriteFolder(LpkFolderInfo folder, string dir, ref int count)
    {
        Directory.CreateDirectory(dir);

        foreach (LpkFileInfo file in folder.GetFilesInfo())
        {
            byte[] data = file.GetData();
            if (data == null)
                continue;
            File.WriteAllBytes(Path.Combine(dir, file.FileName), data);
            count++;
        }

        foreach (LpkFolderInfo sub in folder.GetFoldersInfo())
            WriteFolder(sub, Path.Combine(dir, sub.FolderName), ref count);
    }

    #endregion

    #region 打包：目录 → .lpk

    private static void Pack(string directory)
    {
        string fullDir = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string dirName = Path.GetFileName(fullDir);
        if (string.IsNullOrEmpty(dirName))
            throw new IOException("无法确定目录名称：" + directory);

        string outputPath = Path.Combine(Path.GetDirectoryName(fullDir) ?? ".", dirName + ".lpk");

        var entries = new List<LpkV4.LpkEntry>();
        CollectEntries(fullDir, "", entries);
        if (entries.Count == 0)
            throw new IOException("目录中没有可打包的文件：" + fullDir);

        // 固定顺序，保证同一目录反复打包结果一致
        entries.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

        // 完全按目录内容重新打包，不读取同目录下的旧包
        LpkV4.Encode(outputPath, entries);
        Console.WriteLine($"[打包] {fullDir} → {outputPath}");
        Console.WriteLine($"       共写入 {entries.Count} 个文件");
    }

    private static void CollectEntries(string rootDir, string relative, List<LpkV4.LpkEntry> entries)
    {
        string current = string.IsNullOrEmpty(relative)
            ? rootDir
            : Path.Combine(rootDir, relative.Replace('/', Path.DirectorySeparatorChar));

        foreach (string filePath in Directory.GetFiles(current))
        {
            string fileName = Path.GetFileName(filePath);
            entries.Add(new LpkV4.LpkEntry
            {
                Path = string.IsNullOrEmpty(relative) ? fileName : relative + "/" + fileName,
                SourcePath = filePath
            });
        }

        foreach (string subDir in Directory.GetDirectories(current))
        {
            string name = Path.GetFileName(subDir);
            CollectEntries(rootDir, string.IsNullOrEmpty(relative) ? name : relative + "/" + name, entries);
        }
    }

    #endregion

    private static void WaitForExit()
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey(true);
        }
        catch
        {
            // 输入被重定向时直接退出
        }
    }
}
