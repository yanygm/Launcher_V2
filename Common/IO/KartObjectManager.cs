using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KartLibrary.IO;

public static class KartObjectManager
{
    private static Dictionary<uint, KartObjectInfo> registeredClasses = new Dictionary<uint, KartObjectInfo>();

    public static void Initialize()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            foreach (TypeInfo item in from x in assemblies[i].GetTypes()
                                      select (x) into x
                                      where x.IsSubclassOf(typeof(KartObject)) && x.GetCustomAttribute(typeof(KartObjectImplementAttribute)) != null
                                      select x)
            {
                RegisterClass(item);
            }
        }
    }

    public static void RegisterClass<TRegisterClass>() where TRegisterClass : KartObject, new()
    {
        RegisterClass(typeof(TRegisterClass));
    }

    public static void RegisterClass(Type type)
    {
        Type baseType = type.BaseType;
        while (baseType != null && baseType != typeof(KartObject))
        {
            baseType = baseType.BaseType;
        }

        if ((object)baseType == null)
        {
            throw new Exception("");
        }

        ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[0]);
        if (constructor == null)
        {
            throw new Exception("");
        }

        KartObjectInfo kartObjectInfo = new KartObjectInfo(type, constructor);
        uint classStamp = kartObjectInfo.CreateObject().ClassStamp;
        registeredClasses.Add(classStamp, kartObjectInfo);
    }

    public static void RegisterAssemblyClasses(Assembly assembly)
    {
        assembly.GetTypes();
        foreach (TypeInfo item in assembly.DefinedTypes.Where((TypeInfo x) => x.GetCustomAttributes<KartObjectImplementAttribute>().Count() > 0))
        {
            RegisterClass(item.UnderlyingSystemType);
        }
    }

    public static bool ContainsClass(uint classStamp)
    {
        return registeredClasses.ContainsKey(classStamp);
    }

    public static T CreateObject<T>(uint ClassStamp) where T : KartObject, new()
    {
        if (!registeredClasses.ContainsKey(ClassStamp))
        {
            throw new Exception($"cannot found type: {ClassStamp:x8}");
        }

        KartObjectInfo kartObjectInfo = registeredClasses[ClassStamp];
        if (!kartObjectInfo.CanbeConvertTo(typeof(T)))
        {
            throw new InvalidCastException(kartObjectInfo.BaseType.Name + " cannot be convert to " + typeof(T).Name);
        }

        return (T)kartObjectInfo.CreateObject();
    }

    public static KartObject CreateObject(uint ClassStamp)
    {
        if (!registeredClasses.ContainsKey(ClassStamp))
        {
            throw new Exception($"cannot found type: {ClassStamp:x8}");
        }

        return registeredClasses[ClassStamp].CreateObject();
    }
}