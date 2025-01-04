using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KartLibrary.Text;
using KartLibrary.IO;
using System.Xml;
using System.Dynamic;
using System.Linq.Expressions;
using System.Numerics;
using Vulkan;
using System.Text.Json.Nodes;

namespace KartLibrary.Xml
{
    public class BinaryXmlTag: DynamicObject
    {
        #region Members
        private string _name;
        private string _text;
        private Dictionary<string, string> _attributes;
        private List<BinaryXmlTag> _children;
        #endregion

        #region Properties
        public string Name
        {
            get => _name; 
            set => _name = value;
        }

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public IReadOnlyDictionary<string,string> Attributes => _attributes;

        public IList<BinaryXmlTag> Children => _children;

        public IEnumerable<BinaryXmlTag> this[string t] => _children.Where(x => x.Name == t);
        
        #endregion

        #region Constructor
        public BinaryXmlTag()
        {
            _children = new List<BinaryXmlTag>();
            _attributes = new Dictionary<string, string>();
            _name = "";
            _text = "";
        }

        public BinaryXmlTag(string name): this()
        {
            _name = name;
        }

        public BinaryXmlTag(string name, string text): this()
        {
            _name = name;
            _text = text;
        }

        public BinaryXmlTag(string name, params BinaryXmlTag[] children): this()
        {
            _name = name;
            _children.AddRange(children);
        }
        #endregion

        public dynamic? GetAttribute(string Attribute)
        {
            if (!Attributes.ContainsKey(Attribute))
                return null;
            return new BinaryXmlAttributeValue(Attributes[Attribute]);
        }

        public void SetAttribute(string name,string value)
        {
            if (!Attributes.ContainsKey(name))
                _attributes.Add(name, value);
            else
                _attributes[name] = value;
        }

        public byte[] ToBinary(Encoding encoding)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                //TagName
                bw.WriteString(encoding, Name);
                //Text
                bw.WriteString(encoding, Text);
                //Attributes
                bw.Write(Attributes.Count);
                foreach (KeyValuePair<string, string> key in Attributes)
                    bw.Write(encoding, key.Key, key.Value);
                //SubTags
                bw.Write(Children.Count);
                foreach (BinaryXmlTag sub in Children)
                    bw.Write(sub.ToBinary(encoding));
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            TextFormater tf = new TextFormater()
            {
                LevelDelta = 4
            };
            ToString(ref tf, 0);
            return tf.StartFormat();
        }

        public void ToString(ref TextFormater formater,int nowLevel)
        {
            bool HaveText = Text != null && Text != "";
            bool HaveAttributes = Attributes.Count > 0;
            bool HaveSubTag = Children.Count > 0;
            string Start = "";
            string Att = "";
            string End = "";
            string addition = "";
            bool OneLine = true;
            if((HaveText || HaveSubTag))
            {
                End = $"</{Name}>";
                OneLine = !HaveSubTag;
            }
            else
            {
                End = $"";
                OneLine = true;
                addition = "/";
            }
            if(HaveAttributes)
            {
                List<string> attFormat = new List<string>();
                foreach(KeyValuePair<string,string> KeyPair in Attributes)
                {
                    attFormat.Add($"{KeyPair.Key}=\"{KeyPair.Value}\"");
                }
                Att =$" {String.Join(" ",attFormat)}";
            }
            Start = $"<{Name}{Att}{addition}>";
            if (OneLine)
            {
                formater.AddString(nowLevel, TextAlign.Top,$"{Start}{Text??""}{End}");
            }
            else
            {
                formater.AddString(nowLevel, TextAlign.Top, $"{Start}{Text ?? ""}");
                foreach (BinaryXmlTag sub in Children)
                {
                    sub.ToString(ref formater, nowLevel + 1);
                }
                formater.AddString(nowLevel, TextAlign.Top, End);
            }

        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {

            string attributeName = binder.Name;
            if(!_attributes.ContainsKey(attributeName))
            { 
                result = null;
                return false; 
            }
            else
            {
                string attributeValue = _attributes[attributeName];
                result = new BinaryXmlAttributeValue(attributeValue);
                return true;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            string attributeName = binder.Name;
            string attributeValue = value?.ToString() as string ?? "";
            SetAttribute(attributeName, attributeValue);
            return true;
        }

        public static explicit operator BinaryXmlTag(XmlNode node)
        {
            if (node.NodeType != XmlNodeType.Element)
                throw new InvalidOperationException();
            BinaryXmlTag output = new BinaryXmlTag();
            output.Name = node.Name;
            output.Text = node.InnerText;
            foreach (XmlAttribute xmlAttribute in node.Attributes)
            {
                output._attributes.Add(xmlAttribute.Name, xmlAttribute.Value);
            }
            foreach (XmlNode xmlNode in node.ChildNodes)
            {
                if (xmlNode.NodeType == XmlNodeType.Element)
                {
                    output.Children.Add((BinaryXmlTag)xmlNode);
                }
            }
            return output;
        }

    }
    public sealed class BinaryXmlAttributeValue
    {
        private string _value;

        public string BaseValue => _value;

        internal BinaryXmlAttributeValue(string baseValue)
        {
            _value = baseValue;
        }

        public static implicit operator string(BinaryXmlAttributeValue value)
        {
            return value._value;
        }

        public static implicit operator sbyte(BinaryXmlAttributeValue value)
        {
            return sbyte.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator short(BinaryXmlAttributeValue value)
        {
            return short.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator int(BinaryXmlAttributeValue value)
        {
            return int.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator long(BinaryXmlAttributeValue value)
        {
            return long.Parse(value._value, System.Globalization.NumberStyles.Any);
        }

        public static implicit operator Int128(BinaryXmlAttributeValue value)
        {
            return Int128.Parse(value._value,  System.Globalization.NumberStyles.Number);
        }

        public static implicit operator byte(BinaryXmlAttributeValue value)
        {
            return byte.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator ushort(BinaryXmlAttributeValue value)
        {
            return ushort.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator uint(BinaryXmlAttributeValue value)
        {
            return uint.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator ulong(BinaryXmlAttributeValue value)
        {
            return ulong.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator UInt128(BinaryXmlAttributeValue value)
        {
            return UInt128.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator BigInteger(BinaryXmlAttributeValue value)
        {
            return BigInteger.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator float(BinaryXmlAttributeValue value)
        {
            return float.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator double(BinaryXmlAttributeValue value)
        {
            return double.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator decimal(BinaryXmlAttributeValue value)
        {
            return decimal.Parse(value._value,  System.Globalization.NumberStyles.Any);
        }

        public static implicit operator bool(BinaryXmlAttributeValue value)
        {
            return bool.Parse(value._value.ToLower());
        }

        public static implicit operator DateTime(BinaryXmlAttributeValue value)
        {
            return DateTime.Parse(value._value);
        }
    }
}
