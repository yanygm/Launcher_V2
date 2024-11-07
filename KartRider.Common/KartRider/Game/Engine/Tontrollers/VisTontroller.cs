using System.Collections.Generic;
using System.IO;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

[KartObjectImplement]
public class VisTontroller : Tontroller
{
    private object[] tmp;

    public override string ClassName => "VisTontroller";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        tmp = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            tmp = new object[num2];
            if (num == 3)
            {
                tmp = TontrollerKeyFrameProcFuncs.Func43(reader, num2);
            }

            return tmp;
        });
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }
}