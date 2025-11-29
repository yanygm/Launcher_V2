using ExcData;
using KartRider;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace KartRider
{
    public class Kart
    {
        public static Dictionary<int, string> kartName = new Dictionary<int, string>();
        public static Dictionary<string, XmlDocument> kartSpec = new Dictionary<string, XmlDocument>();
    }

    public class KartSpec
    {
        /// <summary>布尔类型的XML属性名集合（统一管理）</summary>
        private readonly HashSet<string> BooleanAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            "UseTransformBooster", "motorcycleType", "BikeRearWheel", "UseExtendedAfterBooster",
            "dualBoosterSetAuto", "PartsEngineLock", "PartsWheelLock", "PartsSteeringLock",
            "PartsBoosterLock", "PartsCoatingLock", "PartsTailLampLock", "PartsBoosterEffectLock",
            "useExtendedAfterBoosterMore"
        };

        /// <summary>部件默认类型（EngineType/HandleType等的默认值1）</summary>
        private const byte DefaultPartType = 1;
        /// <summary>模型尺寸默认值</summary>
        private const float DefaultModelDimension = 0f;

        /// <summary>卡丁车规格属性配置类（关联XML属性与Kart字段赋值）</summary>
        private class KartSpecConfig
        {
            /// <summary>XML属性名</summary>
            public string AttributeName { get; }
            /// <summary> fallback值（计算时叠加）</summary>
            public decimal FallbackValue { get; }
            /// <summary>默认值（解析失败时使用）</summary>
            public decimal DefaultValue { get; }
            /// <summary>缩放系数</summary>
            public decimal Scale { get; }
            /// <summary>Kart字段赋值委托</summary>
            public Action<decimal> SetKartProperty { get; }

            public KartSpecConfig(
                string attributeName,
                decimal fallbackValue,
                decimal defaultValue,
                decimal scale,
                Action<decimal> setKartProperty)
            {
                AttributeName = attributeName;
                FallbackValue = fallbackValue;
                DefaultValue = defaultValue;
                Scale = scale;
                SetKartProperty = setKartProperty;
            }
        }

        /// <summary>所有卡丁车规格属性的配置（替代原attributes数组, 避免索引依赖）</summary>
        private IEnumerable<KartSpecConfig> KartSpecConfigs => new List<KartSpecConfig>
        {
            new("draftMulAccelFactor", 0M, 1M, 1M, val => draftMulAccelFactor = (float)val),
            new("draftTick", 0M, 0M, 1M, val => draftTick = (int)val),
            new("driftBoostMulAccelFactor", 0M, 1.31M, 1M, val => driftBoostMulAccelFactor = (float)val),
            new("driftBoostTick", 0M, 0M, 1M, val => driftBoostTick = (int)val),
            new("chargeBoostBySpeed", 0M, 2M, 1M, val => chargeBoostBySpeed = (float)val),
            new("SpeedSlotCapacity", 0M, 2M, 1M, val => SpeedSlotCapacity = (byte)val),
            new("ItemSlotCapacity", 0M, 2M, 1M, val => ItemSlotCapacity = (byte)val),
            new("SpecialSlotCapacity", 0M, 1M, 1M, val => SpecialSlotCapacity = (byte)val),
            new("UseTransformBooster", 0M, 0M, 1M, val => UseTransformBooster = (byte)val),
            new("motorcycleType", 0M, 0M, 1M, val => motorcycleType = (byte)val),
            new("BikeRearWheel", 0M, 1M, 1M, val => BikeRearWheel = (byte)val),
            new("Mass", 0M, 100M, 1M, val => Mass = (float)val),
            new("AirFriction", 0M, 3M, 1M, val => AirFriction = (float)val),
            new("DragFactor", 0.75M, 0.75M, 1M, val => DragFactor = (float)val),
            new("ForwardAccelForce", 2150M, 2150M, 1M, val => ForwardAccelForce = (float)val),
            new("BackwardAccelForce", 1725M, 1725M, 1M, val => BackwardAccelForce = (float)val),
            new("GripBrakeForce", 2070M, 2070M, 1M, val => GripBrakeForce = (float)val),
            new("SlipBrakeForce", 1415M, 1415M, 1M, val => SlipBrakeForce = (float)val),
            new("MaxSteerAngle", 0M, 10M, 1M, val => MaxSteerAngle = (float)val),
            new("SteerConstraint", 22.25M, 22.25M, 1M, val => SteerConstraint = (float)val),
            new("FrontGripFactor", 0M, 5M, 1M, val => FrontGripFactor = (float)val),
            new("RearGripFactor", 0M, 5M, 1M, val => RearGripFactor = (float)val),
            new("DriftTriggerFactor", 0M, 0.2M, 1M, val => DriftTriggerFactor = (float)val),
            new("DriftTriggerTime", 0M, 0.2M, 1M, val => DriftTriggerTime = (float)val),
            new("DriftSlipFactor", 0M, 0.2M, 1M, val => DriftSlipFactor = (float)val),
            new("DriftEscapeForce", 2600M, 2600M, 1M, val => DriftEscapeForce = (float)val),
            new("CornerDrawFactor", 0.18M, 0.18M, 1M, val => CornerDrawFactor = (float)val),
            new("DriftLeanFactor", 0.07M, 0.07M, 1M, val => DriftLeanFactor = (float)val),
            new("SteerLeanFactor", 0.01M, 0.01M, 1M, val => SteerLeanFactor = (float)val),
            new("DriftMaxGauge", 4300M, 4300M, 1M, val => DriftMaxGauge = (float)val),
            new("NormalBoosterTime", 3000M, 3000M, 1M, val => NormalBoosterTime = (float)val),
            new("ItemBoosterTime", 3000M, 3000M, 1M, val => ItemBoosterTime = (float)val),
            new("TeamBoosterTime", 4500M, 4500M, 1M, val => TeamBoosterTime = (float)val),
            new("AnimalBoosterTime", 4000M, 4000M, 1M, val => AnimalBoosterTime = (float)val),
            new("SuperBoosterTime", 3500M, 3500M, 1M, val => SuperBoosterTime = (float)val),
            new("TransAccelFactor", -0.0045M, 1.4955M, 1M, val => TransAccelFactor = (float)val),
            new("BoostAccelFactor", -0.006M, 1.494M, 1M, val => BoostAccelFactor = (float)val),
            new("StartBoosterTimeItem", 0M, 1000M, 1M, val => StartBoosterTimeItem = (float)val),
            new("StartBoosterTimeSpeed", 0M, 1000M, 1M, val => StartBoosterTimeSpeed = (float)val),
            new("StartForwardAccelForceItem", 171.6355M, 2304M, 2102.325M, val => StartForwardAccelForceItem = (float)val),
            new("StartForwardAccelForceSpeed", 176.132M, 2304M, 2099.68M, val => StartForwardAccelForceSpeed = (float)val),
            new("DriftGaguePreservePercent", 0M, 0M, 1M, val => DriftGaguePreservePercent = (float)val),
            new("UseExtendedAfterBooster", 0M, 0M, 1M, val => UseExtendedAfterBooster = (byte)val),
            new("BoostAccelFactorOnlyItem", 0M, 1.5M, 1M, val => BoostAccelFactorOnlyItem = (float)val),
            new("antiCollideBalance", 0M, 1M, 1M, val => antiCollideBalance = (float)val),
            new("dualBoosterSetAuto", 0M, 0M, 1M, val => dualBoosterSetAuto = (byte)val),
            new("dualBoosterTickMin", 0M, 40M, 1M, val => dualBoosterTickMin = (int)val),
            new("dualBoosterTickMax", 0M, 60M, 1M, val => dualBoosterTickMax = (int)val),
            new("dualMulAccelFactor", 0M, 1.1M, 1M, val => dualMulAccelFactor = (float)val),
            new("dualTransLowSpeed", 0M, 100M, 1M, val => dualTransLowSpeed = (float)val),
            new("PartsEngineLock", 0M, 0M, 1M, val => PartsEngineLock = (byte)val),
            new("PartsWheelLock", 0M, 0M, 1M, val => PartsWheelLock = (byte)val),
            new("PartsSteeringLock", 0M, 0M, 1M, val => PartsSteeringLock = (byte)val),
            new("PartsBoosterLock", 0M, 0M, 1M, val => PartsBoosterLock = (byte)val),
            new("PartsCoatingLock", 0M, 0M, 1M, val => PartsCoatingLock = (byte)val),
            new("PartsTailLampLock", 0M, 0M, 1M, val => PartsTailLampLock = (byte)val),
            new("PartsBoosterEffectLock", 0M, 0M, 1M, val => PartsBoosterEffectLock = (byte)val),
            new("chargeInstAccelGaugeByBoost", 0M, 0.02M, 1M, val => chargeInstAccelGaugeByBoost = (float)val),
            new("chargeInstAccelGaugeByGrip", 0M, 0.02M, 1M, val => chargeInstAccelGaugeByGrip = (float)val),
            new("chargeInstAccelGaugeByWall", 0M, 0.2M, 1M, val => chargeInstAccelGaugeByWall = (float)val),
            new("instAccelFactor", 0M, 1.25M, 1M, val => instAccelFactor = (float)val),
            new("instAccelGaugeCooldownTime", 0M, 0M, 1000M, val => instAccelGaugeCooldownTime = (int)val),
            new("instAccelGaugeLength", 0M, 0M, 1000M, val => instAccelGaugeLength = (float)val),
            new("instAccelGaugeMinUsable", 0M, 0M, 1000M, val => instAccelGaugeMinUsable = (float)val),
            new("instAccelGaugeMinVelBound", 0M, 200M, 1M, val => instAccelGaugeMinVelBound = (float)val),
            new("instAccelGaugeMinVelLoss", 0M, 50M, 1M, val => instAccelGaugeMinVelLoss = (float)val),
            new("useExtendedAfterBoosterMore", 0M, 0M, 1M, val => useExtendedAfterBoosterMore = (byte)val),
            new("wallCollGaugeCooldownTime", 0M, 0M, 1000M, val => wallCollGaugeCooldownTime = (int)val),
            new("wallCollGaugeMaxVelLoss", 0M, 200M, 1M, val => wallCollGaugeMaxVelLoss = (float)val),
            new("wallCollGaugeMinVelBound", 0M, 200M, 1M, val => wallCollGaugeMinVelBound = (float)val),
            new("wallCollGaugeMinVelLoss", 0M, 50M, 1M, val => wallCollGaugeMinVelLoss = (float)val),
            new("defaultExceedType", 0M, 0M, 1M, val => defaultExceedType = (int)val),
            new("defaultEngineType", 0M, 0M, 1M, val => defaultEngineType = (byte)val),
            new("defaultHandleType", 0M, 0M, 1M, val => defaultHandleType = (byte)val),
            new("defaultWheelType", 0M, 0M, 1M, val => defaultWheelType = (byte)val),
            new("defaultBoosterType", 0M, 0M, 1M, val => defaultBoosterType = (byte)val),
            new("chargeInstAccelGaugeByWallAdded", 0M, 0M, 1M, val => chargeInstAccelGaugeByWallAdded = (float)val),
            new("chargeInstAccelGaugeByBoostAdded", 0M, 0M, 1M, val => chargeInstAccelGaugeByBoostAdded = (float)val),
            new("chargerSystemboosterUseCount", 0M, 0M, 1M, val => chargerSystemboosterUseCount = (int)val),
            new("chargerSystemUseTime", 0M, 0M, 1M, val => chargerSystemUseTime = (float)val),
            new("chargeBoostBySpeedAdded", 0M, 0M, 1M, val => chargeBoostBySpeedAdded = (float)val),
            new("driftGaugeFactor", 0M, 0M, 1M, val => driftGaugeFactor = (float)val),
            new("chargeAntiCollideBalance", 0M, 1M, 1M, val => chargeAntiCollideBalance = (float)val),
        };

        /// <summary>获取卡丁车规格入口方法</summary>
        public void GetKartSpec(string Nickname, bool School = false)
        {
            try
            {
                // 1. 处理卡丁车ID为0的默认情况
                short KartID = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_Kart;
                if (School || KartID == 0)
                {
                    Console.WriteLine("[KartSpec] 卡丁车ID=0, 加载练习车数据");
                }
                // 2. 处理非默认卡丁车ID
                else
                {
                    // 2.1 检查KartName字典是否存在该ID
                    if (!Kart.kartName.TryGetValue(KartID, out var Name))
                    {
                        Console.WriteLine($"[KartSpec] 警告: KartName中未找到ID={KartID}, 加载练习车数据");
                    }

                    Console.WriteLine($"[KartSpec] 加载卡丁车: ID={KartID}, 名称={Name}");

                    // 2.2 检查KartSpec字典是否存在该规格
                    if (!Kart.kartSpec.TryGetValue(Name, out var kartSpecDoc))
                    {
                        Console.WriteLine($"[KartSpec] 警告: KartSpec中未找到名称={Name}的规格, 使用默认");
                    }

                    // 2.3 解析规格XML并赋值
                    ParseKartSpecXml(Nickname, KartID, kartSpecDoc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KartSpec] 错误: 获取规格时异常 - {ex.Message}\n堆栈: {ex.StackTrace}, 使用默认");
                // 异常降级: 确保加载默认规格, 避免程序崩溃
            }
        }

        /// <summary>解析卡丁车规格XML文档</summary>
        private void ParseKartSpecXml(string Nickname, short kartId, XmlDocument specDoc)
        {
            var bodyParams = specDoc.GetElementsByTagName("BodyParam");
            // 检查是否存在BodyParam节点且为XmlElement类型
            if (bodyParams.Count > 0 && bodyParams[0] is XmlElement bodyParamElement)
            {
                AssignKartProperties(kartId, bodyParamElement);
            }
            else
            {
                Console.WriteLine($"[KartSpec] 警告: ID={kartId}的规格XML中无BodyParam节点, 使用默认");
            }
        }

        /// <summary>给Kart静态字段赋值（核心数据映射）</summary>
        private void AssignKartProperties(short kartId, XmlElement bodyParamElement)
        {
            // 1. 赋值基础规格属性
            foreach (var config in KartSpecConfigs)
            {
                var attrValueStr = GetAttributeValue(
                    bodyParamElement,
                    config.AttributeName,
                    config.FallbackValue,
                    config.DefaultValue,
                    config.Scale);

                // 解析数值并赋值（失败则用默认值）
                if (decimal.TryParse(attrValueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVal))
                {
                    config.SetKartProperty(parsedVal);
                }
                else
                {
                    Console.WriteLine($"[KartSpec] 警告: 属性{config.AttributeName}值{attrValueStr}无效, 用默认值{config.DefaultValue}");
                    config.SetKartProperty(config.DefaultValue);
                }
            }

            // 2. 加载模型尺寸（单独处理ModelMax.xml）
            var modelMax = LoadModelMaxDimensions(kartId);
            modelMaxX = modelMax.modelMaxX;
            Console.WriteLine($"[KartSpec] 警告: 属性modelMaxX值为: {modelMaxX}");
            modelMaxY = modelMax.modelMaxY;
            Console.WriteLine($"[KartSpec] 警告: 属性modelMaxY值为: {modelMaxY}");

            // 3. 设置默认部件类型（Engine/Handle等）
            EngineType = DefaultPartType;
            HandleType = DefaultPartType;
            WheelType = DefaultPartType;
            BoosterType = DefaultPartType;

            // 4. 初始化物品ID（保持原逻辑）
            startItemTableId = 0;
            startItemId = 0;
        }

        /// <summary>加载ModelMax.xml中的模型尺寸（modelMaxX/modelMaxY）</summary>
        private (float modelMaxX, float modelMaxY) LoadModelMaxDimensions(short kartId)
        {
            // 检查文件是否存在
            if (!File.Exists(FileName.ModelMax_LoadFile))
            {
                Console.WriteLine($"[KartSpec] 警告: ModelMax.xml不存在 - 路径: {FileName.ModelMax_LoadFile}");
                return (DefaultModelDimension, DefaultModelDimension);
            }

            try
            {
                // 只对文件流使用using, XDocument不实现IDisposable, 不需要using
                using (var fileStream = File.OpenRead(FileName.ModelMax_LoadFile))
                {
                    var xdoc = XDocument.Load(fileStream);

                    var targetNode = xdoc.Descendants("Kart").FirstOrDefault(kart => (short)kart.Attribute("ID") == kartId);

                    if (targetNode == null)
                    {
                        Console.WriteLine($"[KartSpec] 警告: ModelMax.xml中无节点ID={kartId}");
                        return (DefaultModelDimension, DefaultModelDimension);
                    }

                    var maxXAttr = targetNode.Attribute("modelMaxX");
                    var maxYAttr = targetNode.Attribute("modelMaxY");

                    if (maxXAttr == null || maxYAttr == null)
                    {
                        Console.WriteLine($"[KartSpec] 警告: ID={kartId} 缺少modelMaxX或modelMaxY属性");
                        return (DefaultModelDimension, DefaultModelDimension);
                    }

                    if (!float.TryParse(maxXAttr.Value, CultureInfo.InvariantCulture, out float modelMaxX) ||
                        !float.TryParse(maxYAttr.Value, CultureInfo.InvariantCulture, out float modelMaxY))
                    {
                        Console.WriteLine($"[KartSpec] 警告: ID={kartId} 的属性值无法解析为浮点数");
                        return (DefaultModelDimension, DefaultModelDimension);
                    }

                    return (modelMaxX, modelMaxY);
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"[KartSpec] 错误: 解析ModelMax.xml失败 - {ex.Message}");
                return (DefaultModelDimension, DefaultModelDimension);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[KartSpec] 错误: 读取ModelMax.xml失败 - {ex.Message}");
                return (DefaultModelDimension, DefaultModelDimension);
            }
        }

        /// <summary>获取XML属性值（处理布尔/特殊数值逻辑）</summary>
        public string GetAttributeValue(
            XmlElement element,
            string attributeName,
            decimal fallbackValue,
            decimal defaultValue,
            decimal scale)
        {
            // 1. 处理布尔类型属性（转换为0/1）
            if (BooleanAttributes.Contains(attributeName))
            {
                var attrValue = element.GetAttribute(attributeName);
                if (bool.TryParse(attrValue, out var boolVal))
                {
                    return (boolVal ? 1M : 0M).ToString(CultureInfo.InvariantCulture);
                }
                Console.WriteLine($"[KartSpec] 警告: 布尔属性{attributeName}值{attrValue}无效, 用默认值{defaultValue}");
                return defaultValue.ToString(CultureInfo.InvariantCulture);
            }

            // 2. 处理特殊属性: StartForwardAccelForceItem（读取Factor属性）
            if (attributeName == "StartForwardAccelForceItem")
            {
                return GetScaledValue(element, "StartForwardAccelFactorItem", fallbackValue, defaultValue, scale);
            }

            // 3. 处理特殊属性: StartForwardAccelForceSpeed（读取Factor属性）
            if (attributeName == "StartForwardAccelForceSpeed")
            {
                return GetScaledValue(element, "StartForwardAccelFactorSpeed", fallbackValue, defaultValue, scale);
            }

            // 4. 处理特殊属性: instAccelGaugeMinUsable（依赖instAccelGaugeLength）
            if (attributeName == "instAccelGaugeMinUsable")
            {
                // 先获取instAccelGaugeLength的值
                var lengthAttrValue = element.GetAttribute("instAccelGaugeLength");
                var length = decimal.TryParse(lengthAttrValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var l)
                    ? l
                    : 0M;

                // 缩放系数 = 原scale * length
                return GetScaledValue(element, attributeName, fallbackValue, defaultValue, scale * length);
            }

            // 5. 普通数值属性
            return GetScaledValue(element, attributeName, fallbackValue, defaultValue, scale);
        }

        /// <summary>辅助方法: 计算缩放后的属性值（value * scale + fallback）</summary>
        private string GetScaledValue(
            XmlElement element,
            string actualAttrName,
            decimal fallback,
            decimal defaultValue,
            decimal scale)
        {
            var attrValue = element.GetAttribute(actualAttrName);
            if (decimal.TryParse(attrValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVal))
            {
                var result = parsedVal * scale + fallback;
                return result.ToString(CultureInfo.InvariantCulture);
            }

            Console.WriteLine($"[KartSpec] 警告: 属性{actualAttrName}值{attrValue}无效, 用默认值{defaultValue}");
            return defaultValue.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 尾流加速度
        /// </summary>
        public float draftMulAccelFactor { get; set; } = 1.1f;

        /// <summary>
        /// 尾流啟動時間（ms）
        /// </summary>
        public int draftTick { get; set; } = 2000;

        /// <summary>
        /// 瞬間加速器加速度
        /// </summary>
        public float driftBoostMulAccelFactor { get; set; } = 1.4f;

        /// <summary>
        /// 瞬間加速器加速時間
        /// </summary>
        public int driftBoostTick { get; set; } = 500;

        /// <summary>
        /// 自動集氣速度
        /// </summary>
        public float chargeBoostBySpeed { get; set; } = 350f;

        /// <summary>
        /// 競速賽道具欄位數
        /// </summary>
        public byte SpeedSlotCapacity { get; set; } = 2;

        /// <summary>
        /// 道具賽道具欄位數
        /// </summary>
        public byte ItemSlotCapacity { get; set; } = 2;

        /// <summary>
        /// 必殺計欄位數
        /// </summary>
        public byte SpecialSlotCapacity { get; set; } = 1;

        /// <summary>
        /// 啓用變形加速
        /// </summary>
        public byte UseTransformBooster { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 启用摩托车引擎
        /// </summary>
        public byte motorcycleType { get; set; } = (byte)(false ? 1 : 0);

        /// <summary>
        /// 是否有後輪
        /// </summary>
        public byte BikeRearWheel { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 车辆重量
        /// </summary>
        public float Mass { get; set; } = 100f;

        /// <summary>
        /// 空气阻力-
        /// </summary>
        public float AirFriction { get; set; } = 3f;

        /// <summary>
        /// 改裝名称：極限速度-
        /// </summary>
        public float DragFactor { get; set; } = 0.667f;

        /// <summary>
        /// 競速賽起步加速力
        /// </summary>
        public float ForwardAccelForce { get; set; } = 2304f;

        /// <summary>
        /// 倒车加速度+
        /// </summary>
        public float BackwardAccelForce { get; set; } = 1825f;

        /// <summary>
        /// 迴轉減速
        /// </summary>
        public float GripBrakeForce { get; set; } = 2070f;

        /// <summary>
        /// 漂移減速
        /// </summary>
        public float SlipBrakeForce { get; set; } = 1415f;

        /// <summary>
        /// 最大转向角度
        /// </summary>
        public float MaxSteerAngle { get; set; } = 10f;

        /// <summary>
        /// 轉向靈活度
        /// </summary>
        public float SteerConstraint { get; set; } = 24.61f;

        /// <summary>
        /// 弯道敏捷性
        /// </summary>
        public float FrontGripFactor { get; set; } = 5f;

        /// <summary>
        /// 弯道敏捷性
        /// </summary>
        public float RearGripFactor { get; set; } = 5f;

        /// <summary>
        /// 漂移触发系数
        /// </summary>
        public float DriftTriggerFactor { get; set; } = 0.2f;

        /// <summary>
        /// 漂移触发时间
        /// </summary>
        public float DriftTriggerTime { get; set; } = 0.2f;

        /// <summary>
        /// 漂移稳定性
        /// </summary>
        public float DriftSlipFactor { get; set; } = 0.2f;

        /// <summary>
        /// 改裝名称：漂移最佳化
        /// </summary>
        public float DriftEscapeForce { get; set; } = 4200f;

        /// <summary>
        /// 改裝名称：弯道加速度
        /// </summary>
        public float CornerDrawFactor { get; set; } = 0.254f;

        /// <summary>
        /// 漂移车身傾斜度
        /// </summary>
        public float DriftLeanFactor { get; set; } = 0.06f;

        /// <summary>
        /// 转弯倾斜角度
        /// </summary>
        public float SteerLeanFactor { get; set; } = 0.01f;

        /// <summary>
        /// 改裝名称：集气速度-
        /// </summary>
        public float DriftMaxGauge { get; set; } = 3860f;

        /// <summary>
        /// 改裝名称：加速器持续时间
        /// </summary>
        public float NormalBoosterTime { get; set; } = 2900f;

        /// <summary>
        /// 道具赛加速器加速时间
        /// </summary>
        public float ItemBoosterTime { get; set; } = 3000f;

        /// <summary>
        /// 改裝名称：團體加速器時間
        /// </summary>
        public float TeamBoosterTime { get; set; } = 4350f;

        /// <summary>
        /// 道具賽特殊加速器持续时间
        /// </summary>
        public float AnimalBoosterTime { get; set; } = 4000f;

        /// <summary>
        /// 超级加速器時間
        /// </summary>
        public float SuperBoosterTime { get; set; } = 3500f;

        /// <summary>
        /// 改裝名称：变形加速度
        /// </summary>
        public float TransAccelFactor { get; set; } = 1.8495f;

        /// <summary>
        /// 加速器加速度
        /// </summary>
        public float BoostAccelFactor { get; set; } = 1.494f;

        /// <summary>
        /// 道具賽起步時間
        /// </summary>
        public float StartBoosterTimeItem { get; set; } = 1000f;

        /// <summary>
        /// 改裝名称：競速賽起步時間
        /// </summary>
        public float StartBoosterTimeSpeed { get; set; } = 1500f;

        /// <summary>
        /// 道具賽起步加速力
        /// </summary>
        public float StartForwardAccelForceItem { get; set; } = 2304f;

        /// <summary>
        /// 競速賽起步加速力
        /// </summary>
        public float StartForwardAccelForceSpeed { get; set; } = 3745.588f;

        /// <summary>
        /// 碰撞时恢复集气量（％）
        /// </summary>
        public float DriftGaguePreservePercent { get; set; } = 0.5f;

        /// <summary>
        /// 道具赛启用瞬间加速器
        /// </summary>
        public byte UseExtendedAfterBooster { get; set; } = (byte)(false ? 1 : 0);

        /// <summary>
        /// 道具賽特殊加速器加速度
        /// </summary>
        public float BoostAccelFactorOnlyItem { get; set; } = 1.5f;

        /// <summary>
        /// 碰撞防禦力-
        /// </summary>
        public float antiCollideBalance { get; set; } = 0.91f;

        /// <summary>
        /// 启用二段式推進器
        /// </summary>
        public byte dualBoosterSetAuto { get; set; } = (byte)(false ? 1 : 0);

        /// <summary>
        /// 二段加速最小触发时间
        /// </summary>
        public int dualBoosterTickMin { get; set; } = 20;

        /// <summary>
        /// 二段加速最大触发时间
        /// </summary>
        public int dualBoosterTickMax { get; set; } = 30;

        /// <summary>
        /// 二段式推進器加速度
        /// </summary>
        public float dualMulAccelFactor { get; set; } = 1.04f;

        /// <summary>
        /// 二段式推進器最低觸發時速
        /// </summary>
        public float dualTransLowSpeed { get; set; } = 100f;

        /// <summary>
        /// 鎖定能量配件欄位
        /// </summary>
        public byte PartsEngineLock { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 鎖定輪胎欄位
        /// </summary>
        public byte PartsWheelLock { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 鎖定方向盤欄位
        /// </summary>
        public byte PartsSteeringLock { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 鎖定工具箱欄位
        /// </summary>
        public byte PartsBoosterLock { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 鎖定塗料欄位
        /// </summary>
        public byte PartsCoatingLock { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 鎖定尾燈欄位
        /// </summary>
        public byte PartsTailLampLock { get; set; } = (byte)(true ? 1 : 0);

        /// <summary>
        /// 超越推進器能量累積比例 - 開氣
        /// </summary>
        public float chargeInstAccelGaugeByBoost { get; set; } = 0.02f;

        /// <summary>
        /// 超越推進器能量累積比例 - 抓地
        /// </summary>
        public float chargeInstAccelGaugeByGrip { get; set; } = 0.06f;

        /// <summary>
        /// 超越推進器能量累積比例 - 碰撞
        /// </summary>
        public float chargeInstAccelGaugeByWall { get; set; } = 0.15f;

        /// <summary>
        /// 超越推進器加速度
        /// </summary>
        public float instAccelFactor { get; set; } = 1.11f;

        /// <summary>
        /// 超越推進器冷卻時間（s）
        /// </summary>
        public int instAccelGaugeCooldownTime { get; set; } = 3000;

        /// <summary>
        /// 超越推進器加速時間
        /// </summary>
        public float instAccelGaugeLength { get; set; } = 2500f;

        /// <summary>
        /// 超越推進器可用最低比例（%）
        /// </summary>
        public float instAccelGaugeMinUsable { get; set; } = 750f;

        /// <summary>
        /// 超越推進器最低觸發時速
        /// </summary>
        public float instAccelGaugeMinVelBound { get; set; } = 0f;

        /// <summary>
        /// 超越推進器最低減速
        /// </summary>
        public float instAccelGaugeMinVelLoss { get; set; } = 50f;

        public byte useExtendedAfterBoosterMore { get; set; } = (byte)(false ? 1 : 0);

        /// <summary>
        /// 碰撞集氣冷卻時間（s）
        /// </summary>
        public int wallCollGaugeCooldownTime { get; set; } = 3000;

        /// <summary>
        /// 碰撞集氣最高減速
        /// </summary>
        public float wallCollGaugeMaxVelLoss { get; set; } = 200f;

        /// <summary>
        /// 碰撞集氣最低觸發時速
        /// </summary>
        public float wallCollGaugeMinVelBound { get; set; } = 200f;

        /// <summary>
        /// 碰撞集氣最低減速
        /// </summary>
        public float wallCollGaugeMinVelLoss { get; set; } = 50f;

        /// <summary>
        /// 模型最大X軸
        /// </summary>
        public float modelMaxX { get; set; } = 0f;

        /// <summary>
        /// 模型最大Y軸
        /// </summary>
        public float modelMaxY { get; set; } = 0f;

        /// <summary>
        /// 預設超越系統類型
        /// </summary>
        public int defaultExceedType { get; set; } = 0;

        /// <summary>
        /// 預設引擎零件類型
        /// </summary>
        public byte defaultEngineType { get; set; } = 0;

        public byte EngineType { get; set; } = 1;

        /// <summary>
        /// 預設方向盤零件類型
        /// </summary>
        public byte defaultHandleType { get; set; } = 0;

        public byte HandleType { get; set; } = 1;

        /// <summary>
        /// 預設輪胎零件類型
        /// </summary>
        public byte defaultWheelType { get; set; } = 0;

        public byte WheelType { get; set; } = 1;

        /// <summary>
        /// 預設加速器零件類型
        /// </summary>
        public byte defaultBoosterType { get; set; } = 0;

        public byte BoosterType { get; set; } = 1;

        /// <summary>
        /// 賦能：增加超越能量累積 - 碰撞
        /// </summary>
        public float chargeInstAccelGaugeByWallAdded { get; set; } = 0f;

        /// <summary>
        /// 賦能：增加超越能量累積 - 開氣
        /// </summary>
        public float chargeInstAccelGaugeByBoostAdded { get; set; } = 0f;

        /// <summary>
        /// 啓用賦能系統需使用加速器數量
        /// </summary>
        public int chargerSystemboosterUseCount { get; set; } = 0;

        /// <summary>
        /// 賦能系統啓用時間（ms）
        /// </summary>
        public float chargerSystemUseTime { get; set; } = 0f;

        /// <summary>
        /// 賦能：自動集氣速度
        /// </summary>
        public float chargeBoostBySpeedAdded { get; set; } = 0f;

        /// <summary>
        /// 賦能：漂移集氣速度
        /// </summary>
        public float driftGaugeFactor { get; set; } = 0f;

        /// <summary>
        /// 賦能：增加碰撞防禦力
        /// </summary>
        public float chargeAntiCollideBalance { get; set; } = 1f;

        public int startItemTableId { get; set; } = 0;

        public int startItemId { get; set; } = 0;

        public float Unknown1 { get; set; } = 0;

        /// <summary>
        /// 鎖定超越推進器特效欄位
        /// </summary>
        public byte PartsBoosterEffectLock { get; set; } = (byte)(true ? 1 : 0);
    }
}
