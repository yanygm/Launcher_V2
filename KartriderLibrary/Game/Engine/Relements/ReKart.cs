using KartLibrary.IO;
using KartLibrary.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Relements
{
    [KartObjectImplement()]
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
            string indendStr = "".PadLeft(indentLevel << 2, ' ');
            stringBuilder.AppendLine($"{indendStr}    <ReKartProperties>");
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownInt_1", _unknownInt_1);

            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownVec3_2", _unknownVec3_2);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownVec3_3", _unknownVec3_3);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownVec4_4", _unknownVec4_4);
            stringBuilder.AppendLine($"{indendStr}    </ReKartProperties>");
        }
    }
}
