using System;
using ExcData;
using Profile;

namespace KartRider
{
    public class GameDataReset
    {
        public static void DataReset(string Nickname)
        {
            var resetConfig = ProfileService.GetProfileConfig(Nickname);
            if (resetConfig?.Rider == null)
            {
                Console.WriteLine("[DataReset] Warning: ProfileConfig or Rider is null for {0}", Nickname);
                return;
            }
            // 注意: Lucci/RP/Koin/Cash/TcCash 均为 uint 类型，合法范围 0 ~ 4294967295
            if (resetConfig.Rider.Lucci > SessionGroup.LucciMax)
            {
                resetConfig.Rider.Lucci = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.RP > SessionGroup.LucciMax)
            {
                resetConfig.Rider.RP = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.Koin > SessionGroup.LucciMax)
            {
                resetConfig.Rider.Koin = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.Cash > SessionGroup.LucciMax)
            {
                resetConfig.Rider.Cash = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.TcCash > SessionGroup.LucciMax)
            {
                resetConfig.Rider.TcCash = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.SlotChanger > short.MaxValue || resetConfig.Rider.SlotChanger == 1)
            {
                resetConfig.Rider.SlotChanger = (ushort)short.MaxValue;
            }
            ProfileService.Save(Nickname, resetConfig);
            SpeedPatch.SpeedPatcData();
            //GameSupport.PrLogin();
            Console.WriteLine("Login...OK");
        }
    }
}