using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    [KartObjectImplement]
    public class PRSTontroller : Tontroller
    {
        public override string ClassName => "PRSTontroller";

        private IVector3KeyframeData _positionKeyframeData;
        private IRotateKeyframeData _rotateKeyframeData;
        private IVector3KeyframeData _scaleKeyframeData;

        private object[] tmp0;
        private object[] tmp1;
        private object[] tmp2;

        public IVector3KeyframeData? positionKeyFrames; // P
        public IRotateKeyframeData?  rotateKeyFrames;   // R
        public IVector3KeyframeData? scaleKeyFrames;    // S


        private int u3;
        private int u4;
        private int u5;
        private int u6;
        private int u7;
        private int u8;

        public PRSTontroller()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
            {
                // type1
                _positionKeyframeData = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
                {
                    Vector3KeyframeDataType dataType = (Vector3KeyframeDataType)reader.ReadInt32();
                    int count = reader.ReadInt32();
                    Vector3KeyframeDataFactory factory = new Vector3KeyframeDataFactory();
                    IVector3KeyframeData vector3KeyframeData = factory.CreateVector3KeyframeData(dataType);
                    vector3KeyframeData.DecodeObject(reader, count);
                    return vector3KeyframeData;
                });
            }
            if (reader.ReadByte() == 1)
            {
                tmp1 = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
                {
                    int uu1 = reader.ReadInt32();
                    int uu2 = reader.ReadInt32();
                    tmp1 = new object[uu2];
                    // type2
                    switch (uu1)
                    {
                        case 0: tmp1 = TontrollerKeyFrameProcFuncs.Func20(reader, uu2); break;
                        case 1: tmp1 = TontrollerKeyFrameProcFuncs.Func21(reader, uu2); break;
                        case 2: tmp1 = TontrollerKeyFrameProcFuncs.Func22(reader, uu2); break;
                        case 3: tmp1 = TontrollerKeyFrameProcFuncs.Func23(reader, uu2); break;
                        case 4: 
                            rotateKeyFrames = new ThreeAxisRotateKeyframeData();
                            rotateKeyFrames.DecodeObject(reader, uu2);
                            break;
                    }
                    return tmp1;
                });
            }
            if (reader.ReadByte() == 1)
            {
                scaleKeyFrames = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
                {
                    Vector3KeyframeDataType dataType = (Vector3KeyframeDataType)reader.ReadInt32();
                    int count = reader.ReadInt32();
                    Vector3KeyframeDataFactory factory = new Vector3KeyframeDataFactory();
                    IVector3KeyframeData vector3KeyframeData = factory.CreateVector3KeyframeData(dataType);
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
                return null;
            if(t > u4 && u2 == 0)
            {
                t = ((t - u4) % (u4 - u3)) + u3;
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
}
