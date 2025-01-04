using KartLibrary.Game.Engine.Render;
using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using KartLibrary.Text;
using Veldrid;
using KartLibrary.Game.Engine.Render.Veldrid;
using System.IO;

namespace KartLibrary.Game.Engine.Relements
{
    [KartObjectImplement]
    public class ReTriStrip : Relement
    {
        public override string ClassName => "ReTriStrip";

        private int _unknownInt_1;
        private VertexData _vertexData;

        public VertexData Vertex => _vertexData;

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            _unknownInt_1 = reader.ReadInt32();
            _vertexData = reader.ReadField(decodedObjectMap, decodedFieldMap, VertexData.Deserialize);
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }

        protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
        {
            base.constructOtherInfo(stringBuilder, indentLevel);
            string indendStr = "".PadLeft(indentLevel << 2, ' ');
            stringBuilder.AppendLine($"{indendStr}<ReTriStripProperties>");
            stringBuilder.ConstructPropertyString(indentLevel + 1, "_unknownInt_1", _unknownInt_1);
            stringBuilder.AppendLine($"{indendStr}</ReTriStripProperties>");
            stringBuilder.ConstructPropertyString(indentLevel, "TriStrip", Vertex);
        }
    }
}
