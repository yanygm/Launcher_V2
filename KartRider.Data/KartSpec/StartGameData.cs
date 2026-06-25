using System;
using KartRider.IO.Packet;
using ExcData;
using Profile;
using System.Collections.Generic;

namespace KartRider
{
    public class StartGameData
    {
        public static void Start_KartSpac(SessionGroup Parent, string Nickname, byte StartType, byte StartTimeAttack_StartType, int Unk1, uint Track, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            if (StartType == 1)
            {
                Console.WriteLine("故事模式");
                StartGameData.PrKartSpec(Parent, Nickname, StartTimeAttack_SpeedType, KartID, FlyingPetID);
            }
            else if (StartType == 2)
            {
                Console.WriteLine("挑战者");
                StartGameData.PrchallengerKartSpec(Parent, Nickname, StartTimeAttack_SpeedType, KartID, FlyingPetID);
            }
            else if (StartType == 3)
            {
                Console.WriteLine("排行计时");
                if (StartTimeAttack_StartType == 0)
                {
                    StartGameData.PrStartTimeAttack(Parent, Nickname, Unk1, Track, StartTimeAttack_SpeedType, KartID, FlyingPetID);
                }
                else
                {
                    StartGameData.PrStartTimeAttack_QuestType(Parent, Nickname, Unk1, Track, StartTimeAttack_SpeedType, KartID, FlyingPetID);
                }
            }
            else
            {
                GameSupport.OnDisconnect(Parent);
            }
        }

        public static void PrStartTimeAttack(SessionGroup Parent, string Nickname, int Unk1, uint Track, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            using (OutPacket oPacket = new OutPacket("PrStartTimeAttack"))
            {
                oPacket.WriteInt(Unk1);
                oPacket.WriteInt(0);
                GetKartSpac(oPacket, Nickname, StartTimeAttack_SpeedType, KartID, FlyingPetID);
                oPacket.WriteByte(0);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                oPacket.WriteUInt(ProfileService.GetProfileConfig(Nickname).Rider.Lucci);
                oPacket.WriteUInt(ProfileService.GetProfileConfig(Nickname).Rider.Koin);
                oPacket.WriteUInt(Track);
                Parent.Client.Send(oPacket);
            }
        }

        public static void PrchallengerKartSpec(SessionGroup Parent, string Nickname, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            using (OutPacket oPacket = new OutPacket("PrchallengerKartSpec"))
            {
                oPacket.WriteByte(1);
                GetKartSpac(oPacket, Nickname, StartTimeAttack_SpeedType, KartID, FlyingPetID);
                oPacket.WriteInt(0);
                oPacket.WriteByte(0);
                Parent.Client.Send(oPacket);
            }
        }

        public static void PrKartSpec(SessionGroup Parent, string Nickname, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            using (OutPacket oPacket = new OutPacket("PrKartSpec"))
            {
                oPacket.WriteByte(1);
                GetDefaultSpac(oPacket, Nickname, StartTimeAttack_SpeedType, KartID, FlyingPetID);
                oPacket.WriteByte(0);
                Parent.Client.Send(oPacket);
            }
        }

        public static void PrStartTimeAttack_QuestType(SessionGroup Parent, string Nickname, int Unk1, uint Track, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            using (OutPacket oPacket = new OutPacket("PrStartTimeAttack"))
            {
                oPacket.WriteInt(Unk1);
                oPacket.WriteInt(0);
                GetDefaultSpac(oPacket, Nickname, StartTimeAttack_SpeedType, KartID, FlyingPetID);
                oPacket.WriteByte(0);
                oPacket.WriteInt(0);
                oPacket.WriteInt(0);
                oPacket.WriteUInt(ProfileService.GetProfileConfig(Nickname).Rider.Lucci);
                oPacket.WriteUInt(ProfileService.GetProfileConfig(Nickname).Rider.Koin);
                oPacket.WriteUInt(Track);
                Parent.Client.Send(oPacket);
            }
        }

