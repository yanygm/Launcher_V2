using System.Collections.Generic;
using System.IO;
using KartLibrary.Game.Engine.Relements;
using KartLibrary.IO;

namespace KartLibrary.Game.Engine.Track;

[KartObjectImplement]
public class TrackContainer : KartObject
{
    public string u1;

    public Relement TrackScene;

    public override string ClassName => "TrackContainer";

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        u1 = reader.ReadKRString();
        TrackScene = reader.ReadKartObject<Relement>(decodedObjectMap, decodedFieldMap);
        reader.ReadInt32();
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }
}