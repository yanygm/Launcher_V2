using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using KartRider.IO.Packet;

namespace KartRider
{
    public static class Lottery
    {
        public static Dictionary<ushort, LotteryManager> LotteryList = new Dictionary<ushort, LotteryManager>();

        /**
         * 获取指定Id的抽奖管理器，不存在时创建
         * @param lotteryId 抽奖Id
         */
        public static LotteryManager GetOrCreate(ushort lotteryId)
        {
            if (!LotteryList.TryGetValue(lotteryId, out LotteryManager lotteryManager))
            {
                lotteryManager = new LotteryManager(lotteryId);
                LotteryList[lotteryId] = lotteryManager;
            }
            return lotteryManager;
        }

        /**
         * 尝试获取指定Id的抽奖管理器
         * @param lotteryId 抽奖Id
         * @param lotteryManager 输出的抽奖管理器
         */
        public static bool TryGet(ushort lotteryId, out LotteryManager lotteryManager)
        {
            return LotteryList.TryGetValue(lotteryId, out lotteryManager);
        }

        public static void SpRpLotteryPacket(SessionGroup Parent, ushort LotteryID)
        {
            TryGet(LotteryID, out LotteryManager lotteryManager);
            if (lotteryManager != null)
            {
                using (OutPacket outPacket = new OutPacket("SpRpLotteryPacket"))
                {
                    outPacket.WriteInt(0);
                    int stock = lotteryManager.GetRandomStockIds(1)[0];
                    outPacket.WriteInt(stock);
                    outPacket.WriteHexString("FF FF FF FF");
                    outPacket.WriteBytes(new byte[13]);
                    Parent.Client.Send(outPacket);
                }
                Stock.DelNewItem(Parent.Client.Nickname, 24, LotteryID, 1);
            }
            else
            {
                using (OutPacket outPacket = new OutPacket("SpRpLotteryPacket"))
                {
                    outPacket.WriteHexString("05 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
                    Parent.Client.Send(outPacket);
                }
            }
        }
    }

    public class LotteryManager
    {
        // 当前抽奖管理器的Id
        public ushort LotteryId { get; }

        // 存储所有奖励项及其概率
        public List<Reward> RewardList { get; } = new List<Reward>();

        // 总概率，用于计算抽取概率
        public int TotalProbability { get; private set; }

        public LotteryManager(ushort lotteryId)
        {
            this.LotteryId = lotteryId;
        }

        /**
         * 初始化方法，读取XML文件并加载数据
         * @param rewardNodes 奖励节点集合
         * @param writeLog 是否输出加载日志（批量加载时可关闭以避免刷屏）
         */
        public void Initialize(XmlNodeList rewardNodes, bool writeLog = true)
        {
            try
            {
                // 清空现有数据
                RewardList.Clear();
                TotalProbability = 0;

                // 遍历所有奖励项
                foreach (XmlNode node in rewardNodes)
                {
                    XmlElement rewardElement = node as XmlElement;
                    if (rewardElement == null) continue;

                    // 获取stockId和概率
                    if (int.TryParse(rewardElement.GetAttribute("stockId"), out int stockId) &&
                        int.TryParse(rewardElement.GetAttribute("prob"), out int prob))
                    {
                        // 创建奖励对象并添加到列表
                        Reward reward = new Reward(stockId, prob);
                        RewardList.Add(reward);

                        // 累加总概率
                        TotalProbability += prob;
                    }
                }

                if (writeLog)
                    Console.WriteLine($"成功加载 {RewardList.Count} 个奖励项，总概率为: {TotalProbability}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载XML文件出错: {ex.Message}");
            }
        }

        /**
         * 随机获取指定数量的stockId，考虑概率因素
         * @param count 要获取的数量
         * @return 随机选中的stockId列表
         */
        public List<int> GetRandomStockIds(int count)
        {
            List<int> result = new List<int>();

            // 检查是否已初始化
            if (RewardList.Count == 0)
            {
                Console.WriteLine("请先调用Initialize方法加载数据");
                return result;
            }

            // 检查数量是否合理
            if (count <= 0)
            {
                Console.WriteLine("请指定有效的获取数量");
                return result;
            }

            // 获取不重复的奖励ID集合
            HashSet<int> uniqueStockIds = new HashSet<int>(RewardList.Select(r => r.StockId));
            int uniqueRewardCount = uniqueStockIds.Count;

            // 检查请求数量是否超过可用奖励数量
            if (count > uniqueRewardCount)
            {
                Console.WriteLine($"请求数量超过可用的不重复奖励数量，最多只能返回 {uniqueRewardCount} 个结果");
                count = uniqueRewardCount;
            }

            // 创建奖励列表的副本用于操作，避免修改原始数据
            List<Reward> availableRewards = new List<Reward>(RewardList);
            // 使用当前时间作为种子创建Random实例，避免短时间内多次调用产生相同序列
            Random random = new Random(Guid.NewGuid().GetHashCode());

            // 已选中的奖励ID集合，用于双重保证不重复
            HashSet<int> selectedIds = new HashSet<int>();

            // 循环获取指定数量的不重复奖励
            while (selectedIds.Count < count && availableRewards.Count > 0)
            {
                // 计算当前可用奖励的总概率
                int currentTotalProb = availableRewards.Sum(r => r.Probability);
                if (currentTotalProb <= 0) break;

                // 生成0到总概率之间的随机数
                int randomValue = random.Next(0, currentTotalProb);
                int currentSum = 0;
                Reward selectedReward = null;

                // 根据概率分布查找选中的奖励
                foreach (var reward in availableRewards)
                {
                    currentSum += reward.Probability;
                    if (randomValue < currentSum)
                    {
                        selectedReward = reward;
                        break;
                    }
                }

                // 添加选中的奖励ID到结果
                if (selectedReward != null && selectedIds.Add(selectedReward.StockId))
                {
                    result.Add(selectedReward.StockId);
                    // 移除所有相同StockId的奖励，确保不会重复选中
                    availableRewards.RemoveAll(r => r.StockId == selectedReward.StockId);
                }
            }

            // 检查最终结果数量是否符合预期
            if (result.Count < count)
            {
                Console.WriteLine($"警告：实际返回 {result.Count} 个结果，少于请求的 {count} 个");
            }
            return result;
        }
    }
}
