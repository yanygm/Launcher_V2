using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using KartRider.IO.Packet;
using Profile;

namespace KartRider
{
    public class Reward
    {
        public uint StockId { get; set; }
        public uint Probability { get; set; }

        public Reward(uint stockId, uint probability)
        {
            StockId = stockId;
            Probability = probability;
        }
    }

    /**
     * Bingo进度存档结构（保存到 昵称目录/Bingo.json）
     */
    public class BingoSaveData
    {
        public byte BingoItem { get; set; }
        public byte BingoNum { get; set; }
        public short BingoCount { get; set; }
        public List<BingoNumSave> Nums { get; set; } = new List<BingoNumSave>();
        public List<BingoItemSave> Items { get; set; } = new List<BingoItemSave>();
        public List<string> CompletedLines { get; set; } = new List<string>();
    }

    public class BingoNumSave
    {
        public byte Num { get; set; }
        public byte Obtained { get; set; }
    }

    public class BingoItemSave
    {
        public int Item { get; set; }
        public byte Obtained { get; set; }
    }

    /**
     * 单个玩家的Bingo数据（每个昵称一份，互不干扰，存到 昵称目录/Bingo.json）
     */
    public class BingoData
    {
        // 共享随机数生成器（Random非线程安全，加锁使用）
        private static readonly Random RandomGen = new Random();
        private static readonly object RandomLock = new object();

        private static int NextRandom(int minValue, int maxValue)
        {
            lock (RandomLock)
            {
                return RandomGen.Next(minValue, maxValue);
            }
        }

        public string Nickname = "";
        public byte BingoItem = 0;
        public byte BingoNum = 0;
        public short BingoCount = 0;
        public Dictionary<byte, byte> BingoNums = new Dictionary<byte, byte>();
        public List<byte> BingoNumsList = new List<byte>();
        public Dictionary<int, byte> BingoItems = new Dictionary<int, byte>();
        public List<int> BingoItemsList = new List<int>();
        // 已连成的连线（Key为该连线5个数字排序后的组合），用于跳过重复的控制台输出
        public HashSet<string> CompletedLines = new HashSet<string>();

        public BingoData(string nickname)
        {
            Nickname = nickname ?? "";
        }

        /**
         * 重置Bingo面板（数字、道具、已连成的连线）
         */
        public void Reset()
        {
            BingoItem = 0;
            BingoNum = 0;
            BingoCount = 0;
            BingoNums = new Dictionary<byte, byte>();
            BingoNumsList = new List<byte>();
            BingoItems = new Dictionary<int, byte>();
            BingoItemsList = new List<int>();
            CompletedLines = new HashSet<string>();
        }

        /**
         * 保存当前Bingo进度到 昵称目录/Bingo.json
         */
        public void Save()
        {
            if (string.IsNullOrEmpty(Nickname)) return;
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            var data = new BingoSaveData
            {
                BingoItem = BingoItem,
                BingoNum = BingoNum,
                BingoCount = BingoCount,
                Nums = BingoNumsList.Select(num => new BingoNumSave
                {
                    Num = num,
                    Obtained = BingoNums.TryGetValue(num, out byte numState) ? numState : (byte)0
                }).ToList(),
                Items = BingoItemsList.Select(item => new BingoItemSave
                {
                    Item = item,
                    Obtained = BingoItems.TryGetValue(item, out byte itemState) ? itemState : (byte)0
                }).ToList(),
                CompletedLines = CompletedLines.ToList()
            };

            string dir = Path.GetDirectoryName(filename.Bingo_LoadFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filename.Bingo_LoadFile, JsonHelper.Serialize(data));
        }

        /**
         * 从 昵称目录/Bingo.json 加载Bingo进度，文件不存在则保持空面板
         */
        public void Load()
        {
            if (string.IsNullOrEmpty(Nickname)) return;
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];

            Reset();

            if (!File.Exists(filename.Bingo_LoadFile)) return;

            BingoSaveData data = null;
            try
            {
                data = JsonHelper.DeserializeNoBom<BingoSaveData>(filename.Bingo_LoadFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bingo] 读取进度失败：{filename.Bingo_LoadFile} - {ex.Message}");
            }
            if (data == null) return;

            BingoItem = data.BingoItem;
            BingoNum = data.BingoNum;
            BingoCount = data.BingoCount;

            if (data.Nums != null)
            {
                foreach (var numSave in data.Nums)
                {
                    if (!BingoNums.ContainsKey(numSave.Num))
                    {
                        BingoNumsList.Add(numSave.Num);
                    }
                    BingoNums[numSave.Num] = numSave.Obtained;
                }
            }

