namespace Launcher.App.KartSpec
{
    public class Kart
    {
        /// <summary>
        /// 空气阻力-
        /// </summary>
        public static float AirFriction;

        /// <summary>
        /// 道具賽特殊加速器持续时间
        /// </summary>
        public static float AnimalBoosterTime;

        /// <summary>
        /// 碰撞防禦力-
        /// </summary>
        public static float antiCollideBalance;

        /// <summary>
        /// 倒车加速度+
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
        /// 改裝名称：弯道加速度
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
        /// 改裝名称：極限速度-
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
        /// 改裝名称：漂移最佳化
        /// </summary>
        public static float DriftEscapeForce;

        /// <summary>
        /// 碰撞时恢复集气量（％）
        /// </summary>
        public static float DriftGaguePreservePercent;

        /// <summary>
        /// 漂移车身傾斜度
        /// </summary>
        public static float DriftLeanFactor;

        /// <summary>
        /// 改裝名称：集气速度-
        /// </summary>
        public static float DriftMaxGauge;

        /// <summary>
        /// 漂移稳定性
        /// </summary>
        public static float DriftSlipFactor;

        /// <summary>
        /// 漂移触发系数
        /// </summary>
        public static float DriftTriggerFactor;

        /// <summary>
        /// 漂移触发时间
        /// </summary>
        public static float DriftTriggerTime;

        /// <summary>
        /// 競速賽起步加速力
        /// </summary>
        public static float ForwardAccelForce;

        /// <summary>
        /// 弯道敏捷性
        /// </summary>
        public static float FrontGripFactor;

        /// <summary>
        /// 迴轉減速
        /// </summary>
        public static float GripBrakeForce;

        /// <summary>
        /// 道具赛加速器加速时间
        /// </summary>
        public static float ItemBoosterTime;

        /// <summary>
        /// 道具賽道具欄位數
        /// </summary>
        public static byte ItemSlotCapacity;

        /// <summary>
        /// 车辆重量
        /// </summary>
        public static float Mass;

        /// <summary>
        /// 最大转向角度
        /// </summary>
        public static float MaxSteerAngle;

        /// <summary>
        /// 启用摩托车引擎
        /// </summary>
        public static byte motorcycleType;

        /// <summary>
        /// 改裝名称：加速器持续时间
        /// </summary>
        public static float NormalBoosterTime;

        /// <summary>
        /// 弯道敏捷性
        /// </summary>
        public static float RearGripFactor;

        /// <summary>
        /// 漂移減速
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
        /// 改裝名称：競速賽起步時間
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

        /// <summary>
        /// 转弯倾斜角度
        /// </summary>
        public static float SteerLeanFactor;

        /// <summary>
        /// 超级加速器時間
        /// </summary>
        public static float SuperBoosterTime;

        /// <summary>
        /// 改裝名称：團體加速器時間
        /// </summary>
        public static float TeamBoosterTime;

        /// <summary>
        /// 改裝名称：变形加速度
        /// </summary>
        public static float TransAccelFactor;

        /// <summary>
        /// 道具赛启用瞬间加速器
        /// </summary>
        public static byte UseExtendedAfterBooster;

        /// <summary>
        /// 啓用變形加速
        /// </summary>
        public static byte UseTransformBooster;

        /// <summary>
        /// 启用二段式推進器
        /// </summary>
        public static byte dualBoosterSetAuto;

        /// <summary>
        /// 二段加速最小触发时间
        /// </summary>
        public static int dualBoosterTickMin;

        /// <summary>
        /// 二段加速最大触发时间
        /// </summary>
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
        public static byte defaultEngineType;

        public static byte EngineType;

        /// <summary>
        /// 預設方向盤零件類型
        /// </summary>
        public static byte defaultHandleType;

        public static byte HandleType;

        /// <summary>
        /// 預設輪胎零件類型
        /// </summary>
        public static byte defaultWheelType;

        public static byte WheelType;

        /// <summary>
        /// 預設加速器零件類型
        /// </summary>
        public static byte defaultBoosterType;

        public static byte BoosterType;

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
        /// 賦能：漂移集氣速度
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
