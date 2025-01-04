using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KartLibrary.Text;
using System.IO;

namespace KartLibrary.Game.Engine.Relements
{
    [KartObjectImplement]
    public class ReCamera : Relement
    {
        public override string ClassName => "ReCamera";
        private byte u1;
        private float u2;
        private float u3;
        private float u4;

        private KartObject? uObj2;
        private KartObject? uObj3;
        private KartObject? uObj4;
        public ReCamera()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            u1 = reader.ReadByte();
            u2 = reader.ReadSingle();
            if (reader.ReadByte() == 1)
            {
                uObj2 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            }
            u3 = reader.ReadSingle();
            if (reader.ReadByte() == 1)
            {
                uObj3 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            }
            u4 = reader.ReadSingle();
            if (reader.ReadByte() == 1)
            {
                uObj4 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            }
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }

        protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
        {
            base.constructOtherInfo(stringBuilder, indentLevel);
            string indendStr = "".PadLeft(indentLevel << 2, ' ');
            stringBuilder.AppendLine($"{indendStr}<ReCameraProperties>");
            stringBuilder.ConstructPropertyString(indentLevel + 1, "u1", u1);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "u2", u2);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "u3", u3);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "u4", u4);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "uObj2", uObj2);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "uObj3", uObj3);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "uObj4", uObj4);
            stringBuilder.AppendLine($"{indendStr}</ReCameraProperties>");
        }
    }
}
