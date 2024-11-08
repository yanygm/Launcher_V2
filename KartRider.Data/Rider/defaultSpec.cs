using System;
using KartRider;
using ExcData;

namespace RiderData
{
	public class defaultSpec
	{
		public static void DefaultSpec()
		{
			Kart.draftMulAccelFactor = 1.1f;
			Kart.draftTick = 2000;
			Kart.driftBoostMulAccelFactor = 1.4f;
			Kart.driftBoostTick = 500;
			Kart.chargeBoostBySpeed = 350f;
			Kart.SpeedSlotCapacity = 2;
			Kart.ItemSlotCapacity = 2;
			Kart.SpecialSlotCapacity = 1;
			Kart.UseTransformBooster = (byte)(true ? 1 : 0);
			Kart.motorcycleType = (byte)(false ? 1 : 0);
			Kart.BikeRearWheel = (byte)(true ? 1 : 0);
			Kart.Mass = 100f;
			Kart.AirFriction = 3f;
			Kart.DragFactor = 0.667f + SpeedPatch.DragFactor;
			Kart.ForwardAccelForce = 2304f + SpeedPatch.ForwardAccelForce;
			Kart.BackwardAccelForce = 1825f;
			Kart.GripBrakeForce = 2070f;
			Kart.SlipBrakeForce = 1415f;
			Kart.MaxSteerAngle = 10f;
			Kart.SteerConstraint = 24.61f;
			Kart.FrontGripFactor = 5f;
			Kart.RearGripFactor = 5f;
			Kart.DriftTriggerFactor = 0.2f;
			Kart.DriftTriggerTime = 0.2f;
			Kart.DriftSlipFactor = 0.2f;
			Kart.DriftEscapeForce = 4200f + SpeedPatch.DriftEscapeForce;
			Kart.CornerDrawFactor = 0.254f + SpeedPatch.CornerDrawFactor;
			Kart.DriftLeanFactor = 0.06f;
			Kart.SteerLeanFactor = 0.01f;
			Kart.DriftMaxGauge = 3860f + SpeedPatch.DriftMaxGauge;
			Kart.NormalBoosterTime = 2900f;
			Kart.ItemBoosterTime = 3000f;
			Kart.TeamBoosterTime = 4350f;
			Kart.AnimalBoosterTime = 4000f;
			Kart.SuperBoosterTime = 3500f;
			Kart.TransAccelFactor = 1.8495f + SpeedPatch.TransAccelFactor;
			Kart.BoostAccelFactor = 1.494f + SpeedPatch.BoostAccelFactor;
			Kart.StartBoosterTimeItem = 1000f;
			Kart.StartBoosterTimeSpeed = 1500f;
			Kart.StartForwardAccelForceItem = 2304f + SpeedPatch.StartForwardAccelForceItem;
			Kart.StartForwardAccelForceSpeed = 3745.588f + SpeedPatch.StartForwardAccelForceSpeed;
			Kart.DriftGaguePreservePercent = 0.5f;
			Kart.UseExtendedAfterBooster = (byte)(false ? 1 : 0);
			Kart.BoostAccelFactorOnlyItem = 1.5f;
			Kart.antiCollideBalance = 0.91f;
			Kart.dualBoosterSetAuto = (byte)(false ? 1 : 0);
			Kart.dualBoosterTickMin = 20;
			Kart.dualBoosterTickMax = 30;
			Kart.dualMulAccelFactor = 1.04f;
			Kart.dualTransLowSpeed = 100f;
			Kart.PartsEngineLock = (byte)(true ? 1 : 0);
			Kart.PartsWheelLock = (byte)(true ? 1 : 0);
			Kart.PartsSteeringLock = (byte)(true ? 1 : 0);
			Kart.PartsBoosterLock = (byte)(true ? 1 : 0);
			Kart.PartsCoatingLock = (byte)(true ? 1 : 0);
			Kart.PartsTailLampLock = (byte)(true ? 1 : 0);
			Kart.chargeInstAccelGaugeByBoost = 0.02f;
			Kart.chargeInstAccelGaugeByGrip = 0.06f;
			Kart.chargeInstAccelGaugeByWall = 0.15f;
			Kart.instAccelFactor = 1.11f;
			Kart.instAccelGaugeCooldownTime = 3000;
			Kart.instAccelGaugeLength = 2500f;
			Kart.instAccelGaugeMinUsable = 750f;
			Kart.instAccelGaugeMinVelBound = 0f;
			Kart.instAccelGaugeMinVelLoss = 50f;
			Kart.useExtendedAfterBoosterMore = (byte)(false ? 1 : 0);
			Kart.wallCollGaugeCooldownTime = 3000;
			Kart.wallCollGaugeMaxVelLoss = 200f;
			Kart.wallCollGaugeMinVelBound = 200f;
			Kart.wallCollGaugeMinVelLoss = 50f;
			Kart.modelMaxX = 0;
			Kart.modelMaxY = 0;
			Kart.defaultExceedType = 0;
			Kart.v12 = 1;
			Kart.v12_0 = 0;
			Kart.v12_1 = 1;
			Kart.v12_2 = 2;
			Kart.v12_3 = 3;
			Kart.defaultEngineType = 0;
			Kart.defaultHandleType = 0;
			Kart.defaultWheelType = 0;
			Kart.defaultBoosterType = 0;
			Kart.chargeInstAccelGaugeByWallAdded = 0f;
			Kart.chargeInstAccelGaugeByBoostAdded = 0f;
			Kart.chargerSystemboosterUseCount = 0;
			Kart.chargerSystemUseTime = 0;
			Kart.v12_4 = 0;
			Kart.chargeBoostBySpeedAdded = 0f;
			Kart.driftGaugeFactor = 0f;
			Kart.v12_5 = 0f;
			Kart.v12_6 = 0f;
		}
	}
}
