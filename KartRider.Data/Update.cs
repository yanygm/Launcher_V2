using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;
using Profile;

namespace KartRider
{
    class Update
    {
        public static string owner = "yanygm";
        public static string repo = "Launcher_V2";
        static string name;
        static string filePath;

        public static async Task<bool> UpdateDataAsync(bool silent = false)
        {
            filePath = JsonHelper.GetFilePath();
            Console.WriteLine("当前程序路径: " + filePath);
            name = Path.GetFileName(filePath);
            // 计算文件的SHA256哈希值
            string sha256Hash = "sha256:" + CalculateSHA256(filePath);
            Console.WriteLine("当前程序SHA256: " + sha256Hash);
            string Update_Folder = Path.Combine(Path.GetDirectoryName(filePath), "Update");
            string Update_File = Path.Combine(Update_Folder, name);
            Console.WriteLine("开始读取GitHub Releases API数据...");
            Console.WriteLine("==============================");
            try
            {
                // 2. 创建HttpClient（设置User-Agent，避免GitHub API拒绝请求）
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubReleaseParser/1.0"); // GitHub要求必须设置User-Agent
                httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置10秒超时

                // 3. 发送GET请求获取API响应
                var response = await httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest");
                response.EnsureSuccessStatusCode(); // 若状态码不是200-299，抛出异常（如404、500）

                // 4. 读取响应内容并反序列化为C#对象
                var jsonContent = await response.Content.ReadAsStringAsync();
                var releaseData = JsonSerializer.Deserialize<GitHubReleaseRoot>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // 忽略JSON字段大小写（适配API的camelCase）

                // 5. 筛选name="Launcher.exe"的资产
                if (releaseData?.Assets == null || releaseData.Assets.Length == 0)
                {
                    Console.WriteLine("错误：API返回的资产列表为空");
                    return false;
                }

                var launcherExeAsset = Array.Find(releaseData.Assets, asset =>
                    string.Equals(asset.name, name, StringComparison.OrdinalIgnoreCase)); // 忽略文件名大小写

                // 6. 输出结果
                if (launcherExeAsset != null)
                {
                    Console.WriteLine("找到目标文件：" + name);
                    Console.WriteLine($"Digest: {launcherExeAsset.digest}");
                    Console.WriteLine($"Browser_Download_Url: {launcherExeAsset.browser_download_url}");
                    Console.WriteLine($"更新说明: \n{releaseData.body}");
                    Console.WriteLine("==============================");
                    if (launcherExeAsset.digest != sha256Hash)
                    {
                        // 非静默模式（AutoUpdate 关闭）：先弹窗询问，用户确认后才开始下载
                        if (!silent)
                        {
                            DialogResult result = MessageBox.Show(
                                "发现新版本，是否立即更新？\n\n更新过程中启动器会短暂关闭，完成后将自动重新打开。",
                                "发现新版本",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result != DialogResult.Yes)
                            {
                                Console.WriteLine("用户取消更新，保留当前版本");
                                return false;
                            }
                        }

                        try
                        {
                            // 选择下载地址：CN 地区优先使用代理，否则直连
                            string downloadUrl = launcherExeAsset.browser_download_url;
                            var ipInfo = await GetCountryAsync();
                            string country = ipInfo == null ? "" : ipInfo.Country;
                            if (country != "" && country == "CN")
                            {
                                ProfileService.LoadSettings();
                                string url2 = ProfileService.SettingConfig.Proxy + launcherExeAsset.browser_download_url;
                                if (ProfileService.SettingConfig.Proxy == "https://gh-proxy.com/")
                                {
                                    url2 = ProfileService.SettingConfig.Proxy + launcherExeAsset.browser_download_url.Replace("https://", "");
                                }
                                if (await GetUrl(url2))
                                {
                                    downloadUrl = url2;
                                }
                            }

                            int threadCount = 1; // 可根据需要调整线程数
                            var downloader = new MultiThreadedDownloader(downloadUrl, Update_File, threadCount);
                            var downloadResult = await downloader.StartDownloadAsync();
                            if (downloadResult)
                            {
                                return ConfirmAndApplyUpdate(Update_File, launcherExeAsset.digest);
                            }
                            return downloadResult;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"下载过程中出现错误: {ex.Message}");
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("当前已是最新版本，无需更新。");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("未找到名称为\"" + name + "\"的文件");
                    return false;
                }
                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"网络请求错误：{ex.Message}");
                Console.WriteLine("可能原因：API地址无效、网络断开、GitHub API限流");
                return false;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON反序列化错误：{ex.Message}");
                Console.WriteLine("可能原因：API返回格式异常");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"未知错误：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 校验下载文件的完整性，并应用更新。
        /// 用户确认已在进行下载之前完成；不在后台静默替换自身——
        /// "下载后自动执行替换脚本并自杀"会被行为检测（PDM）判为恶意自更新。
        /// </summary>
        /// <param name="updateFilePath">下载到本地的更新文件路径</param>
        /// <param name="expectedDigest">GitHub 公布的 sha256:xxxx 摘要</param>
        /// <returns>是否已应用更新</returns>
        private static bool ConfirmAndApplyUpdate(string updateFilePath, string expectedDigest)
        {
            try
            {
                // 1. 校验下载文件的 SHA256 与官方发布的 digest 一致，防止下载到损坏或被篡改的文件
                string actualHash = "sha256:" + CalculateSHA256(updateFilePath);
                if (!string.Equals(actualHash, expectedDigest, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("校验失败：下载文件的哈希与官方发布不一致，已取消更新");
                    return false;
                }

                // 2. 校验通过后执行替换
                ApplyUpdate(updateFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用更新时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 应用更新：采用"重命名替换"方案，不使用 bat 脚本、不自杀、不删除自身文件，
        /// 以避免 PDM 行为检测（自删/替换/销毁目录/强制终止进程属于高危特征）。
        /// Windows 允许重命名正在运行的映像文件，因此：
        ///   1. 将运行中的自身 exe 重命名为 *.old
        ///   2. 将下载的新 exe 移入主路径
        ///   3. 清理更新目录（托管 API，不经过 cmd）
        ///   4. 启动新版本（携带 /updated 参数，由新实例清理 *.old 备份）
        /// </summary>
        /// <param name="Update_FilePath">下载到本地的更新文件路径</param>
        /// <returns>是否已应用更新</returns>
        public static bool ApplyUpdate(string Update_FilePath)
        {
            try
            {
                string oldFile = filePath + ".old";
                string updateFolder = Path.GetDirectoryName(Update_FilePath);

                // 1. 重命名运行中的自身（若残留旧备份先清理）
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                }
                File.Move(filePath, oldFile);

                // 2. 新 exe 移入主路径
                File.Move(Update_FilePath, filePath);

                // 3. 清理更新目录（含已无用的下载文件）
                try
                {
                    if (Directory.Exists(updateFolder))
                    {
                        Directory.Delete(updateFolder, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清理更新目录时出错: {ex.Message}");
                }

                // 4. 启动新版本，由新实例完成旧备份清理
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = "/updated",
                    UseShellExecute = true
                });

                // 正常退出当前进程（非强制终止）
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n应用更新时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 新版本实例启动后调用：清理上一次更新遗留的旧版本备份文件（*.old）。
        /// </summary>
        public static void CleanupOldVersion()
        {
            try
            {
                string oldFile = filePath + ".old";
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                    Console.WriteLine("已清理旧版本备份: " + oldFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理旧版本备份时出错: {ex.Message}");
            }
        }

        public static async Task<IpInfo> GetCountryAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10); // 与 GetLatestRelease 一致，避免默认100秒超时长时间阻塞
                    HttpResponseMessage response = await client.GetAsync("https://ipinfo.io/json");
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        IpInfo data = JsonSerializer.Deserialize<IpInfo>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return data;
                    }
                    else
                    {
                        Console.WriteLine($"请求失败，状态码: {response.StatusCode}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查URL是否可访问
        /// </summary>
        /// <param name="url">要检查的URL</param>
        /// <returns>如果URL可访问则返回true，否则返回false</returns>
        public static async Task<bool> GetUrl(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 计算指定文件的SHA256哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>SHA256哈希值的字符串表示</returns>
        public static string CalculateSHA256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    // 计算文件的哈希值
                    byte[] hashBytes = sha256.ComputeHash(stream);

                    // 将字节数组转换为十六进制字符串
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static async Task<apiUpdate> GetUpdateAsync()
        {
            // API 地址
            string url = "https://tcgapi.tiancity.com/tcgame/V2/apiUpdate?gameid=4";

            // 创建 HTTP 客户端
            using HttpClient client = new HttpClient();

            try
            {
                // 发送 GET 请求并获取响应字符串
                string json = await client.GetStringAsync(url);

                // 解析 JSON
                apiUpdate data = JsonSerializer.Deserialize<apiUpdate>(json);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"请求或解析失败: {ex.Message}");
                return null;
            }
        }
    }

    // 1. 定义与GitHub Releases API对应的JSON模型（仅包含需要的字段，其他字段可忽略）
    /// <summary>
    /// GitHub Releases API返回的根对象
    /// </summary>
    class GitHubReleaseRoot
    {
        /// <summary>
        /// 发布版本下的所有资产文件（如exe、zip）
        /// </summary>
        public GitHubReleaseAsset[] Assets { get; set; }

        /// <summary>
        /// 发布版本的说明（如版本号、发布日期、更新内容等）
        /// </summary>
        public string body { get; set; }
    }

    public class IpInfo
    {
        public string Ip { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
    }

    /// <summary>
    /// GitHub Releases中的单个资产文件（如Launcher.exe）
    /// </summary>
    public class GitHubReleaseAsset
    {
        /// <summary>
        /// 文件名（如Launcher.exe）
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 文件的SHA256哈希值（格式：sha256:xxxxxx）
        /// </summary>
        public string digest { get; set; }

        /// <summary>
        /// 文件的浏览器下载链接
        /// </summary>
        public string browser_download_url { get; set; }
    }

    public class apiUpdate
    {
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string version { get; set; }
        public string install_pack_time { get; set; }
        public string update_time { get; set; }
        public string download_prefix { get; set; }
        public string update_prefix { get; set; }
    }
}