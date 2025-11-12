using Launcher.App.KartSpec;
using Launcher.App.Server;
using Launcher.Library.IO;

namespace Launcher.App.Rider
{
    public static class RiderSchool
    {
        public static void PrStartRiderSchool()
        {
            SchoolSpec.DefaultSpec();
            using (OutPacket oPacket = new OutPacket("PrStartRiderSchool"))
            {
                oPacket.WriteByte(1);
                StartGameData.GetSchoolSpac(oPacket);
                RouterListener.MySession.Client.Send(oPacket);
            }
        }
    }
}