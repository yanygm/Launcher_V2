using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using KartLibrary.IO;
using KartLibrary.Text;

namespace KartLibrary.Game.Engine.Relements;

[KartObjectImplement]
public class ReKart : Relement
{
    private int _unknownInt_1;

    private Vector3 _unknownVec3_2;

    private Vector3 _unknownVec3_3;

    private Vector4 _unknownVec4_4;

    public override string ClassName => "ReKart";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        _unknownInt_1 = reader.ReadInt32();
        _unknownVec3_2 = reader.ReadVector3();
        _unknownVec3_3 = reader.ReadVector3();
        _unknownVec4_4 = reader.ReadVector4();
    }

    protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
    {
        base.constructOtherInfo(stringBuilder, indentLevel);
        string value = "".PadLeft(indentLevel << 2, ' ');
        StringBuilder stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder3 = stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("    <ReKartProperties>");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownInt_1", _unknownInt_1);
        stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownVec3_2", _unknownVec3_2);
        stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownVec3_3", _unknownVec3_3);
        stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownVec4_4", _unknownVec4_4);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder4 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("    </ReKartProperties>");
        stringBuilder4.AppendLine(ref handler);
    }
}