            if (data.Items != null)
            {
                foreach (var itemSave in data.Items)
                {
                    if (!BingoItems.ContainsKey(itemSave.Item))
                    {
                        BingoItemsList.Add(itemSave.Item);
                    }
                    BingoItems[itemSave.Item] = itemSave.Obtained;
                }
            }

            if (data.CompletedLines != null)
            {
                foreach (string line in data.CompletedLines)
                {
                    CompletedLines.Add(line);
                }
            }

            Console.WriteLine($"[Bingo] 已加载 {Nickname} 的Bingo进度：格子={BingoNumsList.Count} 道具={BingoItemsList.Count} 已连线={CompletedLines.Count}");
        }

        public void BingoNumber()
        {
            // 生成新面板时清空之前的连线记录
            CompletedLines.Clear();

            // 存储不重复随机数的集合
            HashSet<byte> uniqueNumbers = new HashSet<byte>();

            // 生成25个不重复的随机数
            while (uniqueNumbers.Count < 25)
            {
                // 生成1到50之间的随机数
                byte number = (byte)NextRandom(1, 50);

                // 只有当集合中不包含该数字时才会添加成功
                uniqueNumbers.Add(number);
            }
            foreach (byte num in uniqueNumbers)
            {
                BingoNumsList.Add(num);
                BingoNums.TryAdd(num, 0);
            }
        }

        public void SpRpLotteryPacket(SessionGroup Parent, ushort lotteryId)
        {
            Lottery.TryGet(lotteryId, out LotteryManager lotteryManager);
            if (lotteryManager == null)
            {
                Console.WriteLine($"抽奖数据尚未初始化，无法执行抽奖（LotteryId: {lotteryId}）");
                return;
            }
            uint stock1 = lotteryManager.GetRandomStockIds(1)[0];
            if (BingoNums.Count == 0 && BingoNumsList.Count == 0)
            {
                BingoNumber();
                if (BingoItems.Count == 0 && BingoItemsList.Count == 0)
                {
                    var srocks = lotteryManager.GetRandomStockIds(12);
                    foreach (int stock in srocks)
                    {
                        BingoItemsList.Add(stock);
                        BingoItems.TryAdd(stock, 0);
                    }
                }
            }
            // 每次都是1~49随机，允许抽到重复数字
            BingoNum = (byte)NextRandom(1, 50);
            using (OutPacket outPacket = new OutPacket("SpRpLotteryPacket"))
            {
                outPacket.WriteInt(0);
                outPacket.WriteUInt(stock1);
                outPacket.WriteHexString("FFFFFFFF");
                outPacket.WriteByte(0);
                outPacket.WriteByte(BingoNum);
                outPacket.WriteBytes(new byte[11]);
                Parent.Client.Send(outPacket);
            }
            Stock.GetStockItem(Parent, stock1);
            Stock.DelNewItem(Parent.Client.Nickname, 24, lotteryId, 1);
            // 只有"未点亮 -> 点亮"才会连成新线；抽到重复数字或面板外的数字时不再做连线判定，避免重复连线动画
            if (BingoNums.TryGetValue(BingoNum, out byte numState) && numState == 0)
            {
                BingoNums[BingoNum] = 1;
                List<int> newLines = CheckLinesAsArray();
                if (newLines.Count > 0)
                {
                    Console.WriteLine($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{Nickname}] 本次新连成连线={string.Join(",", newLines)} 道具={string.Join(",", newLines.Where(i => i < BingoItemsList.Count).Select(i => BingoItemsList[i]))}");
                }
            }
            else if (BingoNums.ContainsKey(BingoNum))
            {
                Console.WriteLine($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{Nickname}] 数字={BingoNum} 已点亮过，跳过连线判定");
            }
            BingoCount++;
            Save();
            Console.WriteLine($"[Bingo {DateTime.Now:HH:mm:ss.fff}] [{Nickname}] 抽奖 数字={BingoNum} 次数={BingoCount} 已点亮={BingoNums.Count(n => n.Value == 1)}/{BingoNumsList.Count} 已连线={CompletedLines.Count}");
        }

