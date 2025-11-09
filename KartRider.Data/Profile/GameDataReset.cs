using System;
using ExcData;
using Profile;

namespace KartRider
{
    public class GameDataReset
    {
        public static void DataReset(string Nickname)
        {
            if (ProfileService.ProfileConfigs[Nickname].Rider.Lucci > uint.MaxValue)
            {
                ProfileService.ProfileConfigs[Nickname].Rider.Lucci = SessionGroup.LucciMax;
            }
            ProfileService.Save(Nickname);
            SpeedPatch.SpeedPatcData();
            //GameSupport.PrLogin();
            Console.WriteLine("Login...OK");
        }
    }
}
