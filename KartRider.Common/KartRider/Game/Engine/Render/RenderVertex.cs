using System.Numerics;

namespace KartLibrary.Game.Engine.Render;

public struct RenderVertex
{
    public Vector3 Position;

    public Vector2 TextureCoord;

    public const uint SizeOfStruct = 20u;
}