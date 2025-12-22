using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace KartRider;

class MultiThreadedDownloader
{
    // 线程安全的已下载字节数
    private long _totalDownloadedBytes = 0;
    // 文件总大小
    private long _fileTotalSize = 0;
    // 下载线程数
    private readonly int _threadCount;
    // 下载地址
    private readonly string _downloadUrl;
    // 保存路径
    private readonly string _savePath;
    // 临时文件目录
    private readonly string _tempDir;
    // 取消令牌源，用于取消所有下载线程
    private CancellationTokenSource _downloadCancellationTokenSource;
    // 取消令牌
    private CancellationToken _downloadCancellationToken;
    // 进度显示取消令牌源
    private CancellationTokenSource _progressCancellationTokenSource;

    private int lastProgressLength = 0;

    private long previousBytes = 0;

    public MultiThreadedDownloader(string downloadUrl, string savePath, int threadCount = 4)
    {
        _downloadUrl = downloadUrl;
        _savePath = savePath;
        _threadCount = threadCount;
        _tempDir = Path.Combine(Path.GetDirectoryName(savePath) ?? "", $"temp_{Path.GetFileNameWithoutExtension(savePath)}");
    }

    /// <summary>
    /// 开始多线程下载
    /// </summary>
    public async Task<bool> StartDownloadAsync()
    {
        // 初始化下载线程取消令牌源和取消令牌
        _downloadCancellationTokenSource = new CancellationTokenSource();
        _downloadCancellationToken = _downloadCancellationTokenSource.Token;
        // 初始化进度显示取消令牌源
        _progressCancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // 1. 获取文件总大小
            _fileTotalSize = await GetFileSizeAsync(_downloadUrl);
            if (_fileTotalSize <= 0)
            {
                Console.WriteLine("无法获取文件大小，可能服务器不支持分片下载");
                return false;
            }

            Console.WriteLine("==============================");
            Console.WriteLine($"下载地址: {_downloadUrl}");
            Console.WriteLine($"文件总大小: {FormatFileSize(_fileTotalSize)}");
            Console.WriteLine($"使用 {_threadCount} 线程下载...");

            // 2. 创建临时目录
            if (!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }

            // 3. 计算每个线程的下载范围
            long chunkSize = _fileTotalSize / _threadCount;
            var tasks = new Task[_threadCount];

            // 4. 启动进度显示线程，使用单独的取消令牌
            var progressTask = Task.Run(() => ShowProgress(), _progressCancellationTokenSource.Token);

            // 5. 启动下载线程，使用下载取消令牌
            for (int i = 0; i < _threadCount; i++)
            {
                int threadIndex = i;
                long start = i * chunkSize;
                // 最后一个线程下载剩余所有数据
                long end = (i == _threadCount - 1) ? _fileTotalSize - 1 : (i + 1) * chunkSize - 1;

                tasks[i] = Task.Run(async () =>
                {
                    await DownloadChunkAsync(threadIndex, start, end);
                }, _downloadCancellationToken);
            }

            // 6. 等待所有下载线程完成
            await Task.WhenAll(tasks);

            // 7. 检查是否被取消（下载失败导致的取消）
            if (_downloadCancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"\n下载已取消");
                return false;
            }

            // 8. 停止进度显示
            _progressCancellationTokenSource.Cancel();
            progressTask.Wait(1000);
            Console.WriteLine("");

            // 9. 合并文件片段
            await MergeChunksAsync();

            // 10. 删除临时文件
            Directory.Delete(_tempDir, true);

