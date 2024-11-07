using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace KartLibrary.Game.Engine.Render;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct AlphaPropertyInfo
{
    public bool AlphaTestEnabled;

    public ComparisonFunction AlphaTestFunction;

    public int AlphaTestRef;

    public const uint SizeOfStruct = 16u;
}