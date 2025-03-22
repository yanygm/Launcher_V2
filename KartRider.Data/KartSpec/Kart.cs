using System;

namespace KartRider
{
	public class Kart
	{
		public static float AirFriction;

		/// <summary>
		/// 道具賽特殊加速器持續時間
		/// </summary>
		public static float AnimalBoosterTime;

		/// <summary>
		/// 碰撞防禦力
		/// </summary>
		public static float antiCollideBalance;

		/// <summary>
		/// 倒車加速度
		/// </summary>
		public static float BackwardAccelForce;

		public static byte BikeRearWheel;

		/// <summary>
		/// 加速器加速度
		/// </summary>
		public static float BoostAccelFactor;

		/// <summary>
		/// 道具賽特殊加速器加速度
		/// </summary>
		public static float BoostAccelFactorOnlyItem;

		/// <summary>
		/// 自動集氣速度
		/// </summary>
		public static float chargeBoostBySpeed;

		/// <summary>
		/// 改裝名稱：彎道加速度
		/// </summary>
		public static float CornerDrawFactor;

		/// <summary>
		/// 尾流加速度
		/// </summary>
		public static float draftMulAccelFactor;

		/// <summary>
		/// 尾流啟動時間（ms）
		/// </summary>
		public static int draftTick;

		/// <summary>
		/// 改裝名稱：極限速度
		/// </summary>
		public static float DragFactor;

		/// <summary>
		/// 瞬間加速器加速度
		/// </summary>
		public static float driftBoostMulAccelFactor;

		/// <summary>
		/// 瞬間加速器加速時間
		/// </summary>
		public static int driftBoostTick;

		/// <summary>
		/// 改裝名稱：甩尾離心力
		/// </summary>
		public static float DriftEscapeForce;

		/// <summary>
		/// 氮氣恢復量（％）
		/// </summary>
		public static float DriftGaguePreservePercent;

		/// <summary>
		/// 甩尾車身傾斜度
		/// </summary>
		public static float DriftLeanFactor;

		/// <summary>
		/// 改裝名稱：甩尾集氣量
		/// </summary>
		public static float DriftMaxGauge;

		/// <summary>
		/// 彎道迴轉穩定度
		/// </summary>
		public static float DriftSlipFactor;

		public static float DriftTriggerFactor;
		
		public static float DriftTriggerTime;

		/// <summary>
		/// 競速賽起步加速力
		/// </summary>
		public static float ForwardAccelForce;

		public static float FrontGripFactor;

		/// <summary>
		/// 迴轉減速
		/// </summary>
		public static float GripBrakeForce;

		public static float ItemBoosterTime;

		/// <summary>
		/// 道具賽道具欄位數
		/// </summary>
		public static byte ItemSlotCapacity;
		
		public static float Mass;
		
		public static float MaxSteerAngle;

		/// <summary>
		/// 啓用機車引擎
		/// </summary>
		public static byte motorcycleType;

		/// <summary>
		/// 改裝名稱：加速器持續時間
		/// </summary>
		public static float NormalBoosterTime;
		
		public static float RearGripFactor;

		/// <summary>
		/// 甩尾減速
		/// </summary>
		public static float SlipBrakeForce;

		/// <summary>
		/// 必殺計欄位數
		/// </summary>
		public static byte SpecialSlotCapacity;

		/// <summary>
		/// 競速賽道具欄位數
		/// </summary>
		public static byte SpeedSlotCapacity;

		/// <summary>
		/// 道具賽起步時間
		/// </summary>
		public static float StartBoosterTimeItem;

		/// <summary>
		/// 改裝名稱：競速賽起步時間
		/// </summary>
		public static float StartBoosterTimeSpeed;

		/// <summary>
		/// 道具賽起步加速力
		/// </summary>
		public static float StartForwardAccelForceItem;
		
		/// <summary>
		/// 競速賽起步加速力
		/// </summary>
		public static float StartForwardAccelForceSpeed;

		/// <summary>
		/// 轉向靈活度
		/// </summary>
		public static float SteerConstraint;
		
		public static float SteerLeanFactor;
		
		public static float SuperBoosterTime;

		/// <summary>
		/// 改裝名稱：團體加速器時間
		/// </summary>
		public static float TeamBoosterTime;

		/// <summary>
		/// 改裝名稱：變身加速器速度
		/// </summary>
		public static float TransAccelFactor;
		
		public static byte UseExtendedAfterBooster;

		/// <summary>
		/// 啓用變形加速
		/// </summary>
		public static byte UseTransformBooster;
		
