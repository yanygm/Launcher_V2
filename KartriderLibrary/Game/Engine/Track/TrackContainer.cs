using KartLibrary.Game.Engine.Relements;
using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Track
{
    [KartObjectImplement]
    public class TrackContainer : KartObject
    {
        public override  string ClassName => "TrackContainer";

        public string u1;

        public Relement TrackScene;

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            u1 = reader.ReadKRString();
            TrackScene = reader.ReadKartObject<Relement>(decodedObjectMap, decodedFieldMap);
            int eventCount = reader.ReadInt32();
            //List<KartObject?> objs = new List<KartObject?>();
            //for(int i = 0; i < eventCount; i++)
            //    objs.Add(reader.ReadKartObject(decodedObjectMap, decodedFieldMap));
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }
    }
}
