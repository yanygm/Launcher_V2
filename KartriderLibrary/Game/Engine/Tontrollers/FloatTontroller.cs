using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    [KartObjectImplement]
    public class FloatTontroller : Tontroller
    {
        public override string ClassName => "FloatTontroller";

        private (int u1, int u2, object[] tmp, IFloatKeyframeData? KeyframeData)? tmp;

        public IFloatKeyframeData? KeyframeData { get; set; }

        public FloatTontroller()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            var field = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
            {
                int u1 = reader.ReadInt32();
                int u2 = reader.ReadInt32();
                object[] tmp = new object[0];
                IFloatKeyframeData? KeyframeData = null;
                //type0
                switch (u1)
                {
                    case 0:
                        KeyframeData = new CubicFloatKeyframeData();
                        ((CubicFloatKeyframeData)KeyframeData).DecodeObject(reader, u2);
                        break;
                    case 1:
                        KeyframeData = new LinearFloatKeyframeData();
                        ((LinearFloatKeyframeData)KeyframeData).DecodeObject(reader, u2);
                        break;
                    case 2: 
                        tmp = TontrollerKeyFrameProcFuncs.Func02(reader, u2); break;
                    case 3: 
                        tmp = TontrollerKeyFrameProcFuncs.Func03(reader, u2); break;
                }
                return (u1, u2, tmp, KeyframeData);
            });
            tmp = field;
            if(tmp?.KeyframeData is not null)
                KeyframeData = tmp?.KeyframeData;
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }

        public float GetValue(float time)
        {
            if (time > base.endTime && u2 == 0)
            {
                time = ((time - endTime) % (endTime - startTime)) + startTime;
            }
            return KeyframeData?.GetValue(time) ?? 0;
        }
    }
}
