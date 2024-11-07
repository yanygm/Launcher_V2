using System;

namespace KartLibrary.IO;

[AttributeUsage(AttributeTargets.Class)]
public class KartObjectImplementAttribute : Attribute
{
    public CreateObjectFunc? CreateObjectMethod;

    public KartObjectImplementAttribute()
    {
        CreateObjectMethod = null;
    }

    public KartObjectImplementAttribute(CreateObjectFunc? createObjectMethod)
    {
        CreateObjectMethod = createObjectMethod;
    }
}