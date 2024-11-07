using System.Collections.Generic;
using System.IO;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Properities;

[KartObjectImplement]
public class MtlProperty : KartObject
{
    public int u1;

    public int u2;

    public int u3;

    public int u4;

    public int u5;

    public byte u6;

    public int u7;

    public KartObject u8;

    public KartObject u9;

    public KartObject u10;

    public KartObject u11;

    public override string ClassName => "MtlProperty";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        u1 = reader.ReadInt32();
        u2 = reader.ReadInt32();
        u3 = reader.ReadInt32();
        u4 = reader.ReadInt32();
        u5 = reader.ReadInt32();
        u6 = reader.ReadByte();
        u7 = reader.ReadInt32();
        if (reader.ReadByte() != 0)
        {
            u8 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            u9 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            u10 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() != 0)
        {
            u11 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }
}