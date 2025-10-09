using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using ExcData;
using KartRider;
using RiderData;
using Profile;

namespace KartRider
{
    public class KartSpec
    {
        public static Dictionary<int, string> kartName = new Dictionary<int, string>();
        public static Dictionary<string, XmlDocument> kartSpec = new Dictionary<string, XmlDocument>();

        #region 常量定义（消除硬编码）
        /// <summary>布尔类型的XML属性名集合（统一管理）</summary>
        private static readonly HashSet<string> BooleanAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            "UseTransformBooster", "motorcycleType", "BikeRearWheel", "UseExtendedAfterBooster",
            "dualBoosterSetAuto", "PartsEngineLock", "PartsWheelLock", "PartsSteeringLock",
            "PartsBoosterLock", "PartsCoatingLock", "PartsTailLampLock", "useExtendedAfterBoosterMore"
        };

        /// <summary>部件默认类型（EngineType/HandleType等的默认值1）</summary>
        private const byte DefaultPartType = 1;
        /// <summary>模型尺寸默认值</summary>
        private const float DefaultModelDimension = 0f;
        #endregion

        #region 配置类（优化数据映射）
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
        #endregion

        #region 规格配置集合（统一管理属性映射）
        /// <summary>所有卡丁车规格属性的配置（替代原attributes数组，避免索引依赖）</summary>
        private static IEnumerable<KartSpecConfig> KartSpecConfigs => new List<KartSpecConfig>
        {
            new("draftMulAccelFactor", 0M, 1M, 1M, val => Kart.draftMulAccelFactor = (float)val),
            new("draftTick", 0M, 0M, 1M, val => Kart.draftTick = (int)val),
            new("driftBoostMulAccelFactor", 0M, 1.31M, 1M, val => Kart.driftBoostMulAccelFactor = (float)val),
            new("driftBoostTick", 0M, 0M, 1M, val => Kart.driftBoostTick = (int)val),
            new("chargeBoostBySpeed", 0M, 2M, 1M, val => Kart.chargeBoostBySpeed = (float)val),
            new("SpeedSlotCapacity", 0M, 2M, 1M, val => Kart.SpeedSlotCapacity = (byte)val),
            new("ItemSlotCapacity", 0M, 2M, 1M, val => Kart.ItemSlotCapacity = (byte)val),
            new("SpecialSlotCapacity", 0M, 1M, 1M, val => Kart.SpecialSlotCapacity = (byte)val),
            new("UseTransformBooster", 0M, 0M, 1M, val => Kart.UseTransformBooster = (byte)val),
            new("motorcycleType", 0M, 0M, 1M, val => Kart.motorcycleType = (byte)val),
            new("BikeRearWheel", 0M, 1M, 1M, val => Kart.BikeRearWheel = (byte)val),
            new("Mass", 0M, 100M, 1M, val => Kart.Mass = (float)val),
            new("AirFriction", 0M, 3M, 1M, val => Kart.AirFriction = (float)val),
            new("DragFactor", 0.75M, 0.75M, 1M, val => Kart.DragFactor = (float)val),
            new("ForwardAccelForce", 2150M, 2150M, 1M, val => Kart.ForwardAccelForce = (float)val),
            new("BackwardAccelForce", 1725M, 1725M, 1M, val => Kart.BackwardAccelForce = (float)val),
            new("GripBrakeForce", 2070M, 2070M, 1M, val => Kart.GripBrakeForce = (float)val),
            new("SlipBrakeForce", 1415M, 1415M, 1M, val => Kart.SlipBrakeForce = (float)val),
            new("MaxSteerAngle", 0M, 10M, 1M, val => Kart.MaxSteerAngle = (float)val),
            new("SteerConstraint", 22.25M, 22.25M, 1M, val => Kart.SteerConstraint = (float)val),
            new("FrontGripFactor", 0M, 5M, 1M, val => Kart.FrontGripFactor = (float)val),
            new("RearGripFactor", 0M, 5M, 1M, val => Kart.RearGripFactor = (float)val),
            new("DriftTriggerFactor", 0M, 0.2M, 1M, val => Kart.DriftTriggerFactor = (float)val),
            new("DriftTriggerTime", 0M, 0.2M, 1M, val => Kart.DriftTriggerTime = (float)val),
            new("DriftSlipFactor", 0M, 0.2M, 1M, val => Kart.DriftSlipFactor = (float)val),
            new("DriftEscapeForce", 2600M, 2600M, 1M, val => Kart.DriftEscapeForce = (float)val),
            new("CornerDrawFactor", 0.18M, 0.18M, 1M, val => Kart.CornerDrawFactor = (float)val),
            new("DriftLeanFactor", 0.07M, 0.07M, 1M, val => Kart.DriftLeanFactor = (float)val),
            new("SteerLeanFactor", 0.01M, 0.01M, 1M, val => Kart.SteerLeanFactor = (float)val),
            new("DriftMaxGauge", 4300M, 4300M, 1M, val => Kart.DriftMaxGauge = (float)val),
            new("NormalBoosterTime", 3000M, 3000M, 1M, val => Kart.NormalBoosterTime = (float)val),
            new("ItemBoosterTime", 3000M, 3000M, 1M, val => Kart.ItemBoosterTime = (float)val),
            new("TeamBoosterTime", 4500M, 4500M, 1M, val => Kart.TeamBoosterTime = (float)val),
            new("AnimalBoosterTime", 4000M, 4000M, 1M, val => Kart.AnimalBoosterTime = (float)val),
            new("SuperBoosterTime", 3500M, 3500M, 1M, val => Kart.SuperBoosterTime = (float)val),
            new("TransAccelFactor", -0.0045M, 1.4955M, 1M, val => Kart.TransAccelFactor = (float)val),
            new("BoostAccelFactor", -0.006M, 1.494M, 1M, val => Kart.BoostAccelFactor = (float)val),
            new("StartBoosterTimeItem", 0M, 1000M, 1M, val => Kart.StartBoosterTimeItem = (float)val),
            new("StartBoosterTimeSpeed", 0M, 1000M, 1M, val => Kart.StartBoosterTimeSpeed = (float)val),
            new("StartForwardAccelForceItem", 171.6355M, 2304M, 2102.325M, val => Kart.StartForwardAccelForceItem = (float)val),
            new("StartForwardAccelForceSpeed", 176.132M, 2304M, 2099.68M, val => Kart.StartForwardAccelForceSpeed = (float)val),
            new("DriftGaguePreservePercent", 0M, 0M, 1M, val => Kart.DriftGaguePreservePercent = (float)val),
            new("UseExtendedAfterBooster", 0M, 0M, 1M, val => Kart.UseExtendedAfterBooster = (byte)val),
            new("BoostAccelFactorOnlyItem", 0M, 1.5M, 1M, val => Kart.BoostAccelFactorOnlyItem = (float)val),
            new("antiCollideBalance", 0M, 1M, 1M, val => Kart.antiCollideBalance = (float)val),
            new("dualBoosterSetAuto", 0M, 0M, 1M, val => Kart.dualBoosterSetAuto = (byte)val),
            new("dualBoosterTickMin", 0M, 40M, 1M, val => Kart.dualBoosterTickMin = (int)val),
            new("dualBoosterTickMax", 0M, 60M, 1M, val => Kart.dualBoosterTickMax = (int)val),
            new("dualMulAccelFactor", 0M, 1.1M, 1M, val => Kart.dualMulAccelFactor = (float)val),
            new("dualTransLowSpeed", 0M, 100M, 1M, val => Kart.dualTransLowSpeed = (float)val),
            new("PartsEngineLock", 0M, 0M, 1M, val => Kart.PartsEngineLock = (byte)val),
            new("PartsWheelLock", 0M, 0M, 1M, val => Kart.PartsWheelLock = (byte)val),
            new("PartsSteeringLock", 0M, 0M, 1M, val => Kart.PartsSteeringLock = (byte)val),
            new("PartsBoosterLock", 0M, 0M, 1M, val => Kart.PartsBoosterLock = (byte)val),
            new("PartsCoatingLock", 0M, 0M, 1M, val => Kart.PartsCoatingLock = (byte)val),
            new("PartsTailLampLock", 0M, 0M, 1M, val => Kart.PartsTailLampLock = (byte)val),
            new("chargeInstAccelGaugeByBoost", 0M, 0.02M, 1M, val => Kart.chargeInstAccelGaugeByBoost = (float)val),
            new("chargeInstAccelGaugeByGrip", 0M, 0.02M, 1M, val => Kart.chargeInstAccelGaugeByGrip = (float)val),
            new("chargeInstAccelGaugeByWall", 0M, 0.2M, 1M, val => Kart.chargeInstAccelGaugeByWall = (float)val),
            new("instAccelFactor", 0M, 1.25M, 1M, val => Kart.instAccelFactor = (float)val),
            new("instAccelGaugeCooldownTime", 0M, 0M, 1000M, val => Kart.instAccelGaugeCooldownTime = (int)val),
            new("instAccelGaugeLength", 0M, 0M, 1000M, val => Kart.instAccelGaugeLength = (float)val),
            new("instAccelGaugeMinUsable", 0M, 0M, 1000M, val => Kart.instAccelGaugeMinUsable = (float)val),
            new("instAccelGaugeMinVelBound", 0M, 200M, 1M, val => Kart.instAccelGaugeMinVelBound = (float)val),
            new("instAccelGaugeMinVelLoss", 0M, 50M, 1M, val => Kart.instAccelGaugeMinVelLoss = (float)val),
            new("useExtendedAfterBoosterMore", 0M, 0M, 1M, val => Kart.useExtendedAfterBoosterMore = (byte)val),
            new("wallCollGaugeCooldownTime", 0M, 0M, 1000M, val => Kart.wallCollGaugeCooldownTime = (int)val),
            new("wallCollGaugeMaxVelLoss", 0M, 200M, 1M, val => Kart.wallCollGaugeMaxVelLoss = (float)val),
            new("wallCollGaugeMinVelBound", 0M, 200M, 1M, val => Kart.wallCollGaugeMinVelBound = (float)val),
            new("wallCollGaugeMinVelLoss", 0M, 50M, 1M, val => Kart.wallCollGaugeMinVelLoss = (float)val),
            new("defaultExceedType", 0M, 0M, 1M, val => Kart.defaultExceedType = (int)val),
            new("defaultEngineType", 0M, 0M, 1M, val => Kart.defaultEngineType = (byte)val),
            new("defaultHandleType", 0M, 0M, 1M, val => Kart.defaultHandleType = (byte)val),
            new("defaultWheelType", 0M, 0M, 1M, val => Kart.defaultWheelType = (byte)val),
            new("defaultBoosterType", 0M, 0M, 1M, val => Kart.defaultBoosterType = (byte)val),
            new("chargeInstAccelGaugeByWallAdded", 0M, 0M, 1M, val => Kart.chargeInstAccelGaugeByWallAdded = (float)val),
            new("chargeInstAccelGaugeByBoostAdded", 0M, 0M, 1M, val => Kart.chargeInstAccelGaugeByBoostAdded = (float)val),
            new("chargerSystemboosterUseCount", 0M, 0M, 1M, val => Kart.chargerSystemboosterUseCount = (int)val),
            new("chargerSystemUseTime", 0M, 0M, 1M, val => Kart.chargerSystemUseTime = (float)val),
            new("chargeBoostBySpeedAdded", 0M, 0M, 1M, val => Kart.chargeBoostBySpeedAdded = (float)val),
            new("driftGaugeFactor", 0M, 0M, 1M, val => Kart.driftGaugeFactor = (float)val),
            new("chargeAntiCollideBalance", 0M, 1M, 1M, val => Kart.chargeAntiCollideBalance = (float)val),
        };
        #endregion

