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
			defaultSpec.DefaultSpec();
			using (OutPacket oPacket = new OutPacket("PrStartRiderSchool"))
			{
				oPacket.WriteByte(1);
				oPacket.WriteEncFloat(Kart.draftMulAccelFactor);
				oPacket.WriteEncInt(Kart.draftTick);
				oPacket.WriteEncFloat(Kart.driftBoostMulAccelFactor);
				oPacket.WriteEncInt(Kart.driftBoostTick);
				oPacket.WriteEncFloat(Kart.chargeBoostBySpeed);
				oPacket.WriteEncByte(Kart.SpeedSlotCapacity);
				oPacket.WriteEncByte(Kart.ItemSlotCapacity);
				oPacket.WriteEncByte(Kart.SpecialSlotCapacity);
				oPacket.WriteEncByte(Kart.UseTransformBooster);
				oPacket.WriteEncByte(Kart.motorcycleType);
				oPacket.WriteEncByte(Kart.BikeRearWheel);
				oPacket.WriteEncFloat(Kart.Mass);
				oPacket.WriteEncFloat(Kart.AirFriction);
				oPacket.WriteEncFloat(Kart.DragFactor);
				oPacket.WriteEncFloat(Kart.ForwardAccelForce);
				oPacket.WriteEncFloat(Kart.BackwardAccelForce);
				oPacket.WriteEncFloat(Kart.GripBrakeForce);
				oPacket.WriteEncFloat(Kart.SlipBrakeForce);
				oPacket.WriteEncFloat(Kart.MaxSteerAngle);
				oPacket.WriteEncFloat(Kart.SteerConstraint);
				oPacket.WriteEncFloat(Kart.FrontGripFactor);
				oPacket.WriteEncFloat(Kart.RearGripFactor);
				oPacket.WriteEncFloat(Kart.DriftTriggerFactor);
				oPacket.WriteEncFloat(Kart.DriftTriggerTime);
				oPacket.WriteEncFloat(Kart.DriftSlipFactor);
				oPacket.WriteEncFloat(Kart.DriftEscapeForce);
				oPacket.WriteEncFloat(Kart.CornerDrawFactor);
				oPacket.WriteEncFloat(Kart.DriftLeanFactor);
				oPacket.WriteEncFloat(Kart.SteerLeanFactor);
				oPacket.WriteEncFloat(Kart.DriftMaxGauge);
				oPacket.WriteEncFloat(Kart.NormalBoosterTime);
				oPacket.WriteEncFloat(Kart.ItemBoosterTime);
				oPacket.WriteEncFloat(Kart.TeamBoosterTime);
				oPacket.WriteEncFloat(Kart.AnimalBoosterTime);
				oPacket.WriteEncFloat(Kart.SuperBoosterTime);
				oPacket.WriteEncFloat(Kart.TransAccelFactor);
				oPacket.WriteEncFloat(Kart.BoostAccelFactor);
				oPacket.WriteEncFloat(Kart.StartBoosterTimeItem);
				oPacket.WriteEncFloat(Kart.StartBoosterTimeSpeed);
				oPacket.WriteEncFloat(Kart.StartForwardAccelForceItem);
				oPacket.WriteEncFloat(Kart.StartForwardAccelForceSpeed);
				oPacket.WriteEncFloat(Kart.DriftGaguePreservePercent);
				oPacket.WriteEncByte(Kart.UseExtendedAfterBooster);
				oPacket.WriteEncFloat(Kart.BoostAccelFactorOnlyItem);
				oPacket.WriteEncFloat(Kart.antiCollideBalance);
				oPacket.WriteEncByte(Kart.dualBoosterSetAuto);
				oPacket.WriteEncInt(Kart.dualBoosterTickMin);
				oPacket.WriteEncInt(Kart.dualBoosterTickMax);
				oPacket.WriteEncFloat(Kart.dualMulAccelFactor);
				oPacket.WriteEncFloat(Kart.dualTransLowSpeed);
				oPacket.WriteEncByte(Kart.PartsEngineLock);
				oPacket.WriteEncByte(Kart.PartsWheelLock);
				oPacket.WriteEncByte(Kart.PartsSteeringLock);
				oPacket.WriteEncByte(Kart.PartsBoosterLock);
				oPacket.WriteEncByte(Kart.PartsCoatingLock);
				oPacket.WriteEncByte(Kart.PartsTailLampLock);
				oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByBoost);
				oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByGrip);
				oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByWall);
				oPacket.WriteEncFloat(Kart.instAccelFactor);
				oPacket.WriteEncInt(Kart.instAccelGaugeCooldownTime);
				oPacket.WriteEncFloat(Kart.instAccelGaugeLength);
				oPacket.WriteEncFloat(Kart.instAccelGaugeMinUsable);
				oPacket.WriteEncFloat(Kart.instAccelGaugeMinVelBound);
				oPacket.WriteEncFloat(Kart.instAccelGaugeMinVelLoss);
				oPacket.WriteEncByte(Kart.useExtendedAfterBoosterMore);
				oPacket.WriteEncInt(Kart.wallCollGaugeCooldownTime);
				oPacket.WriteEncFloat(Kart.wallCollGaugeMaxVelLoss);
				oPacket.WriteEncFloat(Kart.wallCollGaugeMinVelBound);
				oPacket.WriteEncFloat(Kart.wallCollGaugeMinVelLoss);
				oPacket.WriteEncFloat(Kart.modelMaxX);
				oPacket.WriteEncFloat(Kart.modelMaxY);
				RouterListener.MySession.Client.Send(oPacket);
			}
		}
	}
}