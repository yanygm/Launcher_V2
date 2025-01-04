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
    public class IntTontroller:Tontroller
    {
        public override string ClassName => "IntTontroller";

        public IntTontroller() 
        {
            
        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
            {
                int u1 = reader.ReadInt32();
                int u2 = reader.ReadInt32();
                return (u1, u2);
            });
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }
    }
}
