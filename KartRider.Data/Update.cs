using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KartRider
{
    internal static class Update
    {
        public static async Task<bool> UpdateDataAsync()
        {
            DateTime compilationDate = File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory + "Launcher.exe");
            string formattedDate = compilationDate.ToString("yyMMdd");
            string tag_name = "";
            string owner = "yanygm";
            string repo = "Launcher_V2";
            string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", repo);
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    tag_name = data.tag_name;
                }
                else
                {
                    Console.WriteLine($"请求失败，状态码: {response.StatusCode}");
                }
            }
            if (tag_name != "" && int.Parse(formattedDate) < int.Parse(tag_name))
            {
                string country = await GetCountryAsync();
                if (country != "" && country == "CN")
                {
                    DownloadUpdate("https://ghproxy.cc/?q=https://github.com/yanygm/Launcher_V2/releases/download/" + tag_name + "/Launcher.zip");
                }
                else
                {
                    DownloadUpdate("=https://github.com/yanygm/Launcher_V2/releases/download/" + tag_name + "/Launcher.zip");
                }
                Console.WriteLine($"Launcher正在更新，请耐心等待...");
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async void DownloadUpdate(string UpdatePackageUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(UpdatePackageUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            string folderPath = AppDomain.CurrentDomain.BaseDirectory + "Update";
                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }
                            long? totalBytes = response.Content.Headers.ContentLength;
                            using (FileStream fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "Update\\Launcher.zip", FileMode.Create, FileAccess.Write, FileShare.None))
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
                    Console.WriteLine($"\n{UpdatePackageUrl} 下载完成.");
                    ApplyUpdate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载过程中出现错误: {ex.Message}");
            }
        }

        public static void ApplyUpdate()
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(AppDomain.CurrentDomain.BaseDirectory + "Update\\Launcher.zip", AppDomain.CurrentDomain.BaseDirectory + "Update\\");
                string script = @$"@echo off
timeout /t 3 /nobreak
move {AppDomain.CurrentDomain.BaseDirectory + "Update\\Launcher.exe"} {AppDomain.CurrentDomain.BaseDirectory}
start {AppDomain.CurrentDomain.BaseDirectory + "Launcher.exe"}
";
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "Update.bat";
                try
                {
                    File.WriteAllText(filePath, script);
                    Console.WriteLine("文本已成功写入文件。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"写入文件时出错: {ex.Message}");
                }
                Process.Start(filePath);
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用更新时出错: {ex.Message}");
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
    }
}
