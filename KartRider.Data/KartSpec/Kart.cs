using System;

namespace KartRider
{
	public class Kart
	{
		// --------------------- 物理 ----------------------

		/// <summary>
		/// 物理: 空气阻力-
		/// </summary>
		public static float AirFriction;

		/// <summary>
		/// 物理: 碰撞防御力-
		/// </summary>
		public static float antiCollideBalance;

		/// <summary>
		/// 物理: 漂移车身倾斜度
		/// </summary>
		public static float DriftLeanFactor;

		/// <summary>
		/// 物理: 漂移稳定性
		/// </summary>
		public static float DriftSlipFactor;

		/// <summary>
		/// 物理: 漂移触发系数
		/// </summary>
		public static float DriftTriggerFactor;

		/// <summary>
		/// 物理: 漂移触发时间
		/// </summary>
		public static float DriftTriggerTime;

		/// <summary>
		/// 物理: 弯道敏捷性
		/// </summary>
		public static float FrontGripFactor;

		/// <summary>
		/// 物理: 回转减速
		/// </summary>
		public static float GripBrakeForce;
		
		/// <summary>
		/// 物理: 车辆重量
		/// </summary>
		public static float Mass;

		/// <summary>
		/// 物理: 最大转向角度
		/// </summary>
		public static float MaxSteerAngle;

		/// <summary>
		/// 物理: 适用摩托车特性
		/// </summary>
		public static byte motorcycleType;
		
		/// <summary>
		/// 物理: 弯道敏捷性
		/// </summary>
		public static float RearGripFactor;

		/// <summary>
		/// 物理: 漂移减速
		/// </summary>
		public static float SlipBrakeForce;
		
		/// <summary>
		/// 物理: 转向灵活度
		/// </summary>
		public static float SteerConstraint;

		/// <summary>
		/// 物理: 转弯倾斜角度
		/// </summary>
		public static float SteerLeanFactor;

		// --------------------- 基本 ----------------------

		/// <summary>
		/// 基本: 倒车加速度+
		/// </summary>
		public static float BackwardAccelForce;

		/// <summary>
		/// 基本: 加速器加速度
		/// </summary>
		public static float BoostAccelFactor;

		/// <summary>
		/// 基本: 自动集气速度
		/// </summary>
		public static float chargeBoostBySpeed;
		
		/// <summary>
		/// 基本: 尾流加速度
		/// </summary>
		public static float draftMulAccelFactor;

		/// <summary>
		/// 基本: 尾流启动时间（ms）
		/// </summary>
		public static int draftTick;
		
		/// <summary>
		/// 基本: 瞬间加速器(小喷)加速度
		/// </summary>
		public static float driftBoostMulAccelFactor;

		/// <summary>
		/// 基本: 瞬间加速器(小喷)加速时间
		/// </summary>
		public static int driftBoostTick;

		/// <summary>
		/// 基本: 碰撞恢复集气量（％）
		/// </summary>
		public static float DriftGaguePreservePercent;

		/// <summary>
		/// 基本: 超级加速器时间
		/// </summary>
		public static float SuperBoosterTime;

		// --------------------- 部件 ----------------------

		/// <summary>
		/// 部件: 默认引擎类型
		/// </summary>
		public static byte defaultEngineType;

		/// <summary>
		/// 部件: 引擎类型
		/// </summary>
		public static byte EngineType;

		/// <summary>
		/// 部件: 锁定引擎栏位
		/// </summary>
		public static byte PartsEngineLock;

		/// <summary>
		/// 部件: 默认方向盘类型
		/// </summary>
		public static byte defaultHandleType;

		/// <summary>
		/// 部件: 方向盘类型
		/// </summary>
		public static byte HandleType;

		/// <summary>
		/// 部件: 锁定方向盘栏位
		/// </summary>
		public static byte PartsSteeringLock;

		/// <summary>
		/// 部件: 默认轮胎类型
		/// </summary>
		public static byte defaultWheelType;

		/// <summary>
		/// 部件: 轮胎类型
		/// </summary>

		public static byte WheelType;

		/// <summary>
		/// 部件: 锁定轮胎栏位
		/// </summary>
		public static byte PartsWheelLock;

		/// <summary>
		/// 部件: 默认加速器类型
		/// </summary>
		public static byte defaultBoosterType;

		/// <summary>
		/// 部件: 加速器类型
		/// </summary>
		public static byte BoosterType;

		/// <summary>
		/// 部件: 锁定加速器栏位
		/// </summary>
		public static byte PartsBoosterLock;

		/// <summary>
		/// 部件: 锁定车膜栏位 (V1 or later)
		/// </summary>
		public static byte PartsCoatingLock;

		/// <summary>
		/// 部件: 锁定尾灯栏位 (V1 or later)
		/// </summary>
		public static byte PartsTailLampLock;

		/// <summary>
		/// 部件: 默认超越系统类型
		/// </summary>
		public static int defaultExceedType;

		// --------------------- 改裝 ----------------------

		/// <summary>
		/// 改裝: 弯道加速度
		/// </summary>
		public static float CornerDrawFactor;

		/// <summary>
		/// 改裝：极限速度-
		/// </summary>
		public static float DragFactor;

		/// <summary>
		/// 改裝: 漂移最佳化
		/// </summary>
		public static float DriftEscapeForce;

		/// <summary>
		/// 改裝: 加速器持续时间
		/// </summary>
		public static float NormalBoosterTime;

		/// <summary>
		/// 改裝: 团队加速器时间
		/// </summary>
		public static float TeamBoosterTime;

		/// <summary>
		/// 改裝: 变形加速度
		/// </summary>
		public static float TransAccelFactor;

