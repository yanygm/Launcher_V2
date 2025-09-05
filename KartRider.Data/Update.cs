using Newtonsoft.Json.Linq;
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

namespace KartRider
{
    // 1. 定义与GitHub Releases API对应的JSON模型（仅包含需要的字段，其他字段可忽略）
    /// <summary>
    /// GitHub Releases API返回的根对象
    /// </summary>
    public class GitHubReleaseRoot
    {
        /// <summary>
        /// 发布版本下的所有资产文件（如exe、zip）
        /// </summary>
        public GitHubReleaseAsset[] Assets { get; set; }
    }

    /// <summary>
    /// GitHub Releases中的单个资产文件（如Launcher.exe）
    /// </summary>
    public class GitHubReleaseAsset
    {
        /// <summary>
        /// 文件名（如Launcher.exe）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 文件的SHA256哈希值（格式：sha256:xxxxxx）
        /// </summary>
        public string Digest { get; set; }

        /// <summary>
        /// 文件的浏览器下载链接
        /// </summary>
        public string Browser_Download_Url { get; set; }
    }

    internal static class Update
    {
        static string owner = "yanygm";
        static string repo = "Launcher_V2";
        static string name = "Launcher.exe";

        public static async Task<bool> UpdateDataAsync()
        {
            // 获取当前执行程序集的路径
            string filePath = Assembly.GetExecutingAssembly().Location;
            // 验证路径是否有效
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                // 尝试备选方法获取路径
                filePath = Process.GetCurrentProcess().MainModule.FileName;

                // 再次验证
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Console.WriteLine("无法获取有效的程序集路径");
                    return false;
                }
            }
            Console.WriteLine("当前程序路径: " + filePath);
            name = Path.GetFileName(filePath);
            // 计算文件的SHA256哈希值
            string sha256Hash = "sha256:" + CalculateSHA256(filePath);
            Console.WriteLine("当前程序SHA256: " + sha256Hash);
            // 删除旧的Update文件夹（如果存在）
            string Update_Folder = Path.Combine(Path.GetDirectoryName(filePath), "Update");
            if (Directory.Exists(Update_Folder))
            {
                Directory.Delete(Update_Folder, true);
            }
            Console.WriteLine("开始读取GitHub Releases API数据...");
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
                    string.Equals(asset.Name, name, StringComparison.OrdinalIgnoreCase)); // 忽略文件名大小写

                // 6. 输出结果
                if (launcherExeAsset != null)
                {
                    Console.WriteLine("找到目标文件：" + name);
                    Console.WriteLine("==============================");
                    Console.WriteLine($"Digest: {launcherExeAsset.Digest}");
                    Console.WriteLine($"Browser_Download_Url: {launcherExeAsset.Browser_Download_Url}");
                    if (launcherExeAsset.Digest != sha256Hash)
                    {
                        Console.WriteLine($"发现新版本, 请问是否需要更新? (Y/n)");
                        string input = Console.ReadLine();
                        if (input.ToLower() == "n")
                        {
                            return false;
                        }
                        try
                        {
                            string country = await GetCountryAsync();
                            if (country != "" && country == "CN")
                            {
                                List<string> urls = new List<string>() { "https://ghproxy.net/", "https://gh-proxy.com/" };
                                foreach (string url_ in urls)
                                {
                                    string url2 = "";
                                    if (url_ == "https://ghproxy.net/")
                                    {
                                        url2 = url_ + launcherExeAsset.Browser_Download_Url;
                                    }
                                    else
                                    {
                                        url2 = url_ + launcherExeAsset.Browser_Download_Url.Replace("https://", "");
                                    }
                                    if (await GetUrl(url2))
                                    {
                                        return await DownloadUpdate(filePath, url2);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                return await DownloadUpdate(filePath, launcherExeAsset.Browser_Download_Url);
                            }
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

        public static async Task<bool> DownloadUpdate(string filePath, string UpdatePackageUrl)
        {
            string Update_Folder = Path.Combine(Path.GetDirectoryName(filePath), "Update");
            string Update_FilePath = Path.Combine(Update_Folder, Path.GetFileName(filePath));
            try
            {
                Console.WriteLine($"开始下载更新包: {UpdatePackageUrl}");
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(UpdatePackageUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            if (!Directory.Exists(Update_Folder))
                            {
                                Directory.CreateDirectory(Update_Folder);
                            }
                            long? totalBytes = response.Content.Headers.ContentLength;
                            using (FileStream fileStream = new FileStream(Update_FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;
                                long totalRead = 0;
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalRead += bytesRead;
                                    double progress = totalBytes.HasValue ? (double)totalRead / totalBytes.Value * 100 : 0;
                                    Console.Write($"\r下载进度: {progress:F2}%");
                                }
                            }
                        }
                    }
                    return ApplyUpdate(filePath, Update_FilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载过程中出现错误: {ex.Message}");
                return false;
            }
        }

        public static bool ApplyUpdate(string filePath, string Update_FilePath)
        {
            string Update_Bat = Path.GetDirectoryName(Update_FilePath) + @"\Update.bat";
            try
            {
                string script = @$"@echo off{Environment.NewLine}timeout /t 3 /nobreak{Environment.NewLine}del {"\"" + filePath + "\""}{Environment.NewLine}move {"\"" + Update_FilePath + "\""} {"\"" + filePath + "\""}{Environment.NewLine}start {"\"\" \"" + filePath + "\""}{Environment.NewLine}del %0";
                try
                {
                    File.WriteAllText(Update_Bat, script, Program.targetEncoding);
                    Console.WriteLine("\n写入文件成功。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n写入文件时出错: {ex.Message}");
                }
                Process.Start(Update_Bat);
                Process.GetCurrentProcess().Kill();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n应用更新时出错: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> GetCountryAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://ipinfo.io/json");
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(json);
                        string country = data["country"]?.ToString();
                        return country;
                    }
                    else
                    {
                        Console.WriteLine($"请求失败，状态码: {response.StatusCode}");
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常: {ex.Message}");
                return "";
            }
        }

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
        static string CalculateSHA256(string filePath)
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
    }
}

