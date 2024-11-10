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
		public static short[] partsV2 = new short[] { 0, 2, 4, 7, 10, 13, 17, 21, 25, 30 };

		public static void ExceedSpec()
		{
			if (Kart.defaultExceedType > 0)
			{
				Kart.TransAccelFactor = Kart.TransAccelFactor + 0.45254f;
				Kart.SteerConstraint = Kart.SteerConstraint + 0.304f;
				Kart.DriftEscapeForce = Kart.DriftEscapeForce + 402f;
				Kart.NormalBoosterTime = Kart.NormalBoosterTime + -59f;
				var kartAndSN = new { Id = SetRiderItem.Set_Kart, Sn = SetRiderItem.Set_KartSN };
				var parts12List = KartExcData.Parts12List;
				var existingParts = parts12List.FirstOrDefault(list => list[0] == kartAndSN.Id && list[1] == kartAndSN.Sn);
				if (existingParts != null)
				{
					Console.WriteLine("-------------------------------------------------------------");
					int TransAccelFactor = (int)existingParts[2];
					if (TransAccelFactor > 0)
						TransAccelFactor = TransAccelFactor - 1;
					float V2_TransAccelFactor = (float)(0.00004M * (decimal)partsV2[TransAccelFactor]);
					Kart.TransAccelFactor = Kart.TransAccelFactor + V2_TransAccelFactor;
					Console.WriteLine("V2_TransAccelFactor: " + V2_TransAccelFactor);
					int SteerConstraint = (int)existingParts[3];
					if (SteerConstraint > 0)
						SteerConstraint = SteerConstraint - 1;
					float V2_SteerConstraint = (float)(0.004M / 3 * (decimal)partsV2[SteerConstraint]);
					Kart.SteerConstraint = Kart.SteerConstraint + V2_SteerConstraint;
					Console.WriteLine("V2_SteerConstraint: " + V2_SteerConstraint);
					int DriftEscapeForce = (int)existingParts[4];
					if (DriftEscapeForce > 0)
						DriftEscapeForce = DriftEscapeForce - 1;
					float V2_DriftEscapeForce = (float)(2M * (decimal)partsV2[DriftEscapeForce]);
					Kart.DriftEscapeForce = Kart.DriftEscapeForce + V2_DriftEscapeForce;
					Console.WriteLine("V2_DriftEscapeForce: " + V2_DriftEscapeForce);
					int NormalBoosterTime = (int)existingParts[5];
					if (NormalBoosterTime > 0)
						NormalBoosterTime = NormalBoosterTime - 1;
					float V2_NormalBoosterTime = (float)(1M * (decimal)partsV2[NormalBoosterTime]);
					Kart.NormalBoosterTime = Kart.NormalBoosterTime + V2_NormalBoosterTime;
					Console.WriteLine("V2_NormalBoosterTime: " + V2_NormalBoosterTime);
					Console.WriteLine("-------------------------------------------------------------");
				}
				var level12List = KartExcData.Level12List;
				var existingLevel = level12List.FirstOrDefault(list => list[0] == kartAndSN.Id && list[1] == kartAndSN.Sn);
				if (existingLevel != null)
				{
					Dictionary<short, short> Skill = new Dictionary<short, short>();
					if (existingLevel[3] != 0)
						Skill.Add(existingLevel[3], existingLevel[4]);
					if (existingLevel[3] != 0)
						Skill.Add(existingLevel[5], existingLevel[6]);
					if (existingLevel[3] != 0)
						Skill.Add(existingLevel[7], existingLevel[8]);
					Console.WriteLine("-------------------------------------------------------------");
					if (Skill.Keys.Contains(1))
					{
						int ForwardAccelForce = (int)Skill[1];
						if (ForwardAccelForce > 0)
						{
							float V2_ForwardAccelForce = 0.35f;
							if (ForwardAccelForce != 1)
								V2_ForwardAccelForce = (float)(0.35M * (decimal)partsV2[ForwardAccelForce - 1]);
							Kart.ForwardAccelForce = Kart.ForwardAccelForce + V2_ForwardAccelForce;
							Console.WriteLine("V2_ForwardAccelForce: " + V2_ForwardAccelForce);
						}
					}
					if (Skill.Keys.Contains(2))
					{
						int CornerDrawFactor = (int)Skill[2];
						if (CornerDrawFactor > 0)
						{
							float V2_CornerDrawFactor = 0.00015f;
							if (CornerDrawFactor != 1)
								V2_CornerDrawFactor = (float)(0.00015M * (decimal)partsV2[CornerDrawFactor - 1]);
							Kart.CornerDrawFactor = Kart.CornerDrawFactor + V2_CornerDrawFactor;
							Console.WriteLine("V2_CornerDrawFactor: " + V2_CornerDrawFactor);
						}
					}
					if (Skill.Keys.Contains(3))
					{
						int DragFactor = (int)Skill[3];
						if (DragFactor > 0)
						{
							float V2_DragFactor = -0.000225f;
							if (DragFactor != 1)
								V2_DragFactor = (float)(-0.000225M * (decimal)partsV2[DragFactor - 1]);
							Kart.DragFactor = Kart.DragFactor + V2_DragFactor;
							Console.WriteLine("V2_DragFactor: " + V2_DragFactor);
						}
					}
					if (Skill.Keys.Contains(4))
					{
						int NormalBoosterTime = (int)Skill[4];
						if (NormalBoosterTime > 0)
						{
							float V2_NormalBoosterTime = 15f;
							if (NormalBoosterTime != 1)
								V2_NormalBoosterTime = (float)(15M * (decimal)partsV2[NormalBoosterTime - 1]);
							Kart.NormalBoosterTime = Kart.NormalBoosterTime + V2_NormalBoosterTime;
							Console.WriteLine("V2_NormalBoosterTime: " + V2_NormalBoosterTime);
						}
					}
					if (Skill.Keys.Contains(5))
					{
						int TeamBoosterTime = (int)Skill[5];
						if (TeamBoosterTime > 0)
						{
							float V2_TeamBoosterTime = 20f;
							if (TeamBoosterTime != 1)
								V2_TeamBoosterTime = (float)(20M * (decimal)partsV2[TeamBoosterTime - 1]);
							Kart.TeamBoosterTime = Kart.TeamBoosterTime + V2_TeamBoosterTime;
							Console.WriteLine("V2_TeamBoosterTime: " + V2_TeamBoosterTime);
						}
					}
					if (Skill.Keys.Contains(6))
					{
						int StartBoosterTimeSpeed = (int)Skill[6];
						if (StartBoosterTimeSpeed > 0)
						{
							float V2_StartBoosterTimeSpeed = 70f;
							if (StartBoosterTimeSpeed != 1)
								V2_StartBoosterTimeSpeed = (float)(70M * (decimal)partsV2[StartBoosterTimeSpeed - 1]);
							Kart.StartBoosterTimeSpeed = Kart.StartBoosterTimeSpeed + V2_StartBoosterTimeSpeed;
							Console.WriteLine("V2_StartBoosterTimeSpeed: " + V2_StartBoosterTimeSpeed);
						}
					}
					if (Skill.Keys.Contains(7))
					{
						int TransAccelFactor = (int)Skill[7];
						if (TransAccelFactor > 0)
						{
							float V2_TransAccelFactor = 0.001f;
							if (TransAccelFactor != 1)
								V2_TransAccelFactor = (float)(0.001M * (decimal)partsV2[TransAccelFactor - 1]);
							Kart.TransAccelFactor = Kart.TransAccelFactor + V2_TransAccelFactor;
							Console.WriteLine("V2_TransAccelFactor: " + V2_TransAccelFactor);
						}
					}
					if (Skill.Keys.Contains(8))
					{
						int DriftEscapeForce = (int)Skill[8];
						if (DriftEscapeForce > 0)
						{
							float V2_DriftEscapeForce = 10.5f;
							if (DriftEscapeForce != 1)
								V2_DriftEscapeForce = (float)(10.5M * (decimal)partsV2[DriftEscapeForce - 1]);
							Kart.DriftEscapeForce = Kart.DriftEscapeForce + V2_DriftEscapeForce;
							Console.WriteLine("V2_DriftEscapeForce: " + V2_DriftEscapeForce);
						}
					}
					if (Skill.Keys.Contains(9))
					{
						int DriftMaxGauge = (int)Skill[9];
						if (DriftMaxGauge > 0)
						{
							float V2_DriftMaxGauge = -12f;
							if (DriftMaxGauge != 1)
								V2_DriftMaxGauge = (float)(-12M * (decimal)partsV2[DriftMaxGauge - 1]);
							Kart.DriftMaxGauge = Kart.DriftMaxGauge + V2_DriftMaxGauge;
							Console.WriteLine("V2_DriftMaxGauge: " + V2_DriftMaxGauge);
						}
					}
					Console.WriteLine("-------------------------------------------------------------");
				}
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
