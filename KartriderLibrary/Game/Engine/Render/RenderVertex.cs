using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Render
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RenderVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoord;
        public const uint SizeOfStruct = 20;
    }
}