		public static byte dualBoosterSetAuto;
		
		public static int dualBoosterTickMin;
		
		public static int dualBoosterTickMax;

		/// <summary>
		/// 二段式推進器加速度
		/// </summary>
		public static float dualMulAccelFactor;

		/// <summary>
		/// 二段式推進器最低觸發時速
		/// </summary>
		public static float dualTransLowSpeed;

		/// <summary>
		/// 鎖定能量配件欄位
		/// </summary>
		public static byte PartsEngineLock;

		/// <summary>
		/// 鎖定輪胎欄位
		/// </summary>
		public static byte PartsWheelLock;

		/// <summary>
		/// 鎖定方向盤欄位
		/// </summary>
		public static byte PartsSteeringLock;

		/// <summary>
		/// 鎖定工具箱欄位
		/// </summary>
		public static byte PartsBoosterLock;

		/// <summary>
		/// 鎖定塗料欄位
		/// </summary>
		public static byte PartsCoatingLock;

		/// <summary>
		/// 鎖定尾燈欄位
		/// </summary>
		public static byte PartsTailLampLock;

		/// <summary>
		/// 超越推進器能量累積比例 - 開氣
		/// </summary>
		public static float chargeInstAccelGaugeByBoost;

		/// <summary>
		/// 超越推進器能量累積比例 - 抓地
		/// </summary>
		public static float chargeInstAccelGaugeByGrip;

		/// <summary>
		/// 超越推進器能量累積比例 - 碰撞
		/// </summary>
		public static float chargeInstAccelGaugeByWall;

		/// <summary>
		/// 超越推進器加速度
		/// </summary>
		public static float instAccelFactor;

		/// <summary>
		/// 超越推進器冷卻時間（s）
		/// </summary>
		public static int instAccelGaugeCooldownTime;

		/// <summary>
		/// 超越推進器加速時間
		/// </summary>
		public static float instAccelGaugeLength;

		/// <summary>
		/// 超越推進器可用最低比例（%）
		/// </summary>
		public static float instAccelGaugeMinUsable;

		/// <summary>
		/// 超越推進器最低觸發時速
		/// </summary>
		public static float instAccelGaugeMinVelBound;

		/// <summary>
		/// 超越推進器最低減速
		/// </summary>
		public static float instAccelGaugeMinVelLoss;

		public static byte useExtendedAfterBoosterMore;

		/// <summary>
		/// 碰撞集氣冷卻時間（s）
		/// </summary>
		public static int wallCollGaugeCooldownTime;

		/// <summary>
		/// 碰撞集氣最高減速
		/// </summary>
		public static float wallCollGaugeMaxVelLoss;

		/// <summary>
		/// 碰撞集氣最低觸發時速
		/// </summary>
		public static float wallCollGaugeMinVelBound;

		/// <summary>
		/// 碰撞集氣最低減速
		/// </summary>
		public static float wallCollGaugeMinVelLoss;

		public static float modelMaxX;
		
		public static float modelMaxY;

		/// <summary>
		/// 預設超越系統類型
		/// </summary>
		public static int defaultExceedType;

		/// <summary>
		/// 預設引擎零件類型
		/// </summary>
		public static short defaultEngineType;

		/// <summary>
		/// 預設方向盤零件類型
		/// </summary>
		public static short defaultHandleType;

		/// <summary>
		/// 預設輪胎零件類型
		/// </summary>
		public static short defaultWheelType;

		/// <summary>
		/// 預設加速器零件類型
		/// </summary>
		public static short defaultBoosterType;

		/// <summary>
		/// 賦能：增加超越能量累積 - 碰撞
		/// </summary>
		public static float chargeInstAccelGaugeByWallAdded;

		/// <summary>
		/// 賦能：增加超越能量累積 - 開氣
		/// </summary>
		public static float chargeInstAccelGaugeByBoostAdded;

		/// <summary>
		/// 啓用賦能系統需使用加速器數量
		/// </summary>
		public static int chargerSystemboosterUseCount;

		/// <summary>
		/// 賦能系統啓用時間（ms）
		/// </summary>
		public static float chargerSystemUseTime;

		/// <summary>
		/// 賦能：自動集氣速度
		/// </summary>
		public static float chargeBoostBySpeedAdded;

		/// <summary>
		/// 賦能：甩尾集氣速度
		/// </summary>
		public static float driftGaugeFactor;

		/// <summary>
		/// 賦能：增加碰撞防禦力
		/// </summary>
		public static float chargeAntiCollideBalance;
		
		public static int startItemTableId;
		
		public static int startItemId;
	}
}
