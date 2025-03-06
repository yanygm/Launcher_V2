using System;
using System.Linq;

namespace ExcData
{
	public class ExcSpec
	{
		public static float Tune_DragFactor = 0f;
		public static float Tune_ForwardAccel = 0f;
		public static float Tune_CornerDrawFactor = 0f;
		public static float Tune_TeamBoosterTime = 0f;
		public static float Tune_NormalBoosterTime = 0f;
		public static float Tune_StartBoosterTimeSpeed = 0f;
		public static float Tune_TransAccelFactor = 0f;
		public static float Tune_DriftMaxGauge = 0f;
		public static float Tune_DriftEscapeForce = 0f;

		public static float Plant43_TransAccelFactor = 0f;
		public static float Plant43_DragFactor = 0f;
		public static float Plant43_StartForwardAccelSpeed = 0f;
		public static float Plant43_ForwardAccel = 0f;
		public static float Plant43_StartBoosterTimeSpeed = 0f;

		public static float Plant44_SlipBrake = 0f;
		public static float Plant44_GripBrake = 0f;
		public static float Plant44_RearGripFactor = 0f;
		public static float Plant44_FrontGripFactor = 0f;
		public static float Plant44_CornerDrawFactor = 0f;
		public static float Plant44_SteerConstraint = 0f;

		public static float Plant45_DriftEscapeForce = 0f;
		public static float Plant45_DriftMaxGauge = 0f;
		public static float Plant45_CornerDrawFactor = 0f;
		public static float Plant45_SlipBrake = 0f;
		public static float Plant45_AnimalBoosterTime = 0f;
		public static float Plant45_AntiCollideBalance = 0f;
		public static float Plant45_DragFactor = 0f;

		public static float Plant46_DriftMaxGauge = 0f;
		public static float Plant46_NormalBoosterTime = 0f;
		public static float Plant46_DriftSlipFactor = 0f;
		public static float Plant46_ForwardAccel = 0f;
		public static float Plant46_AnimalBoosterTime = 0f;
		public static float Plant46_TeamBoosterTime = 0f;
		public static float Plant46_StartForwardAccelSpeed = 0f;
		public static float Plant46_StartForwardAccelItem = 0f;
		public static float Plant46_StartBoosterTimeSpeed = 0f;
		public static float Plant46_StartBoosterTimeItem = 0f;
		public static byte Plant46_ItemSlotCapacity = 0;
		public static byte Plant46_SpeedSlotCapacity = 0;
		public static float Plant46_GripBrake = 0f;
		public static float Plant46_SlipBrake = 0f;

		public static float KartLevel_DragFactor = 0f;
		public static float KartLevel_ForwardAccel = 0f;
		public static float KartLevel_CornerDrawFactor = 0f;
		public static float KartLevel_SteerConstraint = 0f;
		public static float KartLevel_DriftEscapeForce = 0f;
		public static float KartLevel_TransAccelFactor = 0f;
		public static float KartLevel_StartBoosterTimeSpeed = 0f;
		public static float KartLevel_StartBoosterTimeItem = 0f;
		public static float KartLevel_BoostAccelFactorOnlyItem = 0f;

		public static float PartSpec_TransAccelFactor = 0f; //엔진
		public static float PartSpec_SteerConstraint = 0f; //핸들
		public static float PartSpec_DriftEscapeForce = 0f; //바퀴
		public static float PartSpec_NormalBoosterTime = 0f; //부스터

		public static void Reset_PartSpec_SpecData()
		{
			ExcSpec.PartSpec_TransAccelFactor = 0f;
			ExcSpec.PartSpec_SteerConstraint = 0f;
			ExcSpec.PartSpec_DriftEscapeForce = 0f;
			ExcSpec.PartSpec_NormalBoosterTime = 0f;
			Console.WriteLine("Part_Spec: OFF");
		}