        public static void GetKartSpac(OutPacket oPacket, string Nickname, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            var speed = GetSpeedType(Nickname, StartTimeAttack_SpeedType);
            var speedType = speed.Item1;
            StartTimeAttack_SpeedType = speed.Item2;

            var FlyingPet = new FlyingPetSpec();
            FlyingPet.FlyingPet_Spec(FlyingPetID);

            var Kart = new KartSpec();
            Kart.GetKartSpec(KartID);

            var excSpecs = new ExcSpecs();

            int StartPosition = oPacket.Position;

            if (KartID == ProfileService.GetProfileConfig(Nickname).RiderItem.Set_Kart)
            {
                ExcSpec.Use_TuneSpec(Nickname, excSpecs);
                ExcSpec.Use_PlantSpec(Nickname, excSpecs);
                ExcSpec.Use_KartLevelSpec(Nickname, excSpecs);
                ExcSpec.Use_PartsSpec(Nickname, excSpecs);
            }

            var V2Spec = new V2Specs();
            V2Spec.ExceedSpec(Nickname, Kart);

            float DriftEscapeForce = FlyingPet.DriftEscapeForce + excSpecs.Tune_DriftEscapeForce + excSpecs.Plant45_DriftEscapeForce + excSpecs.KartLevel_DriftEscapeForce + SpeedPatch.DriftEscapeForce + V2Spec.V2Parts_DriftEscapeForce + V2Spec.V2Level_DriftEscapeForce;
            float NormalBoosterTime = FlyingPet.NormalBoosterTime + excSpecs.Tune_NormalBoosterTime + excSpecs.Plant46_NormalBoosterTime + V2Spec.V2Parts_NormalBoosterTime + V2Spec.V2Level_NormalBoosterTime;
            float TransAccelFactor = excSpecs.Tune_TransAccelFactor + excSpecs.Plant43_TransAccelFactor + excSpecs.KartLevel_TransAccelFactor + SpeedPatch.TransAccelFactor + V2Spec.V2Parts_TransAccelFactor + V2Spec.V2Level_TransAccelFactor;
            float ForwardAccelForce = speedType.ForwardAccelForce + Kart.ForwardAccelForce + FlyingPet.ForwardAccelForce + excSpecs.Tune_ForwardAccel + excSpecs.Plant43_ForwardAccel + excSpecs.Plant46_ForwardAccel + excSpecs.KartLevel_ForwardAccel + SpeedPatch.ForwardAccelForce + V2Spec.V2Level_ForwardAccelForce;
            float StartForwardAccelForceItem = StartForwardAccelForce(Kart.StartForwardAccelFactorItem, ForwardAccelForce);
            float StartForwardAccelForceSpeed = StartForwardAccelForce(Kart.StartForwardAccelFactorSpeed, ForwardAccelForce);
            //------------------------------------------------------------------------KartSpac Start
            oPacket.WriteEncFloat(Kart.draftMulAccelFactor);
            oPacket.WriteEncInt(Kart.draftTick);
            oPacket.WriteEncFloat(Kart.driftBoostMulAccelFactor);
            oPacket.WriteEncInt(Kart.driftBoostTick);
            oPacket.WriteEncFloat(Kart.chargeBoostBySpeed);
            oPacket.WriteEncByte((byte)(Kart.SpeedSlotCapacity + excSpecs.Plant46_SpeedSlotCapacity));
            oPacket.WriteEncByte((byte)(Kart.ItemSlotCapacity + excSpecs.Plant46_ItemSlotCapacity));
            oPacket.WriteEncByte(Kart.SpecialSlotCapacity);
            oPacket.WriteEncByte(Kart.UseTransformBooster);
            oPacket.WriteEncByte(Kart.motorcycleType);
            oPacket.WriteEncByte(Kart.BikeRearWheel);
            oPacket.WriteEncFloat(speedType.Mass + Kart.Mass);
            oPacket.WriteEncFloat(speedType.AirFriction + Kart.AirFriction);
            oPacket.WriteEncFloat(speedType.DragFactor + Kart.DragFactor + FlyingPet.DragFactor + SpeedPatch.DragFactor + excSpecs.Tune_DragFactor + excSpecs.Plant43_DragFactor + excSpecs.Plant45_DragFactor + excSpecs.KartLevel_DragFactor);
            oPacket.WriteEncFloat(ForwardAccelForce);
            oPacket.WriteEncFloat(speedType.BackwardAccelForce + Kart.BackwardAccelForce);
            oPacket.WriteEncFloat(speedType.GripBrakeForce + Kart.GripBrakeForce + excSpecs.Plant44_GripBrake + excSpecs.Plant46_GripBrake);
            oPacket.WriteEncFloat(speedType.SlipBrakeForce + Kart.SlipBrakeForce + excSpecs.Plant44_SlipBrake + excSpecs.Plant45_SlipBrake + excSpecs.Plant46_SlipBrake);
            oPacket.WriteEncFloat(speedType.MaxSteerAngle + Kart.MaxSteerAngle);
            if (excSpecs.PartSpec_SteerConstraint == 0f)
            {
                oPacket.WriteEncFloat(speedType.SteerConstraint + Kart.SteerConstraint + excSpecs.Plant44_SteerConstraint + excSpecs.KartLevel_SteerConstraint + V2Spec.V2Parts_SteerConstraint);
            }
            else
            {
                oPacket.WriteEncFloat(excSpecs.PartSpec_SteerConstraint + speedType.AddSpec_SteerConstraint + excSpecs.Plant44_SteerConstraint + excSpecs.KartLevel_SteerConstraint + V2Spec.V2Parts_SteerConstraint);
            }
            oPacket.WriteEncFloat(speedType.FrontGripFactor + Kart.FrontGripFactor + excSpecs.Plant44_FrontGripFactor);
            oPacket.WriteEncFloat(speedType.RearGripFactor + Kart.RearGripFactor + excSpecs.Plant44_RearGripFactor);
            oPacket.WriteEncFloat(speedType.DriftTriggerFactor + Kart.DriftTriggerFactor);
            oPacket.WriteEncFloat(speedType.DriftTriggerTime + Kart.DriftTriggerTime);
            oPacket.WriteEncFloat(speedType.DriftSlipFactor + Kart.DriftSlipFactor + excSpecs.Plant46_DriftSlipFactor);
            if (excSpecs.PartSpec_DriftEscapeForce == 0f)
            {
                oPacket.WriteEncFloat(speedType.DriftEscapeForce + Kart.DriftEscapeForce + DriftEscapeForce);
            }
            else
            {
                oPacket.WriteEncFloat(excSpecs.PartSpec_DriftEscapeForce + speedType.AddSpec_DriftEscapeForce + DriftEscapeForce);
            }
            oPacket.WriteEncFloat(speedType.CornerDrawFactor + Kart.CornerDrawFactor + FlyingPet.CornerDrawFactor + excSpecs.Tune_CornerDrawFactor + excSpecs.Plant44_CornerDrawFactor + excSpecs.Plant45_CornerDrawFactor + excSpecs.KartLevel_CornerDrawFactor + SpeedPatch.CornerDrawFactor + V2Spec.V2Level_CornerDrawFactor);
            oPacket.WriteEncFloat(Kart.DriftLeanFactor);
            oPacket.WriteEncFloat(speedType.SteerLeanFactor + Kart.SteerLeanFactor);
            if (speedType.DriftMaxGauge == GameType.S4_DriftMaxGauge || StartTimeAttack_SpeedType == 4)
            {
                oPacket.WriteEncFloat(GameType.S4_DriftMaxGauge);
            }
            else
            {
                oPacket.WriteEncFloat(speedType.DriftMaxGauge + Kart.DriftMaxGauge + excSpecs.Tune_DriftMaxGauge + excSpecs.Plant45_DriftMaxGauge + excSpecs.Plant46_DriftMaxGauge + SpeedPatch.DriftMaxGauge + V2Spec.V2Level_DriftMaxGauge);
            }
            if (speedType.NormalBoosterTime == GameType.S6_BoosterTime || StartTimeAttack_SpeedType == 6)
            {
                oPacket.WriteEncFloat(GameType.S6_BoosterTime);
            }
            else
            {
                if (excSpecs.PartSpec_NormalBoosterTime == 0f)
                {
                    oPacket.WriteEncFloat(Kart.NormalBoosterTime + NormalBoosterTime);
                }
                else
                {
                    oPacket.WriteEncFloat(excSpecs.PartSpec_NormalBoosterTime + NormalBoosterTime);
                }
            }
            oPacket.WriteEncFloat(Kart.ItemBoosterTime + FlyingPet.ItemBoosterTime);
            if (speedType.TeamBoosterTime == GameType.S6_BoosterTime || StartTimeAttack_SpeedType == 6)
            {
                oPacket.WriteEncFloat(GameType.S6_BoosterTime);
            }
            else
            {
                oPacket.WriteEncFloat(Kart.TeamBoosterTime + FlyingPet.TeamBoosterTime + excSpecs.Tune_TeamBoosterTime + excSpecs.Plant46_TeamBoosterTime + V2Spec.V2Level_TeamBoosterTime);
            }
            oPacket.WriteEncFloat(Kart.AnimalBoosterTime + excSpecs.Plant45_AnimalBoosterTime + excSpecs.Plant46_AnimalBoosterTime);
            oPacket.WriteEncFloat(Kart.SuperBoosterTime);
            if (excSpecs.PartSpec_TransAccelFactor == 0f)
            {
                oPacket.WriteEncFloat(speedType.TransAccelFactor + Kart.TransAccelFactor + TransAccelFactor);
            }
            else
            {
                oPacket.WriteEncFloat(excSpecs.PartSpec_TransAccelFactor + speedType.AddSpec_TransAccelFactor + TransAccelFactor);
            }
            oPacket.WriteEncFloat(speedType.BoostAccelFactor + Kart.BoostAccelFactor + SpeedPatch.BoostAccelFactor);
            oPacket.WriteEncFloat(Kart.StartBoosterTimeItem + excSpecs.KartLevel_StartBoosterTimeItem + excSpecs.Plant46_StartBoosterTimeItem);
            oPacket.WriteEncFloat(Kart.StartBoosterTimeSpeed + excSpecs.Tune_StartBoosterTimeSpeed + excSpecs.Plant43_StartBoosterTimeSpeed + excSpecs.Plant46_StartBoosterTimeSpeed + excSpecs.KartLevel_StartBoosterTimeSpeed + V2Spec.V2Level_StartBoosterTimeSpeed);
            oPacket.WriteEncFloat(StartForwardAccelForceItem + FlyingPet.StartForwardAccelForceItem + SpeedPatch.StartForwardAccelForceItem + excSpecs.Plant46_StartForwardAccelItem);
            oPacket.WriteEncFloat(StartForwardAccelForceSpeed + FlyingPet.StartForwardAccelForceSpeed + SpeedPatch.StartForwardAccelForceSpeed + excSpecs.Plant43_StartForwardAccelSpeed + excSpecs.Plant46_StartForwardAccelSpeed);
            oPacket.WriteEncFloat(Kart.DriftGaguePreservePercent);
            oPacket.WriteEncByte(Kart.UseExtendedAfterBooster);
            oPacket.WriteEncFloat(Kart.BoostAccelFactorOnlyItem + excSpecs.KartLevel_BoostAccelFactorOnlyItem);
            oPacket.WriteEncFloat(Kart.antiCollideBalance + excSpecs.Plant45_AntiCollideBalance);
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
            oPacket.WriteEncInt(Kart.defaultExceedType);
            oPacket.WriteEncByte(Kart.defaultEngineType);
            oPacket.WriteEncByte(Kart.EngineType);
            oPacket.WriteEncByte(Kart.defaultHandleType);
            oPacket.WriteEncByte(Kart.HandleType);
            oPacket.WriteEncByte(Kart.defaultWheelType);
            oPacket.WriteEncByte(Kart.WheelType);
            oPacket.WriteEncByte(Kart.defaultBoosterType);
            oPacket.WriteEncByte(Kart.BoosterType);
            oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByWallAdded);
            oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByBoostAdded);
            oPacket.WriteEncInt(Kart.chargerSystemboosterUseCount);
            oPacket.WriteEncFloat(Kart.chargerSystemUseTime);
            oPacket.WriteEncFloat(Kart.chargeBoostBySpeedAdded);
            oPacket.WriteEncFloat(Kart.driftGaugeFactor);
            oPacket.WriteEncFloat(Kart.chargeAntiCollideBalance);
            oPacket.WriteEncInt(Kart.startItemTableId);
            oPacket.WriteEncInt(Kart.startItemId);
            oPacket.WriteEncFloat(Kart.Unknown1);
            oPacket.WriteEncByte(Kart.PartsBoosterEffectLock);
            //------------------------------------------------------------------------KartSpac End
            KartSpecLog(oPacket, StartPosition);
        }

        public static void GetDefaultSpac(OutPacket oPacket, string Nickname, byte StartTimeAttack_SpeedType, ushort KartID, ushort FlyingPetID)
        {
            var speed = GetSpeedType(Nickname, StartTimeAttack_SpeedType);
            var speedType = speed.Item1;
            StartTimeAttack_SpeedType = speed.Item2;

            var FlyingPet = new FlyingPetSpec();
            FlyingPet.FlyingPet_Spec(FlyingPetID);
            var Kart = new KartSpec();
            Kart.GetKartSpec(KartID);

            var V2Spec = new V2Specs();
            V2Spec.ExceedSpec(Nickname, Kart);

            int StartPosition = oPacket.Position;

            float ForwardAccelForce = speedType.ForwardAccelForce + Kart.ForwardAccelForce + FlyingPet.ForwardAccelForce + SpeedPatch.ForwardAccelForce;
            float StartForwardAccelForceItem = StartForwardAccelForce(Kart.StartForwardAccelFactorItem, ForwardAccelForce);
            float StartForwardAccelForceSpeed = StartForwardAccelForce(Kart.StartForwardAccelFactorSpeed, ForwardAccelForce);
            //------------------------------------------------------------------------KartSpac Start
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
            oPacket.WriteEncFloat(speedType.Mass + Kart.Mass);
            oPacket.WriteEncFloat(speedType.AirFriction + Kart.AirFriction);
            oPacket.WriteEncFloat(speedType.DragFactor + Kart.DragFactor + FlyingPet.DragFactor + SpeedPatch.DragFactor);
            oPacket.WriteEncFloat(ForwardAccelForce);
            oPacket.WriteEncFloat(speedType.BackwardAccelForce + Kart.BackwardAccelForce);
            oPacket.WriteEncFloat(speedType.GripBrakeForce + Kart.GripBrakeForce);
            oPacket.WriteEncFloat(speedType.SlipBrakeForce + Kart.SlipBrakeForce);
            oPacket.WriteEncFloat(speedType.MaxSteerAngle + Kart.MaxSteerAngle);
            oPacket.WriteEncFloat(speedType.SteerConstraint + Kart.SteerConstraint + V2Spec.V2Default_SteerConstraint);
            oPacket.WriteEncFloat(speedType.FrontGripFactor + Kart.FrontGripFactor);
            oPacket.WriteEncFloat(speedType.RearGripFactor + Kart.RearGripFactor);
            oPacket.WriteEncFloat(speedType.DriftTriggerFactor + Kart.DriftTriggerFactor);
            oPacket.WriteEncFloat(speedType.DriftTriggerTime + Kart.DriftTriggerTime);
            oPacket.WriteEncFloat(speedType.DriftSlipFactor + Kart.DriftSlipFactor);
            oPacket.WriteEncFloat(speedType.DriftEscapeForce + Kart.DriftEscapeForce + FlyingPet.DriftEscapeForce + SpeedPatch.DriftEscapeForce + V2Spec.V2Default_DriftEscapeForce);
            oPacket.WriteEncFloat(speedType.CornerDrawFactor + Kart.CornerDrawFactor + FlyingPet.CornerDrawFactor + SpeedPatch.CornerDrawFactor);
            oPacket.WriteEncFloat(Kart.DriftLeanFactor);
            oPacket.WriteEncFloat(speedType.SteerLeanFactor + Kart.SteerLeanFactor);
            if (speedType.DriftMaxGauge == GameType.S4_DriftMaxGauge || StartTimeAttack_SpeedType == 4)
            {
                oPacket.WriteEncFloat(GameType.S4_DriftMaxGauge);
            }
            else
            {
                oPacket.WriteEncFloat(speedType.DriftMaxGauge + Kart.DriftMaxGauge + SpeedPatch.DriftMaxGauge);
            }
            if (speedType.NormalBoosterTime == GameType.S6_BoosterTime || StartTimeAttack_SpeedType == 6)
            {
                oPacket.WriteEncFloat(GameType.S6_BoosterTime);
            }
            else
            {
                oPacket.WriteEncFloat(speedType.NormalBoosterTime + Kart.NormalBoosterTime + FlyingPet.NormalBoosterTime + V2Spec.V2Default_NormalBoosterTime);
            }
            oPacket.WriteEncFloat(Kart.ItemBoosterTime + FlyingPet.ItemBoosterTime);
            if (speedType.TeamBoosterTime == GameType.S6_BoosterTime || StartTimeAttack_SpeedType == 6)
            {
                oPacket.WriteEncFloat(GameType.S6_BoosterTime);
            }
            else
            {
                oPacket.WriteEncFloat(speedType.TeamBoosterTime + Kart.TeamBoosterTime + FlyingPet.TeamBoosterTime);
            }
            oPacket.WriteEncFloat(Kart.AnimalBoosterTime);
            oPacket.WriteEncFloat(Kart.SuperBoosterTime);
            oPacket.WriteEncFloat(speedType.TransAccelFactor + Kart.TransAccelFactor + SpeedPatch.TransAccelFactor + V2Spec.V2Default_TransAccelFactor);
            oPacket.WriteEncFloat(speedType.BoostAccelFactor + Kart.BoostAccelFactor + SpeedPatch.BoostAccelFactor);
            oPacket.WriteEncFloat(Kart.StartBoosterTimeItem);
            oPacket.WriteEncFloat(Kart.StartBoosterTimeSpeed);
            oPacket.WriteEncFloat(StartForwardAccelForceItem + FlyingPet.StartForwardAccelForceItem + SpeedPatch.StartForwardAccelForceItem);
            oPacket.WriteEncFloat(StartForwardAccelForceSpeed + FlyingPet.StartForwardAccelForceSpeed + SpeedPatch.StartForwardAccelForceSpeed);
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
            oPacket.WriteEncInt(Kart.defaultExceedType);
            oPacket.WriteEncByte(Kart.defaultEngineType);
            oPacket.WriteEncByte(Kart.EngineType);
            oPacket.WriteEncByte(Kart.defaultHandleType);
            oPacket.WriteEncByte(Kart.HandleType);
            oPacket.WriteEncByte(Kart.defaultWheelType);
            oPacket.WriteEncByte(Kart.WheelType);
            oPacket.WriteEncByte(Kart.defaultBoosterType);
            oPacket.WriteEncByte(Kart.BoosterType);
            oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByWallAdded);
            oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByBoostAdded);
            oPacket.WriteEncInt(Kart.chargerSystemboosterUseCount);
            oPacket.WriteEncFloat(Kart.chargerSystemUseTime);
            oPacket.WriteEncFloat(Kart.chargeBoostBySpeedAdded);
            oPacket.WriteEncFloat(Kart.driftGaugeFactor);
            oPacket.WriteEncFloat(Kart.chargeAntiCollideBalance);
            oPacket.WriteEncInt(Kart.startItemTableId);
            oPacket.WriteEncInt(Kart.startItemId);
            oPacket.WriteEncFloat(Kart.Unknown1);
            oPacket.WriteEncByte(Kart.PartsBoosterEffectLock);
            //------------------------------------------------------------------------KartSpac End
            KartSpecLog(oPacket, StartPosition);
        }

        public static void GetSchoolSpac(OutPacket oPacket, string Nickname)
        {
            var speedType = new SpeedType();
            speedType.SpeedTypeData("国服", 7);

            var Kart = new KartSpec();
            Kart.GetKartSpec(0, true);

            var V2Spec = new V2Specs();
            V2Spec.ExceedSpec(Nickname, Kart);

            int StartPosition = oPacket.Position;

            float ForwardAccelForce = speedType.ForwardAccelForce + Kart.ForwardAccelForce + SpeedPatch.ForwardAccelForce;
            float StartForwardAccelForceItem = StartForwardAccelForce(Kart.StartForwardAccelFactorItem, ForwardAccelForce);
            float StartForwardAccelForceSpeed = StartForwardAccelForce(Kart.StartForwardAccelFactorSpeed, ForwardAccelForce);
            //------------------------------------------------------------------------KartSpac Start
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
            oPacket.WriteEncFloat(speedType.Mass + Kart.Mass);
            oPacket.WriteEncFloat(speedType.AirFriction + Kart.AirFriction);
            oPacket.WriteEncFloat(speedType.DragFactor + Kart.DragFactor);
            oPacket.WriteEncFloat(ForwardAccelForce);
            oPacket.WriteEncFloat(speedType.BackwardAccelForce + Kart.BackwardAccelForce);
            oPacket.WriteEncFloat(speedType.GripBrakeForce + Kart.GripBrakeForce);
            oPacket.WriteEncFloat(speedType.SlipBrakeForce + Kart.SlipBrakeForce);
            oPacket.WriteEncFloat(speedType.MaxSteerAngle + Kart.MaxSteerAngle);
            oPacket.WriteEncFloat(speedType.SteerConstraint + Kart.SteerConstraint + V2Spec.V2Default_SteerConstraint);
            oPacket.WriteEncFloat(speedType.FrontGripFactor + Kart.FrontGripFactor);
            oPacket.WriteEncFloat(speedType.RearGripFactor + Kart.RearGripFactor);
            oPacket.WriteEncFloat(speedType.DriftTriggerFactor + Kart.DriftTriggerFactor);
            oPacket.WriteEncFloat(speedType.DriftTriggerTime + Kart.DriftTriggerTime);
            oPacket.WriteEncFloat(speedType.DriftSlipFactor + Kart.DriftSlipFactor);
            oPacket.WriteEncFloat(speedType.DriftEscapeForce + Kart.DriftEscapeForce + SpeedPatch.DriftEscapeForce + V2Spec.V2Default_DriftEscapeForce);
            oPacket.WriteEncFloat(speedType.CornerDrawFactor + Kart.CornerDrawFactor + SpeedPatch.CornerDrawFactor);
            oPacket.WriteEncFloat(Kart.DriftLeanFactor);
            oPacket.WriteEncFloat(speedType.SteerLeanFactor + Kart.SteerLeanFactor);
            oPacket.WriteEncFloat(speedType.DriftMaxGauge + Kart.DriftMaxGauge + SpeedPatch.DriftMaxGauge);
            oPacket.WriteEncFloat(speedType.NormalBoosterTime + Kart.NormalBoosterTime + V2Spec.V2Default_NormalBoosterTime);
            oPacket.WriteEncFloat(Kart.ItemBoosterTime);
            oPacket.WriteEncFloat(speedType.TeamBoosterTime + Kart.TeamBoosterTime);
            oPacket.WriteEncFloat(Kart.AnimalBoosterTime);
            oPacket.WriteEncFloat(Kart.SuperBoosterTime);
            oPacket.WriteEncFloat(speedType.TransAccelFactor + Kart.TransAccelFactor + SpeedPatch.TransAccelFactor + V2Spec.V2Default_TransAccelFactor);
            oPacket.WriteEncFloat(speedType.BoostAccelFactor + Kart.BoostAccelFactor + SpeedPatch.BoostAccelFactor);
            oPacket.WriteEncFloat(Kart.StartBoosterTimeItem);
            oPacket.WriteEncFloat(Kart.StartBoosterTimeSpeed);
            oPacket.WriteEncFloat(StartForwardAccelForceItem + SpeedPatch.StartForwardAccelForceItem);
            oPacket.WriteEncFloat(StartForwardAccelForceSpeed + SpeedPatch.StartForwardAccelForceSpeed);
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
            oPacket.WriteEncInt(Kart.defaultExceedType);
            oPacket.WriteEncByte(Kart.defaultEngineType);
            oPacket.WriteEncByte(Kart.EngineType);
            oPacket.WriteEncByte(Kart.defaultHandleType);
            oPacket.WriteEncByte(Kart.HandleType);
            oPacket.WriteEncByte(Kart.defaultWheelType);
            oPacket.WriteEncByte(Kart.WheelType);
            oPacket.WriteEncByte(Kart.defaultBoosterType);
            oPacket.WriteEncByte(Kart.BoosterType);
            oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByWallAdded);
            oPacket.WriteEncFloat(Kart.chargeInstAccelGaugeByBoostAdded);
            oPacket.WriteEncInt(Kart.chargerSystemboosterUseCount);
            oPacket.WriteEncFloat(Kart.chargerSystemUseTime);
            oPacket.WriteEncFloat(Kart.chargeBoostBySpeedAdded);
            oPacket.WriteEncFloat(Kart.driftGaugeFactor);
            oPacket.WriteEncFloat(Kart.chargeAntiCollideBalance);
            oPacket.WriteEncInt(Kart.startItemTableId);
            oPacket.WriteEncInt(Kart.startItemId);
            oPacket.WriteEncFloat(Kart.Unknown1);
            oPacket.WriteEncByte(Kart.PartsBoosterEffectLock);
            //------------------------------------------------------------------------KartSpac End
            KartSpecLog(oPacket, StartPosition);
        }

        public static (SpeedType, byte) GetSpeedType(string nickname, byte StartTimeAttack_SpeedType)
        {
            string version = ProfileService.SettingConfig.Version;
            byte speed = ProfileService.SettingConfig.SpeedType;
            if (version == "国服" && speed == 7)
            {
                speed = StartTimeAttack_SpeedType;
            }
            int roomId = RoomManager.TryGetRoomId(nickname);
            if (roomId != -1)
            {
                var room = RoomManager.GetRoom(roomId);
                if (room != null)
                {
                    StartTimeAttack_SpeedType = room.SpeedType;
                    var parsed = SpeedType.Parse(room.RoomName);
                    if (parsed.HasValue)
                    {
                        if (parsed.Value.infinite != byte.MaxValue)
                        {
                            StartTimeAttack_SpeedType = parsed.Value.infinite;
                        }
                        if (parsed.Value.speed != byte.MaxValue)
                        {
                            speed = parsed.Value.speed;
                            version = parsed.Value.version;
                        }
                        Console.WriteLine($"RoomName: {room.RoomName}, 速度类型: {StartTimeAttack_SpeedType}, 版本: {version}, 速度: {speed}");
                    }
                }
            }

            SpeedType speedType = new SpeedType();
            speedType.SpeedTypeData(version, speed);
            return (speedType, StartTimeAttack_SpeedType);
        }

        public static float StartForwardAccelForce(float ForwardAccelForce, float StartForwardAccelFactor)
        {
            Dictionary<Decimal, Decimal> Offsets = new Dictionary<Decimal, Decimal> { { 1.65m, -158.679m }, { 1.7m, -171.212m }, { 1.8m, -191.644m }, { 1.85m, -204.276m }, { 1.9m, -211.547m }, { 2.1m, -240.324m } };
            if (ForwardAccelForce == 0f)
            {
                return StartForwardAccelFactor;
            }
            if (Offsets.ContainsKey((Decimal)ForwardAccelForce))
            {
                return (float)((Decimal)StartForwardAccelFactor * (Decimal)ForwardAccelForce + Offsets[(Decimal)ForwardAccelForce]);
            }
            else
            {
                return (float)((Decimal)StartForwardAccelFactor * (Decimal)ForwardAccelForce + (-58.8m * (Decimal)ForwardAccelForce * (Decimal)ForwardAccelForce));
            }
        }

        public static void KartSpecLog(OutPacket oPacket, int StartPosition)
        {
            InPacket iPacket = new InPacket(oPacket.ToArray());
            iPacket.Position = StartPosition;
            Console.WriteLine($"-------------------------------------------------------------");
            Console.WriteLine($"draftMulAccelFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"draftTick:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"driftBoostMulAccelFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"driftBoostTick:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"chargeBoostBySpeed:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"SpeedSlotCapacity:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"ItemSlotCapacity:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"SpecialSlotCapacity:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"UseTransformBooster:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"motorcycleType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"BikeRearWheel:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"Mass:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"AirFriction:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DragFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"ForwardAccelForce:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"BackwardAccelForce:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"GripBrakeForce:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"SlipBrakeForce:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"MaxSteerAngle:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"SteerConstraint:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"FrontGripFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"RearGripFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftTriggerFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftTriggerTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftSlipFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftEscapeForce:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"CornerDrawFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftLeanFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"SteerLeanFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftMaxGauge:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"NormalBoosterTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"ItemBoosterTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"TeamBoosterTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"AnimalBoosterTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"SuperBoosterTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"TransAccelFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"BoostAccelFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"StartBoosterTimeItem:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"StartBoosterTimeSpeed:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"StartForwardAccelForceItem:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"StartForwardAccelForceSpeed:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"DriftGaguePreservePercent:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"UseExtendedAfterBooster:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"BoostAccelFactorOnlyItem:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"antiCollideBalance:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"dualBoosterSetAuto:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"dualBoosterTickMin:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"dualBoosterTickMax:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"dualMulAccelFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"dualTransLowSpeed:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"PartsEngineLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"PartsWheelLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"PartsSteeringLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"PartsBoosterLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"PartsCoatingLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"PartsTailLampLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"chargeInstAccelGaugeByBoost:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"chargeInstAccelGaugeByGrip:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"chargeInstAccelGaugeByWall:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"instAccelFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"instAccelGaugeCooldownTime:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"instAccelGaugeLength:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"instAccelGaugeMinUsable:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"instAccelGaugeMinVelBound:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"instAccelGaugeMinVelLoss:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"useExtendedAfterBoosterMore:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"wallCollGaugeCooldownTime:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"wallCollGaugeMaxVelLoss:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"wallCollGaugeMinVelBound:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"wallCollGaugeMinVelLoss:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"modelMaxX:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"modelMaxY:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"defaultExceedType:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"defaultEngineType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"EngineType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"defaultHandleType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"HandleType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"defaultWheelType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"WheelType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"defaultBoosterType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"BoosterType:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"chargeInstAccelGaugeByWallAdded:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"chargeInstAccelGaugeByBoostAdded:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"chargerSystemboosterUseCount:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"chargerSystemUseTime:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"chargeBoostBySpeedAdded:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"driftGaugeFactor:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"chargeAntiCollideBalance:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"startItemTableId:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"startItemId:{iPacket.ReadEncodedInt()}");
            Console.WriteLine($"Unknown1:{iPacket.ReadEncodedFloat()}");
            Console.WriteLine($"PartsBoosterEffectLock:{iPacket.ReadEncodedByte()}");
            Console.WriteLine($"-------------------------------------------------------------");
        }
    }
}