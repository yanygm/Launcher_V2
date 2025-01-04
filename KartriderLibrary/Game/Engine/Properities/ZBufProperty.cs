using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Properities
{
    [KartObjectImplement]
    public class ZBufProperty : KartObject
    {
        public override string ClassName => "ZBufProperty";
        public int u1;
        public byte u2;

        public ZBufProperty()
        {

        }

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
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"<u1>{u1}({u1:x8})</u1>");
            stringBuilder.AppendLine($"<u2>{u2}({u2:x2})</u2>");
            return stringBuilder.ToString();
        }
    }
}
