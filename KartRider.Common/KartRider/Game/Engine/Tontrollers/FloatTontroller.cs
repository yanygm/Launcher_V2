using System.Collections.Generic;
using System.IO;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

[KartObjectImplement]
public class FloatTontroller : Tontroller
{
    private (int u1, int u2, object[] tmp, IFloatKeyframeData? KeyframeData)? tmp;

    public override string ClassName => "FloatTontroller";

    public IFloatKeyframeData? KeyframeData { get; set; }

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        (int, int, object[], IFloatKeyframeData) value = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            object[] item = new object[0];
            IFloatKeyframeData floatKeyframeData = null;
            switch (num)
            {
                case 0:
                    floatKeyframeData = new CubicFloatKeyframeData();
                    ((CubicFloatKeyframeData)floatKeyframeData).DecodeObject(reader, num2);
                    break;
                case 1:
                    floatKeyframeData = new LinearFloatKeyframeData();
                    ((LinearFloatKeyframeData)floatKeyframeData).DecodeObject(reader, num2);
                    break;
                case 2:
                    item = TontrollerKeyFrameProcFuncs.Func02(reader, num2);
                    break;
                case 3:
                    item = TontrollerKeyFrameProcFuncs.Func03(reader, num2);
                    break;
            }

            return (num, num2, item, floatKeyframeData);
        });
        tmp = value;
        if (tmp?.KeyframeData != null)
        {
            KeyframeData = tmp?.KeyframeData;
        }
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

    public float GetValue(float time)
    {
        if (time > (float)endTime && u2 == 0)
        {
            time = (time - (float)endTime) % (float)(endTime - startTime) + (float)startTime;
        }

        return KeyframeData?.GetValue(time) ?? 0f;
    }
}