		public static void Reset_Tune_SpecData()
		{
			ExcSpec.Tune_DragFactor = 0f;
			ExcSpec.Tune_ForwardAccel = 0f;
			ExcSpec.Tune_CornerDrawFactor = 0f;
			ExcSpec.Tune_TeamBoosterTime = 0f;
			ExcSpec.Tune_NormalBoosterTime = 0f;
			ExcSpec.Tune_StartBoosterTimeSpeed = 0f;
			ExcSpec.Tune_TransAccelFactor = 0f;
			ExcSpec.Tune_DriftMaxGauge = 0f;
			ExcSpec.Tune_DriftEscapeForce = 0f;
			Console.WriteLine("Tune_Type: OFF");
		}

		public static void Reset_Plant_SpecData()
		{
			ExcSpec.Plant43_TransAccelFactor = 0f;
			ExcSpec.Plant43_DragFactor = 0f;
			ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
			ExcSpec.Plant43_ForwardAccel = 0f;
			ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;

			ExcSpec.Plant44_SlipBrake = 0f;
			ExcSpec.Plant44_GripBrake = 0f;
			ExcSpec.Plant44_RearGripFactor = 0f;
			ExcSpec.Plant44_FrontGripFactor = 0f;
			ExcSpec.Plant44_CornerDrawFactor = 0f;
			ExcSpec.Plant44_SteerConstraint = 0f;

			ExcSpec.Plant45_DriftEscapeForce = 0f;
			ExcSpec.Plant45_DriftMaxGauge = 0f;
			ExcSpec.Plant45_CornerDrawFactor = 0f;
			ExcSpec.Plant45_SlipBrake = 0f;
			ExcSpec.Plant45_AnimalBoosterTime = 0f;
			ExcSpec.Plant45_AntiCollideBalance = 0f;
			ExcSpec.Plant45_DragFactor = 0f;

			ExcSpec.Plant46_DriftMaxGauge = 0f;
			ExcSpec.Plant46_NormalBoosterTime = 0f;
			ExcSpec.Plant46_DriftSlipFactor = 0f;
			ExcSpec.Plant46_ForwardAccel = 0f;
			ExcSpec.Plant46_AnimalBoosterTime = 0f;
			ExcSpec.Plant46_TeamBoosterTime = 0f;
			ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
			ExcSpec.Plant46_StartForwardAccelItem = 0f;
			ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
			ExcSpec.Plant46_StartBoosterTimeItem = 0f;
			ExcSpec.Plant46_ItemSlotCapacity = 0;
			ExcSpec.Plant46_SpeedSlotCapacity = 0;
			ExcSpec.Plant46_GripBrake = 0f;
			ExcSpec.Plant46_SlipBrake = 0f;
			Console.WriteLine("Plant_Spec: OFF");
		}

		public static void Reset_KartLevel_SpecData()
		{
			ExcSpec.KartLevel_DragFactor = 0f;
			ExcSpec.KartLevel_ForwardAccel = 0f;
			ExcSpec.KartLevel_CornerDrawFactor = 0f;
			ExcSpec.KartLevel_SteerConstraint = 0f;
			ExcSpec.KartLevel_DriftEscapeForce = 0f;
			ExcSpec.KartLevel_TransAccelFactor = 0f;
			ExcSpec.KartLevel_StartBoosterTimeSpeed = 0f;
			ExcSpec.KartLevel_StartBoosterTimeItem = 0f;
			ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0f;
			Console.WriteLine("Level_Spec: OFF");
		}

