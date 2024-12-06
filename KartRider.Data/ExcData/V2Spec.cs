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
		public static short[] LevelV2 = new short[] { 0, 1, 2, 4, 7, 10 };

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

		public static void ExceedSpec()
		{
			if (Kart.defaultExceedType > 0)
			{
				var kartAndSN = new { Id = SetRiderItem.Set_Kart, Sn = SetRiderItem.Set_KartSN };
				var parts12List = KartExcData.Parts12List;
				var existingParts = parts12List.FirstOrDefault(list => list[0] == kartAndSN.Id && list[1] == kartAndSN.Sn);
				Console.WriteLine("-------------------------------------------------------------");
				short Parts_TransAccelFactor = 1;
				if (existingParts != null && existingParts[2] > 0) Parts_TransAccelFactor = existingParts[2];
				float V2Parts_TransAccelFactor = (float)((Get12Parts(Parts_TransAccelFactor) * 1.0M - 800M) / 25000.0M + 0.4765M);
				Kart.TransAccelFactor = Kart.TransAccelFactor + V2Parts_TransAccelFactor;
				Console.WriteLine("V2Parts_TransAccelFactor: " + V2Parts_TransAccelFactor);

				short Parts_SteerConstraint = 1;
				if (existingParts != null && existingParts[3] > 0) Parts_SteerConstraint = existingParts[3];
				float V2Parts_SteerConstraint = (float)(((Get12Parts(Parts_SteerConstraint) * 1.0M - 800M) / 250.0M + 3.308M) / 3M);
				Kart.SteerConstraint = Kart.SteerConstraint + V2Parts_SteerConstraint;
				Console.WriteLine("V2Parts_SteerConstraint: " + V2Parts_SteerConstraint);

				short Parts_DriftEscapeForce = 1;
				if (existingParts != null && existingParts[4] > 0) Parts_DriftEscapeForce = existingParts[4];
				float V2Parts_DriftEscapeForce = (float)(Get12Parts(Parts_DriftEscapeForce) * 2.0M);
				Kart.DriftEscapeForce = Kart.DriftEscapeForce + V2Parts_DriftEscapeForce;
				Console.WriteLine("V2Parts_DriftEscapeForce: " + V2Parts_DriftEscapeForce);

				short Parts_NormalBoosterTime = 1;
				if (existingParts != null && existingParts[5] > 0) Parts_NormalBoosterTime = existingParts[5];
				float V2Parts_NormalBoosterTime = (float)(Get12Parts(Parts_NormalBoosterTime) * 1.0M - 260M);
				Kart.NormalBoosterTime = Kart.NormalBoosterTime + V2Parts_NormalBoosterTime;
				Console.WriteLine("V2Parts_NormalBoosterTime: " + V2Parts_NormalBoosterTime);
				Console.WriteLine("-------------------------------------------------------------");

				var level12List = KartExcData.Level12List;
				var existingLevel = level12List.FirstOrDefault(list => list[0] == kartAndSN.Id && list[1] == kartAndSN.Sn);
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
					foreach (var skill in Skill)
					{
						if (skill[0] == 1)
						{
							int Level_ForwardAccelForce = (int)skill[1];
							if (Level_ForwardAccelForce > 0)
							{
								float V2Level_ForwardAccelForce = (float)(0.35M * (decimal)LevelV2[Level_ForwardAccelForce]);
								Kart.ForwardAccelForce = Kart.ForwardAccelForce + V2Level_ForwardAccelForce;
								Console.WriteLine("V2Level_ForwardAccelForce: " + V2Level_ForwardAccelForce);
							}
						}
						if (skill[0] == 2)
						{
							int Level_CornerDrawFactor = (int)skill[1];
							if (Level_CornerDrawFactor > 0)
							{
								float V2Level_CornerDrawFactor = (float)(0.00015M * (decimal)LevelV2[Level_CornerDrawFactor]);
								Kart.CornerDrawFactor = Kart.CornerDrawFactor + V2Level_CornerDrawFactor;
								Console.WriteLine("V2Level_CornerDrawFactor: " + V2Level_CornerDrawFactor);
							}
						}
						if (skill[0] == 3)
						{
							int Level_DragFactor = (int)skill[1];
							if (Level_DragFactor > 0)
							{
								float V2Level_DragFactor = (float)(-0.000225M * (decimal)LevelV2[Level_DragFactor]);
								Kart.DragFactor = Kart.DragFactor + V2Level_DragFactor;
								Console.WriteLine("V2Level_DragFactor: " + V2Level_DragFactor);
							}
						}
						if (skill[0] == 4)
						{
							int Level_NormalBoosterTime = (int)skill[1];;
							if (Level_NormalBoosterTime > 0)
							{
								float V2Level_NormalBoosterTime = (float)(15M * (decimal)LevelV2[Level_NormalBoosterTime]);
								Kart.NormalBoosterTime = Kart.NormalBoosterTime + V2Level_NormalBoosterTime;
								Console.WriteLine("V2Level_NormalBoosterTime: " + V2Level_NormalBoosterTime);
							}
						}
						if (skill[0] == 5)
						{
							int Level_TeamBoosterTime = (int)skill[1];
							if (Level_TeamBoosterTime > 0)
							{
								float V2Level_TeamBoosterTime = (float)(20M * (decimal)LevelV2[Level_TeamBoosterTime]);
								Kart.TeamBoosterTime = Kart.TeamBoosterTime + V2Level_TeamBoosterTime;
								Console.WriteLine("V2Level_TeamBoosterTime: " + V2Level_TeamBoosterTime);
							}
						}
						if (skill[0] == 6)
						{
							int Level_StartBoosterTimeSpeed = (int)skill[1];
							if (Level_StartBoosterTimeSpeed > 0)
							{
								float V2Level_StartBoosterTimeSpeed = (float)(70M * (decimal)LevelV2[Level_StartBoosterTimeSpeed]);
								Kart.StartBoosterTimeSpeed = Kart.StartBoosterTimeSpeed + V2Level_StartBoosterTimeSpeed;
								Console.WriteLine("V2Level_StartBoosterTimeSpeed: " + V2Level_StartBoosterTimeSpeed);
							}
						}
						if (skill[0] == 7)
						{
							int Level_TransAccelFactor = (int)skill[1];
							if (Level_TransAccelFactor > 0)
							{
								float V2Level_TransAccelFactor = (float)(0.001M * (decimal)LevelV2[Level_TransAccelFactor]);
								Kart.TransAccelFactor = Kart.TransAccelFactor + V2Level_TransAccelFactor;
								Console.WriteLine("V2Level_TransAccelFactor: " + V2Level_TransAccelFactor);
							}
						}
						if (skill[0] == 8)
						{
							int Level_DriftEscapeForce = (int)skill[1];
							if (Level_DriftEscapeForce > 0)
							{
								float V2Level_DriftEscapeForce = (float)(10.5M * (decimal)LevelV2[Level_DriftEscapeForce]);
								Kart.DriftEscapeForce = Kart.DriftEscapeForce + V2Level_DriftEscapeForce;
								Console.WriteLine("V2Level_DriftEscapeForce: " + V2Level_DriftEscapeForce);
							}
						}
						if (skill[0] == 9)
						{
							int Level_DriftMaxGauge = (int)skill[1];
							if (Level_DriftMaxGauge > 0)
							{
								float V2Level_DriftMaxGauge = (float)(-12M * (decimal)LevelV2[Level_DriftMaxGauge]);
								Kart.DriftMaxGauge = Kart.DriftMaxGauge + V2Level_DriftMaxGauge;
								Console.WriteLine("V2Level_DriftMaxGauge: " + V2Level_DriftMaxGauge);
							}
						}
					}
					Console.WriteLine("-------------------------------------------------------------");
				}
				if (Kart.defaultExceedType == 1)
				{
					Kart.chargeInstAccelGaugeByBoost = 0.016f;
					Kart.chargeInstAccelGaugeByGrip = 0.07f;
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
				if (Kart.defaultExceedType == 2)
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
				}
				if (Kart.defaultExceedType == 3)
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
				}
				if (Kart.defaultExceedType == 4)
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
				}
			}
		}
	}
}
