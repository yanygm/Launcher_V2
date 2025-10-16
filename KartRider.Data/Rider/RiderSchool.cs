using System;
using KartRider.IO.Packet;
using KartRider;
using ExcData;

namespace RiderData
{
    public static class RiderSchool
    {
        public static void PrStartRiderSchool(SessionGroup Parent)
        {
            SchoolSpec.DefaultSpec();
            using (OutPacket oPacket = new OutPacket("PrStartRiderSchool"))
            {
                oPacket.WriteByte(1);
                StartGameData.GetSchoolSpac(oPacket);
                Parent.Client.Send(oPacket);
            }
        }
    }
}
