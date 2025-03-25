using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using ExcData;
using KartRider;
using RiderData;

namespace KartRider
{
	public class KartSpec
	{
		public static void GetKartSpec()
		{
			if (StartGameData.Kart_id == 0)
			{
				defaultSpec.DefaultSpec();
				//StartGameData.Start_KartSpac();
			}
			else
			{
				if (KartExcData.KartName.ContainsKey(StartGameData.Kart_id))
				{
					string Name = KartExcData.KartName[StartGameData.Kart_id];
					Console.WriteLine($"Kart:{StartGameData.Kart_id},Name:{Name}");
					if (KartExcData.KartSpec.ContainsKey(Name))
					{
						XmlDocument Spec = KartExcData.KartSpec[Name];
						Kart_Spec(StartGameData.Kart_id, Spec);
					}
					else
					{
						defaultSpec.DefaultSpec();
						//StartGameData.Start_KartSpac();
					}
				}
				else
				{
					defaultSpec.DefaultSpec();
					//StartGameData.Start_KartSpac();
				}
			}
		}

		public static void Kart_Spec(short id, XmlDocument Spec)
		{
			XmlNodeList bodyParams = Spec.GetElementsByTagName("BodyParam");
			if (bodyParams.Count > 0)
			{
				foreach (XmlNode xn in bodyParams)
				{
					XmlElement xe = (XmlElement)xn;
					CreateKartSpec(id, xe);
					break;
				}
			}
			else
			{
				defaultSpec.DefaultSpec();
				StartGameData.Start_KartSpac();
			}
		}

