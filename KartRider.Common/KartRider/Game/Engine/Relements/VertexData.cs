using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using KartLibrary.IO;
using KartLibrary.Text;

namespace KartLibrary.Game.Engine.Relements;

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
        VertexData vertexData = new VertexData();
        vertexData.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        return vertexData;
    }

    public void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        int num = reader.ReadInt16();
        Vertices = new Vector3[num];
        if (reader.ReadByte() != 0)
        {
            for (int i = 0; i < num; i++)
            {
                Vertices[i] = reader.ReadVector3();
            }
        }

        Unknown1 = new Vector3[num];
        if (reader.ReadByte() != 0)
        {
            for (int j = 0; j < num; j++)
            {
                Unknown1[j] = reader.ReadVector3();
            }
        }

        if (reader.ReadByte() != 0)
        {
            Unknown2 = new float[num];
            for (int k = 0; k < num; k++)
            {
                Unknown2[k] = reader.ReadSingle();
            }
        }

        TexCoordPerVertex = reader.ReadInt16();
        TextureUVs = new Vector2[num, TexCoordPerVertex];
        for (int l = 0; l < num; l++)
        {
            for (int m = 0; m < TexCoordPerVertex; m++)
            {
                TextureUVs[l, m] = reader.ReadVector2();
            }
        }

        reader.ReadByte();
        short num2 = reader.ReadInt16();
        Indexes = new short[num2];
        for (int n = 0; n < num2; n++)
        {
            Indexes[n] = reader.ReadInt16();
        }
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