        /**
         * 12条连线对应的格子索引（每行5个数字）
         * 0-4：横线（0是最上方，4是最下方）
         * 5：右上到左下对角线
         * 6-10：竖线（6是最左方，10是最右方）
         * 11：左上到右下对角线
         */
        public static List<int[]> GetLineIndexes()
        {
            List<int[]> lines = new List<int[]>();

            // 横线(0-4)
            for (int row = 0; row < 5; row++)
                lines.Add(Enumerable.Range(0, 5).Select(col => row * 5 + col).ToArray());

            // 右上到左下对角线(5)：(0,4), (1,3), (2,2), (3,1), (4,0)
            lines.Add(Enumerable.Range(0, 5).Select(i => i * 5 + (4 - i)).ToArray());

            // 竖线(6-10)
            for (int col = 0; col < 5; col++)
                lines.Add(Enumerable.Range(0, 5).Select(row => row * 5 + col).ToArray());

            // 左上到右下对角线(11)：(0,0), (1,1), (2,2), (3,3), (4,4)
            lines.Add(Enumerable.Range(0, 5).Select(i => i * 5 + i).ToArray());

            return lines;
        }

        /**
         * 检测连线，返回本次"新"连成的连线索引
         * 已连成过的连线直接跳过，不再重复置道具状态、不再重复输出，避免重复触发连线动画
         */
        public List<int> CheckLinesAsArray()
        {
            List<int> newLines = new List<int>();

            // 验证字典包含25个元素
            if (BingoNums == null || BingoNumsList.Count < 25)
            {
                Console.WriteLine("Bingo宫格必须包含25个数字");
                return newLines;
            }

            List<int[]> lines = GetLineIndexes();

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                byte[] numbers = lines[lineIndex].Select(index => BingoNumsList[index]).ToArray();

                // 5个格子全部已点亮才算连成（0 = 未点亮，1 = 已点亮）
                bool isCompleted = numbers.All(number => BingoNums.TryGetValue(number, out byte state) && state == 1);

                // 该连线的Key（5个数字排序后拼接，与格子顺序无关）
                string lineKey = string.Join(",", numbers.OrderBy(number => number));

                // 已经连成过的连线不再重复处理
                if (CompletedLines.Contains(lineKey)) continue;

                if (!isCompleted) continue;

                CompletedLines.Add(lineKey);
                newLines.Add(lineIndex);

                // 同步连线对应的道具状态（保持原有0/1数据格式）
                if (lineIndex < BingoItemsList.Count)
                    BingoItems[BingoItemsList[lineIndex]] = 1;

                Console.WriteLine($"[{Nickname}] " + string.Join(" ", numbers));
            }

            return newLines;
        }
    }

    /**
     * Bingo数据管理器：按昵称缓存各自独立的BingoData，互不干扰
     */
    public static class Bingo
    {
        private static readonly Dictionary<string, BingoData> Datas = new Dictionary<string, BingoData>();
        private static readonly object SyncRoot = new object();

        /**
         * 获取指定昵称的Bingo数据（首次获取时自动从 昵称目录/Bingo.json 加载）
         * 昵称为空时返回临时对象（不缓存、不落盘），避免空引用
         */
        public static BingoData Get(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return new BingoData("");
            }
            lock (SyncRoot)
            {
                if (Datas.TryGetValue(nickname, out BingoData existing)) return existing;
                BingoData data = new BingoData(nickname);
                Datas[nickname] = data;
                data.Load();
                return data;
            }
        }

        /**
         * 登录时预加载指定昵称的Bingo进度
         */
        public static void LoadProgress(string nickname)
        {
            Get(nickname);
        }

        /**
         * 保存指定昵称的Bingo进度
         */
        public static void SaveProgress(string nickname)
        {
            Get(nickname).Save();
        }

        /**
         * 抽奖入口：取当前昵称自己的数据再处理
         */
        public static void SpRpLotteryPacket(SessionGroup Parent, ushort lotteryId)
        {
            Get(Parent.Client.Nickname).SpRpLotteryPacket(Parent, lotteryId);
        }

        /**
         * 昵称变更时把缓存迁到新昵称下并重新保存
         */
        public static void MigrateNickname(string oldNickname, string newNickname)
        {
            if (string.IsNullOrEmpty(oldNickname) || string.IsNullOrEmpty(newNickname)) return;
            if (oldNickname == newNickname) return;
            lock (SyncRoot)
            {
                if (!Datas.TryGetValue(oldNickname, out BingoData data)) return;
                Datas.Remove(oldNickname);
                data.Nickname = newNickname;
                Datas[newNickname] = data;
                data.Save();
            }
        }

        /**
         * 释放指定昵称的缓存（下次获取时重新从文件加载）
         */
        public static void Unload(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return;
            lock (SyncRoot)
            {
                if (Datas.TryGetValue(nickname, out BingoData data))
                {
                    data.Save();
                    Datas.Remove(nickname);
                }
            }
        }
    }
}
