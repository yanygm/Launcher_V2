using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.IO;
using KartLibrary.Text;
using Vortice.Direct3D11;

namespace KartLibrary.Game.Engine.Properities;

[KartObjectImplement]
public class BackFaceProperty : KartObject
{
    private CullMode _cullMode;

    public CullMode CullMode => _cullMode;

    public override string ClassName => "BackFaceProperty";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        _cullMode = (CullMode)reader.ReadInt32();
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

    public override string ToString()
    {
        //IL_0019: Unknown result type (might be due to invalid IL or missing references)
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("<BackFaceProperty>");
        stringBuilder.ConstructPropertyString<CullMode>(1, "CullMode", CullMode);
        stringBuilder.Append("</BackFaceProperty>");
        return stringBuilder.ToString();
    }
}