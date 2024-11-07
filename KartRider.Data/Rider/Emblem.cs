using System;
using KartRider.IO.Packet;
using KartRider;
using System.Xml;
using ExcData;
using System.Collections.Generic;

namespace RiderData
{
	public static class Emblem
	{
		public static void RmOwnerEmblemPacket()
		{
			int All_Emblem = KartExcData.emblem.Count;
			using (OutPacket outPacket = new OutPacket("RmOwnerEmblemPacket"))
			{
				outPacket.WriteInt(1);
				outPacket.WriteInt(1);
				outPacket.WriteInt(All_Emblem);
				for (int i = 0; i < KartExcData.emblem.Count; i++)
				{
					outPacket.WriteShort(KartExcData.emblem[i]);
				}
				RouterListener.MySession.Client.Send(outPacket);
			}
		}
	}
}