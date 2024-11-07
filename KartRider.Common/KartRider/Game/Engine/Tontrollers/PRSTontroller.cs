using System.Collections.Generic;
using System.IO;
using System.Numerics;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

[KartObjectImplement]
public class PRSTontroller : Tontroller
{
    private IVector3KeyframeData _positionKeyframeData;

    private IRotateKeyframeData _rotateKeyframeData;

    private IVector3KeyframeData _scaleKeyframeData;

    private object[] tmp0;

    private object[] tmp1;

    private object[] tmp2;

    public IVector3KeyframeData? positionKeyFrames;

    public IRotateKeyframeData? rotateKeyFrames;

    public IVector3KeyframeData? scaleKeyFrames;

    private int u3;

    private int u4;

    private int u5;

    private int u6;

    private int u7;

    private int u8;

    public override string ClassName => "PRSTontroller";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        if (reader.ReadByte() == 1)
        {
            _positionKeyframeData = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap)
            {
                Vector3KeyframeDataType dataType2 = (Vector3KeyframeDataType)reader.ReadInt32();
                int count2 = reader.ReadInt32();
                IVector3KeyframeData vector3KeyframeData2 = new Vector3KeyframeDataFactory().CreateVector3KeyframeData(dataType2);
                vector3KeyframeData2.DecodeObject(reader, count2);
                return vector3KeyframeData2;
            });
        }

        if (reader.ReadByte() == 1)
        {
            tmp1 = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap)
            {
                int num = reader.ReadInt32();
                int num2 = reader.ReadInt32();
                tmp1 = new object[num2];
                switch (num)
                {
                    case 0:
                        tmp1 = TontrollerKeyFrameProcFuncs.Func20(reader, num2);
                        break;
                    case 1:
                        tmp1 = TontrollerKeyFrameProcFuncs.Func21(reader, num2);
                        break;
                    case 2:
                        tmp1 = TontrollerKeyFrameProcFuncs.Func22(reader, num2);
                        break;
                    case 3:
                        tmp1 = TontrollerKeyFrameProcFuncs.Func23(reader, num2);
                        break;
                    case 4:
                        rotateKeyFrames = new ThreeAxisRotateKeyframeData();
                        rotateKeyFrames.DecodeObject(reader, num2);
                        break;
                }

                return tmp1;
            });
        }

        if (reader.ReadByte() == 1)
        {
            scaleKeyFrames = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap)
            {
                Vector3KeyframeDataType dataType = (Vector3KeyframeDataType)reader.ReadInt32();
                int count = reader.ReadInt32();
                IVector3KeyframeData vector3KeyframeData = new Vector3KeyframeDataFactory().CreateVector3KeyframeData(dataType);
                vector3KeyframeData.DecodeObject(reader, count);
                return vector3KeyframeData;
            });
        }

        u3 = reader.ReadInt32();
        u4 = reader.ReadInt32();
        u5 = reader.ReadInt32();
        u6 = reader.ReadInt32();
        u7 = reader.ReadInt32();
        u8 = reader.ReadInt32();
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

    public Vector3? GetPosition(float t)
    {
        if (positionKeyFrames == null)
        {
            return null;
        }

        if (t > (float)u4 && u2 == 0)
        {
            t = (t - (float)u4) % (float)(u4 - u3) + (float)u3;
        }

        return positionKeyFrames.GetValue(t);
    }

    public Quaternion? GetRotation(float t)
    {
        return null;
    }

    public Vector3? GetScale(float t)
    {
        return null;
    }
}