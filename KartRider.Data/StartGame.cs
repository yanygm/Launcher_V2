using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KartRider.Common.Data;
using KartRider.IO.Packet;
using KartRider.Logging;
using Profile;

namespace KartRider;

/// <summary>
/// 启动客户端。
///
/// 原实现用"每秒起两个 netstat 进程 + 字符串匹配"判断客户端是否连上，
/// 最多 30 轮（60 个进程），并且 detectThread.Join() 会阻塞调用线程 30 秒。
/// 现在改成：服务端接受连接时直接通知（ClientManager.ClientConnected），
/// 无进程创建、无字符串解析，超时行为一致。
/// </summary>
class StartGame
{
    private const int ConnectTimeoutSeconds = 30;

    private string _pinFile;
    private string _pinFileBak;
    private string _kartRiderDirectory;

    public void Start(string kartRiderDirectory, string pinFile, string pinFileBak)
    {
        this._pinFile = pinFile;
        this._pinFileBak = pinFileBak;
        this._kartRiderDirectory = kartRiderDirectory;

        DataPacket packet = new DataPacket
        {
            Nickname = ProfileService.SettingConfig.Name,
            ClientVersion = ProfileService.SettingConfig.ClientVersion,
            CompileTime = CompileTime.Time,
        };

        try
        {
            RestorePinFile();
            File.Copy(pinFile, pinFileBak, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error($"备份 PIN 文件失败: {ex.Message}", ex);
            return;
        }

        PINFile val;
        try
        {
            val = new PINFile(pinFile);
        }
        catch (Exception ex)
        {
            Log.Error($"读取 PIN 文件失败: {ex.Message}", ex);
            return;
        }

        if (val.AuthMethods != null)
        {
            var ip = LanIpGetter.IsIPv6(ProfileService.SettingConfig.ServerIP) ? "127.0.0.1" : ProfileService.SettingConfig.ServerIP;
            foreach (PINFile.AuthMethod authMethod in val.AuthMethods)
            {
                authMethod.LoginServers?.Clear();
                authMethod.LoginServers?.Add(new PINFile.IPEndPoint
                {
                    IP = ip,
                    Port = ProfileService.SettingConfig.ServerPort
                });
            }
        }

        if (!ProfileService.SettingConfig.NgsOn && val.BmlObjects != null)
        {
            foreach (BmlObject bml in val.BmlObjects)
            {
                if (bml.Name == "extra" && bml.SubObjects != null)
                {
                    for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                    {
                        if (bml.SubObjects[i].Item1 == "NgsOn")
                        {
                            Log.Info($"Removing {bml.SubObjects[i].Item1}");
                            bml.SubObjects.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        try
        {
            File.WriteAllBytes(pinFile, val.GetEncryptedData());
        }
        catch (Exception ex)
        {
            Log.Error($"写入 PIN 文件失败: {ex.Message}", ex);
            return;
        }

        Task<SessionGroup> waitForClient = ClientManager.WaitForClientAsync(TimeSpan.FromSeconds(ConnectTimeoutSeconds));

        Process process = null;
        try
        {
            string passport = Base64Helper.Encode(JsonHelper.Serialize(packet));
            ProcessStartInfo startInfo = new ProcessStartInfo("KartRider.exe", $"TGC -region:3 -passport:{passport}")
            {
                WorkingDirectory = Path.GetFullPath(kartRiderDirectory),
                UseShellExecute = true
            };

            process = Process.Start(startInfo);
            Log.Info($"客户端进程已启动, ID: {process?.Id}");

            // 等待"客户端真正连上本机服务端"，而不是盲等固定时长
            SessionGroup connected = waitForClient.GetAwaiter().GetResult();
            if (connected != null)
            {
                if (!RestorePinFile())
                {
                    Log.Warn("[PIN] 客户端已连接，但恢复 PIN 文件失败");
                }
                else
                {
                    Log.Info("[PIN] 客户端已连接，PIN 文件已恢复");
                }
            }
            else
            {
                Log.Warn($"[TCP检测] 超过 {ConnectTimeoutSeconds} 秒未检测到连接，停止等待");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Error($"UAC取消或权限不足: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Log.Error($"操作失败: {ex.Message}", ex);
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// 恢复备份的 PIN 文件
    /// </summary>
    /// <returns>是否恢复成功</returns>
    private bool RestorePinFile()
    {
        try
        {
            if (string.IsNullOrEmpty(_pinFileBak) || !File.Exists(_pinFileBak))
            {
                return false;
            }

            if (File.Exists(_pinFile))
                File.Delete(_pinFile);

            File.Move(_pinFileBak, _pinFile);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"[PIN] 恢复 PIN 文件失败: {ex.Message}");
            return false;
        }
    }
}