		public static void Use_ExcSpec(short Set_Kart, short Set_KartSN)
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
			var existingTune = KartExcData.TuneList.FirstOrDefault(list => list[0] == Set_Kart && list[1] == Set_KartSN);
			if (existingTune != null)
			{
				ExcSpec.Reset_Tune_SpecData();
				if (existingTune[2] == 103 || existingTune[3] == 103 || existingTune[4] == 103)
				{
					ExcSpec.Tune_DragFactor = Tune_DragFactor_List[3];
				}
				if (existingTune[2] == 203 || existingTune[3] == 203 || existingTune[4] == 203)
				{
					ExcSpec.Tune_ForwardAccel = Tune_ForwardAccel_List[3];
				}
				if (existingTune[2] == 303 || existingTune[3] == 303 || existingTune[4] == 303)
				{
					ExcSpec.Tune_CornerDrawFactor = Tune_CornerDrawFactor_List[3];
				}
				if (existingTune[2] == 403 || existingTune[3] == 403 || existingTune[4] == 403)
				{
					ExcSpec.Tune_TeamBoosterTime = Tune_TeamBoosterTime_List[3];
				}
				if (existingTune[2] == 503 || existingTune[3] == 503 || existingTune[4] == 503)
				{
					ExcSpec.Tune_NormalBoosterTime = Tune_NormalBoosterTime_List[3];
				}
				if (existingTune[2] == 603 || existingTune[3] == 603 || existingTune[4] == 603)
				{
					ExcSpec.Tune_StartBoosterTimeSpeed = Tune_StartBoosterTimeSpeed_List[3];
				}
				if (existingTune[2] == 703 || existingTune[3] == 703 || existingTune[4] == 703)
				{
					ExcSpec.Tune_TransAccelFactor = Tune_TransAccelFactor_List[3];
				}
				if (existingTune[2] == 803 || existingTune[3] == 803 || existingTune[4] == 803)
				{
					ExcSpec.Tune_DriftMaxGauge = Tune_DriftMaxGauge_List[3];
				}
				if (existingTune[2] == 903 || existingTune[3] == 903 || existingTune[4] == 903)
				{
					ExcSpec.Tune_DriftEscapeForce = Tune_DriftEscapeForce_List[3];
				}
			}
			else
			{
				ExcSpec.Reset_Tune_SpecData();
			}
		}

