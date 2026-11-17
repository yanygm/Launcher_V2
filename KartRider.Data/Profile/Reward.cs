using System;

namespace KartRider;

public class TimeReward
{
    private static readonly Random _rand = new Random();

    public static (uint RP, uint Lucci) Reward(int playerRanking)
    {
        // 设定分界：排名≤8才有加成，>8、负数全部加成=0
        int baseLine = 8;
        int add = Math.Max(0, baseLine - playerRanking);

        // RP：0~50
        int rpRaw = _rand.Next(0, 51) + add / 3;
        int rp = Math.Clamp(rpRaw, 0, 50);

        // Lucci：0~500
        int lucRaw = _rand.Next(0, 501) + add * 3;
        int luc = Math.Clamp(lucRaw, 0, 500);

        return ((uint)rp, (uint)luc);
    }

    /// <summary>
    /// 挑战赛难度加成百分比：0 简单 = 无加成、1 普通 = +30%、2 困难 = +50%。
    /// 难度值来自客户端 <c>GrChangeCampaignPacket</c> 的最后一字节（见 <see cref="GameRoom.CampaignDifficulty"/>）。
    /// </summary>
    public static int CampaignBonusPercent(byte difficulty)
    {
        switch (difficulty)
        {
            case 1: return 30;
            case 2: return 50;
            default: return 0;
        }
    }

    /// <summary>
    /// 在一份已经算好的结算奖励上按百分比加成（挑战赛难度用）。
    /// 加成基于**原结算结果**，不是重抽一遍，所以难度只放大不改变随机性。
    /// </summary>
    public static (uint RP, uint Lucci) ApplyPercent((uint RP, uint Lucci) reward, int percent)
    {
        if (percent <= 0)
        {
            return reward;
        }

        uint rp = reward.RP * (uint)(100 + percent) / 100;
        uint lucci = reward.Lucci * (uint)(100 + percent) / 100;
        return (rp, lucci);
    }

    public static (uint RP, uint Lucci) FinishReward(byte RewardType)
    {
        if (RewardType == 0)
        {
            return (10, 20);
        }
        else if (RewardType == 1)
        {
            return (20, 50);
        }
        else
        {
            return (0, 0);
        }
    }
}