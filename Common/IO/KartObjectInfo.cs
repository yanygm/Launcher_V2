using System;
using System.Reflection;

namespace KartLibrary.IO;

internal record KartObjectInfo(Type BaseType, ConstructorInfo ConstructorInfo)
{
    public KartObject CreateObject() => (KartObject)this.ConstructorInfo.Invoke(new object[0]);

    public bool CanbeConvertTo(Type targetType)
    {
        for (Type type = targetType; type != (Type)null; type = type.BaseType)
        {
            if (type == targetType)
                return true;
        }
        return false;
    }
}