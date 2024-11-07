using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.Game.Engine.Enums;
using KartLibrary.IO;
using KartLibrary.Text;
using Vortice.Direct3D11;

namespace KartLibrary.Game.Engine.Properities;

[KartObjectImplement]
public class AlphaProperty : KartObject
{
    public byte u1;

    public byte u4;

    public int u5;

    public byte u6;

    public override string ClassName => "AlphaProperty";

    public bool UseBlendTest { get; set; }

    public BlendFactor SourceColorFactor { get; set; }

    public BlendFactor DestinationColorFactor { get; set; }

    public bool UseAlphaTest { get; set; }

    public ComparisonFunction AlphaFunction { get; set; }

    public byte AlphaTestRef { get; set; }

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        u1 = reader.ReadByte();
        UseBlendTest = u1 == 1;
        SourceColorFactor = (BlendFactor)reader.ReadInt32();
        DestinationColorFactor = (BlendFactor)reader.ReadInt32();
        u4 = reader.ReadByte();
        UseAlphaTest = u4 == 1;
        u5 = reader.ReadInt32();
        AlphaFunction = (ComparisonFunction)u5;
        u6 = reader.ReadByte();
        AlphaTestRef = u6;
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
    }

    public override string ToString()
    {
        //IL_0061: Unknown result type (might be due to invalid IL or missing references)
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("<AlphaProperty>");
        stringBuilder.ConstructPropertyString(1, "UseBlendTest", UseBlendTest);
        stringBuilder.ConstructPropertyString(1, "SourceColorFactor", SourceColorFactor);
        stringBuilder.ConstructPropertyString(1, "DestinationColorFactor", DestinationColorFactor);
        stringBuilder.ConstructPropertyString(1, "UseAlphaTest", UseAlphaTest);
        stringBuilder.ConstructPropertyString<ComparisonFunction>(1, "AlphaFunction", AlphaFunction);
        stringBuilder.ConstructPropertyString(1, "AlphaTestRef", AlphaTestRef);
        stringBuilder.AppendLine("</AlphaProperty>");
        return stringBuilder.ToString();
    }
}