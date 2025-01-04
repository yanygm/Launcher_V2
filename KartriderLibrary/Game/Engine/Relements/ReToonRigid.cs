using KartLibrary.Game.Engine.Render;
using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using KartLibrary.Text;
using System.IO;

namespace KartLibrary.Game.Engine.Relements
{
    [KartObjectImplement]
    public class ReToonRigid : Relement
    {
        public override string ClassName => "ReToonRigid";

        private int _unknownInt_1;
        private Vector3[] _vertices;
        private Vector3[] _normalVecs;
        private Vector3[] _texCoords;
        private ReToonRigidMeshFace[] _meshFaces;

        public int UnknownInt1 => _unknownInt_1;

        public Vector3[] Vertices => _vertices;
        public Vector3[] NormalVectors => _normalVecs;
        public Vector3[] TexCoords => _texCoords;
        public ReToonRigidMeshFace[] MeshFaces => _meshFaces;

        public ReToonRigid()
        {

        }

        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            _unknownInt_1 = reader.ReadInt32();
            (Vector3[] vertices, Vector3[] normalVecs, Vector3[] texCoords, ReToonRigidMeshFace[] meshFaces) meshData = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decodedObjectMap, decodedFieldMap) =>
            {
                int vertexCount = reader.ReadInt32();
                Vector3[] vertices = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = reader.ReadVector3();
                }

                int normalVecCount = reader.ReadInt32();
                Vector3[] normalVecs = new Vector3[normalVecCount];
                for (int i = 0; i < normalVecCount; i++)
                {
                    normalVecs[i] = reader.ReadVector3();
                }

                int texCoordCount = reader.ReadInt32();
                Vector3[] texCoords = new Vector3[texCoordCount];
                for (int i = 0; i < texCoordCount; i++)
                {
                    texCoords[i] = reader.ReadVector3();
                }

                int meshFaceCount = reader.ReadInt32();
                ReToonRigidMeshFace[] meshFaces = new ReToonRigidMeshFace[meshFaceCount];
                for (int i = 0; i < meshFaceCount; i++)
                {
                    meshFaces[i] = new ReToonRigidMeshFace()
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
                        Unknown = reader.ReadInt16(),
                    };
                }
                return (vertices, normalVecs, texCoords, meshFaces);
            });
            _vertices = meshData.vertices;
            _normalVecs = meshData.normalVecs;
            _texCoords = meshData.texCoords;
            _meshFaces = meshData.meshFaces;
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }

        protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
        {
            base.constructOtherInfo(stringBuilder, indentLevel);
            string indentStr = "".PadLeft(indentLevel << 2, ' ');
            stringBuilder.AppendLine($"{indentStr}<ReToonRigidProperties>");
            stringBuilder.ConstructPropertyString(indentLevel + 1, "Vertices", Vertices);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "NormalVectors", NormalVectors);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "TexCoords", TexCoords);
            stringBuilder.ConstructPropertyString(indentLevel + 1, "MeshFaces", MeshFaces);
            stringBuilder.AppendLine($"{indentStr}</ReToonRigidProperties>");
        }
    }

    public struct ReToonRigidMeshFace
    {
        public int TexCoordIndex1;
        public int TexCoordIndex2;
        public int TexCoordIndex3;

        public int NormalVectorIndex1;
        public int NormalVectorIndex2;
        public int NormalVectorIndex3;

        public int VertexIndex1;
        public int VertexIndex2;
        public int VertexIndex3;

        public int Unknown;

        public override string ToString()
        {
            return $"<Face>" +
                 $" v:{TexCoordIndex1},{TexCoordIndex2},{TexCoordIndex3}" +
                 $" n:{NormalVectorIndex1},{NormalVectorIndex2},{NormalVectorIndex1}" +
                 $" t:{TexCoordIndex1},{TexCoordIndex2},{TexCoordIndex3}" +
                 $" un{Unknown}</Face>";
        }
    }
}
