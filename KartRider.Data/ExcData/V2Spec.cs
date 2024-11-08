using KartRider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcData
{
	public class V2Spec
	{
		public static void ExceedSpec()
		{
			if (Kart.defaultExceedType > 0)
			{
				Kart.SteerConstraint = Kart.SteerConstraint + 0.304f;
				Kart.DriftEscapeForce = Kart.DriftEscapeForce + 403f;
				Kart.TransAccelFactor = Kart.TransAccelFactor + 0.45254f;
				if (Kart.defaultExceedType == 1)
				{
					Kart.chargeInstAccelGaugeByBoost = 0.016f;
					Kart.chargeInstAccelGaugeByGrip = 0.06f;
					Kart.chargeInstAccelGaugeByWall = 0.15f;
					Kart.instAccelFactor = 1.3f;
					Kart.instAccelGaugeCooldownTime = 3000;
					Kart.instAccelGaugeLength = 1000f;
					Kart.instAccelGaugeMinUsable = 300f;
					Kart.instAccelGaugeMinVelBound = 0f;
					Kart.instAccelGaugeMinVelLoss = 50f;
					Kart.useExtendedAfterBoosterMore = 1;
					Kart.wallCollGaugeCooldownTime = 3000;
					Kart.wallCollGaugeMaxVelLoss = 200f;
					Kart.wallCollGaugeMinVelBound = 160f;
					Kart.wallCollGaugeMinVelLoss = 50f;
				}
				if (Kart.defaultExceedType == 3)
				{
					Kart.chargeInstAccelGaugeByBoost = 0.02f;
					Kart.chargeInstAccelGaugeByGrip = 0.06f;
					Kart.chargeInstAccelGaugeByWall = 0.15f;
					Kart.instAccelFactor = 1.18f;
					Kart.instAccelGaugeCooldownTime = 3000;
					Kart.instAccelGaugeLength = 2000f;
					Kart.instAccelGaugeMinUsable = 400f;
					Kart.instAccelGaugeMinVelBound = 0f;
					Kart.instAccelGaugeMinVelLoss = 50f;
					Kart.useExtendedAfterBoosterMore = 0;
					Kart.wallCollGaugeCooldownTime = 3000;
					Kart.wallCollGaugeMaxVelLoss = 200f;
					Kart.wallCollGaugeMinVelBound = 200f;
					Kart.wallCollGaugeMinVelLoss = 50f;
				}
			}
		}
	}
}
