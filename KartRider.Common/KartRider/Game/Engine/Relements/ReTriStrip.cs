using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.IO;
using KartLibrary.Text;

namespace KartLibrary.Game.Engine.Relements;

[KartObjectImplement]
public class ReTriStrip : Relement
{
    private int _unknownInt_1;

    private VertexData _vertexData;

    public override string ClassName => "ReTriStrip";

    public VertexData Vertex => _vertexData;

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        _unknownInt_1 = reader.ReadInt32();
        _vertexData = reader.ReadField(decodedObjectMap, decodedFieldMap, VertexData.Deserialize);
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

    protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
    {
        base.constructOtherInfo(stringBuilder, indentLevel);
        string value = "".PadLeft(indentLevel << 2, ' ');
        StringBuilder stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder3 = stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("<ReTriStripProperties>");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder.ConstructPropertyString(indentLevel + 1, "_unknownInt_1", _unknownInt_1);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder4 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("</ReTriStripProperties>");
        stringBuilder4.AppendLine(ref handler);
        stringBuilder.ConstructPropertyString(indentLevel, "TriStrip", Vertex);
    }
}