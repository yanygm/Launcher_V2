using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Enums
{
    // DirectX 9
    public enum D3DTextureFilterType
    {
        None = 0,
        Point = 1,
        Linear = 2,
        Anisotropic = 3,
        PyramidalQuad = 6,
        GaussianQuad = 7,
        ConvolutionMono = 8,
    }
}