            return true;
        }
        catch (OperationCanceledException ex)
        {
            // 区分是下载取消还是进度显示取消
            if (ex.CancellationToken == _downloadCancellationToken)
            {
                Console.WriteLine($"\n下载已取消");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n下载出错: {ex.Message}");
        }
        finally
        {
            // 确保清理资源
            _downloadCancellationTokenSource?.Dispose();
            _progressCancellationTokenSource?.Dispose();
            // 清理临时文件
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        return false;
    }

    /// <summary>
    /// 获取文件大小
    /// </summary>
    private async Task<long> GetFileSizeAsync(string url)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "HEAD";
        using (var response = (HttpWebResponse)await request.GetResponseAsync())
        {
            return response.ContentLength;
        }
    }

    /// <summary>
    /// 下载单个文件片段
    /// </summary>
    private async Task DownloadChunkAsync(int threadIndex, long start, long end)
    {
        string tempFilePath = Path.Combine(_tempDir, $"chunk_{threadIndex}.tmp");
        long chunkSize = end - start + 1;
        long downloaded = 0;

        try
        {
            var request = (HttpWebRequest)WebRequest.Create(_downloadUrl);
            request.AddRange(start, end);

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[8192]; // 8KB缓冲区
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _downloadCancellationToken)) > 0)
                {
                    // 检查是否需要取消下载
                    _downloadCancellationToken.ThrowIfCancellationRequested();
                    
                    await fileStream.WriteAsync(buffer, 0, bytesRead, _downloadCancellationToken);
                    downloaded += bytesRead;
                    // 线程安全地更新总下载字节数
                    Interlocked.Add(ref _totalDownloadedBytes, bytesRead);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 这是正常的取消操作，不需要特别处理
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n线程 {threadIndex} 下载失败: {ex.Message}");
            // 下载失败，取消所有下载线程
            _downloadCancellationTokenSource.Cancel();
            throw;
        }
    }

    /// <summary>
    /// 合并文件片段
    /// </summary>
    private async Task MergeChunksAsync()
    {
        using (var outputStream = new FileStream(_savePath, FileMode.Create, FileAccess.Write))
        {
            for (int i = 0; i < _threadCount; i++)
            {
                // 检查是否需要取消
                _downloadCancellationToken.ThrowIfCancellationRequested();
                
                string tempFilePath = Path.Combine(_tempDir, $"chunk_{i}.tmp");
                if (File.Exists(tempFilePath))
                {
                    using (var inputStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                    {
                        await inputStream.CopyToAsync(outputStream, _downloadCancellationToken);
                    }
                    File.Delete(tempFilePath);
                }
            }
        }
    }

    /// <summary>
    /// 实时显示下载进度
    /// </summary>
    private void ShowProgress()
    {
        try
        {
            while (_totalDownloadedBytes < _fileTotalSize)
            {
                // 检查是否需要取消
                _progressCancellationTokenSource.Token.ThrowIfCancellationRequested();
                
                // 计算整体进度
                double totalProgress = (double)_totalDownloadedBytes / _fileTotalSize * 100;

                int progressBarWidth = 50;
                int progress = (int)(totalProgress / 100 * progressBarWidth);

                long currentBytes = _totalDownloadedBytes;
                long speedBytesPerSecond = currentBytes - previousBytes;
                previousBytes = currentBytes;

                // 格式化速度显示（转换为 KB/s 或 MB/s）
                string speedDisplay = FormatSpeed(speedBytesPerSecond);

                string progressText = $"[{new string('#', progress)}{new string(' ', progressBarWidth - progress)}] " + $"{totalProgress:F2}%  " + $"({FormatFileSize(_totalDownloadedBytes)} / {FormatFileSize(_fileTotalSize)}) " + $"({speedDisplay})";
                Console.Write($"\r{new string(' ', lastProgressLength)}");
                Console.Write($"\r{progressText}");
                lastProgressLength = progressText.Length;

                // 控制刷新频率（每秒1次），并检查取消
                if (!_progressCancellationTokenSource.Token.WaitHandle.WaitOne(1000))
                {
                    // 如果等待超时（即1秒已过且未取消），继续循环
                    continue;
                }
                // 如果WaitOne返回true，表示取消请求已发出
                _progressCancellationTokenSource.Token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不处理
        }
    }

    /// <summary>
    /// 格式化文件大小显示（B/KB/MB/GB）
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }

    /// <summary>
    /// 格式化下载速度（字节/秒 转 KB/s/MB/s）
    /// </summary>
    /// <param name="bytesPerSecond">每秒字节数</param>
    /// <returns>格式化后的速度字符串</returns>
    private static string FormatSpeed(long bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
        {
            return $"{bytesPerSecond} B/s";
        }
        else if (bytesPerSecond < 1024 * 1024)
        {
            return $"{bytesPerSecond / 1024.0:0.00} KB/s";
        }
        else
        {
            return $"{bytesPerSecond / (1024.0 * 1024):0.00} MB/s";
        }
    }
}