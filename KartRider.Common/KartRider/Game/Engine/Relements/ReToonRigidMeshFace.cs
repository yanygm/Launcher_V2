namespace KartLibrary.Game.Engine.Relements;

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
        return $"<Face> v:{TexCoordIndex1},{TexCoordIndex2},{TexCoordIndex3} n:{NormalVectorIndex1},{NormalVectorIndex2},{NormalVectorIndex1} t:{TexCoordIndex1},{TexCoordIndex2},{TexCoordIndex3} un{Unknown}</Face>";
    }
}