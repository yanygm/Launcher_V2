using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Enums
{
    public enum BlendFactor
    {
        Zero = 1,
        One = 2,
        SourceColor = 3,
        InverseSourceColor = 4,
        SourceAlpha = 5,
        InverseSourceAlpha = 6,
        DestinationAlpha = 7,
        InverseDestinationAlpha = 8,
        DestinationColor = 9,
        InverseDestinationColor = 10,
        SourceAlphaSaturate = 11,
        BothSourceAlpha = 12,
        BothInverseSourceAlpha = 13,
        BlendFactor = 14,
        InverseBlendFactor = 15,
        SourceColor2 = 16,
        InverseSourceColor2 = 17,
    }
}
