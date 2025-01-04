using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.IO
{
    public static class KartObjectManager
    {
        private static Dictionary<uint, KartObjectInfo> registeredClasses = new Dictionary<uint, KartObjectInfo>();
        /// <summary>
        /// Initialize <see cref="KartObjectManager"/>. Notes that it will register all classes that have <see cref="KartObjectImplementAttribute"/> attribute.
        /// </summary>
        public static void Initialize()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach(Assembly assembly in assemblies)
                foreach (TypeInfo type in 
                    assembly.GetTypes().Select(x => x).Where(x => x.IsSubclassOf(typeof(KartObject)) && x.GetCustomAttribute(typeof(KartObjectImplementAttribute)) is not null))
                    RegisterClass(type);
        }

        public static void RegisterClass<TRegisterClass>() where TRegisterClass : KartObject, new()
        {
            Type type = typeof(TRegisterClass);
            RegisterClass(type);
        }

        public static void RegisterClass(Type type)
        {
            Type? baseType = type.BaseType;
            while(baseType != null && baseType != typeof(KartObject))
                baseType = baseType.BaseType;
            if (baseType is null)
                throw new Exception("");
            ConstructorInfo? constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[0]);
            if (constructorInfo == null)
                throw new Exception("");
            KartObjectInfo kartObjectInfo = new(type, constructorInfo);
            KartObject newObj = kartObjectInfo.CreateObject();
            uint classStamp = newObj.ClassStamp;
            registeredClasses.Add(classStamp, kartObjectInfo);
        }

        public static void RegisterAssemblyClasses(Assembly assembly)
        {
            assembly.GetTypes();
            IEnumerable<TypeInfo> foundTypes = assembly.DefinedTypes.Where(x => x.GetCustomAttributes<KartObjectImplementAttribute>().Count() > 0);
            foreach (TypeInfo typeInfo in foundTypes)
                RegisterClass(typeInfo.UnderlyingSystemType);
        }

        public static bool ContainsClass(uint classStamp)
        {
            return registeredClasses.ContainsKey(classStamp);
        }

        public static T CreateObject<T>(uint ClassStamp) where T : KartObject, new()
        {
            if (!registeredClasses.ContainsKey(ClassStamp))
                throw new Exception($"cannot found type: {ClassStamp:x8}");
            KartObjectInfo kartObjectInfo = registeredClasses[ClassStamp];
            if (!kartObjectInfo.CanbeConvertTo(typeof(T)))
                throw new InvalidCastException($"{kartObjectInfo.BaseType.Name} cannot be convert to {typeof(T).Name}");
            return (T)kartObjectInfo.CreateObject();
        }

        public static KartObject CreateObject(uint ClassStamp)
        {
            if (!registeredClasses.ContainsKey(ClassStamp))
                throw new Exception($"cannot found type: {ClassStamp:x8}");
            KartObjectInfo kartObjectInfo = registeredClasses[ClassStamp];
            return kartObjectInfo.CreateObject();
        }
    }

    internal record class KartObjectInfo(Type BaseType, ConstructorInfo ConstructorInfo)
    {
        public KartObject CreateObject()
        {
            return (KartObject)ConstructorInfo.Invoke(new object[0]);
        }

        public bool CanbeConvertTo(Type targetType)
        {
            Type? superType = targetType;
            while(superType != null)
            {
                if(superType == targetType)
                    return true;
                superType = superType.BaseType;
            }
            return false;
        }
    }
}