		/// <summary>
		/// 改装: 启用变形加速
		/// </summary>
		public static byte UseTransformBooster;

		/// <summary>
		/// 改裝: 竞速赛起步时间
		/// </summary>
		public static float StartBoosterTimeSpeed;
		
        /// <summary>
		/// 改裝: 集气速度-
		/// </summary>
		public static float DriftMaxGauge;

		// --------------------- 二段 ----------------------

		/// <summary>
		/// 二段: 启用自动二段加速器
		/// </summary>
		public static byte dualBoosterSetAuto;

		/// <summary>
		/// 二段: 最小触发时间
		/// </summary>
		public static int dualBoosterTickMin;

		/// <summary>
		/// 二段: 最大触发时间
		/// </summary>
		public static int dualBoosterTickMax;

		/// <summary>
		/// 二段: 加速度
		/// </summary>
		public static float dualMulAccelFactor;

		/// <summary>
		/// 二段: 最低触发速度
		/// </summary>
		public static float dualTransLowSpeed;

		// --------------------- 三段 ----------------------

		/// <summary>
		/// 三段: 能量累计比例 - 开启加速器
		/// </summary>
		public static float chargeInstAccelGaugeByBoost;

		/// <summary>
		/// 三段: 能量累计比例 - 抓地
		/// </summary>
		public static float chargeInstAccelGaugeByGrip;

		/// <summary>
		/// 三段: 能量累计比例 - 碰撞
		/// </summary>
		public static float chargeInstAccelGaugeByWall;

		/// <summary>
		/// 三段: 加速度
		/// </summary>
		public static float instAccelFactor;

		/// <summary>
		/// 三段: 加速时间
		/// </summary>
		public static float instAccelGaugeLength;

		/// <summary>
		/// 三段: 冷却时间（s）
		/// </summary>
		public static int instAccelGaugeCooldownTime;

		/// <summary>
		/// 三段: 可用最低比例（%）
		/// </summary>
		public static float instAccelGaugeMinUsable;

		/// <summary>
		/// 三段: 最低触发速度
		/// </summary>
		public static float instAccelGaugeMinVelBound;

		/// <summary>
		/// 三段: 最低减速
		/// </summary>
		public static float instAccelGaugeMinVelLoss;

		// --------------------- 碰撞集气 ----------------------

		/// <summary>
		/// 碰撞集气: 冷却时间 (s)
		/// </summary>
		public static int wallCollGaugeCooldownTime;

		/// <summary>
		/// 碰撞集气: 最高减速
		/// </summary>
		public static float wallCollGaugeMaxVelLoss;

		/// <summary>
		/// 碰撞集气: 最低触发速度
		/// </summary>
		public static float wallCollGaugeMinVelBound;

		/// <summary>
		/// 碰撞集气: 最低减速
		/// </summary>
		public static float wallCollGaugeMinVelLoss;

		// --------------------- 赋能系统 ----------------------

		/// <summary>
		/// 赋能系统: 增加超越能量累积 - 碰撞
		/// </summary>
		public static float chargeInstAccelGaugeByWallAdded;

		/// <summary>
		/// 赋能系统: 增加超越能量累积 - 加速器
		/// </summary>
		public static float chargeInstAccelGaugeByBoostAdded;

		/// <summary>
		/// 赋能系统: 需使用加速器数量
		/// </summary>
		public static int chargerSystemboosterUseCount;

		/// <summary>
		/// 赋能系统: 启用时间（ms）
		/// </summary>
		public static float chargerSystemUseTime;

		/// <summary>
		/// 赋能系统: 自动集气速度
		/// </summary>
		public static float chargeBoostBySpeedAdded;

		/// <summary>
		/// 赋能系统: 漂移集气速度
		/// </summary>
		public static float driftGaugeFactor;

		/// <summary>
		/// 赋能系统: 增加碰撞防御力
		/// </summary>
		public static float chargeAntiCollideBalance;

		// --------------------- 道具赛参数 ---------------------
        
		/// <summary>
		/// 道具赛: 特殊加速器加速时间
		/// </summary>
		public static float AnimalBoosterTime;

		/// <summary>
		/// 道具赛: 特殊加速器加速度
		/// </summary>
		public static float BoostAccelFactorOnlyItem;

		/// <summary>
		/// 道具赛: 加速器加速时间
		/// </summary>
		public static float ItemBoosterTime;

		/// <summary>
		/// 道具赛: 起步加速时间
		/// </summary>
		public static float StartBoosterTimeItem;

		/// <summary>
		/// 道具赛: 起步加速力
		/// </summary>
		public static float StartForwardAccelForceItem;
		
		/// <summary>
		/// 道具赛: 启用瞬间加速器(小喷)
		/// </summary>
		public static byte UseExtendedAfterBooster;

		/// <summary>
		/// 道具赛: 道具栏位数
		/// </summary>
		public static byte ItemSlotCapacity;

        /// <summary>
		/// 道具赛: 必杀技栏位数
		/// </summary>
		public static byte SpecialSlotCapacity;

        // --------------------- 竞速赛参数 ---------------------

        /// <summary>
		/// 竞速赛: 起步加速力
		/// </summary>
		public static float ForwardAccelForce;

        /// <summary>
		/// 竞速赛: 起步加速度
		/// </summary>
		public static float StartForwardAccelForceSpeed;

        /// <summary>
		/// 竞速赛: 道具栏位数
		/// </summary>
		public static byte SpeedSlotCapacity;
		
		// --------------------- 其他/未知 ----------------------
		
		public static int startItemTableId;
		
		public static int startItemId;

		public static float modelMaxX;

		public static float modelMaxY;
		
		public static byte useExtendedAfterBoosterMore;

		public static byte BikeRearWheel;
	}
}