        #region 对外接口（主逻辑）
        /// <summary>获取卡丁车规格入口方法</summary>
        public static void GetKartSpec()
        {
            try
            {
                // 1. 处理卡丁车ID为0的默认情况
                if (StartGameData.Kart_id == 0)
                {
                    Console.WriteLine("[KartSpec] 卡丁车ID=0，加载默认学校规格");
                    ApplyDefaultSpec();
                }
                // 2. 处理非默认卡丁车ID
                else
                {
                    var kartId = StartGameData.Kart_id;
                    // 2.1 检查KartName字典是否存在该ID
                    if (!kartName.TryGetValue(kartId, out var Name))
                    {
                        Console.WriteLine($"[KartSpec] 警告：KartName中未找到ID={kartId}，加载默认规格");
                        ApplyDefaultSpec();
                        return;
                    }

                    Console.WriteLine($"[KartSpec] 加载卡丁车：ID={kartId}，名称={Name}");

                    // 2.2 检查KartSpec字典是否存在该规格
                    if (!kartSpec.TryGetValue(Name, out var kartSpecDoc))
                    {
                        Console.WriteLine($"[KartSpec] 警告：KartSpec中未找到名称={Name}的规格，加载默认规格");
                        ApplyDefaultSpec();
                        return;
                    }

                    // 2.3 解析规格XML并赋值
                    ParseKartSpecXml(kartId, kartSpecDoc);
                    // 3. 初始化V2规格（保持原逻辑）
                    new V2Spec().ExceedSpec();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KartSpec] 错误：获取规格时异常 - {ex.Message}\n堆栈：{ex.StackTrace}");
                // 异常降级：确保加载默认规格，避免程序崩溃
                ApplyDefaultSpec();
            }
        }
        #endregion

