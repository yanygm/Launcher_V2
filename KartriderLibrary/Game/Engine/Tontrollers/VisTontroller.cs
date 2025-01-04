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
    public class VisTontroller : Tontroller
    {
        public override string ClassName => "VisTontroller";

        private object[] tmp;

        public VisTontroller()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            tmp = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
            {
                int u1 = reader.ReadInt32();
                int u2 = reader.ReadInt32();
                //type4
                tmp = new object[u2];
                switch (u1)
                {
                    case 3: tmp = TontrollerKeyFrameProcFuncs.Func43(reader, u2); break;
                }
                return tmp;
            });
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }
    }
}
