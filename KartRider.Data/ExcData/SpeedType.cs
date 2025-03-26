using System;
using KartRider;
using Set_Data;

namespace ExcData
{
	public class SpeedType
	{
		public static float AddSpec_TransAccelFactor = 0f;
		public static float AddSpec_SteerConstraint = 0f;
		public static float AddSpec_DriftEscapeForce = 0f;

		public static float DragFactor = 0f;
		public static float ForwardAccelForce = 0f;
		public static float BackwardAccelForce = 0f;
		public static float GripBrakeForce = 0f;
		public static float SlipBrakeForce = 0f;
		public static float SteerConstraint = 0f;
		public static float DriftEscapeForce = 0f;
		public static float CornerDrawFactor = 0f;
		public static float DriftMaxGauge = 0f;
		public static float TransAccelFactor = 0f;
		public static float BoostAccelFactor = 0f;
		public static float StartForwardAccelForceItem = 0f;
		public static float StartForwardAccelForceSpeed = 0f;

		public static void SpeedTypeData()
		{
			if (config.SpeedType == 3)//S0 보통
			{
				SpeedType.AddSpec_SteerConstraint = -0.3f;
				SpeedType.AddSpec_DriftEscapeForce = -350f;
				SpeedType.AddSpec_TransAccelFactor = -0.015f;
				SpeedType.DragFactor = -0.05f;
				SpeedType.ForwardAccelForce = -530f;
				SpeedType.BackwardAccelForce = -225f;
				SpeedType.GripBrakeForce = -570f;
				SpeedType.SlipBrakeForce = -215f;
				SpeedType.SteerConstraint = -2.25f;
				SpeedType.DriftEscapeForce = -750f;
				SpeedType.CornerDrawFactor = -0.05f;
				SpeedType.DriftMaxGauge = 750f;
				SpeedType.TransAccelFactor = -0.2155f;
				SpeedType.BoostAccelFactor = 0.006f;
				SpeedType.StartForwardAccelForceItem = -530f;
				SpeedType.StartForwardAccelForceSpeed = -950f;
				Console.WriteLine("SpeedType:S0");
			}
			else if (config.SpeedType == 0)//S1 빠름
			{
				SpeedType.AddSpec_SteerConstraint = 1.7f;
				SpeedType.AddSpec_DriftEscapeForce = 150f;
				SpeedType.AddSpec_TransAccelFactor = 0.199f;
				SpeedType.DragFactor = -0.015f;
				SpeedType.ForwardAccelForce = -200f;
				SpeedType.BackwardAccelForce = -225f;
				SpeedType.GripBrakeForce = -270f;
				SpeedType.SlipBrakeForce = -165f;
				SpeedType.SteerConstraint = -0.25f;
				SpeedType.DriftEscapeForce = -250f;
				SpeedType.CornerDrawFactor = -0.03f;
				SpeedType.DriftMaxGauge = -330f;
				SpeedType.TransAccelFactor = -0.0015f;
				SpeedType.BoostAccelFactor = 0.006f;
				SpeedType.StartForwardAccelForceItem = -200f;
				SpeedType.StartForwardAccelForceSpeed = -360f;
				Console.WriteLine("SpeedType:S1");
			}
			else if (config.SpeedType == 1)//S2 매우빠름
			{
				SpeedType.AddSpec_SteerConstraint = 2.2f;
				SpeedType.AddSpec_DriftEscapeForce = 1100f;
				SpeedType.AddSpec_TransAccelFactor = 0.202f;
				SpeedType.DragFactor = 0.0121f;
				SpeedType.ForwardAccelForce = 200f;
				SpeedType.BackwardAccelForce = 225f;
				SpeedType.GripBrakeForce = 270f;
				SpeedType.SlipBrakeForce = 165f;
				SpeedType.SteerConstraint = 0.25f;
				SpeedType.DriftEscapeForce = 700f;
				SpeedType.CornerDrawFactor = 0f;
				SpeedType.DriftMaxGauge = 580f;
				SpeedType.TransAccelFactor = 0.0015f;
				SpeedType.BoostAccelFactor = 0.006f;
				SpeedType.StartForwardAccelForceItem = 200f;
				SpeedType.StartForwardAccelForceSpeed = 360f;
				Console.WriteLine("SpeedType:S2");
			}
			else if (config.SpeedType == 2)//S3 가장빠름
			{
				SpeedType.AddSpec_SteerConstraint = 2.7f;
				SpeedType.AddSpec_DriftEscapeForce = 1500f;
				SpeedType.AddSpec_TransAccelFactor = 0.2f;
				SpeedType.DragFactor = 0.04f;
				SpeedType.ForwardAccelForce = 750f;
				SpeedType.BackwardAccelForce = 450f;
				SpeedType.GripBrakeForce = 540f;
				SpeedType.SlipBrakeForce = 325f;
				SpeedType.SteerConstraint = 0.75f;
				SpeedType.DriftEscapeForce = 1100f;
				SpeedType.CornerDrawFactor = -0.02f;
				SpeedType.DriftMaxGauge = 1700f;
				SpeedType.TransAccelFactor = -0.0005f;
				SpeedType.BoostAccelFactor = 0.006f;
				SpeedType.StartForwardAccelForceItem = 750f;
				SpeedType.StartForwardAccelForceSpeed = 1350f;
				Console.WriteLine("SpeedType:S3");
			}
			else if (config.SpeedType == 4 || config.SpeedType == 6 || config.SpeedType == 7)//무부, 통합 S1.5
			{
				SpeedType.AddSpec_SteerConstraint = 1.95f;
				SpeedType.AddSpec_DriftEscapeForce = 400f;
				SpeedType.AddSpec_TransAccelFactor = 0.2005f;
				SpeedType.DragFactor = 0f;
				SpeedType.ForwardAccelForce = 0f;
				SpeedType.BackwardAccelForce = 0f;
				SpeedType.GripBrakeForce = 0f;
				SpeedType.SlipBrakeForce = 0f;
				SpeedType.SteerConstraint = 0f;
				SpeedType.DriftEscapeForce = 0f;
				SpeedType.CornerDrawFactor = 0f;
				SpeedType.DriftMaxGauge = 0f;
				SpeedType.TransAccelFactor = 0f;
				SpeedType.BoostAccelFactor = 0f;
				SpeedType.StartForwardAccelForceItem = 0f;
				SpeedType.StartForwardAccelForceSpeed = 0f;
				Console.WriteLine("SpeedType:统合 S1.5");
			}
			else
			{
				GameSupport.OnDisconnect();
				Console.WriteLine("SpeedType:null");
			}
			FlyingPet.FlyingPet_Spec();
		}
	}
}