		public static void CreateKartSpec(short id, XmlElement xe)
		{
			var attributes = new[]
		{
		("draftMulAccelFactor", 0M, 1M, 1M),
		("draftTick", 0M, 0M, 1M),
		("driftBoostMulAccelFactor", 0M, 1.31M, 1M),
		("driftBoostTick", 0M, 0M, 1M),
		("chargeBoostBySpeed", 0M, 2M, 1M),
		("SpeedSlotCapacity", 0M, 2M, 1M),
		("ItemSlotCapacity", 0M, 2M, 1M),
		("SpecialSlotCapacity", 0M, 1M, 1M),
		("UseTransformBooster", 0M, 0M, 1M),
		("motorcycleType", 0M, 0M, 1M),
		("BikeRearWheel", 0M, 1M, 1M),
		("Mass", 0M, 100M, 1M),
		("AirFriction", 0M, 3M, 1M),
		("DragFactor", 0.75M, 0.75M, 1M),
		("ForwardAccelForce", 2150M, 2150M, 1M),
		("BackwardAccelForce", 1725M, 1725M, 1M),
		("GripBrakeForce", 2070M, 2070M, 1M),
		("SlipBrakeForce", 1415M, 1415M, 1M),
		("MaxSteerAngle", 0M, 10M, 1M),
		("SteerConstraint", 22.25M, 22.25M, 1M),
		("FrontGripFactor", 0M, 5M, 1M),
		("RearGripFactor", 0M, 5M, 1M),
		("DriftTriggerFactor", 0M, 0.2M, 1M),
		("DriftTriggerTime", 0M, 0.2M, 1M),
		("DriftSlipFactor", 0M, 0.2M, 1M),
		("DriftEscapeForce", 2600M, 2600M, 1M),
		("CornerDrawFactor", 0.18M, 0.18M, 1M),
		("DriftLeanFactor", 0.07M, 0.07M, 1M),
		("SteerLeanFactor", 0.01M, 0.01M, 1M),
		("DriftMaxGauge", 4300M, 4300M, 1M),
		("NormalBoosterTime", 3000M, 3000M, 1M),
		("ItemBoosterTime", 3000M, 3000M, 1M),
		("TeamBoosterTime", 4500M, 4500M, 1M),
		("AnimalBoosterTime", 4000M, 4000M, 1M),
		("SuperBoosterTime", 3500M, 3500M, 1M),
		("TransAccelFactor", -0.0045M, 1.4955M, 1M),
		("BoostAccelFactor", -0.006M, 1.494M, 1M),
		("StartBoosterTimeItem", 0M, 1000M, 1M),
		("StartBoosterTimeSpeed", 0M, 1000M, 1M),
		("StartForwardAccelForceItem", 171.6355M, 2304M, 2102.325M),
		("StartForwardAccelForceSpeed", 176.132M, 2304M, 2099.68M),
		("DriftGaguePreservePercent", 0M, 0M, 1M),
		("UseExtendedAfterBooster", 0M, 0M, 1M),
		("BoostAccelFactorOnlyItem", 0M, 1.5M, 1M),
		("antiCollideBalance", 0M, 1M, 1M),
		("dualBoosterSetAuto", 0M, 0M, 1M),
		("dualBoosterTickMin", 0M, 40M, 1M),
		("dualBoosterTickMax", 0M, 60M, 1M),
		("dualMulAccelFactor", 0M, 1.1M, 1M),
		("dualTransLowSpeed", 0M, 100M, 1M),
		("PartsEngineLock", 0M, 0M, 1M),
		("PartsWheelLock", 0M, 0M, 1M),
		("PartsSteeringLock", 0M, 0M, 1M),
		("PartsBoosterLock", 0M, 0M, 1M),
		("PartsCoatingLock", 0M, 0M, 1M),
		("PartsTailLampLock", 0M, 0M, 1M),
		("chargeInstAccelGaugeByBoost", 0M, 0.02M, 1M),
		("chargeInstAccelGaugeByGrip", 0M, 0.02M, 1M),
		("chargeInstAccelGaugeByWall", 0M, 0.2M, 1M),
		("instAccelFactor", 0M, 1.25M, 1M),
		("instAccelGaugeCooldownTime", 0M, 0M, 1000M),
		("instAccelGaugeLength", 0M, 0M, 1000M),
		("instAccelGaugeMinUsable", 0M, 0M, 1000M),
		("instAccelGaugeMinVelBound", 0M, 200M, 1M),
		("instAccelGaugeMinVelLoss", 0M, 50M, 1M),
		("useExtendedAfterBoosterMore", 0M, 0M, 1M),
		("wallCollGaugeCooldownTime", 0M, 0M, 1000M),
		("wallCollGaugeMaxVelLoss", 0M, 200M, 1M),
		("wallCollGaugeMinVelBound", 0M, 200M, 1M),
		("wallCollGaugeMinVelLoss", 0M, 50M, 1M),
		("modelMaxX", 0M, 0M, 1M),
		("modelMaxY", 0M, 0M, 1M),
		("defaultExceedType", 0M, 0M, 1M),
		("defaultEngineType", 0M, 0M, 1M),
		("defaultHandleType", 0M, 0M, 1M),
		("defaultWheelType", 0M, 0M, 1M),
		("defaultBoosterType", 0M, 0M, 1M),
		("chargeInstAccelGaugeByWallAdded", 0M, 0M, 1M),
		("chargeInstAccelGaugeByBoostAdded", 0M, 0M, 1M),
		("chargerSystemboosterUseCount", 0M, 0M, 1M),
		("chargerSystemUseTime", 0M, 0M, 1M),
		("chargeBoostBySpeedAdded", 0M, 0M, 1M),
		("driftGaugeFactor", 0M, 0M, 1M),
		("chargeAntiCollideBalance", 0M, 1M, 1M),
		};
			List<string> AddList = new List<string>();
			foreach (var (attributeName, fallbackValue, defaultValue, scale) in attributes)
			{
				string attrValue = GetAttributeValue(xe, attributeName, fallbackValue, defaultValue, scale);
				AddList.Add(attrValue);
			}
			Kart.draftMulAccelFactor = float.Parse(AddList[0]);
			Kart.draftTick = int.Parse(AddList[1]);
			Kart.driftBoostMulAccelFactor = float.Parse(AddList[2]);
			Kart.driftBoostTick = int.Parse(AddList[3]);
			Kart.chargeBoostBySpeed = float.Parse(AddList[4]);
			Kart.SpeedSlotCapacity = byte.Parse(AddList[5]);
			Kart.ItemSlotCapacity = byte.Parse(AddList[6]);
			Kart.SpecialSlotCapacity = byte.Parse(AddList[7]);
			Kart.UseTransformBooster = byte.Parse(AddList[8]);
			Kart.motorcycleType = byte.Parse(AddList[9]);
			Kart.BikeRearWheel = byte.Parse(AddList[10]);
			Kart.Mass = float.Parse(AddList[11]);
			Kart.AirFriction = float.Parse(AddList[12]);
			Kart.DragFactor = float.Parse(AddList[13]);
			Kart.ForwardAccelForce = float.Parse(AddList[14]);
			Kart.BackwardAccelForce = float.Parse(AddList[15]);
			Kart.GripBrakeForce = float.Parse(AddList[16]);
			Kart.SlipBrakeForce = float.Parse(AddList[17]);
			Kart.MaxSteerAngle = float.Parse(AddList[18]);
			Kart.SteerConstraint = float.Parse(AddList[19]);
			Kart.FrontGripFactor = float.Parse(AddList[20]);
			Kart.RearGripFactor = float.Parse(AddList[21]);
			Kart.DriftTriggerFactor = float.Parse(AddList[22]);
			Kart.DriftTriggerTime = float.Parse(AddList[23]);
			Kart.DriftSlipFactor = float.Parse(AddList[24]);
			Kart.DriftEscapeForce = float.Parse(AddList[25]);
			Kart.CornerDrawFactor = float.Parse(AddList[26]);
			Kart.DriftLeanFactor = float.Parse(AddList[27]);
			Kart.SteerLeanFactor = float.Parse(AddList[28]);
			Kart.DriftMaxGauge = float.Parse(AddList[29]);
			Kart.NormalBoosterTime = float.Parse(AddList[30]);
			Kart.ItemBoosterTime = float.Parse(AddList[31]);
			Kart.TeamBoosterTime = float.Parse(AddList[32]);
			Kart.AnimalBoosterTime = float.Parse(AddList[33]);
			Kart.SuperBoosterTime = float.Parse(AddList[34]);
			Kart.TransAccelFactor = float.Parse(AddList[35]);
			Kart.BoostAccelFactor = float.Parse(AddList[36]);
			Kart.StartBoosterTimeItem = float.Parse(AddList[37]);
			Kart.StartBoosterTimeSpeed = float.Parse(AddList[38]);
			Kart.StartForwardAccelForceItem = float.Parse(AddList[39]);
			Kart.StartForwardAccelForceSpeed = float.Parse(AddList[40]);
			Kart.DriftGaguePreservePercent = float.Parse(AddList[41]);
			Kart.UseExtendedAfterBooster = byte.Parse(AddList[42]);
			Kart.BoostAccelFactorOnlyItem = float.Parse(AddList[43]);
			Kart.antiCollideBalance = float.Parse(AddList[44]);
			Kart.dualBoosterSetAuto = byte.Parse(AddList[45]);
			Kart.dualBoosterTickMin = int.Parse(AddList[46]);
			Kart.dualBoosterTickMax = int.Parse(AddList[47]);
			Kart.dualMulAccelFactor = float.Parse(AddList[48]);
			Kart.dualTransLowSpeed = float.Parse(AddList[49]);
			Kart.PartsEngineLock = byte.Parse(AddList[50]);
			Kart.PartsWheelLock = byte.Parse(AddList[51]);
			Kart.PartsSteeringLock = byte.Parse(AddList[52]);
			Kart.PartsBoosterLock = byte.Parse(AddList[53]);
			Kart.PartsCoatingLock = byte.Parse(AddList[54]);
			Kart.PartsTailLampLock = byte.Parse(AddList[55]);
			Kart.chargeInstAccelGaugeByBoost = float.Parse(AddList[56]);
			Kart.chargeInstAccelGaugeByGrip = float.Parse(AddList[57]);
			Kart.chargeInstAccelGaugeByWall = float.Parse(AddList[58]);
			Kart.instAccelFactor = float.Parse(AddList[59]);
			Kart.instAccelGaugeCooldownTime = int.Parse(AddList[60]);
			Kart.instAccelGaugeLength = float.Parse(AddList[61]);
			Kart.instAccelGaugeMinUsable = float.Parse(AddList[62]);
			Kart.instAccelGaugeMinVelBound = float.Parse(AddList[63]);
			Kart.instAccelGaugeMinVelLoss = float.Parse(AddList[64]);
			Kart.useExtendedAfterBoosterMore = byte.Parse(AddList[65]);
			Kart.wallCollGaugeCooldownTime = int.Parse(AddList[66]);
			Kart.wallCollGaugeMaxVelLoss = float.Parse(AddList[67]);
			Kart.wallCollGaugeMinVelBound = float.Parse(AddList[68]);
			Kart.wallCollGaugeMinVelLoss = float.Parse(AddList[69]);
			Kart.modelMaxX = float.Parse(AddList[70]);
			Kart.modelMaxY = float.Parse(AddList[71]);
			Kart.defaultExceedType = int.Parse(AddList[72]);
			Kart.defaultEngineType = byte.Parse(AddList[73]);
			Kart.defaultHandleType = byte.Parse(AddList[74]);
			Kart.defaultWheelType = byte.Parse(AddList[75]);
			Kart.defaultBoosterType = byte.Parse(AddList[76]);
			Kart.chargeInstAccelGaugeByWallAdded = float.Parse(AddList[77]);
			Kart.chargeInstAccelGaugeByBoostAdded = float.Parse(AddList[78]);
			Kart.chargerSystemboosterUseCount = int.Parse(AddList[79]);
			Kart.chargerSystemUseTime = float.Parse(AddList[80]);
			Kart.chargeBoostBySpeedAdded = float.Parse(AddList[81]);
			Kart.driftGaugeFactor = float.Parse(AddList[82]);
			Kart.chargeAntiCollideBalance = float.Parse(AddList[83]);
			Kart.startItemTableId = 0;
			Kart.startItemId = 0;
			V2Spec.ExceedSpec();
			//StartGameData.Start_KartSpac();
		}

