using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Enums
{
    public enum D3DTextureOp
    {
        Disable = 1,
        SelectARG1 = 2,
        SelectARG2 = 3,
        Modulate = 4,
        Modulate2X = 5,
        Modulate4X = 6,
        Add = 7,
        AddSIGNED = 8,
        AddSIGNED2X = 9,
        Subtract = 10,
        AddSMOOTH = 11,
        BlendDiffuseAlpha = 12,
        BlendTextureAlpha = 13,
        BlendFACTORAlpha = 14,
        BlendTextureAlphaPM = 15,
        BlendCurrentAlpha = 16,
        PreModulate = 17,
        ModulateAlphaAddColor = 18,
        ModulateColorAddAlpha = 19,
        ModulateINVAlphaAddColor = 20,
        ModulateINVColorAddAlpha = 21,
        BumpENVMAP = 22,
        BumpEnvMapluminance = 23,
        DotProduct3 = 24,
        MultiplyAdd = 25,
        Lerp = 26,
    }
}
