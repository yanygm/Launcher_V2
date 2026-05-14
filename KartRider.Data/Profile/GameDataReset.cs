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
            if (resetConfig.Rider.Lucci > uint.MaxValue)
            {
                resetConfig.Rider.Lucci = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.RP > uint.MaxValue)
            {
                resetConfig.Rider.RP = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.Koin > uint.MaxValue)
            {
                resetConfig.Rider.Koin = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.Cash > uint.MaxValue)
            {
                resetConfig.Rider.Cash = SessionGroup.LucciMax;
            }
            if (resetConfig.Rider.TcCash > uint.MaxValue)
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