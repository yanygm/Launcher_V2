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

namespace KartRider
{
    internal static class Update
    {
        public static async Task<bool> UpdateDataAsync()
        {
            string executablePath = Process.GetCurrentProcess().MainModule.FileName;
            DateTime compilationDate = File.GetLastWriteTime(executablePath);
            string formattedDate = compilationDate.ToString("yyMMdd");
            string tag_name = await GetTag_name();
            string url = "https://github.com/yanygm/Launcher_V2/releases/download/" + tag_name + "/Launcher.zip";
            Console.WriteLine($"当前版本为: {formattedDate}");
            if (tag_name != "" && int.Parse(formattedDate) < int.Parse(tag_name))
            {
                Console.WriteLine($"发现新版本: {tag_name}, 请问是否需要更新? (Y/n)");
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
                        List<string> urls = new List<string>() { "https://ghproxy.net/", "https://gh-proxy.com/", "https://hub.myany.uk/", "http://kra.myany.uk:2233/", "http://krb.myany.uk:2233/" };
                        foreach (string url_ in urls)
                        {
                            string url2 = "";
                            if (url_ == "https://ghproxy.net/" || url_ == "https://hub.myany.uk/")
                            {
                                url2 = url_ + url;
                            }
                            else
                            {
                                url2 = url_ + url.Replace("https://", "");
                            }
                            if (await GetUrl(url2))
                            {
                                return await DownloadUpdate(url2);
                                break;
                            }
                        }
                    }
                    else
                    {
                        return await DownloadUpdate(url);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"下载过程中出现错误: {ex.Message}");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static async Task<bool> DownloadUpdate(string UpdatePackageUrl)
        {
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
                    return ApplyUpdate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载过程中出现错误: {ex.Message}");
                return false;
            }
        }

        public static bool ApplyUpdate()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = assembly.GetName();
            string simpleName = assemblyName.Name + ".exe";
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(AppDomain.CurrentDomain.BaseDirectory + "Update\\Launcher.zip", AppDomain.CurrentDomain.BaseDirectory + "Update\\");
                string script = @$"@echo off
timeout /t 3 /nobreak
move {"\"" + AppDomain.CurrentDomain.BaseDirectory + "Update\\" + simpleName + "\""} {"\"" + AppDomain.CurrentDomain.BaseDirectory + "\""}
start {"\"\" \"" + AppDomain.CurrentDomain.BaseDirectory + simpleName + "\""}
";
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "Update.bat";
                try
                {
                    File.WriteAllText(filePath, script);
                    Console.WriteLine("\n写入文件成功。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n写入文件时出错: {ex.Message}");
                }
                Process.Start(filePath);
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

        public static async Task<string> GetTag_name()
        {
            try
            {
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
                        return data.tag_name;
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
    }
}
