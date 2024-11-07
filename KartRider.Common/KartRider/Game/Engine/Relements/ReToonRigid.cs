using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using KartLibrary.IO;
using KartLibrary.Text;

namespace KartLibrary.Game.Engine.Relements;

[KartObjectImplement]
public class ReToonRigid : Relement
{
    private int _unknownInt_1;

    private Vector3[] _vertices;

    private Vector3[] _normalVecs;

    private Vector3[] _texCoords;

    private ReToonRigidMeshFace[] _meshFaces;

    public override string ClassName => "ReToonRigid";

    public int UnknownInt1 => _unknownInt_1;

    public Vector3[] Vertices => _vertices;

    public Vector3[] NormalVectors => _normalVecs;

    public Vector3[] TexCoords => _texCoords;

    public ReToonRigidMeshFace[] MeshFaces => _meshFaces;

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        _unknownInt_1 = reader.ReadInt32();
        (_vertices, _normalVecs, _texCoords, _meshFaces) = reader.ReadField(decodedObjectMap, decodedFieldMap, delegate (BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            int num = reader.ReadInt32();
            Vector3[] array = new Vector3[num];
            for (int i = 0; i < num; i++)
            {
                array[i] = reader.ReadVector3();
            }

            int num2 = reader.ReadInt32();
            Vector3[] array2 = new Vector3[num2];
            for (int j = 0; j < num2; j++)
            {
                array2[j] = reader.ReadVector3();
            }

            int num3 = reader.ReadInt32();
            Vector3[] array3 = new Vector3[num3];
            for (int k = 0; k < num3; k++)
            {
                array3[k] = reader.ReadVector3();
            }

            int num4 = reader.ReadInt32();
            ReToonRigidMeshFace[] array4 = new ReToonRigidMeshFace[num4];
            for (int l = 0; l < num4; l++)
            {
                array4[l] = new ReToonRigidMeshFace
                {
                    TexCoordIndex1 = reader.ReadInt16(),
                    TexCoordIndex2 = reader.ReadInt16(),
                    TexCoordIndex3 = reader.ReadInt16(),
                    NormalVectorIndex1 = reader.ReadInt16(),
                    NormalVectorIndex2 = reader.ReadInt16(),
                    NormalVectorIndex3 = reader.ReadInt16(),
                    VertexIndex1 = reader.ReadInt16(),
                    VertexIndex2 = reader.ReadInt16(),
                    VertexIndex3 = reader.ReadInt16(),
                    Unknown = reader.ReadInt16()
                };
            }

            return (array, array2, array3, array4);
        });
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

    protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
    {
        base.constructOtherInfo(stringBuilder, indentLevel);
        string value = "".PadLeft(indentLevel << 2, ' ');
        StringBuilder stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder3 = stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("<ReToonRigidProperties>");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder.ConstructPropertyString(indentLevel + 1, "Vertices", Vertices);
        stringBuilder.ConstructPropertyString(indentLevel + 1, "NormalVectors", NormalVectors);
        stringBuilder.ConstructPropertyString(indentLevel + 1, "TexCoords", TexCoords);
        stringBuilder.ConstructPropertyString(indentLevel + 1, "MeshFaces", MeshFaces);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder4 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("</ReToonRigidProperties>");
        stringBuilder4.AppendLine(ref handler);
    }
}