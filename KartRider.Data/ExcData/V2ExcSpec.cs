using KartRider;
using Set_Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcData
{
	public static class V2Spec
	{
		public static float V2Parts_TransAccelFactor = 0f;
		public static float V2Parts_SteerConstraint = 0f;
		public static float V2Parts_DriftEscapeForce = 0f;
		public static float V2Parts_NormalBoosterTime = 0f;

		public static float V2Level_ForwardAccelForce = 0f;
		public static float V2Level_CornerDrawFactor = 0f;
		public static float V2Level_DragFactor = 0f;
		public static float V2Level_NormalBoosterTime = 0f;
		public static float V2Level_TeamBoosterTime = 0f;
		public static float V2Level_StartBoosterTimeSpeed = 0f;
		public static float V2Level_TransAccelFactor = 0f;
		public static float V2Level_DriftEscapeForce = 0f;
		public static float V2Level_DriftMaxGauge = 0f;

		public static byte GetGrade(byte leve)
		{
			if (leve > 40)
			{
				return 1;
			}
			else if (leve > 30)
			{
				return 2;
			}
			else if (leve > 20)
			{
				return 3;
			}
			else if (leve > 1)
			{
				return 4;
			}
			return 0;
		}

		public static short Get12Parts(short input)
		{
			if (input < 11)
			{
				int increment = 2;
				int result = 0;
				for (int i = 1; i <= (int)input; i++)
				{
					if (i % 3 == 1)
					{
						increment++;
					}
					result += increment;
				}
				return (short)(result + 199 - (int)input);
			}
			else if (input < 21)
			{
				int increment = 2;
				int result = 0;
				for (int i = 1; i <= (int)input - 10; i++)
				{
					if (i % 3 == 1)
					{
						increment++;
					}
					result += increment;
				}
				return (short)(result + 232 - (int)input);
			}
			else if (input < 31)
			{
				int increment = 2;
				int result = 0;
				for (int i = 1; i <= (int)input - 20; i++)
				{
					if (i % 3 == 1)
					{
						increment++;
					}
					result += increment;
				}
				return (short)(result + 265 - (int)input);
			}
			else if (input < 41)
			{
				int increment = 2;
				int result = 0;
				for (int i = 1; i <= (int)input - 30; i++)
				{
					if (i % 3 == 1)
					{
						increment++;
					}
					result += increment;
				}
				return (short)(result + 298 - (int)input);
			}
			else
			{
				return (short)(201);
			}
		}

		public static void Reset_V2Level_SpecData()
		{
			V2Level_ForwardAccelForce = 0f;
			V2Level_CornerDrawFactor = 0f;
			V2Level_DragFactor = 0f;
			V2Level_NormalBoosterTime = 0f;
			V2Level_TeamBoosterTime = 0f;
			V2Level_StartBoosterTimeSpeed = 0f;
			V2Level_TransAccelFactor = 0f;
			V2Level_DriftEscapeForce = 0f;
			V2Level_DriftMaxGauge = 0f;
		}

		public static void Reset_V2Parts_SpecData()
		{
			V2Parts_TransAccelFactor = 0f;
			V2Parts_SteerConstraint = 0f;
			V2Parts_DriftEscapeForce = 0f;
			V2Parts_NormalBoosterTime = 0f;
		}

		public static void ExceedSpec()
		{
			if (Kart.defaultExceedType > 0)
			{
				var KartAndSN = new { Kart = SetRiderItem.Set_Kart, SN = SetRiderItem.Set_KartSN };
				var existingParts = KartExcData.Parts12List.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);
				if (existingParts != null && existingParts[17] != 0)
				{
					Kart.defaultExceedType = (int)existingParts[17];
				}
				Console.WriteLine("-------------------------------------------------------------");
				short Parts_TransAccelFactor;
				if (existingParts == null || existingParts[4] < 1)
				{
					Parts_TransAccelFactor = Get12Parts((short)Kart.defaultEngineType);
				}
				else
				{
					Kart.EngineType = (byte)existingParts[2];
					Parts_TransAccelFactor = (short)(Get12Parts((short)Kart.defaultEngineType) + existingParts[4]);
				}
				V2Parts_TransAccelFactor = (float)((Parts_TransAccelFactor * 1.0M - 800M) / 25000.0M + 0.4765M);
				Console.WriteLine("V2Parts_TransAccelFactor: " + V2Parts_TransAccelFactor);

				short Parts_SteerConstraint;
				if (existingParts == null || existingParts[7] < 1)
				{
					Parts_SteerConstraint = Get12Parts((short)Kart.defaultHandleType);
				}
				else
				{
					Kart.HandleType = (byte)existingParts[5];
					Parts_SteerConstraint = (short)(Get12Parts((short)Kart.defaultHandleType) + existingParts[7]);
				}
				V2Parts_SteerConstraint = (float)((Parts_SteerConstraint * 1.0M - 800M) / 250.0M + 2.7M);
				Console.WriteLine("V2Parts_SteerConstraint: " + V2Parts_SteerConstraint);

				short Parts_DriftEscapeForce;
				if (existingParts == null || existingParts[10] < 1)
				{
					Parts_DriftEscapeForce = Get12Parts((short)Kart.defaultWheelType);
				}
				else
				{
					Kart.WheelType = (byte)existingParts[8];
					Parts_DriftEscapeForce = (short)(Get12Parts((short)Kart.defaultWheelType) + existingParts[10]);
				}
				V2Parts_DriftEscapeForce = (float)(Parts_DriftEscapeForce * 2.0M);
				Console.WriteLine("V2Parts_DriftEscapeForce: " + V2Parts_DriftEscapeForce);

				short Parts_NormalBoosterTime;
				if (existingParts == null || existingParts[13] < 1)
				{
					Parts_NormalBoosterTime = Get12Parts((short)Kart.defaultBoosterType);
				}
				else
				{
					Kart.BoosterType = (byte)existingParts[11];
					Parts_NormalBoosterTime = (short)(Get12Parts((short)Kart.defaultBoosterType) + existingParts[13]);
				}
				V2Parts_NormalBoosterTime = (float)(Parts_NormalBoosterTime * 1.0M - 260M);
				Console.WriteLine("V2Parts_NormalBoosterTime: " + V2Parts_NormalBoosterTime);
				Console.WriteLine("-------------------------------------------------------------");

				Reset_V2Level_SpecData();
				var existingLevel = KartExcData.Level12List.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);
				if (existingLevel != null)
				{
					List<List<short>> Skill = new List<List<short>>();
					if (existingLevel[3] != 0)
						Skill.Add(new List<short> { existingLevel[3], existingLevel[4] });
					if (existingLevel[5] != 0)
						Skill.Add(new List<short> { existingLevel[5], existingLevel[6] });
					if (existingLevel[7] != 0)
						Skill.Add(new List<short> { existingLevel[7], existingLevel[8] });
					Console.WriteLine("-------------------------------------------------------------");
					float[] ForwardAccelForce = { 0f, 1.5f, 1.7f, 2f, 2.5f, 3.5f };
					float[] CornerDrawFactor = { 0f, 0.0007f, 0.0008f, 0.001f, 0.0012f, 0.0015f };
					float[] DragFactor = { 0f, -0.0008f, -0.001f, -0.0013f, -0.0017f, -0.00225f };
					float[] NormalBoosterTime = { 0f, 50f, 70f, 90f, 120f, 150f };
					float[] TeamBoosterTime = { 0f, 100f, 110f, 130f, 150f, 200f };
					float[] StartBoosterTimeSpeed = { 0f, 150f, 200f, 300f, 450f, 700f };
					float[] TransAccelFactor = { 0f, 0.003f, 0.004f, 0.005f, 0.007f, 0.01f };
					float[] DriftEscapeForce = { 0f, 35f, 50f, 65f, 90f, 105f };
					float[] DriftMaxGauge = { 0f, -50f, -60f, -70f, -90f, -120f };
					foreach (var skill in Skill)
					{
						if (skill[0] == 1)
						{
							V2Level_ForwardAccelForce = ForwardAccelForce[(int)skill[1]];
							Console.WriteLine("V2Level_ForwardAccelForce: " + V2Level_ForwardAccelForce);
						}
						if (skill[0] == 2)
						{
							V2Level_CornerDrawFactor = CornerDrawFactor[(int)skill[1]];
							Console.WriteLine("V2Level_CornerDrawFactor: " + V2Level_CornerDrawFactor);
						}
						if (skill[0] == 3)
						{
							V2Level_DragFactor = DragFactor[(int)skill[1]];
							Console.WriteLine("V2Level_DragFactor: " + V2Level_DragFactor);
						}
						if (skill[0] == 4)
						{
							V2Level_NormalBoosterTime = NormalBoosterTime[(int)skill[1]];
							Console.WriteLine("V2Level_NormalBoosterTime: " + V2Level_NormalBoosterTime);
						}
						if (skill[0] == 5)
						{
							V2Level_TeamBoosterTime = TeamBoosterTime[(int)skill[1]];
							Console.WriteLine("V2Level_TeamBoosterTime: " + V2Level_TeamBoosterTime);
						}
						if (skill[0] == 6)
						{
							V2Level_StartBoosterTimeSpeed = StartBoosterTimeSpeed[(int)skill[1]];
							Console.WriteLine("V2Level_StartBoosterTimeSpeed: " + V2Level_StartBoosterTimeSpeed);
						}
						if (skill[0] == 7)
						{
							V2Level_TransAccelFactor = TransAccelFactor[(int)skill[1]];
							Console.WriteLine("V2Level_TransAccelFactor: " + V2Level_TransAccelFactor);
						}
						if (skill[0] == 8)
						{
							V2Level_DriftEscapeForce = DriftEscapeForce[(int)skill[1]];
							Console.WriteLine("V2Level_DriftEscapeForce: " + V2Level_DriftEscapeForce);
						}
						if (skill[0] == 9)
						{
							V2Level_DriftMaxGauge = DriftMaxGauge[(int)skill[1]];
							Console.WriteLine("V2Level_DriftMaxGauge: " + V2Level_DriftMaxGauge);
						}
					}
					Console.WriteLine("-------------------------------------------------------------");
				}
				if (Kart.defaultExceedType == 1)//item S
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

					Kart.chargeAntiCollideBalance = 0f;
					Kart.chargeInstAccelGaugeByWallAdded = 0f;
					Kart.chargeInstAccelGaugeByBoostAdded = 0f;
					Kart.chargerSystemboosterUseCount = 0;
					Kart.chargerSystemUseTime = 0;
					Kart.chargeBoostBySpeedAdded = 0f;
					Kart.driftGaugeFactor = 0f;
				}
				if (Kart.defaultExceedType == 2)//S
				{
					Kart.chargeInstAccelGaugeByBoost = 0.02f;
					Kart.chargeInstAccelGaugeByGrip = 0.07f;
					Kart.chargeInstAccelGaugeByWall = 0.15f;
					Kart.instAccelFactor = 1.29f;
					Kart.instAccelGaugeCooldownTime = 3000;
					Kart.instAccelGaugeLength = 1040f;
					Kart.instAccelGaugeMinUsable = 208f;
					Kart.instAccelGaugeMinVelBound = 0f;
					Kart.instAccelGaugeMinVelLoss = 50f;
					Kart.useExtendedAfterBoosterMore = 0;
					Kart.wallCollGaugeCooldownTime = 3000;
					Kart.wallCollGaugeMaxVelLoss = 200f;
					Kart.wallCollGaugeMinVelBound = 200f;
					Kart.wallCollGaugeMinVelLoss = 50f;

					Kart.chargeAntiCollideBalance = 0.8f;
					Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
					Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
					Kart.chargerSystemboosterUseCount = 4;
					Kart.chargerSystemUseTime = 3000;
					Kart.chargeBoostBySpeedAdded = 350.0f;
					Kart.driftGaugeFactor = 2.0f;
				}
				if (Kart.defaultExceedType == 3)//B
				{
					Kart.chargeInstAccelGaugeByBoost = 0.02f;
					Kart.chargeInstAccelGaugeByGrip = 0.07f;
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

					Kart.chargeAntiCollideBalance = 0.8f;
					Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
					Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
					Kart.chargerSystemboosterUseCount = 5;
					Kart.chargerSystemUseTime = 3750;
					Kart.chargeBoostBySpeedAdded = 350.0f;
					Kart.driftGaugeFactor = 2.0f;
				}
				if (Kart.defaultExceedType == 4)//L
				{
					Kart.chargeInstAccelGaugeByBoost = 0.02f;
					Kart.chargeInstAccelGaugeByGrip = 0.07f;
					Kart.chargeInstAccelGaugeByWall = 0.15f;
					Kart.instAccelFactor = 1.14f;
					Kart.instAccelGaugeCooldownTime = 3000;
					Kart.instAccelGaugeLength = 2500f;
					Kart.instAccelGaugeMinUsable = 500f;
					Kart.instAccelGaugeMinVelBound = 0f;
					Kart.instAccelGaugeMinVelLoss = 50f;
					Kart.useExtendedAfterBoosterMore = 0;
					Kart.wallCollGaugeCooldownTime = 3000;
					Kart.wallCollGaugeMaxVelLoss = 200f;
					Kart.wallCollGaugeMinVelBound = 200f;
					Kart.wallCollGaugeMinVelLoss = 50f;

					Kart.chargeAntiCollideBalance = 0.8f;
					Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
					Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
					Kart.chargerSystemboosterUseCount = 6;
					Kart.chargerSystemUseTime = 4500;
					Kart.chargeBoostBySpeedAdded = 350.0f;
					Kart.driftGaugeFactor = 2.0f;
				}
				if (Kart.defaultExceedType == 5)//?
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
					Kart.useExtendedAfterBoosterMore = 0;
					Kart.wallCollGaugeCooldownTime = 3000;
					Kart.wallCollGaugeMaxVelLoss = 200f;
					Kart.wallCollGaugeMinVelBound = 200;
					Kart.wallCollGaugeMinVelLoss = 50f;

					Kart.chargeAntiCollideBalance = 0f;
					Kart.chargeInstAccelGaugeByWallAdded = 0f;
					Kart.chargeInstAccelGaugeByBoostAdded = 0f;
					Kart.chargerSystemboosterUseCount = 0;
					Kart.chargerSystemUseTime = 0;
					Kart.chargeBoostBySpeedAdded = 0f;
					Kart.driftGaugeFactor = 0f;
				}
				if (Kart.defaultExceedType == 6)//L+
				{
					Kart.chargeInstAccelGaugeByBoost = 0.017f;
					Kart.chargeInstAccelGaugeByGrip = 0.07f;
					Kart.chargeInstAccelGaugeByWall = 0.15f;
					Kart.instAccelFactor = 1.14f;
					Kart.instAccelGaugeCooldownTime = 3000;
					Kart.instAccelGaugeLength = 3000f;
					Kart.instAccelGaugeMinUsable = 600f;
					Kart.instAccelGaugeMinVelBound = 0f;
					Kart.instAccelGaugeMinVelLoss = 50f;
					Kart.useExtendedAfterBoosterMore = 0;
					Kart.wallCollGaugeCooldownTime = 3000;
					Kart.wallCollGaugeMaxVelLoss = 200f;
					Kart.wallCollGaugeMinVelBound = 200f;
					Kart.wallCollGaugeMinVelLoss = 50f;

					Kart.chargeAntiCollideBalance = 0.8f;
					Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
					Kart.chargeInstAccelGaugeByBoostAdded = 0.02f;
					Kart.chargerSystemboosterUseCount = 6;
					Kart.chargerSystemUseTime = 4500;
					Kart.chargeBoostBySpeedAdded = 350.0f;
					Kart.driftGaugeFactor = 2.0f;
				}
			}
			else
			{
				Reset_V2Parts_SpecData();
				Reset_V2Level_SpecData();
			}
		}
	}
}
