using System;
using KartRider;

namespace ExcData
{
	public class SpeedPatch
	{
		public static float DragFactor = 0f; //최고 속도
		public static float ForwardAccelForce = 0f; //전진 가속도
		public static float DriftEscapeForce = 0f; //드리프트 탈출력
		public static float CornerDrawFactor = 0f; //코너 가속
		public static float DriftMaxGauge = 0f; //게이지 충전량
		public static float TransAccelFactor = 0f; //변신 부스터 가속력
		public static float BoostAccelFactor = 0f; //부스터 가속력
		public static float StartForwardAccelForceItem = 0f; //출발 부스터 가속 아이템
		public static float StartForwardAccelForceSpeed = 0f; //출발 부스터 가속 스피드

		public static void SpeedPatcData()
		{
			if (Program.SpeedPatch)
			{
				SpeedPatch.DragFactor = -0.003f;
				SpeedPatch.ForwardAccelForce = 30f;
				SpeedPatch.DriftEscapeForce = 200f;
				SpeedPatch.CornerDrawFactor = 0.0015f;
				SpeedPatch.DriftMaxGauge = -70f;
				SpeedPatch.TransAccelFactor = 0.005f;
				SpeedPatch.BoostAccelFactor = 0.005f;
				SpeedPatch.StartForwardAccelForceItem = 100f;
				SpeedPatch.StartForwardAccelForceSpeed = 100f;
			}
			else
			{
				SpeedPatch.DragFactor = 0f;
				SpeedPatch.ForwardAccelForce = 0f;
				SpeedPatch.DriftEscapeForce = 0f;
				SpeedPatch.CornerDrawFactor = 0f;
				SpeedPatch.DriftMaxGauge = 0f;
				SpeedPatch.TransAccelFactor = 0f;
				SpeedPatch.BoostAccelFactor = 0f;
				SpeedPatch.StartForwardAccelForceItem = 0f;
				SpeedPatch.StartForwardAccelForceSpeed = 0f;
			}
		}
	}
}