using System.Collections.Generic;
using System.IO;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

[KartObjectImplement]
public class IntTontroller : Tontroller
{
    public override string ClassName => "IntTontroller";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap)
        {
            int item = reader.ReadInt32();
            int item2 = reader.ReadInt32();
            return (item, item2);
        });
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }
}