		public static string GetAttributeValue(XmlElement xe, string attributeName, decimal fallbackValue, decimal defaultValue, decimal scale)
		{
			if (attributeName == "UseTransformBooster" || attributeName == "motorcycleType" || attributeName == "BikeRearWheel" || attributeName == "UseExtendedAfterBooster" || attributeName == "dualBoosterSetAuto" || attributeName == "PartsEngineLock" || attributeName == "PartsWheelLock" || attributeName == "PartsSteeringLock" || attributeName == "PartsBoosterLock" || attributeName == "PartsCoatingLock" || attributeName == "PartsTailLampLock" || attributeName == "useExtendedAfterBoosterMore")
			{
				return bool.TryParse(xe.GetAttribute(attributeName), out var value) ? (value ? 1M : 0M).ToString("G29") : defaultValue.ToString("G29");
			}
			else
			{
				if (attributeName == "StartForwardAccelForceItem")
				{
					return decimal.TryParse(xe.GetAttribute("StartForwardAccelFactorItem"), out var value) ? (value * scale + fallbackValue).ToString("G29") : defaultValue.ToString("G29");
				}
				if (attributeName == "StartForwardAccelForceSpeed")
				{
					return decimal.TryParse(xe.GetAttribute("StartForwardAccelFactorSpeed"), out var value) ? (value * scale + fallbackValue).ToString("G29") : defaultValue.ToString("G29");
				}
				if (attributeName == "instAccelGaugeMinUsable")
				{
					decimal instAccelGaugeLength = decimal.TryParse(xe.GetAttribute("instAccelGaugeLength"), out var tempinstAccelGaugeLength) ? tempinstAccelGaugeLength : 0M;
					return decimal.TryParse(xe.GetAttribute(attributeName), out var value) ? (value * instAccelGaugeLength * scale + fallbackValue).ToString("G29") : defaultValue.ToString("G29");
				}
				else
				{
					return decimal.TryParse(xe.GetAttribute(attributeName), out var value) ? (value * scale + fallbackValue).ToString("G29") : defaultValue.ToString("G29");
				}
			}
		}
	}
}
