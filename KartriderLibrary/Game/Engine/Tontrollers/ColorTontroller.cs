using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    public class ColorTontroller:Tontroller
    {
        public override string ClassName => "ColorTontroller";

        private IIntKeyframeData _colorKeyframeData;

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            _colorKeyframeData = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decodedObjectMap, decodedFieldMap) =>
            {
                IntKeyframeDataType dataType = (IntKeyframeDataType)reader.ReadInt32();
                int count = reader.ReadInt32();
                IntKeyframeDataFactory factory = new IntKeyframeDataFactory();
                IIntKeyframeData intKeyframeData = factory.CreateIntKeyframeData(dataType);
                intKeyframeData.DecodeObject(reader, count);
                return intKeyframeData;
            });
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }

    }
}
