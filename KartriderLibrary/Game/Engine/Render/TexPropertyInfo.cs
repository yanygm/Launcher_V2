using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Render
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TexPropertyInfo
    {
        public float TexAlpha;
        public float TexOffsetX;
        public float TexOffsetY;
        private float _padding1;

        public const uint SizeOfStruct = 16;
    }
}
