using KartLibrary.IO;
using KartLibrary.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace KartLibrary.Game.Engine.Properities
{
    [KartObjectImplement]
    public class BackFaceProperty : KartObject
    {
        private CullMode _cullMode;

        public CullMode CullMode => _cullMode;

        public override string ClassName => "BackFaceProperty";

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            _cullMode = (CullMode)reader.ReadInt32();
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<BackFaceProperty>");
            stringBuilder.ConstructPropertyString(1, "CullMode", CullMode);
            stringBuilder.Append("</BackFaceProperty>");
            return stringBuilder.ToString();
        }
    }
}
