using System;
using System.Collections.Generic;
using System.IO;

namespace KartLibrary.IO;

public static class KartObjectExt
{
    public static KartObject? ReadKartObject(this BinaryReader br, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        KartObject kartObject;
        if (decodedObjectMap != null)
        {
            if (br.ReadUInt16() == 18363)
            {
                short key = br.ReadInt16();
                if (!decodedObjectMap.ContainsKey(key))
                {
                    throw new IndexOutOfRangeException();
                }

                kartObject = decodedObjectMap[key];
            }
            else
            {
                uint classStamp = br.ReadUInt32();
                short key2 = br.ReadInt16();
                kartObject = KartObjectManager.CreateObject(classStamp);
                kartObject?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
                decodedObjectMap.Add(key2, kartObject);
            }
        }
        else
        {
            kartObject = KartObjectManager.CreateObject(br.ReadUInt32());
            kartObject?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
        }

        return kartObject;
    }

    public static TBase ReadKartObject<TBase>(this BinaryReader br, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap) where TBase : KartObject, new()
    {
        TBase val2;
        if (decodedObjectMap != null)
        {
            if (br.ReadUInt16() == 18363)
            {
                short key = br.ReadInt16();
                if (!decodedObjectMap.ContainsKey(key))
                {
                    throw new IndexOutOfRangeException();
                }

                if (!(decodedObjectMap[key] is TBase val))
                {
                    throw new InvalidCastException();
                }

                val2 = val;
            }
            else
            {
                uint classStamp = br.ReadUInt32();
                short key2 = br.ReadInt16();
                val2 = KartObjectManager.CreateObject<TBase>(classStamp);
                val2?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
                decodedObjectMap.Add(key2, val2);
            }
        }
        else
        {
            val2 = KartObjectManager.CreateObject<TBase>(br.ReadUInt32());
            val2?.DecodeObject(br, decodedObjectMap, decodedFieldMap);
        }

        return val2;
    }

    public static T ReadField<T>(this BinaryReader br, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap, DecodeFieldFunc<T> decodeFieldFunc)
    {
        if (decodedFieldMap != null)
        {
            switch (br.ReadUInt16())
            {
                case 10154:
                    {
                        short key2 = br.ReadInt16();
                        T val = decodeFieldFunc(br, decodedObjectMap, decodedFieldMap);
                        decodedFieldMap.Add(key2, val);
                        return val;
                    }
                case 10171:
                    {
                        short key = br.ReadInt16();
                        if (decodedFieldMap.ContainsKey(key))
                        {
                            object obj = decodedFieldMap[key];
                            if (obj is T)
                            {
                                return (T)obj;
                            }

                            throw new InvalidCastException();
                        }

                        throw new IndexOutOfRangeException();
                    }
                default:
                    throw new Exception();
            }
        }

        return decodeFieldFunc(br, decodedObjectMap, decodedFieldMap);
    }
}