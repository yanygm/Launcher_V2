using System.Collections.Generic;
using System.IO;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class ColorTontroller : Tontroller
{
    private IIntKeyframeData _colorKeyframeData;

    public override string ClassName => "ColorTontroller";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        _colorKeyframeData = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            IntKeyframeDataType dataType = (IntKeyframeDataType)reader.ReadInt32();
            int count = reader.ReadInt32();
            IIntKeyframeData intKeyframeData = new IntKeyframeDataFactory().CreateIntKeyframeData(dataType);
            intKeyframeData.DecodeObject(reader, count);
            return intKeyframeData;
        });
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }
}