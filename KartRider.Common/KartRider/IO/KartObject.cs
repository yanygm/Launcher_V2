using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KartLibrary.IO;

public abstract class KartObject
{
    public virtual string ClassName => GetType().Name;

    public uint ClassStamp
    {
        get
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ClassName);
            return Adler.Adler32(0u, bytes, 0, bytes.Length);
        }
    }

    public virtual void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
    }

    public virtual void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
    }
}