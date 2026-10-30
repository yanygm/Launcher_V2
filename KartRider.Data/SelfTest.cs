using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KartRider.Common.Network;
using KartRider.Common.Security;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 无界面自检：dotnet Launcher.exe --selftest
    /// 覆盖本次改造中风险最高的几处纯逻辑（帧编解码、分发表、档案缓存、并发容器）。
    /// </summary>
    public static class SelfTest
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> Failures = new List<string>();

        public static int RunAll(bool verbose)
        {
            var sw = Stopwatch.StartNew();
            string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Log.Init(logDir, verbose ? LogLevel.Debug : LogLevel.Warn, packetTrace: false);

            Console.WriteLine("=== KartRider Launcher 自检 ===");

            Run("帧编解码：单帧往返", FrameRoundTrip);
            Run("帧编解码：多帧粘包", FramePipelined);
            Run("帧编解码：半包等待", FramePartial);
            Run("帧编解码：超长帧拒绝", FrameOversized);
            Run("帧编解码：明文首帧", FramePlaintext);
            Run("帧编解码：随机字节不崩溃", FrameFuzz);
            Run("包名哈希表与 Adler32 一致", PacketHashMatches);
            Run("档案缓存：命中不再读盘", ProfileCacheHit);
            Run("档案保存：内容未变不写盘", ProfileNoopSave);
            Run("档案保存：损坏档案不覆盖", ProfileCorruptGuard);
            Run("原子写：无残留临时文件", AtomicWriteClean);
            Run("并发字典：FileName 并发注册", FileNameConcurrent);
            Run("房间槽位集合：并发读写", SlotSetConcurrent);
            Run("客户端身份：断线后 UserNO 仍可反查（换频道依赖）", UserNoSurvivesDisconnect);
            Run("ModelMax 表：命中/缺失/大 id 均符合预期", ModelMaxTableLookup);
            Run("包环形缓冲：记录/过滤/清空", PacketRingWorks);
            Run("道具规则：概率表与名次档位", ItemRuleMatchesSpec);
            Run("道具规则：firing/fired 两张表落点正确", KartRuleTableDirection);
            Run("道具规则：item.kml 晚于规则表加载不丢规则", KartRuleLoadOrder);
            Run("LPK v4：分卷目录可读（需 LPK_TEST_DIR）", LpkDataFolderReads);
            Run("Rho：3571 LPK 数据可加载（需 RHO_TEST_DIR）", RhoDumpLoads);

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine($"通过 {_passed} 项，失败 {_failed} 项，用时 {sw.ElapsedMilliseconds} ms");
            foreach (string f in Failures)
            {
                Console.WriteLine("  失败: " + f);
            }

            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "selftest.txt"),
                    $"passed={_passed} failed={_failed}{Environment.NewLine}{string.Join(Environment.NewLine, Failures)}");
            }
            catch
            {
            }

            Log.Shutdown();
            return _failed == 0 ? 0 : 1;
        }

        /// <summary>真实客户端目录加载自检：设置 RHO_TEST_DIR 指向含 Data/DataPack*.lpk 的客户端根目录。</summary>
        private static void RhoDumpLoads()
        {
            string dir = Environment.GetEnvironmentVariable("RHO_TEST_DIR");
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            var manager = KartRhoFile.Dump(Path.Combine(dir, "Data", "aaa.pk"));
            if (manager == null) throw new Exception("KartRhoFile.Dump 返回 null");
            if (GameSupport.questInfo == null) throw new Exception("questInfo 未加载（quest xml 匹配失败）");
            if (GameSupport.Channels.Count == 0) throw new Exception("Channels 为空（channel.xml 匹配失败）");
        }

        /// <summary>LPK v4 集成自检：设置环境变量 LPK_TEST_DIR 指向含 DataPack*.lpk 的目录。</summary>
        private static void LpkDataFolderReads()
        {
          try {
            string dir = Environment.GetEnvironmentVariable("LPK_TEST_DIR");
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            var manager = new KartLibrary.File.PackFolderManager();
            manager.OpenLpkDataFolder(dir);
            var queue = new Queue<KartLibrary.File.PackFolderInfo>();
            queue.Enqueue(manager.GetRootFolder());
            int count = 0;
            byte[] sample = null;
            while (queue.Count > 0)
            {
                var folder = queue.Dequeue();
                foreach (var file in folder.GetFilesInfo())
                {
                    count++;
                    if (sample == null) sample = file.GetData();
                }
                foreach (var sub in folder.GetFoldersInfo()) queue.Enqueue(sub);
            }
            if (count <= 0) throw new Exception("LPK 目录没有文件");
            if (sample == null || sample.Length == 0) throw new Exception("LPK 文件数据为空");
            _ = sample.Length;
          } catch (Exception ex) { throw new Exception(ex.ToString()); }
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine($"  [PASS] {name}");
            }
            catch (Exception ex)
            {
                _failed++;
                Failures.Add($"{name}: {ex.Message}");
                Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void FrameRoundTrip()
        {
            uint siv = 0x1234_5678;
            uint riv = siv;
            byte[] payload = new byte[200];
            new Random(1).NextBytes(payload);
            byte[] original = (byte[])payload.Clone();

            byte[] frame = SessionFrame.BuildFrame(payload, payload.Length, ref siv);
            Assert(frame.Length == original.Length + 8, "帧长度应为 payload + 8");
            Assert(siv == 0x1234_5678 + SessionFrame.IvStep, "发送 IV 应递增");

            SessionFrame.FrameStatus st = SessionFrame.TryExtract(frame, frame.Length, ref riv, out byte[] parsed, out int consumed, out bool ok, out string err);
            Assert(st == SessionFrame.FrameStatus.FrameReady, "应解析出一帧：" + err);
            Assert(ok, "校验和应匹配");
            Assert(consumed == frame.Length, $"消费字节数不符 {consumed}/{frame.Length}");
            Assert(parsed.Length == original.Length, $"payload 长度不符 {parsed.Length}/{original.Length}");
            Assert(parsed.SequenceEqual(original), "payload 内容不一致");
            Assert(riv == 0x1234_5678 + SessionFrame.IvStep, "接收 IV 应递增");
        }

        private static void FramePipelined()
        {
            uint siv = 0xABCD_0001;
            var frames = new List<byte>();
            var expected = new List<byte[]>();
            for (int i = 0; i < 5; i++)
            {
                byte[] p = new byte[10 + i * 37];
                new Random(100 + i).NextBytes(p);
                expected.Add((byte[])p.Clone());
                frames.AddRange(SessionFrame.BuildFrame(p, p.Length, ref siv));
            }

            byte[] all = frames.ToArray();
            uint riv = 0xABCD_0001;
            int offset = 0;
            int index = 0;
            while (offset < all.Length)
            {
                byte[] window = new byte[all.Length - offset];
                Buffer.BlockCopy(all, offset, window, 0, window.Length);
                SessionFrame.FrameStatus st = SessionFrame.TryExtract(window, window.Length, ref riv, out byte[] parsed, out int consumed, out bool ok, out _);
                Assert(st == SessionFrame.FrameStatus.FrameReady, "应逐帧解析");
                Assert(ok, "校验和应匹配");
                Assert(parsed.SequenceEqual(expected[index]), $"第 {index} 帧内容不一致");
                offset += consumed;
                index++;
            }
            Assert(index == 5, $"应解析出 5 帧，实际 {index}");
        }

        private static void FramePartial()
        {
            uint siv = 0x55AA_0011;
            byte[] payload = new byte[300];
            new Random(7).NextBytes(payload);
            byte[] frame = SessionFrame.BuildFrame(payload, payload.Length, ref siv);

            uint riv = 0x55AA_0011;
            byte[] head = new byte[frame.Length - 1];
            Buffer.BlockCopy(frame, 0, head, 0, head.Length);
            SessionFrame.FrameStatus st = SessionFrame.TryExtract(head, head.Length, ref riv, out _, out _, out _, out _);
            Assert(st == SessionFrame.FrameStatus.NeedMoreData, "半包应返回 NeedMoreData");
            Assert(riv == 0x55AA_0011, "半包不应推进 IV");
        }

        private static void FrameOversized()
        {
            uint riv = 0x1111_1111;
            byte[] bogus = new byte[64];
            // 构造一个解出来为 0xFFFFFFFF 的帧长
            uint header = 0x1111_1111 ^ 0xFFFF_FFFF ^ SessionFrame.HeaderKey;
            Buffer.BlockCopy(BitConverter.GetBytes(header), 0, bogus, 0, 4);
            SessionFrame.FrameStatus st = SessionFrame.TryExtract(bogus, bogus.Length, ref riv, out _, out _, out _, out string err);
            Assert(st == SessionFrame.FrameStatus.ProtocolError, "超长帧应判为协议错误");
            Assert(!string.IsNullOrEmpty(err), "应给出错误原因");
        }

        private static void FramePlaintext()
        {
            byte[] payload = Encoding.UTF8.GetBytes("PcFirstMessage-hello");
            uint siv = 0; // 握手首包：无加密
            byte[] frame = SessionFrame.BuildFrame(payload, payload.Length, ref siv);
            Assert(frame.Length == payload.Length + 4, "明文帧应为 payload + 4");
            Assert(BitConverter.ToInt32(frame, 0) == payload.Length, "明文帧头应为长度");

            uint riv = 0;
            SessionFrame.FrameStatus st = SessionFrame.TryExtract(frame, frame.Length, ref riv, out byte[] parsed, out int consumed, out bool ok, out _);
            Assert(st == SessionFrame.FrameStatus.FrameReady, "明文帧应可解析");
            Assert(consumed == frame.Length, "明文帧消费长度不符");
            Assert(parsed.SequenceEqual(payload), "明文 payload 不一致");
        }

        private static void FrameFuzz()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 2000; i++)
            {
                int len = rnd.Next(0, 400);
                byte[] buf = new byte[len];
                rnd.NextBytes(buf);
                uint riv = (uint)rnd.Next(1, int.MaxValue);
                try
                {
                    SessionFrame.TryExtract(buf, len, ref riv, out _, out _, out _, out _);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"随机输入抛异常: {ex.GetType().Name}");
                }
            }
        }

        private static void PacketHashMatches()
        {
            foreach (var pair in PacketHash.All)
            {
                uint expected = Adler32Helper.GenerateAdler32_ASCII(pair.Key, 0);
                Assert(pair.Value == expected, $"包名 {pair.Key} 哈希不一致: 0x{pair.Value:X8} != 0x{expected:X8}");
            }
            Assert(PacketHash.All.Count > 100, $"哈希表项过少：{PacketHash.All.Count}");
        }

        private static void ProfileCacheHit()
        {
            string root = CreateTempProfileRoot("cachehit");
            try
            {
                long readsBefore = Interlocked.Read(ref Metrics.ProfileDiskReads);
                ProfileConfig a = ProfileService.GetProfileConfig("Tester");
                ProfileConfig b = ProfileService.GetProfileConfig("Tester");
                long readsAfter = Interlocked.Read(ref Metrics.ProfileDiskReads);
                Assert(ReferenceEquals(a, b), "同一昵称应返回同一缓存实例");
                Assert(readsAfter - readsBefore <= 1, $"两次读取最多读盘一次，实际 {readsAfter - readsBefore}");

                a.Rider.Lucci = 12345;
                ProfileService.Save("Tester", a);
                ProfileService.Forget("Tester");
                ProfileConfig c = ProfileService.GetProfileConfig("Tester");
                Assert(c.Rider.Lucci == 12345, "保存后重新加载应读到新值");
            }
            finally
            {
                RestoreProfileRoot(root);
            }
        }

        private static void ProfileNoopSave()
        {
            string root = CreateTempProfileRoot("noop");
            try
            {
                ProfileConfig cfg = ProfileService.GetProfileConfig("NoopUser");
                ProfileService.Save("NoopUser", cfg);
                long writesBefore = Interlocked.Read(ref Metrics.ProfileDiskWrites);
                ProfileService.Save("NoopUser", cfg);
                ProfileService.Save("NoopUser", cfg);
                long writesAfter = Interlocked.Read(ref Metrics.ProfileDiskWrites);
                Assert(writesAfter == writesBefore, $"内容未变不应重复写盘，实际多写 {writesAfter - writesBefore} 次");
            }
            finally
            {
                RestoreProfileRoot(root);
            }
        }

        private static void ProfileCorruptGuard()
        {
            string root = CreateTempProfileRoot("corrupt");
            try
            {
                fileName file = FileName.Load("Broken");
                string original = "{ this is not json";
                File.WriteAllText(file.config_path, original);

                ProfileConfig cfg = ProfileService.GetProfileConfig("Broken");
                Assert(cfg != null, "损坏档案应返回可用对象而不是 null");

                cfg.Rider.Lucci = 999;
                ProfileService.Save("Broken", cfg);

                string after = File.ReadAllText(file.config_path);
                Assert(after == original, "损坏档案在未能成功加载时不得被覆盖");
            }
            finally
            {
                RestoreProfileRoot(root);
            }
        }

        private static void AtomicWriteClean()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kr_atomic_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "a.json");
                JsonHelper.WriteAllTextAtomic(path, "{\"x\":1}");
                JsonHelper.WriteAllTextAtomic(path, "{\"x\":2}");
                Assert(File.ReadAllText(path).Contains("\"x\":2"), "原子写应更新内容");
                Assert(!File.Exists(path + ".tmp"), "不应残留 .tmp");
                Assert(File.Exists(path), "配置应写入成功");
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private static void FileNameConcurrent()
        {
            var errors = new ConcurrentBag<Exception>();
            Parallel.For(0, 64, i =>
            {
                try
                {
                    for (int k = 0; k < 200; k++)
                    {
                        fileName f = FileName.Load("User" + (i % 8));
                        Assert(f != null && !string.IsNullOrEmpty(f.config_path), "FileName 注册失败");
                        FileName.FileNames.TryGetValue("User" + (i % 8), out _);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });
            Assert(errors.IsEmpty, "并发注册抛异常: " + errors.FirstOrDefault()?.Message);
        }

        /// <summary>
        /// 回归用例：换频道会新开连接并用 UserNO 重新握手，
        /// 所以断线清理绝不能把 UserNO ↔ 昵称 映射一起清掉。
        /// </summary>
        private static void UserNoSurvivesDisconnect()
        {
            const string nick = "PersistUser";
            uint no = ClientManager.GetUserNO(nick);
            Assert(no != 0, "UserNO 不应为 0");
            Assert(ClientManager.GetNickname(no) == nick, "初始映射应可反查");

            // 模拟旧连接断开时的会话缓存清理
            ClientManager.CleanupSessionCaches(nick);

            Assert(ClientManager.GetNickname(no) == nick,
                "断线清理后 UserNO 仍必须能反查昵称，否则换频道握手会失败并卡死");
            Assert(ClientManager.GetUserNO(nick) == no,
                "同一昵称必须复用同一个 UserNO（客户端保存的是它）");
        }

        /// <summary>
        /// 回归用例：ModelMax 表必须按 int 主键命中（原实现用 (short) 转换，id&gt;32767 会失配），
        /// 缺失 id 返回 false 由调用方回退 0。
        /// </summary>
        private static void ModelMaxTableLookup()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kr_modelmax_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "ModelMax.xml");
            File.WriteAllText(path,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><ModelMax>"
                + "<kart id='1500' name='A' modelMaxX='0.771' modelMaxY='1.05' />"
                + "<kart id='40000' name='B' modelMaxX='0.9' modelMaxY='1.2' />"
                + "</ModelMax>", new UTF8Encoding(false));

            string old = FileName.ModelMax_LoadFile;
            FileName.ModelMax_LoadFile = path;
            ModelMaxTable.Invalidate();
            try
            {
                Assert(ModelMaxTable.TryGet(1500, out float x, out float y), "应命中 id=1500");
                Assert(Math.Abs(x - 0.771f) < 1e-4f && Math.Abs(y - 1.05f) < 1e-4f, $"值不一致: {x}/{y}");
                Assert(ModelMaxTable.TryGet(40000, out _, out _), "id>32767 也必须命中（原实现 short 转换会失配）");
                Assert(!ModelMaxTable.TryGet(9999, out _, out _), "缺失 id 应返回 false，由调用方回退 0");
            }
            finally
            {
                FileName.ModelMax_LoadFile = old;
                ModelMaxTable.Invalidate();
            }
        }

        /// <summary>
        /// 回归用例：词条→道具映射必须能被配置文件覆盖（不改代码即可对齐客户端文案），
        /// 且未配置的词条不能凭空生效。
        /// </summary>
        /// <summary>
        /// 回归用例：客户端 firing2Gain = 主动生道具（使用后获得）→ OnUse；
        /// fired2Gain = 被动生道具（被击后获得）→ OnAttacked。
        /// v99 曾把两者接反：黯泣V1"使用导弹得磁铁"完全不触发、货拉拉迅 变成必定获得。
        /// </summary>
        private static void KartRuleTableDirection()
        {
            ClientItemRules.ApplyFiring2Gain(System.Xml.Linq.XDocument.Parse(
                "<firing2Gain>" +
                "<item kartId='900001' firingItemIdx='rocket' firingStep='1' gainItemIdx='magnet' probability='100'/>" +
                "<item kartId='900001' firingItemIdx='waterFly' firingStep='1' gainItemIdx='shield' probability='33'/>" +
                "</firing2Gain>"), false);

            Assert(ClientItemRules.TryGet(900001, out ClientItemRules.KartRule useRule), "firing2Gain 未建立规则");
            Assert(useRule.OnUse.Count == 2, $"firing2Gain 应写入 OnUse，实际 OnUse={useRule.OnUse.Count}");
            Assert(useRule.OnAttacked.Count == 0, $"firing2Gain 不应写入 OnAttacked，实际 {useRule.OnAttacked.Count}");
            Assert(ClientItemRules.TryOnUse(900001, 7, out short useTo) && useTo == 5, "使用导弹应得磁铁");
            Assert(!ClientItemRules.TryOnAttacked(900001, 7, out _), "使用后获得不应出现在被攻击查询里");
            Assert(useRule.OnUse.Any(r => r.From == 4 && r.Probability == 33), "firing2Gain 的 probability 应原样保留");

            ClientItemRules.ApplyFired2Gain(System.Xml.Linq.XDocument.Parse(
                "<fired2Gain>" +
                "<item kartId='900002' firedItemIdx='magnet' gainItemIdx='thunderbolt' probability='100'/>" +
                "</fired2Gain>"), false);

            Assert(ClientItemRules.TryGet(900002, out ClientItemRules.KartRule hitRule), "fired2Gain 未建立规则");
            Assert(hitRule.OnAttacked.Count == 1, $"fired2Gain 应写入 OnAttacked，实际 OnAttacked={hitRule.OnAttacked.Count}");
            Assert(hitRule.OnUse.Count == 0, $"fired2Gain 不应写入 OnUse，实际 {hitRule.OnUse.Count}");
            Assert(ClientItemRules.TryOnAttacked(900002, 5, out short hitTo) && hitTo == 111, "被磁铁击中应得闪电");
            Assert(!ClientItemRules.TryOnUse(900002, 5, out _), "被击后获得不应出现在使用后查询里");
        }

        /// <summary>
        /// 回归用例：item.kml 在 rho5、item/slot 的规则表在 rho，运行时表一定先加载。
        /// v99/v2 的 Load() 开头有 Rules.Clear()，会把表规则整体冲掉，
        /// 表现为"黯泣V1 使用导弹不触发、货拉拉 迅 必定获得"。这里按真实顺序复现一遍。
        /// </summary>
        private static void KartRuleLoadOrder()
        {
            const int kartId = 900003;

            // 1) 先按真实顺序加载规则表（firing → OnUse）
            ClientItemRules.ApplyFiring2Gain(System.Xml.Linq.XDocument.Parse(
                "<firing2Gain><item kartId='900003' firingItemIdx='rocket' firingStep='1' gainItemIdx='magnet' probability='33'/></firing2Gain>"),
                false);

            // 2) 再加载 item.kml（文本里也有同一条 100% 规则）
            string kml = "<itemtable><item itemCatId='3' itemId='900003' itemName='测试车' " +
                         "itemEffect='使用[导弹]时获得[磁铁]'/></itemtable>";
            ClientItemRules.Load(Encoding.Unicode.GetBytes(kml));

            // 3) 表规则必须还在，且概率仍是表里的 33（不能被文本的 100 覆盖）
            Assert(ClientItemRules.TryGet(kartId, out ClientItemRules.KartRule rule), "规则丢失");
            Assert(rule.OnUse.Count == 1, $"OnUse 应保留表里的 1 条，实际 {rule.OnUse.Count}");
            Assert(rule.OnUse[0].From == 7 && rule.OnUse[0].To == 5, "导弹→磁铁 规则丢失");
            Assert(rule.OnUse[0].Probability == 33, $"概率应保留客户端表的 33，实际 {rule.OnUse[0].Probability}");
            Assert(rule.OnUse[0].FromClientTable, "该条应标记为来自客户端表");
        }

        private static void ItemRuleMatchesSpec()
        {
            foreach (byte gameType in new byte[] { 2, 4 })
            {
                IReadOnlyDictionary<short, int[]> table = ItemRule.Table(gameType);
                for (int group = 0; group < 4; group++)
                {
                    int sum = 0;
                    foreach (KeyValuePair<short, int[]> kv in table)
                    {
                        sum += kv.Value[group];
                    }
                    Assert(sum == 1000, $"模式 {gameType} 第 {group} 档概率合计应为 1000‰，实际 {sum}‰");
                }
            }

            Assert(ItemRule.Group(8, 1) == RankGroup.Top, "8 人第 1 名应为 1位");
            Assert(ItemRule.Group(8, 2) == RankGroup.High, "8 人第 2 名应为上位");
            Assert(ItemRule.Group(8, 4) == RankGroup.Mid, "8 人第 4 名应为中位");
            Assert(ItemRule.Group(8, 8) == RankGroup.Low, "8 人第 8 名应为下位");
            Assert(ItemRule.Group(4, 2) == RankGroup.Mid, "4 人第 2 名应为中位");
            Assert(ItemRule.Group(3, 3) == RankGroup.Low, "3 人第 3 名应为下位");
            Assert(ItemRule.Group(2, 2) == RankGroup.Mid, "2 人第 2 名应为中位");

            for (int i = 0; i < 300; i++)
            {
                short teamItem = ItemRule.Roll(4, 8, 1, excludeRare: true);
                Assert(!ItemRule.IsRare(teamItem), $"排除稀有道具后仍抽到 {teamItem}");
            }

            var room = new GameRoom(9999);
            // 里程数据不全（AI/他人不上报）时必须以客户端名次为准，不能恒为第 1
            Assert(ItemRule.ComputeRank(room, 1, 6, 8) == 7, "无里程数据时应回退客户端名次（8 人第 7 名）");
            Assert(ItemRule.ComputeRank(room, 1, 0, 8) == 1, "客户端报第 1 名时应为 1");
            Assert(ItemRule.Group(8, ItemRule.ComputeRank(room, 1, 6, 8)) == RankGroup.Low, "8 人第 7 名应为下位");
            Assert(room.TryConsumeRare(1), "第 1 个稀有道具应允许");
            Assert(room.TryConsumeRare(1), "第 2 个稀有道具应允许");
            Assert(!room.TryConsumeRare(1), "第 3 个稀有道具应被拒绝");
            room.ResetRareItems();
            Assert(room.RareCount(1) == 0, "重置后计数应为 0");
        }

        private static void PacketRingWorks()
        {
            PacketRing.Clear();
            byte[] data = new byte[64];
            for (int i = 0; i < 5; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(0x1000u + (uint)i), 0, data, 0, 4);
                PacketRing.Add(0, 0x1000u + (uint)i, "Tester", data, 64);
            }

            List<string> all = PacketRing.Snapshot(10);
            Assert(all.Count == 5, $"应记录 5 条，实际 {all.Count}");
            Assert(all[0].Contains("0x00001000"), "应包含 opcode 十六进制");
            Assert(all[0].Contains("Tester"), "应包含昵称");

            Assert(PacketRing.Snapshot(10, null, "S").Count == 0, "方向过滤应生效（本次全是 C）");
            Assert(PacketRing.Snapshot(10, null, "C").Count == 5, "方向过滤 C 应为 5 条");

            PacketRing.Clear();
            Assert(PacketRing.Snapshot(10).Count == 0, "清空后应为空");
        }

        private static void SlotSetConcurrent()
        {
            var set = new SlotIdSet();
            var writer = Task.Run(() =>
            {
                for (int i = 0; i < 2000; i++)
                {
                    set.Add((byte)(i % 8));
                }
            });
            var reader = Task.Run(() =>
            {
                for (int i = 0; i < 2000; i++)
                {
                    foreach (byte b in set.Snapshot())
                    {
                        _ = b;
                    }
                    _ = set.Count;
                }
            });
            Task.WaitAll(writer, reader);
            Assert(set.Count <= 8, "槽位集合元素数异常");
            set.Clear();
            Assert(set.Count == 0, "Clear 后应为空");
        }

        private static string CreateTempProfileRoot(string tag)
        {
            string old = FileName.ProfileDir;
            string dir = Path.Combine(Path.GetTempPath(), $"kr_profile_{tag}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            FileName.ProfileDir = dir;
            return dir + "|" + old;
        }

        private static void RestoreProfileRoot(string packed)
        {
            string[] parts = packed.Split('|');
            try
            {
                if (Directory.Exists(parts[0]))
                {
                    Directory.Delete(parts[0], true);
                }
            }
            catch
            {
            }
            FileName.ProfileDir = parts[1];
            foreach (string key in FileName.FileNames.Keys.Where(k => k.StartsWith("User", StringComparison.Ordinal) || k == "Tester" || k == "NoopUser" || k == "Broken").ToList())
            {
                FileName.FileNames.TryRemove(key, out _);
            }
            ProfileService.Forget("Tester");
            ProfileService.Forget("NoopUser");
            ProfileService.Forget("Broken");
        }
    }
}
