using KartLibrary.IO;
using KartLibrary.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Relements
{
    public class VertexData
    {
        public Vector3[]? Vertices;
        public Vector3[]? Unknown1;
        public float[]? Unknown2;
        public short TexCoordPerVertex;
        public Vector2[,]? TextureUVs;
        public short[]? Indexes;

        public static VertexData Deserialize(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            VertexData output = new VertexData();
            output.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            return output;
        }

        public void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            int u2 = reader.ReadInt16();
            Vertices = new Vector3[u2];
            if (reader.ReadByte() != 0)
            {
                for (int i = 0; i < u2; i++)
                {
                    Vertices[i] = reader.ReadVector3();
                }
            }
            Unknown1 = new Vector3[u2];
            if (reader.ReadByte() != 0)
            {
                for (int i = 0; i < u2; i++)
                {
                    Unknown1[i] = reader.ReadVector3();
                }
            }
            if (reader.ReadByte() != 0)
            {
                Unknown2 = new float[u2];
                for (int i = 0; i < u2; i++)
                {
                    Unknown2[i] = reader.ReadSingle();
                }
            }
            TexCoordPerVertex = reader.ReadInt16();
            TextureUVs = new Vector2[u2, TexCoordPerVertex];
            for (int i = 0; i < u2; i++)
            {
                for (int j = 0; j < TexCoordPerVertex; j++)
                {
                    TextureUVs[i, j] = reader.ReadVector2();
                }
            }
            byte u9 = reader.ReadByte();
            short indexCount = reader.ReadInt16();
            Indexes = new short[indexCount];
            for (int i = 0; i < indexCount; i++)
                Indexes[i] = reader.ReadInt16();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<VertexData>");
            stringBuilder.ConstructPropertyString(1, "Vertices", Vertices);
            stringBuilder.ConstructPropertyString(1, "Unknown1", Unknown1);
            stringBuilder.ConstructPropertyString(1, "Unknown2", Unknown2);
            stringBuilder.ConstructPropertyString(1, "TexCoordPerVertex", TexCoordPerVertex);
            stringBuilder.ConstructPropertyString(1, "TextureUVs", TextureUVs);
            stringBuilder.ConstructPropertyString(1, "Indexes", Indexes);
            stringBuilder.AppendLine("</VertexData>");
            return stringBuilder.ToString();
        }
    }
}
