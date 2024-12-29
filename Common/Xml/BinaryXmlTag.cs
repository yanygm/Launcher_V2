using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using KartLibrary.IO;
using KartLibrary.Text;

namespace KartLibrary.Xml;

public class BinaryXmlTag : DynamicObject
{
    private string _name;

    private string _text;

    private Dictionary<string, string> _attributes;

    private List<BinaryXmlTag> _children;

    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }

    public string Text
    {
        get
        {
            return _text;
        }
        set
        {
            _text = value;
        }
    }

    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    public IList<BinaryXmlTag> Children => _children;

    public IEnumerable<BinaryXmlTag> this[string t]
    {
        get
        {
            string t2 = t;
            return _children.Where((BinaryXmlTag x) => x.Name == t2);
        }
    }

    public BinaryXmlTag()
    {
        _children = new List<BinaryXmlTag>();
        _attributes = new Dictionary<string, string>();
        _name = "";
        _text = "";
    }

    public BinaryXmlTag(string name)
        : this()
    {
        _name = name;
    }

    public BinaryXmlTag(string name, string text)
        : this()
    {
        _name = name;
        _text = text;
    }

    public BinaryXmlTag(string name, params BinaryXmlTag[] children)
        : this()
    {
        _name = name;
        _children.AddRange(children);
    }

    public dynamic? GetAttribute(string Attribute)
    {
        if (!Attributes.ContainsKey(Attribute))
        {
            return null;
        }

        return new BinaryXmlAttributeValue(Attributes[Attribute]);
    }

    public void SetAttribute(string name, string value)
    {
        if (!Attributes.ContainsKey(name))
        {
            _attributes.Add(name, value);
        }
        else
        {
            _attributes[name] = value;
        }
    }

    public byte[] ToBinary(Encoding encoding)
    {
        using MemoryStream memoryStream = new MemoryStream();
        BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
        binaryWriter.WriteString(encoding, Name);
        binaryWriter.WriteString(encoding, Text);
        binaryWriter.Write(Attributes.Count);
        foreach (KeyValuePair<string, string> attribute in Attributes)
        {
            binaryWriter.Write(encoding, attribute.Key, attribute.Value);
        }

        binaryWriter.Write(Children.Count);
        foreach (BinaryXmlTag child in Children)
        {
            binaryWriter.Write(child.ToBinary(encoding));
        }

        return memoryStream.ToArray();
    }

    public override string ToString()
    {
        TextFormater formater = new TextFormater
        {
            LevelDelta = 4
        };
        ToString(ref formater, 0);
        return formater.StartFormat();
    }

    public void ToString(ref TextFormater formater, int nowLevel)
    {
        bool num = Text != null && Text != "";
        bool flag = Attributes.Count > 0;
        bool flag2 = Children.Count > 0;
        string text = "";
        string value = "";
        string text2 = "";
        string value2 = "";
        bool flag3 = true;
        if (num || flag2)
        {
            text2 = "</" + Name + ">";
            flag3 = !flag2;
        }
        else
        {
            text2 = "";
            flag3 = true;
            value2 = "/";
        }

        if (flag)
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, string> attribute in Attributes)
            {
                list.Add(attribute.Key + "=\"" + attribute.Value + "\"");
            }

            value = " " + string.Join(" ", list);
        }

        text = $"<{Name}{value}{value2}>";
        if (flag3)
        {
            formater.AddString(nowLevel, TextAlign.Top, text + Text + text2);
            return;
        }

        formater.AddString(nowLevel, TextAlign.Top, text + Text);
        foreach (BinaryXmlTag child in Children)
        {
            child.ToString(ref formater, nowLevel + 1);
        }

        formater.AddString(nowLevel, TextAlign.Top, text2);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        string name = binder.Name;
        if (!_attributes.ContainsKey(name))
        {
            result = null;
            return false;
        }

        string baseValue = _attributes[name];
        result = new BinaryXmlAttributeValue(baseValue);
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        string name = binder.Name;
        string value2 = value?.ToString() ?? "";
        SetAttribute(name, value2);
        return true;
    }

    public static explicit operator BinaryXmlTag(XmlNode node)
    {
        if (node.NodeType != XmlNodeType.Element)
        {
            throw new InvalidOperationException();
        }

        BinaryXmlTag binaryXmlTag = new BinaryXmlTag();
        binaryXmlTag.Name = node.Name;
        binaryXmlTag.Text = node.InnerText;
        foreach (XmlAttribute attribute in node.Attributes)
        {
            binaryXmlTag._attributes.Add(attribute.Name, attribute.Value);
        }

        foreach (XmlNode childNode in node.ChildNodes)
        {
            if (childNode.NodeType == XmlNodeType.Element)
            {
                binaryXmlTag.Children.Add((BinaryXmlTag)childNode);
            }
        }

        return binaryXmlTag;
    }
}