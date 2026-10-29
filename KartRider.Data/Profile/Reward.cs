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