        #region 私有辅助方法（拆分复杂逻辑）
        /// <summary>应用默认规格（消除重复调用）</summary>
        private static void ApplyDefaultSpec()
        {
            SchoolSpec.DefaultSpec();
            new V2Spec().ExceedSpec();
            // 原注释代码：若后续需要恢复，可取消注释并添加用途说明
            // StartGameData.Start_KartSpac();
        }

        /// <summary>解析卡丁车规格XML文档</summary>
        private static void ParseKartSpecXml(short kartId, XmlDocument specDoc)
        {
            var bodyParams = specDoc.GetElementsByTagName("BodyParam");
            // 检查是否存在BodyParam节点且为XmlElement类型
            if (bodyParams.Count > 0 && bodyParams[0] is XmlElement bodyParamElement)
            {
                AssignKartProperties(kartId, bodyParamElement);
            }
            else
            {
                Console.WriteLine($"[KartSpec] 警告：ID={kartId}的规格XML中无BodyParam节点，加载默认规格");
                ApplyDefaultSpec();
            }
        }

        /// <summary>给Kart静态字段赋值（核心数据映射）</summary>
        private static void AssignKartProperties(short kartId, XmlElement bodyParamElement)
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
                    Console.WriteLine($"[KartSpec] 警告：属性{config.AttributeName}值{attrValueStr}无效，用默认值{config.DefaultValue}");
                    config.SetKartProperty(config.DefaultValue);
                }
            }

            // 2. 加载模型尺寸（单独处理ModelMax.xml）
            var (modelMaxX, modelMaxY) = LoadModelMaxDimensions(kartId);
            Kart.modelMaxX = modelMaxX;
            Kart.modelMaxY = modelMaxY;

            // 3. 设置默认部件类型（Engine/Handle等）
            Kart.EngineType = DefaultPartType;
            Kart.HandleType = DefaultPartType;
            Kart.WheelType = DefaultPartType;
            Kart.BoosterType = DefaultPartType;

            // 4. 初始化物品ID（保持原逻辑）
            Kart.startItemTableId = 0;
            Kart.startItemId = 0;
        }

        /// <summary>加载ModelMax.xml中的模型尺寸（modelMaxX/modelMaxY）</summary>
        private static (float modelMaxX, float modelMaxY) LoadModelMaxDimensions(short kartId)
        {
            // 检查文件是否存在
            if (!File.Exists(FileName.ModelMax_LoadFile))
            {
                Console.WriteLine($"[KartSpec] 警告：ModelMax.xml不存在 - 路径：{FileName.ModelMax_LoadFile}");
                return (DefaultModelDimension, DefaultModelDimension);
            }

            try
            {
                // 只对文件流使用using，XDocument不实现IDisposable，不需要using
                using (var fileStream = File.OpenRead(FileName.ModelMax_LoadFile))
                {
                    var xdoc = XDocument.Load(fileStream);

                    // 查找对应ID的节点（如id123）
                    var targetNodeName = $"id{kartId}";
                    var targetNode = xdoc.Descendants(targetNodeName).FirstOrDefault();
                    if (targetNode == null)
                    {
                        Console.WriteLine($"[KartSpec] 警告：ModelMax.xml中无节点{targetNodeName}（ID={kartId}）");
                        return (DefaultModelDimension, DefaultModelDimension);
                    }

                    // 解析modelMaxX
                    var modelMaxX = ParseAttributeToFloat(targetNode.Attribute("modelMaxX"), "modelMaxX", kartId);
                    // 解析modelMaxY
                    var modelMaxY = ParseAttributeToFloat(targetNode.Attribute("modelMaxY"), "modelMaxY", kartId);

                    return (modelMaxX, modelMaxY);
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"[KartSpec] 错误：解析ModelMax.xml失败 - {ex.Message}");
                return (DefaultModelDimension, DefaultModelDimension);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[KartSpec] 错误：读取ModelMax.xml失败 - {ex.Message}");
                return (DefaultModelDimension, DefaultModelDimension);
            }
        }

        /// <summary>辅助方法：将XAttribute解析为float（失败返回默认值）</summary>
        private static float ParseAttributeToFloat(XAttribute attr, string attrName, short kartId)
        {
            if (attr == null)
            {
                Console.WriteLine($"[KartSpec] 警告：ID={kartId}的{attrName}属性不存在");
                return DefaultModelDimension;
            }

            if (float.TryParse(attr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            Console.WriteLine($"[KartSpec] 警告：ID={kartId}的{attrName}值{attr.Value}无效");
            return DefaultModelDimension;
        }

        /// <summary>获取XML属性值（处理布尔/特殊数值逻辑）</summary>
        public static string GetAttributeValue(
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
                Console.WriteLine($"[KartSpec] 警告：布尔属性{attributeName}值{attrValue}无效，用默认值{defaultValue}");
                return defaultValue.ToString(CultureInfo.InvariantCulture);
            }

            // 2. 处理特殊属性：StartForwardAccelForceItem（读取Factor属性）
            if (attributeName == "StartForwardAccelForceItem")
            {
                return GetScaledValue(element, "StartForwardAccelFactorItem", fallbackValue, defaultValue, scale);
            }

            // 3. 处理特殊属性：StartForwardAccelForceSpeed（读取Factor属性）
            if (attributeName == "StartForwardAccelForceSpeed")
            {
                return GetScaledValue(element, "StartForwardAccelFactorSpeed", fallbackValue, defaultValue, scale);
            }

            // 4. 处理特殊属性：instAccelGaugeMinUsable（依赖instAccelGaugeLength）
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

        /// <summary>辅助方法：计算缩放后的属性值（value * scale + fallback）</summary>
        private static string GetScaledValue(
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

            Console.WriteLine($"[KartSpec] 警告：属性{actualAttrName}值{attrValue}无效，用默认值{defaultValue}");
            return defaultValue.ToString(CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
