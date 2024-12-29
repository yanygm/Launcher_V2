using System;
using System.Globalization;
using System.Numerics;

namespace KartLibrary.Xml;

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
        return sbyte.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator short(BinaryXmlAttributeValue value)
    {
        return short.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator int(BinaryXmlAttributeValue value)
    {
        return int.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator long(BinaryXmlAttributeValue value)
    {
        return long.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator Int128(BinaryXmlAttributeValue value)
    {
        return Int128.Parse(value._value, NumberStyles.Number);
    }

    public static implicit operator byte(BinaryXmlAttributeValue value)
    {
        return byte.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator ushort(BinaryXmlAttributeValue value)
    {
        return ushort.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator uint(BinaryXmlAttributeValue value)
    {
        return uint.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator ulong(BinaryXmlAttributeValue value)
    {
        return ulong.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator UInt128(BinaryXmlAttributeValue value)
    {
        return UInt128.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator BigInteger(BinaryXmlAttributeValue value)
    {
        return BigInteger.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator float(BinaryXmlAttributeValue value)
    {
        return float.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator double(BinaryXmlAttributeValue value)
    {
        return double.Parse(value._value, NumberStyles.Any);
    }

    public static implicit operator decimal(BinaryXmlAttributeValue value)
    {
        return decimal.Parse(value._value, NumberStyles.Any);
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