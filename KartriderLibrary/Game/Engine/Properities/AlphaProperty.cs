using KartLibrary.Game.Engine.Enums;
using KartLibrary.IO;
using KartLibrary.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.DCommon;
using Vortice.Direct3D11;

namespace KartLibrary.Game.Engine.Properities
{
    [KartObjectImplement]
    public class AlphaProperty : KartObject
    {
        public override string ClassName => "AlphaProperty";
        public bool UseBlendTest { get; set; }
        public BlendFactor SourceColorFactor { get; set; } //Source Color Factor (D3DBLEND)
        public BlendFactor DestinationColorFactor { get; set; } // Desc Color Factor (D3DBLEND)
        public bool UseAlphaTest { get; set; }
        public ComparisonFunction AlphaFunction { get; set; }
        public byte AlphaTestRef { get; set; }

        public byte u1; // use blend
        public byte u4; // use alpha test
        public int u5; // alphatest cmp mode
        public byte u6; // alphaRef



        public AlphaProperty()
        {

        }

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
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<AlphaProperty>");
            stringBuilder.ConstructPropertyString(1, "UseBlendTest", UseBlendTest);
            stringBuilder.ConstructPropertyString(1, "SourceColorFactor", SourceColorFactor);
            stringBuilder.ConstructPropertyString(1, "DestinationColorFactor", DestinationColorFactor);
            stringBuilder.ConstructPropertyString(1, "UseAlphaTest", UseAlphaTest);
            stringBuilder.ConstructPropertyString(1, "AlphaFunction", AlphaFunction);
            stringBuilder.ConstructPropertyString(1, "AlphaTestRef", AlphaTestRef);
            stringBuilder.AppendLine("</AlphaProperty>");
            return stringBuilder.ToString();
        }
    }
}
