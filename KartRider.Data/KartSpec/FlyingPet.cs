using KartRider.Logging;
using ExcData;
using KartRider;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;

namespace KartRider
{
    public class FlyingPet
    {
        public static Dictionary<int, string> flyingName = new Dictionary<int, string>();
        public static Dictionary<string, XmlDocument> flyingSpec = new Dictionary<string, XmlDocument>();
    }

    public class FlyingPetSpec
    {
        public float DragFactor { get; set; } = 0f;
        public float ForwardAccelForce { get; set; } = 0f;
        public float DriftEscapeForce { get; set; } = 0f;
        public float CornerDrawFactor { get; set; } = 0f;
        public float NormalBoosterTime { get; set; } = 0f;
        public float ItemBoosterTime { get; set; } = 0f;
        public float TeamBoosterTime { get; set; } = 0f;
        public float StartForwardAccelForceItem { get; set; } = 0f;
        public float StartForwardAccelForceSpeed { get; set; } = 0f;

        public void FlyingPet_Spec(ushort FlyingPetID)
        {
            if (FlyingPet.flyingName.TryGetValue(FlyingPetID, out string petName))
            {
                Log.Debug($"flying:{FlyingPetID},Name:{petName}");
                // 必须从根节点读属性：原来 foreach 遍历 XmlDocument 时第一个节点是 <?xml ...?> 声明节点，
                // 它没有属性，于是所有数值被写成 0 后立刻 break，根节点 <PetParam> 永远读不到。
                XmlElement petParam = FindSpecElement(petName);
                this.DragFactor = ReadFloat(petParam, "DragFactor");
                this.ForwardAccelForce = ReadFloat(petParam, "ForwardAccelForce");
                this.DriftEscapeForce = ReadFloat(petParam, "DriftEscapeForce");
                this.CornerDrawFactor = ReadFloat(petParam, "CornerDrawFactor");
                this.NormalBoosterTime = ReadFloat(petParam, "NormalBoosterTime");
                this.ItemBoosterTime = ReadFloat(petParam, "ItemBoosterTime");
                this.TeamBoosterTime = ReadFloat(petParam, "TeamBoosterTime");
                // 客户端实际写的是 StartForwardAccelItem / StartForwardAccelSpeed，旧写法一并兼容
                this.StartForwardAccelForceItem = ReadFloat(petParam, "StartForwardAccelItem", "StartForwardAccelForceItem");
                this.StartForwardAccelForceSpeed = ReadFloat(petParam, "StartForwardAccelSpeed", "StartForwardAccelForceSpeed");
            }
            Log.Debug($"-------------------------------------------------------------");
            Log.Debug($"FlyingPet DragFactor:{this.DragFactor}");
            Log.Debug($"FlyingPet ForwardAccelForce:{this.ForwardAccelForce}");
            Log.Debug($"FlyingPet DriftEscapeForce:{this.DriftEscapeForce}");
            Log.Debug($"FlyingPet CornerDrawFactor:{this.CornerDrawFactor}");
            Log.Debug($"FlyingPet NormalBoosterTime:{this.NormalBoosterTime}");
            Log.Debug($"FlyingPet ItemBoosterTime:{this.ItemBoosterTime}");
            Log.Debug($"FlyingPet TeamBoosterTime:{this.TeamBoosterTime}");
            Log.Debug($"FlyingPet StartForwardAccelForceItem:{this.StartForwardAccelForceItem}");
            Log.Debug($"FlyingPet StartForwardAccelForceSpeed:{this.StartForwardAccelForceSpeed}");
            Log.Debug($"-------------------------------------------------------------");
        }

        /// <summary>按飞宠名字找参数 XML 的根节点：先精确匹配，再忽略大小写 / 路径后缀匹配。</summary>
        private static XmlElement FindSpecElement(string petName)
        {
            if (string.IsNullOrWhiteSpace(petName)) return null;
            string name = petName.Trim().TrimEnd('/');
            if (FlyingPet.flyingSpec.TryGetValue(name, out XmlDocument exact) && exact != null)
            {
                return exact.DocumentElement;
            }
            foreach (KeyValuePair<string, XmlDocument> pair in FlyingPet.flyingSpec)
            {
                if (pair.Value == null) continue;
                string key = pair.Key.TrimEnd('/');
                if (key.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    key.EndsWith("/" + name, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value.DocumentElement;
                }
            }
            return null;
        }

        /// <summary>读取浮点属性：属性名忽略大小写、支持多个别名，数值固定按 InvariantCulture 解析。</summary>
        private static float ReadFloat(XmlElement element, params string[] attributeNames)
        {
            if (element == null || attributeNames == null) return 0f;
            foreach (string attributeName in attributeNames)
            {
                string text = GetAttribute(element, attributeName);
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return value;
                if (float.TryParse(text, out value)) return value;
            }
            return 0f;
        }

        private static string GetAttribute(XmlElement element, string attributeName)
        {
            if (element?.Attributes == null) return string.Empty;
            XmlAttribute attribute = element.Attributes[attributeName];
            if (attribute != null) return attribute.Value;
            foreach (XmlAttribute candidate in element.Attributes)
            {
                if (string.Equals(candidate.Name, attributeName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate.Value;
                }
            }
            return string.Empty;
        }
    }
}