using KartRider;
using KartRider.Logging;
using RiderData;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Profile
{
    /// <summary>
    /// 玩家档案服务。
    ///
    /// 原实现每次 GetProfileConfig 都会：读整个文件 → 两次编码探测 → 一次 JsonDocument 校验 →
    /// 一次反序列化，并且失败时静默返回默认档案（随后任意 Save 会把真实存档覆盖成空白）。
    ///
    /// 现在：进程内缓存 + 写时比较（内容未变不落盘）+ 原子替换 + 失败保留上次好数据。
    /// </summary>
    public class ProfileService
    {
        private sealed class Entry
        {
            public ProfileConfig Config;
            public string LastJson;
            public bool LoadedFromDisk;
        }

        private static readonly ConcurrentDictionary<string, Entry> Cache =
            new ConcurrentDictionary<string, Entry>(StringComparer.Ordinal);

        private static readonly object SettingsLock = new object();

        public static Setting SettingConfig { get; set; } = new Setting();

        public static int CachedProfiles => Cache.Count;

        public static ProfileConfig GetProfileConfig(string Nickname)
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                return new ProfileConfig();
            }

            if (Cache.TryGetValue(Nickname, out Entry cached))
            {
                Metrics.Inc(ref Metrics.ProfileCacheHits);
                return cached.Config;
            }

            Entry entry = Cache.GetOrAdd(Nickname, key => LoadEntry(key));
            return entry.Config;
        }

        private static Entry LoadEntry(string nickname)
        {
            Metrics.Inc(ref Metrics.ProfileCacheMisses);
            var entry = new Entry
            {
                Config = new ProfileConfig(),
                LastJson = null,
                LoadedFromDisk = false
            };

            try
            {
                fileName file = FileName.Load(nickname);
                if (file == null)
                {
                    return entry;
                }
                if (File.Exists(file.config_path))
                {
                    Metrics.Inc(ref Metrics.ProfileDiskReads);
                    ProfileConfig loaded = JsonHelper.DeserializeNoBom<ProfileConfig>(file.config_path);
                    if (loaded != null)
                    {
                        entry.Config = loaded;
                        entry.LoadedFromDisk = true;
                        // 记住磁盘上的原始内容，后续 Save 可据此跳过无意义的写盘
                        entry.LastJson = JsonHelper.Serialize(loaded);
                    }
                }
                else
                {
                    // 首次登录：落一份默认档案（原子写入）
                    string json = JsonHelper.Serialize(entry.Config);
                    JsonHelper.WriteAllTextAtomic(file.config_path, json);
                    entry.LastJson = json;
                    entry.LoadedFromDisk = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Profile] 读取档案失败 {nickname}，使用默认值（不落盘，避免覆盖真实数据）: {ex.Message}", ex);
                entry.LoadedFromDisk = false;
                entry.LastJson = null;
            }

            return entry;
        }

        public static void SaveSettings()
        {
            lock (SettingsLock)
            {
                try
                {
                    string json = JsonHelper.Serialize(SettingConfig);

                    // 内容没变就不写：避免每次启动都 File.Replace 一次，把用户手改的配置挤成 .bak
                    try
                    {
                        if (File.Exists(FileName.Load_Settings))
                        {
                            string existing = JsonHelper.ReadAllTextDetect(FileName.Load_Settings);
                            if (string.Equals(existing, json, StringComparison.Ordinal))
                            {
                                return;
                            }
                        }
                    }
                    catch
                    {
                        // 读不出来就照常写
                    }

                    JsonHelper.WriteAllTextAtomic(FileName.Load_Settings, json);
                }
                catch (Exception ex)
                {
                    Log.Error("[Profile] 保存设置失败", ex);
                }
            }
        }

        public static void LoadSettings()
        {
            if (File.Exists(FileName.Load_Settings))
            {
                SettingConfig = JsonHelper.DeserializeNoBom<Setting>(FileName.Load_Settings) ?? new Setting();

                // 旧版配置迁移（只做一次）：
                // v8 时期 PacketTrace 默认 false，且设置界面保存时会把 false 写进 json，
                // 单改代码默认值覆盖不了。这里按版本号强制迁移一次，之后以用户配置为准。
                // v3：默认开局等待从 3000ms 调整为 4000ms，仅迁移仍在使用旧默认值的用户
                if (SettingConfig.SettingsVersion < 3)
                {
                    if (SettingConfig.StartDelayMs == 3000)
                    {
                        SettingConfig.StartDelayMs = 4000;
                    }
                    SettingConfig.SettingsVersion = 3;
                    SaveSettings();
                }

                if (SettingConfig.SettingsVersion < 2)
                {
                    SettingConfig.SettingsVersion = 2;
                    SettingConfig.PacketTrace = true;
                    SettingConfig.PacketTraceConsole = true;
                    if (SettingConfig.PacketTraceSample <= 0)
                    {
                        SettingConfig.PacketTraceSample = 2;
                    }
                    SettingConfig.PacketTraceFilter ??= "";
                    Log.Info("[Config] 旧版配置已迁移：包输出默认开启（PacketTrace/PacketTraceConsole=true，采样 1/2）");
                    SaveSettings();
                }
            }
            else
            {
                SettingConfig = new Setting { SettingsVersion = 2 };
                SaveSettings();
            }
        }

        public static void Save(string Nickname, ProfileConfig config)
        {
            if (string.IsNullOrEmpty(Nickname) || config == null)
            {
                return;
            }

            fileName file = FileName.Load(Nickname);
            if (file == null)
            {
                return;
            }

            Entry entry = Cache.GetOrAdd(Nickname, _ => new Entry { Config = config, LoadedFromDisk = true });

            lock (entry)
            {
                entry.Config = config;

                if (!entry.LoadedFromDisk)
                {
                    // 档案从未成功读到过真实数据：拒绝写盘，避免把玩家存档洗成默认值
                    Log.Warn($"[Profile] 拒绝保存 {Nickname}：当前档案未从磁盘成功加载（保护数据）");
                    return;
                }

                try
                {
                    string json = JsonHelper.Serialize(config);
                    if (entry.LastJson != null && string.Equals(entry.LastJson, json, StringComparison.Ordinal) && File.Exists(file.config_path))
                    {
                        return; // 内容未变化，跳过写盘
                    }
                    Metrics.Inc(ref Metrics.ProfileDiskWrites);
                    JsonHelper.WriteAllTextAtomic(file.config_path, json);
                    entry.LastJson = json;
                }
                catch (Exception ex)
                {
                    Log.Error($"[Profile] 保存档案失败 {Nickname}", ex);
                }
            }
        }

        public static void Load(string Nickname)
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                return;
            }
            GetProfileConfig(Nickname);
        }

        /// <summary>改名后迁移缓存键，避免旧昵称档案占用内存。</summary>
        public static void OnNicknameChanged(string oldNickname, string newNickname)
        {
            if (string.IsNullOrEmpty(oldNickname) || string.IsNullOrEmpty(newNickname) || oldNickname == newNickname)
            {
                return;
            }
            if (Cache.TryRemove(oldNickname, out Entry entry))
            {
                Cache[newNickname] = entry;
            }
        }

        public static void Forget(string Nickname)
        {
            if (!string.IsNullOrEmpty(Nickname))
            {
                Cache.TryRemove(Nickname, out _);
            }
        }
    }
}