		public static void Use_PlantSpec(short Set_Kart, short Set_KartSN)
		{
			var existingPlant = KartExcData.PlantList.FirstOrDefault(list => list[0] == Set_Kart && list[1] == Set_KartSN);
			if (existingPlant!= null)
			{
				if (existingPlant[2] == 43)
				{
					if (existingPlant[3] == 1)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.02f;
						ExcSpec.Plant43_DragFactor = -0.0007f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0.02f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 2)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.02f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 2f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 3)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0.02f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 15f;
					}
					else if (existingPlant[3] == 4 || existingPlant[3] == 5)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0.04f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 6)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = -0.0021f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 7)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = -0.0014f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 8)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0.02f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 9)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0.02f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 10)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 2f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 11)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 2f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 12)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = -0.0007f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 13)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = -0.0007f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 14)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = -0.0007f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 15)
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = -0.0014f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 16)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0002f;
						ExcSpec.Plant43_DragFactor = -0.0014f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 17)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0004f;
						ExcSpec.Plant43_DragFactor = -0.0007f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 18)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0002f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 2f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 19)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0004f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 20)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0006f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 21)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0008f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 22)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.0012f;
						ExcSpec.Plant43_DragFactor = -0.0014f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
					else if (existingPlant[3] == 23)
					{
						ExcSpec.Plant43_TransAccelFactor = 0.002f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 1f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 30f;
					}
					else
					{
						ExcSpec.Plant43_TransAccelFactor = 0f;
						ExcSpec.Plant43_DragFactor = 0f;
						ExcSpec.Plant43_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant43_ForwardAccel = 0f;
						ExcSpec.Plant43_StartBoosterTimeSpeed = 0f;
					}
				}
				else if (existingPlant[4] == 44)
				{
					if (existingPlant[5] == 1)
					{
						ExcSpec.Plant44_SlipBrake = -40f;
						ExcSpec.Plant44_GripBrake = -40f;
						ExcSpec.Plant44_RearGripFactor = 0.2f;
						ExcSpec.Plant44_FrontGripFactor = 0.2f;
						ExcSpec.Plant44_CornerDrawFactor = 0.0005f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 2)
					{
						ExcSpec.Plant44_SlipBrake = -12f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0.3f;
						ExcSpec.Plant44_FrontGripFactor = 0.3f;
						ExcSpec.Plant44_CornerDrawFactor = 0.001f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 3)
					{
						ExcSpec.Plant44_SlipBrake = -10f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0.2f;
						ExcSpec.Plant44_FrontGripFactor = 0.2f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 4)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0.1f;
						ExcSpec.Plant44_FrontGripFactor = 0.1f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 5)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = -20f;
						ExcSpec.Plant44_RearGripFactor = 0.05f;
						ExcSpec.Plant44_FrontGripFactor = 0.05f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 6)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = -20f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 7)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = -15f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 8)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0.2f;
					}
					else if (existingPlant[5] == 9)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0.4f;
					}
					else if (existingPlant[5] == 10)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0.8f;
					}
					else if (existingPlant[5] == 11)
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = -0.4f;
					}
					else if (existingPlant[5] == 12)
					{
						ExcSpec.Plant44_SlipBrake = -8f;
						ExcSpec.Plant44_GripBrake = -5f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 13)
					{
						ExcSpec.Plant44_SlipBrake = -6f;
						ExcSpec.Plant44_GripBrake = -7f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 14)
					{
						ExcSpec.Plant44_SlipBrake = -4f;
						ExcSpec.Plant44_GripBrake = -9f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else if (existingPlant[5] == 15)
					{
						ExcSpec.Plant44_SlipBrake = -2f;
						ExcSpec.Plant44_GripBrake = -11f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
					else
					{
						ExcSpec.Plant44_SlipBrake = 0f;
						ExcSpec.Plant44_GripBrake = 0f;
						ExcSpec.Plant44_RearGripFactor = 0f;
						ExcSpec.Plant44_FrontGripFactor = 0f;
						ExcSpec.Plant44_CornerDrawFactor = 0f;
						ExcSpec.Plant44_SteerConstraint = 0f;
					}
				}
				else if (existingPlant[6] == 45)
				{
					if (existingPlant[7] == 0)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 1)
					{
						ExcSpec.Plant45_DriftEscapeForce = 70f;
						ExcSpec.Plant45_DriftMaxGauge = -40f;
						ExcSpec.Plant45_CornerDrawFactor = 0.001f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 2)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = -60f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = -192f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 3)
					{
						ExcSpec.Plant45_DriftEscapeForce = 70f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 100f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 4)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = -60f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 5)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = -40f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 100f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 6)
					{
						ExcSpec.Plant45_DriftEscapeForce = 50f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 7)
					{
						ExcSpec.Plant45_DriftEscapeForce = 30f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0.0005f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 8)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = -40f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 9)
					{
						ExcSpec.Plant45_DriftEscapeForce = -20f;
						ExcSpec.Plant45_DriftMaxGauge = -60f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 10)
					{
						ExcSpec.Plant45_DriftEscapeForce = -60f;
						ExcSpec.Plant45_DriftMaxGauge = -100f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 11)
					{
						ExcSpec.Plant45_DriftEscapeForce = -40f;
						ExcSpec.Plant45_DriftMaxGauge = -80f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 12)
					{
						ExcSpec.Plant45_DriftEscapeForce = 10f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 13)
					{
						ExcSpec.Plant45_DriftEscapeForce = 30f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 14)
					{
						ExcSpec.Plant45_DriftEscapeForce = 50f;
						ExcSpec.Plant45_DriftMaxGauge = 40f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 15)
					{
						ExcSpec.Plant45_DriftEscapeForce = 70f;
						ExcSpec.Plant45_DriftMaxGauge = 60f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 16)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0.0005f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.005f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 17)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.005f;
						ExcSpec.Plant45_DragFactor = -0.0007f;
					}
					else if (existingPlant[7] == 18)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = -40f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.005f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 19)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.01f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 20)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = -30f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.01f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 21)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.015f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 22)
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 30f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = -0.02f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else if (existingPlant[7] == 23)
					{
						ExcSpec.Plant45_DriftEscapeForce = 90f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0.0005f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
					else
					{
						ExcSpec.Plant45_DriftEscapeForce = 0f;
						ExcSpec.Plant45_DriftMaxGauge = 0f;
						ExcSpec.Plant45_CornerDrawFactor = 0f;
						ExcSpec.Plant45_SlipBrake = 0f;
						ExcSpec.Plant45_AnimalBoosterTime = 0f;
						ExcSpec.Plant45_AntiCollideBalance = 0f;
						ExcSpec.Plant45_DragFactor = 0f;
					}
				}
				else if (existingPlant[8] == 46)
				{
					if (existingPlant[9] == 1)
					{
						ExcSpec.Plant46_DriftMaxGauge = -80f;
						ExcSpec.Plant46_NormalBoosterTime = 120f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 2)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = -0.03f;
						ExcSpec.Plant46_ForwardAccel = 2f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 3)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 200f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 5)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 90f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 50f;
						ExcSpec.Plant46_TeamBoosterTime = 60f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0.02f;
						ExcSpec.Plant46_StartForwardAccelItem = 0.02f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 7)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 105f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 8)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 105f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 11)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 3;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 12)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 3;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 15 || existingPlant[9] == 16)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 100f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 10f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 17)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 100f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 9f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 18)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 120f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 23)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 60f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 24)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 60f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 25)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 90f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = -30f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else if (existingPlant[9] == 26)
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = -30f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 90f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
					else
					{
						ExcSpec.Plant46_DriftMaxGauge = 0f;
						ExcSpec.Plant46_NormalBoosterTime = 0f;
						ExcSpec.Plant46_DriftSlipFactor = 0f;
						ExcSpec.Plant46_ForwardAccel = 0f;
						ExcSpec.Plant46_AnimalBoosterTime = 0f;
						ExcSpec.Plant46_TeamBoosterTime = 0f;
						ExcSpec.Plant46_StartForwardAccelSpeed = 0f;
						ExcSpec.Plant46_StartForwardAccelItem = 0f;
						ExcSpec.Plant46_StartBoosterTimeSpeed = 0f;
						ExcSpec.Plant46_StartBoosterTimeItem = 0f;
						ExcSpec.Plant46_ItemSlotCapacity = 0;
						ExcSpec.Plant46_SpeedSlotCapacity = 0;
						ExcSpec.Plant46_GripBrake = 0f;
						ExcSpec.Plant46_SlipBrake = 0f;
					}
				}
			}
			else
			{
				ExcSpec.Reset_Plant_SpecData();
			}
		}

		public static void Use_KartLevelSpec(short Set_Kart, short Set_KartSN)
		{
			var existingLevel = KartExcData.LevelList.FirstOrDefault(list => list[0] == Set_Kart && list[1] == Set_KartSN);
			if (existingLevel!= null)
			{
				if (existingLevel[4] == 0)
				{
					ExcSpec.KartLevel_DragFactor = 0f;
					ExcSpec.KartLevel_ForwardAccel = 0f;
				}
				else if(existingLevel[4] == 1)
				{
					ExcSpec.KartLevel_DragFactor = -0.0001f;
					ExcSpec.KartLevel_ForwardAccel = 0.1f;
				}
				else if (existingLevel[4] == 2)
				{
					ExcSpec.KartLevel_DragFactor = -0.0002f;
					ExcSpec.KartLevel_ForwardAccel = 0.2f;
				}
				else if (existingLevel[4] == 3)
				{
					ExcSpec.KartLevel_DragFactor = -0.0003f;
					ExcSpec.KartLevel_ForwardAccel = 0.3f;
				}
				else if (existingLevel[4] == 4)
				{
					ExcSpec.KartLevel_DragFactor = -0.0004f;
					ExcSpec.KartLevel_ForwardAccel = 0.4f;
				}
				else if (existingLevel[4] == 5)
				{
					ExcSpec.KartLevel_DragFactor = -0.0005f;
					ExcSpec.KartLevel_ForwardAccel = 0.5f;
				}
				else if (existingLevel[4] == 6)
				{
					ExcSpec.KartLevel_DragFactor = -0.0006f;
					ExcSpec.KartLevel_ForwardAccel = 0.6f;
				}
				else if (existingLevel[4] == 7)
				{
					ExcSpec.KartLevel_DragFactor = -0.0007f;
					ExcSpec.KartLevel_ForwardAccel = 0.7f;
				}
				else if (existingLevel[4] == 8)
				{
					ExcSpec.KartLevel_DragFactor = -0.0008f;
					ExcSpec.KartLevel_ForwardAccel = 0.8f;
				}
				else if (existingLevel[4] == 9)
				{
					ExcSpec.KartLevel_DragFactor = -0.001f;
					ExcSpec.KartLevel_ForwardAccel = 1f;
				}
				else if (existingLevel[4] == 10)
				{
					ExcSpec.KartLevel_DragFactor = -0.0012f;
					ExcSpec.KartLevel_ForwardAccel = 1.5f;
				}
				if (existingLevel[5] == 0)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0f;
					ExcSpec.KartLevel_SteerConstraint = 0f;
				}
				else if (existingLevel[5] == 1)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0001f;
					ExcSpec.KartLevel_SteerConstraint = 0.01f;
				}
				else if (existingLevel[5] == 2)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0002f;
					ExcSpec.KartLevel_SteerConstraint = 0.02f;
				}
				else if (existingLevel[5] == 3)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0003f;
					ExcSpec.KartLevel_SteerConstraint = 0.03f;
				}
				else if (existingLevel[5] == 4)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0004f;
					ExcSpec.KartLevel_SteerConstraint = 0.04f;
				}
				else if (existingLevel[5] == 5)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0005f;
					ExcSpec.KartLevel_SteerConstraint = 0.05f;
				}
				else if (existingLevel[5] == 6)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0006f;
					ExcSpec.KartLevel_SteerConstraint = 0.06f;
				}
				else if (existingLevel[5] == 7)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0007f;
					ExcSpec.KartLevel_SteerConstraint = 0.08f;
				}
				else if (existingLevel[5] == 8)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0008f;
					ExcSpec.KartLevel_SteerConstraint = 0.11f;
				}
				else if (existingLevel[5] == 9)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.0009f;
					ExcSpec.KartLevel_SteerConstraint = 0.15f;
				}
				else if (existingLevel[5] == 10)
				{
					ExcSpec.KartLevel_CornerDrawFactor = 0.001f;
					ExcSpec.KartLevel_SteerConstraint = 0.2f;
				}
				if (existingLevel[6] == 0)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 0f;
				}
				else if (existingLevel[6] == 1)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 1f;
				}
				else if (existingLevel[6] == 2)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 3f;
				}
				else if (existingLevel[6] == 3)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 6f;
				}
				else if (existingLevel[6] == 4)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 10f;
				}
				else if (existingLevel[6] == 5)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 15f;
				}
				else if (existingLevel[6] == 6)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 20f;
				}
				else if (existingLevel[6] == 7)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 26f;
				}
				else if (existingLevel[6] == 8)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 33f;
				}
				else if (existingLevel[6] == 9)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 40f;
				}
				else if (existingLevel[6] == 10)
				{
					ExcSpec.KartLevel_DriftEscapeForce = 50f;
				}
				if (existingLevel[7] == 0)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 0f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 0f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0f;
				}
				else if (existingLevel[7] == 1)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0001f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 5f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 5f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.001f;
				}
				else if (existingLevel[7] == 2)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0003f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 10f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 10f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.003f;
				}
				else if (existingLevel[7] == 3)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0006f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 15f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 15f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.005f;
				}
				else if (existingLevel[7] == 4)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.001f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 20f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 20f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.009f;
				}
				else if (existingLevel[7] == 5)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0014f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 30f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 30f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.013f;
				}
				else if (existingLevel[7] == 6)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0019f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 40f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 40f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.019f;
				}
				else if (existingLevel[7] == 7)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0025f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 50f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 50f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.025f;
				}
				else if (existingLevel[7] == 8)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.0032f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 65f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 65f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.033f;
				}
				else if (existingLevel[7] == 9)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.004f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 80f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 80f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.041f;
				}
				else if (existingLevel[7] == 10)
				{
					ExcSpec.KartLevel_TransAccelFactor = 0.005f;
					ExcSpec.KartLevel_StartBoosterTimeSpeed = 100f;
					ExcSpec.KartLevel_StartBoosterTimeItem = 100f;
					ExcSpec.KartLevel_BoostAccelFactorOnlyItem = 0.05f;
				}
			}
			else
			{
				ExcSpec.Reset_KartLevel_SpecData();
			}
		}

		public static void Use_PartsSpec(short id, short sn)
		{
			var existingParts = KartExcData.PartsList.FirstOrDefault(list => list[0] == id && list[1] == sn);
			if (existingParts!= null)
			{
				for (short i = 63; i < 67; i++)
				{
					if (i == 63)
					{
						short Item_Id = existingParts[2];
						short Grade = existingParts[3];
						short PartsValue = existingParts[4];
						if (PartsValue != 0)
						{
							ExcSpec.PartSpec_TransAccelFactor = (float)(((decimal)existingParts[4] * 1.0M - 800.0M) / 25000.0M + 1.85M + -0.205M);
						}
						else
						{
							ExcSpec.PartSpec_TransAccelFactor = 0f;
						}
					}
					else if (i == 64)
					{
						short Item_Id = existingParts[5];
						short Grade = (byte)existingParts[6];
						short PartsValue = existingParts[7];
						if (PartsValue != 0)
						{
							ExcSpec.PartSpec_SteerConstraint = (float)(((decimal)existingParts[7] * 1.0M - 800.0M) / 250.0M + 2.1M + 20.3M);
						}
						else
						{
							ExcSpec.PartSpec_SteerConstraint = 0f;
						}
					}
					else if (i == 65)
					{
						short Item_Id = existingParts[8];
						short Grade = (byte)existingParts[9];
						short PartsValue = existingParts[10];
						if (PartsValue != 0)
						{
							ExcSpec.PartSpec_DriftEscapeForce = (float)((decimal)existingParts[10] * 2.0M + 2200.0M);
						}
						else
						{
							ExcSpec.PartSpec_DriftEscapeForce = 0f;
						}
					}
					else if (i == 66)
					{
						short Item_Id = existingParts[11];
						short Grade = (byte)existingParts[12];
						short PartsValue = existingParts[13];
						if (PartsValue != 0)
						{
							ExcSpec.PartSpec_NormalBoosterTime = (float)((decimal)existingParts[13] * 1.0M - 940.0M + 3000.0M);
						}
						else
						{
							ExcSpec.PartSpec_NormalBoosterTime = 0f;
						}
					}
					Console.WriteLine("-------------------------------------------------------------");
					Console.WriteLine("TransAccelFactor:{0}", PartSpec_TransAccelFactor);
					Console.WriteLine("SteerConstraint:{0}", PartSpec_SteerConstraint);
					Console.WriteLine("DriftEscapeForce:{0}", PartSpec_DriftEscapeForce);
					Console.WriteLine("NormalBoosterTime:{0}", PartSpec_NormalBoosterTime);
					Console.WriteLine("-------------------------------------------------------------");
				}
			}
			else
			{
				ExcSpec.Reset_PartSpec_SpecData();
			}
		}
	}
}
