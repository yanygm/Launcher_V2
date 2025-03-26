using System;
using KartRider.IO.Packet;
using KartRider;
using ExcData;

namespace RiderData
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