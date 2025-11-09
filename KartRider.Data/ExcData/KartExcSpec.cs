using KartRider;
using Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ExcData
{
    public class ExcSpecs
    {
        public float Tune_DragFactor { get; set; } = 0f;
        public float Tune_ForwardAccel { get; set; } = 0f;
        public float Tune_CornerDrawFactor { get; set; } = 0f;
        public float Tune_TeamBoosterTime { get; set; } = 0f;
        public float Tune_NormalBoosterTime { get; set; } = 0f;
        public float Tune_StartBoosterTimeSpeed { get; set; } = 0f;
        public float Tune_TransAccelFactor { get; set; } = 0f;
        public float Tune_DriftMaxGauge { get; set; } = 0f;
        public float Tune_DriftEscapeForce { get; set; } = 0f;

        public float Plant43_TransAccelFactor { get; set; } = 0f;
        public float Plant43_DragFactor { get; set; } = 0f;
        public float Plant43_StartForwardAccelSpeed { get; set; } = 0f;
        public float Plant43_ForwardAccel { get; set; } = 0f;
        public float Plant43_StartBoosterTimeSpeed { get; set; } = 0f;

        public float Plant44_SlipBrake { get; set; } = 0f;
        public float Plant44_GripBrake { get; set; } = 0f;
        public float Plant44_RearGripFactor { get; set; } = 0f;
        public float Plant44_FrontGripFactor { get; set; } = 0f;
        public float Plant44_CornerDrawFactor { get; set; } = 0f;
        public float Plant44_SteerConstraint { get; set; } = 0f;

        public float Plant45_DriftEscapeForce { get; set; } = 0f;
        public float Plant45_DriftMaxGauge { get; set; } = 0f;
        public float Plant45_CornerDrawFactor { get; set; } = 0f;
        public float Plant45_SlipBrake { get; set; } = 0f;
        public float Plant45_AnimalBoosterTime { get; set; } = 0f;
        public float Plant45_AntiCollideBalance { get; set; } = 0f;
        public float Plant45_DragFactor { get; set; } = 0f;

        public float Plant46_DriftMaxGauge { get; set; } = 0f;
        public float Plant46_NormalBoosterTime { get; set; } = 0f;
        public float Plant46_DriftSlipFactor { get; set; } = 0f;
        public float Plant46_ForwardAccel { get; set; } = 0f;
        public float Plant46_AnimalBoosterTime { get; set; } = 0f;
        public float Plant46_TeamBoosterTime { get; set; } = 0f;
        public float Plant46_StartForwardAccelSpeed { get; set; } = 0f;
        public float Plant46_StartForwardAccelItem { get; set; } = 0f;
        public float Plant46_StartBoosterTimeSpeed { get; set; } = 0f;
        public float Plant46_StartBoosterTimeItem { get; set; } = 0f;
        public byte Plant46_ItemSlotCapacity { get; set; } = 0;
        public byte Plant46_SpeedSlotCapacity { get; set; } = 0;
        public float Plant46_GripBrake { get; set; } = 0f;
        public float Plant46_SlipBrake { get; set; } = 0f;

        public float KartLevel_DragFactor { get; set; } = 0f;
        public float KartLevel_ForwardAccel { get; set; } = 0f;
        public float KartLevel_CornerDrawFactor { get; set; } = 0f;
        public float KartLevel_SteerConstraint { get; set; } = 0f;
        public float KartLevel_DriftEscapeForce { get; set; } = 0f;
        public float KartLevel_TransAccelFactor { get; set; } = 0f;
        public float KartLevel_StartBoosterTimeSpeed { get; set; } = 0f;
        public float KartLevel_StartBoosterTimeItem { get; set; } = 0f;
        public float KartLevel_BoostAccelFactorOnlyItem { get; set; } = 0f;

        public float PartSpec_TransAccelFactor { get; set; } = 0f; //엔진
        public float PartSpec_SteerConstraint { get; set; } = 0f; //핸들
        public float PartSpec_DriftEscapeForce { get; set; } = 0f; //바퀴
        public float PartSpec_NormalBoosterTime { get; set; } = 0f; //부스터
    }

    public class ExcSpec
    {
        public static void Use_TuneSpec(string Nickname, ExcSpecs excSpecs)
        {
            float[] Tune_DragFactor_List = { 0f, -0.0008f, -0.0015f, -0.0022f };
            float[] Tune_ForwardAccel_List = { 0f, 1.5f, 2.5f, 3.5f };
            float[] Tune_CornerDrawFactor_List = { 0f, 0.0007f, 0.0014f, 0.002f };
            float[] Tune_TeamBoosterTime_List = { 0f, 100f, 180f, 250f };
            float[] Tune_NormalBoosterTime_List = { 0f, 70f, 120f, 190f };
            float[] Tune_StartBoosterTimeSpeed_List = { 0f, 200f, 400f, 800f };
            float[] Tune_TransAccelFactor_List = { 0f, 0.006f, 0.01f, 0.018f };
            float[] Tune_DriftMaxGauge_List = { 0f, -70f, -140f, -200f };
            float[] Tune_DriftEscapeForce_List = { 0f, 80f, 140f, 210f };
            KartExcData.TuneLists.TryAdd(Nickname, new List<Tune>());
            var TuneList = KartExcData.TuneLists[Nickname];
            short Set_Kart = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart;
            short Set_KartSN = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartSN;
            var existingTune = TuneList.FirstOrDefault(tune => tune.ID == Set_Kart && tune.SN == Set_KartSN);
            if (existingTune != null)
            {
                if (existingTune.Tune1 == 103 || existingTune.Tune2 == 103 || existingTune.Tune3 == 103)
                {
                    excSpecs.Tune_DragFactor = Tune_DragFactor_List[3];
                }
                if (existingTune.Tune1 == 203 || existingTune.Tune2 == 203 || existingTune.Tune3 == 203)
                {
                    excSpecs.Tune_ForwardAccel = Tune_ForwardAccel_List[3];
                }
                if (existingTune.Tune1 == 303 || existingTune.Tune2 == 303 || existingTune.Tune3 == 303)
                {
                    excSpecs.Tune_CornerDrawFactor = Tune_CornerDrawFactor_List[3];
                }
                if (existingTune.Tune1 == 403 || existingTune.Tune2 == 403 || existingTune.Tune3 == 403)
                {
                    excSpecs.Tune_TeamBoosterTime = Tune_TeamBoosterTime_List[3];
                }
                if (existingTune.Tune1 == 503 || existingTune.Tune2 == 503 || existingTune.Tune3 == 503)
                {
                    excSpecs.Tune_NormalBoosterTime = Tune_NormalBoosterTime_List[3];
                }
                if (existingTune.Tune1 == 603 || existingTune.Tune2 == 603 || existingTune.Tune3 == 603)
                {
                    excSpecs.Tune_StartBoosterTimeSpeed = Tune_StartBoosterTimeSpeed_List[3];
                }
                if (existingTune.Tune1 == 703 || existingTune.Tune2 == 703 || existingTune.Tune3 == 703)
                {
                    excSpecs.Tune_TransAccelFactor = Tune_TransAccelFactor_List[3];
                }
                if (existingTune.Tune1 == 803 || existingTune.Tune2 == 803 || existingTune.Tune3 == 803)
                {
                    excSpecs.Tune_DriftMaxGauge = Tune_DriftMaxGauge_List[3];
                }
                if (existingTune.Tune1 == 903 || existingTune.Tune2 == 903 || existingTune.Tune3 == 903)
                {
                    excSpecs.Tune_DriftEscapeForce = Tune_DriftEscapeForce_List[3];
                }
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("TuneSpec DragFactor:{0}", excSpecs.Tune_DragFactor);
                Console.WriteLine("TuneSpec ForwardAccel:{0}", excSpecs.Tune_ForwardAccel);
                Console.WriteLine("TuneSpec CornerDrawFactor:{0}", excSpecs.Tune_CornerDrawFactor);
                Console.WriteLine("TuneSpec TeamBoosterTime:{0}", excSpecs.Tune_TeamBoosterTime);
                Console.WriteLine("TuneSpec NormalBoosterTime:{0}", excSpecs.Tune_NormalBoosterTime);
                Console.WriteLine("TuneSpec StartBoosterTimeSpeed:{0}", excSpecs.Tune_StartBoosterTimeSpeed);
                Console.WriteLine("TuneSpec TransAccelFactor:{0}", excSpecs.Tune_TransAccelFactor);
                Console.WriteLine("TuneSpec DriftMaxGauge:{0}", excSpecs.Tune_DriftMaxGauge);
                Console.WriteLine("TuneSpec DriftEscapeForce:{0}", excSpecs.Tune_DriftEscapeForce);
                Console.WriteLine("-------------------------------------------------------------");
            }
        }

        public static void Use_PlantSpec(string Nickname, ExcSpecs excSpecs)
        {
            KartExcData.PlantLists.TryAdd(Nickname, new List<Plant>());
            var PlantList = KartExcData.PlantLists[Nickname];
            short Set_Kart = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart;
            short Set_KartSN = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartSN;
            var existingPlant = PlantList.FirstOrDefault(plant => plant.ID == Set_Kart && plant.SN == Set_KartSN);
            if (existingPlant != null)
            {
                if (existingPlant.Engine == 43)
                {
                    if (existingPlant.EngineID == 1)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.02f;
                        excSpecs.Plant43_DragFactor = -0.0007f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0.02f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 2)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.02f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 2f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 3)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0.02f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 15f;
                    }
                    else if (existingPlant.EngineID == 4 || existingPlant.EngineID == 5)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0.04f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 6)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = -0.0021f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 7)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = -0.0014f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 8)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0.02f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 9)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0.02f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 10)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 2f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 11)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 2f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 12)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = -0.0007f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 13)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = -0.0007f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 14)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = -0.0007f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 15)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = -0.0014f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 16)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0002f;
                        excSpecs.Plant43_DragFactor = -0.0014f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 17)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0004f;
                        excSpecs.Plant43_DragFactor = -0.0007f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 18)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0002f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 2f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 19)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0004f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 20)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0006f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 21)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0008f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 22)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.0012f;
                        excSpecs.Plant43_DragFactor = -0.0014f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                    else if (existingPlant.EngineID == 23)
                    {
                        excSpecs.Plant43_TransAccelFactor = 0.002f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 1f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 30f;
                    }
                    else
                    {
                        excSpecs.Plant43_TransAccelFactor = 0f;
                        excSpecs.Plant43_DragFactor = 0f;
                        excSpecs.Plant43_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant43_ForwardAccel = 0f;
                        excSpecs.Plant43_StartBoosterTimeSpeed = 0f;
                    }
                }
                if (existingPlant.Handle == 44)
                {
                    if (existingPlant.HandleID == 1)
                    {
                        excSpecs.Plant44_SlipBrake = -40f;
                        excSpecs.Plant44_GripBrake = -40f;
                        excSpecs.Plant44_RearGripFactor = 0.2f;
                        excSpecs.Plant44_FrontGripFactor = 0.2f;
                        excSpecs.Plant44_CornerDrawFactor = 0.0005f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 2)
                    {
                        excSpecs.Plant44_SlipBrake = -12f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0.3f;
                        excSpecs.Plant44_FrontGripFactor = 0.3f;
                        excSpecs.Plant44_CornerDrawFactor = 0.001f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 3)
                    {
                        excSpecs.Plant44_SlipBrake = -10f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0.2f;
                        excSpecs.Plant44_FrontGripFactor = 0.2f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 4)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0.1f;
                        excSpecs.Plant44_FrontGripFactor = 0.1f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 5)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = -20f;
                        excSpecs.Plant44_RearGripFactor = 0.05f;
                        excSpecs.Plant44_FrontGripFactor = 0.05f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 6)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = -20f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 7)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = -15f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 8)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0.2f;
                    }
                    else if (existingPlant.HandleID == 9)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0.4f;
                    }
                    else if (existingPlant.HandleID == 10)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0.8f;
                    }
                    else if (existingPlant.HandleID == 11)
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = -0.4f;
                    }
                    else if (existingPlant.HandleID == 12)
                    {
                        excSpecs.Plant44_SlipBrake = -8f;
                        excSpecs.Plant44_GripBrake = -5f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 13)
                    {
                        excSpecs.Plant44_SlipBrake = -6f;
                        excSpecs.Plant44_GripBrake = -7f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 14)
                    {
                        excSpecs.Plant44_SlipBrake = -4f;
                        excSpecs.Plant44_GripBrake = -9f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else if (existingPlant.HandleID == 15)
                    {
                        excSpecs.Plant44_SlipBrake = -2f;
                        excSpecs.Plant44_GripBrake = -11f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                    else
                    {
                        excSpecs.Plant44_SlipBrake = 0f;
                        excSpecs.Plant44_GripBrake = 0f;
                        excSpecs.Plant44_RearGripFactor = 0f;
                        excSpecs.Plant44_FrontGripFactor = 0f;
                        excSpecs.Plant44_CornerDrawFactor = 0f;
                        excSpecs.Plant44_SteerConstraint = 0f;
                    }
                }
                if (existingPlant.Wheel == 45)
                {
                    if (existingPlant.WheelID == 0)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 1)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 70f;
                        excSpecs.Plant45_DriftMaxGauge = -40f;
                        excSpecs.Plant45_CornerDrawFactor = 0.001f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 2)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = -60f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = -192f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 3)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 70f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 100f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 4)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = -60f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 5)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = -40f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 100f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 6)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 50f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 7)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 30f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0.0005f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 8)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = -40f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 9)
                    {
                        excSpecs.Plant45_DriftEscapeForce = -20f;
                        excSpecs.Plant45_DriftMaxGauge = -60f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 10)
                    {
                        excSpecs.Plant45_DriftEscapeForce = -60f;
                        excSpecs.Plant45_DriftMaxGauge = -100f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 11)
                    {
                        excSpecs.Plant45_DriftEscapeForce = -40f;
                        excSpecs.Plant45_DriftMaxGauge = -80f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 12)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 10f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 13)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 30f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 14)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 50f;
                        excSpecs.Plant45_DriftMaxGauge = 40f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 15)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 70f;
                        excSpecs.Plant45_DriftMaxGauge = 60f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 16)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0.0005f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.005f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 17)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.005f;
                        excSpecs.Plant45_DragFactor = -0.0007f;
                    }
                    else if (existingPlant.WheelID == 18)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = -40f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.005f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 19)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.01f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 20)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = -30f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.01f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 21)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.015f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 22)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 30f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = -0.02f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else if (existingPlant.WheelID == 23)
                    {
                        excSpecs.Plant45_DriftEscapeForce = 90f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0.0005f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                    else
                    {
                        excSpecs.Plant45_DriftEscapeForce = 0f;
                        excSpecs.Plant45_DriftMaxGauge = 0f;
                        excSpecs.Plant45_CornerDrawFactor = 0f;
                        excSpecs.Plant45_SlipBrake = 0f;
                        excSpecs.Plant45_AnimalBoosterTime = 0f;
                        excSpecs.Plant45_AntiCollideBalance = 0f;
                        excSpecs.Plant45_DragFactor = 0f;
                    }
                }
                if (existingPlant.Kit == 46)
                {
                    if (existingPlant.KitID == 1)
                    {
                        excSpecs.Plant46_DriftMaxGauge = -80f;
                        excSpecs.Plant46_NormalBoosterTime = 120f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 2)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = -0.03f;
                        excSpecs.Plant46_ForwardAccel = 2f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 3)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 200f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 5)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 90f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 50f;
                        excSpecs.Plant46_TeamBoosterTime = 60f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0.02f;
                        excSpecs.Plant46_StartForwardAccelItem = 0.02f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 7)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 105f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 8)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 105f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 11)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 3;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 12)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 3;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 15 || existingPlant.KitID == 16)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 100f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 10f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 17)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 100f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 9f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 18)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 120f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 23)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 60f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 24)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 60f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 25)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 90f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = -30f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else if (existingPlant.KitID == 26)
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = -30f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 90f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                    else
                    {
                        excSpecs.Plant46_DriftMaxGauge = 0f;
                        excSpecs.Plant46_NormalBoosterTime = 0f;
                        excSpecs.Plant46_DriftSlipFactor = 0f;
                        excSpecs.Plant46_ForwardAccel = 0f;
                        excSpecs.Plant46_AnimalBoosterTime = 0f;
                        excSpecs.Plant46_TeamBoosterTime = 0f;
                        excSpecs.Plant46_StartForwardAccelSpeed = 0f;
                        excSpecs.Plant46_StartForwardAccelItem = 0f;
                        excSpecs.Plant46_StartBoosterTimeSpeed = 0f;
                        excSpecs.Plant46_StartBoosterTimeItem = 0f;
                        excSpecs.Plant46_ItemSlotCapacity = 0;
                        excSpecs.Plant46_SpeedSlotCapacity = 0;
                        excSpecs.Plant46_GripBrake = 0f;
                        excSpecs.Plant46_SlipBrake = 0f;
                    }
                }
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("Plant43 TransAccelFactor:{0}", excSpecs.Plant43_TransAccelFactor);
                Console.WriteLine("Plant43 DragFactor:{0}", excSpecs.Plant43_DragFactor);
                Console.WriteLine("Plant43 StartForwardAccelSpeed:{0}", excSpecs.Plant43_StartForwardAccelSpeed);
                Console.WriteLine("Plant43 ForwardAccel:{0}", excSpecs.Plant43_ForwardAccel);
                Console.WriteLine("Plant43 StartBoosterTimeSpeed:{0}", excSpecs.Plant43_StartBoosterTimeSpeed);

                Console.WriteLine("Plant44 SlipBrake:{0}", excSpecs.Plant44_SlipBrake);
                Console.WriteLine("Plant44 GripBrake:{0}", excSpecs.Plant44_GripBrake);
                Console.WriteLine("Plant44 RearGripFactor:{0}", excSpecs.Plant44_RearGripFactor);
                Console.WriteLine("Plant44 FrontGripFactor:{0}", excSpecs.Plant44_FrontGripFactor);
                Console.WriteLine("Plant44 CornerDrawFactor:{0}", excSpecs.Plant44_CornerDrawFactor);
                Console.WriteLine("Plant44 SteerConstraint:{0}", excSpecs.Plant44_SteerConstraint);

                Console.WriteLine("Plant45 DriftEscapeForce:{0}", excSpecs.Plant45_DriftEscapeForce);
                Console.WriteLine("Plant45 DriftMaxGauge:{0}", excSpecs.Plant45_DriftMaxGauge);
                Console.WriteLine("Plant45 CornerDrawFactor:{0}", excSpecs.Plant45_CornerDrawFactor);
                Console.WriteLine("Plant45 SlipBrake:{0}", excSpecs.Plant45_SlipBrake);
                Console.WriteLine("Plant45 AnimalBoosterTime:{0}", excSpecs.Plant45_AnimalBoosterTime);
                Console.WriteLine("Plant45 AntiCollideBalance:{0}", excSpecs.Plant45_AntiCollideBalance);
                Console.WriteLine("Plant45 DragFactor:{0}", excSpecs.Plant45_DragFactor);

                Console.WriteLine("Plant46 DriftMaxGauge:{0}", excSpecs.Plant46_DriftMaxGauge);
                Console.WriteLine("Plant46 NormalBoosterTime:{0}", excSpecs.Plant46_NormalBoosterTime);
                Console.WriteLine("Plant46 DriftSlipFactor:{0}", excSpecs.Plant46_DriftSlipFactor);
                Console.WriteLine("Plant46 ForwardAccel:{0}", excSpecs.Plant46_ForwardAccel);
                Console.WriteLine("Plant46 AnimalBoosterTime:{0}", excSpecs.Plant46_AnimalBoosterTime);
                Console.WriteLine("Plant46 TeamBoosterTime:{0}", excSpecs.Plant46_TeamBoosterTime);
                Console.WriteLine("Plant46 StartForwardAccelSpeed:{0}", excSpecs.Plant46_StartForwardAccelSpeed);
                Console.WriteLine("Plant46 StartForwardAccelItem:{0}", excSpecs.Plant46_StartForwardAccelItem);
                Console.WriteLine("Plant46 StartBoosterTimeSpeed:{0}", excSpecs.Plant46_StartBoosterTimeSpeed);
                Console.WriteLine("Plant46 StartBoosterTimeItem:{0}", excSpecs.Plant46_StartBoosterTimeItem);
                Console.WriteLine("Plant46 ItemSlotCapacity:{0}", excSpecs.Plant46_ItemSlotCapacity);
                Console.WriteLine("Plant46 SpeedSlotCapacity:{0}", excSpecs.Plant46_SpeedSlotCapacity);
                Console.WriteLine("Plant46 GripBrake:{0}", excSpecs.Plant46_GripBrake);
                Console.WriteLine("Plant46 SlipBrake:{0}", excSpecs.Plant46_SlipBrake);
                Console.WriteLine("-------------------------------------------------------------");
            }
        }

        public static void Use_KartLevelSpec(string Nickname, ExcSpecs excSpecs)
        {
            float[] KartLevel_DragFactor = { 0f, -0.0001f, -0.0002f, -0.0003f, -0.0004f, -0.0005f, -0.0006f, -0.0007f, -0.0008f, -0.001f, -0.0012f };
            float[] KartLevel_ForwardAccel = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 1.0f, 1.5f };
            float[] KartLevel_CornerDrawFactor = { 0f, 0.0001f, 0.0002f, 0.0003f, 0.0004f, 0.0005f, 0.0006f, 0.0007f, 0.0008f, 0.0009f, 0.001f };
            float[] KartLevel_SteerConstraint = { 0f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.08f, 0.11f, 0.15f, 0.2f };
            float[] KartLevel_DriftEscapeForce = { 0f, 1f, 3f, 6f, 10f, 15f, 20f, 26f, 33f, 40f, 50f };
            float[] KartLevel_TransAccelFactor = { 0f, 0.0001f, 0.0003f, 0.0006f, 0.001f, 0.0014f, 0.0019f, 0.0025f, 0.0032f, 0.004f, 0.005f };
            float[] KartLevel_StartBoosterTimeSpeed = { 0f, 5f, 10f, 15f, 20f, 30f, 40f, 50f, 65f, 80f, 100f };
            float[] KartLevel_StartBoosterTimeItem = { 0f, 5f, 10f, 15f, 20f, 30f, 40f, 50f, 65f, 80f, 100f };
            float[] KartLevel_BoostAccelFactorOnlyItem = { 0f, 0.001f, 0.003f, 0.005f, 0.009f, 0.013f, 0.019f, 0.025f, 0.033f, 0.041f, 0.05f };
            KartExcData.LevelLists.TryAdd(Nickname, new List<Level>());
            var LevelList = KartExcData.LevelLists[Nickname];
            short Set_Kart = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart;
            short Set_KartSN = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartSN;
            var existingLevel = LevelList.FirstOrDefault(level => level.ID == Set_Kart && level.SN == Set_KartSN);
            if (existingLevel != null)
            {
                excSpecs.KartLevel_DragFactor = KartLevel_DragFactor[existingLevel.Level1];
                excSpecs.KartLevel_ForwardAccel = KartLevel_ForwardAccel[existingLevel.Level1];
                excSpecs.KartLevel_CornerDrawFactor = KartLevel_CornerDrawFactor[existingLevel.Level2];
                excSpecs.KartLevel_SteerConstraint = KartLevel_SteerConstraint[existingLevel.Level2];
                excSpecs.KartLevel_DriftEscapeForce = KartLevel_DriftEscapeForce[existingLevel.Level3];
                excSpecs.KartLevel_TransAccelFactor = KartLevel_TransAccelFactor[existingLevel.Level4];
                excSpecs.KartLevel_StartBoosterTimeSpeed = KartLevel_StartBoosterTimeSpeed[existingLevel.Level4];
                excSpecs.KartLevel_StartBoosterTimeItem = KartLevel_StartBoosterTimeItem[existingLevel.Level4];
                excSpecs.KartLevel_BoostAccelFactorOnlyItem = KartLevel_BoostAccelFactorOnlyItem[existingLevel.Level4];
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("KartLevel DragFactor:{0}", excSpecs.KartLevel_DragFactor);
                Console.WriteLine("KartLevel ForwardAccel:{0}", excSpecs.KartLevel_ForwardAccel);
                Console.WriteLine("KartLevel CornerDrawFactor:{0}", excSpecs.KartLevel_CornerDrawFactor);
                Console.WriteLine("KartLevel SteerConstraint:{0}", excSpecs.KartLevel_SteerConstraint);
                Console.WriteLine("KartLevel DriftEscapeForce:{0}", excSpecs.KartLevel_DriftEscapeForce);
                Console.WriteLine("KartLevel TransAccelFactor:{0}", excSpecs.KartLevel_TransAccelFactor);
                Console.WriteLine("KartLevel StartBoosterTimeSpeed:{0}", excSpecs.KartLevel_StartBoosterTimeSpeed);
                Console.WriteLine("KartLevel StartBoosterTimeItem:{0}", excSpecs.KartLevel_StartBoosterTimeItem);
                Console.WriteLine("KartLevel BoostAccelFactorOnlyItem:{0}", excSpecs.KartLevel_BoostAccelFactorOnlyItem);
                Console.WriteLine("-------------------------------------------------------------");
            }
        }

        public static void Use_PartsSpec(string Nickname, ExcSpecs excSpecs)
        {
            KartExcData.PartsLists.TryAdd(Nickname, new List<Parts>());
            var PartsList = KartExcData.PartsLists[Nickname];
            short Set_Kart = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart;
            short Set_KartSN = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_KartSN;
            var existingParts = PartsList.FirstOrDefault(parts => parts.ID == Set_Kart && parts.SN == Set_KartSN);
            if (existingParts != null)
            {
                for (short i = 63; i < 67; i++)
                {
                    if (i == 63)
                    {
                        short PartsValue = existingParts.EngineValue;
                        if (PartsValue != 0)
                        {
                            excSpecs.PartSpec_TransAccelFactor = (float)(((decimal)PartsValue * 1.0M - 800.0M) / 25000.0M + 1.85M + -0.205M);
                        }
                        else
                        {
                            excSpecs.PartSpec_TransAccelFactor = 0f;
                        }
                    }
                    else if (i == 64)
                    {
                        short PartsValue = existingParts.HandleValue;
                        if (PartsValue != 0)
                        {
                            excSpecs.PartSpec_SteerConstraint = (float)(((decimal)PartsValue * 1.0M - 800.0M) / 250.0M + 2.1M + 20.3M);
                        }
                        else
                        {
                            excSpecs.PartSpec_SteerConstraint = 0f;
                        }
                    }
                    else if (i == 65)
                    {
                        short PartsValue = existingParts.WheelValue;
                        if (PartsValue != 0)
                        {
                            excSpecs.PartSpec_DriftEscapeForce = (float)((decimal)PartsValue * 2.0M + 2200.0M);
                        }
                        else
                        {
                            excSpecs.PartSpec_DriftEscapeForce = 0f;
                        }
                    }
                    else if (i == 66)
                    {
                        short PartsValue = existingParts.BoosterValue;
                        if (PartsValue != 0)
                        {
                            excSpecs.PartSpec_NormalBoosterTime = (float)((decimal)PartsValue * 1.0M - 940.0M + 3000.0M);
                        }
                        else
                        {
                            excSpecs.PartSpec_NormalBoosterTime = 0f;
                        }
                    }
                }
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("PartSpec TransAccelFactor:{0}", excSpecs.PartSpec_TransAccelFactor);
                Console.WriteLine("PartSpec SteerConstraint:{0}", excSpecs.PartSpec_SteerConstraint);
                Console.WriteLine("PartSpec DriftEscapeForce:{0}", excSpecs.PartSpec_DriftEscapeForce);
                Console.WriteLine("PartSpec NormalBoosterTime:{0}", excSpecs.PartSpec_NormalBoosterTime);
                Console.WriteLine("-------------------------------------------------------------");
            }
        }
    }
}
