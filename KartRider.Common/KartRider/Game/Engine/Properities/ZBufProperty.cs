using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Properities;

[KartObjectImplement]
public class ZBufProperty : KartObject
{
    public int u1;

    public byte u2;

    public override string ClassName => "ZBufProperty";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        u1 = reader.ReadInt32();
        u2 = reader.ReadByte();
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
    }

    public override string ToString()
    {
        StringBuilder stringBuilder;
        StringBuilder stringBuilder2 = (stringBuilder = new StringBuilder());
        StringBuilder stringBuilder3 = stringBuilder;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u1>");
        handler.AppendFormatted(u1);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u1, "x8");
        handler.AppendLiteral(")</u1>");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder = stringBuilder2;
        StringBuilder stringBuilder4 = stringBuilder;
        handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder);
        handler.AppendLiteral("<u2>");
        handler.AppendFormatted(u2);
        handler.AppendLiteral("(");
        handler.AppendFormatted(u2, "x2");
        handler.AppendLiteral(")</u2>");
        stringBuilder4.AppendLine(ref handler);
        return stringBuilder2.ToString();
    }
}