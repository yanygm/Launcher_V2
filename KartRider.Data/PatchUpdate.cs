using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using KartRider.Common.Data;

namespace KartRider
{
    class PatchUpdate
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task UpdateDataAsync(string GameFolder)
        {
            string DataFolder = Path.Combine(GameFolder, @"Data");
            var GitFiles = await GetGitFiles(GameFolder);
            if (GitFiles == null)
            {
                return;
            }
            try
            {
                // 调用方法获取符合条件的文件列表
                var matchingFiles = GetDataPack0Rho5Files(DataFolder);

                foreach (var matchingFile in matchingFiles)
                {
                    var filename = Path.GetFileName(matchingFile);
                    var gitFile = GitFiles.FirstOrDefault(file => file.name == filename);
                    if (gitFile == null)
                    {
                        File.Delete(matchingFile);
                    }
                }

                var aaa = Path.Combine(DataFolder, @"aaa.pk");
                if (File.Exists(aaa))
                {
                    matchingFiles.Add(aaa);
                }
                var sound_bgm_korea = Path.Combine(DataFolder, @"sound_bgm_korea.rho");
                if (File.Exists(sound_bgm_korea))
                {
                    matchingFiles.Add(sound_bgm_korea);
                }
                var sound_bgm_lotte = Path.Combine(DataFolder, @"sound_bgm_lotte.rho");
                if (File.Exists(sound_bgm_lotte))
                {
                    matchingFiles.Add(sound_bgm_lotte);
                }

                // 输出结果
                Console.WriteLine("==============================");
                foreach (var Git in GitFiles)
                {
                    var gitFile = matchingFiles.FirstOrDefault(file => Path.GetFileName(file) == Git.name);
                    if (gitFile != null)
                    {
                        string sha256Hash = "sha256:" + Update.CalculateSHA256(gitFile);
                        if (Git.digest != sha256Hash)
                        {
                            await Downloader(gitFile, Git.browser_download_url);
                        }
                    }
                    else
                    {
                        string savePath = Path.Combine(DataFolder, Git.name);
                        await Downloader(savePath, Git.browser_download_url);
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"错误：指定的目录 {DataFolder} 不存在！");
                return;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"错误：没有访问目录 {DataFolder} 的权限！");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询文件时出错：{ex.Message}");
                return;
            }
        }

        public static async Task<bool> Downloader(string fileName, string download_url)
        {
            try
            {
                string country = await Update.GetCountryAsync();
                if (country != "" && country == "CN")
                {
                    foreach (string url_ in Update.urls)
                    {
                        string url2 = url_ + download_url;
                        if (url_ == "https://gh-proxy.com/")
                        {
                            url2 = url_ + download_url.Replace("https://", "");
                        }
                        if (await Update.GetUrl(url2))
                        {
                            int threadCount = 16; // 可根据需要调整线程数
                            var downloader = new MultiThreadedDownloader(url2, fileName, threadCount);
                            var downloadResult1 = await downloader.StartDownloadAsync();
                            return downloadResult1;
                            break;
                        }
                    }
                }
                else
                {
                    int threadCount = 16; // 可根据需要调整线程数
                    var downloader = new MultiThreadedDownloader(download_url, fileName, threadCount);
                    var downloadResult2 = await downloader.StartDownloadAsync();
                    return downloadResult2;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载过程中出现错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取指定目录下（不包含子目录）DataPack0_开头且后缀为.rho5的文件
        /// </summary>
        /// <param name="directoryPath">目标目录路径</param>
        /// <returns>符合条件的文件完整路径数组</returns>
        public static List<string> GetDataPack0Rho5Files(string directoryPath)
        {
            // 先验证目录是否存在
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"目录不存在: {directoryPath}");
                return new List<string>();
            }

            // 核心筛选：通配符 DataPack0_*.rho5 精准匹配「开头+后缀」
            // SearchOption.TopDirectoryOnly 确保不搜索子目录
            string[] matchingFiles = Directory.GetFiles(
                path: directoryPath,
                searchPattern: "DataPack0_*.rho5",
                searchOption: SearchOption.TopDirectoryOnly
            );

            return new List<string>(matchingFiles);
        }

        public static async Task<List<GitHubReleaseAsset>> GetGitFiles(string GameFolder)
        {
            try
            {
                // 初始化 HttpClient（添加 User-Agent 避免 GitHub API 拒绝请求）
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CSharp-Release-Fetcher", "1.0"));
                // 可选：添加 GitHub Token（解决 API 限流问题，若无 Token 每小时仅能请求 60 次）
                // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", "你的GitHub个人令牌");

                // 调用 API 获取指定标签的文件列表
                PINFile val = new PINFile(Path.Combine(GameFolder, @"KartRider.pin"));
                var targetTag = "P" + val.Header.MinorVersion;
                var assetList = await GetReleaseAssetsByTag(Update.owner, Update.repo, targetTag);

                // 输出结果
                if (assetList == null || assetList.Count == 0)
                {
                    Console.WriteLine($"未找到标签为 {targetTag} 的发布版本，或该版本无文件！");
                    return null;
                }
                else
                {
                    return assetList;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"网络请求错误：{ex.Message}");
                Console.WriteLine("可能原因：API地址无效、网络断开、GitHub API限流");
                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON反序列化错误：{ex.Message}");
                Console.WriteLine("可能原因：API返回格式异常");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"未知错误：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据标签获取指定 Release 的文件列表
        /// </summary>
        /// <param name="owner">仓库所有者</param>
        /// <param name="repo">仓库名称</param>
        /// <param name="tagName">目标标签</param>
        /// <returns>文件列表</returns>
        public static async Task<List<GitHubReleaseAsset>> GetReleaseAssetsByTag(string owner, string repo, string tagName)
        {
            // GitHub Releases API 地址
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";

            // 发送 GET 请求获取所有 Release
            var response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode(); // 确保请求成功（非 2xx 会抛异常）

            // 读取响应内容并解析为 Release 列表
            string jsonContent = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // 忽略字段大小写（适配 API 返回的蛇形命名）
            });

            // 筛选指定 tag_name 的 Release
            var targetRelease = releases.Find(r => r.tag_name == tagName);
            return targetRelease?.assets ?? new List<GitHubReleaseAsset>();
        }
    }

    // 对应 GitHub API 返回的 Release 数据结构（仅保留核心字段）
    public class GitHubRelease
    {
        public string tag_name { get; set; }
        public List<GitHubReleaseAsset> assets { get; set; }
    }
}