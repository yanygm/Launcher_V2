using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class IntKeyframeDataFactory
{
    public IIntKeyframeData CreateIntKeyframeData(IntKeyframeDataType dataType)
    {
        return dataType switch
        {
            IntKeyframeDataType.Cubic => new CubicIntKeyframeData(),
            IntKeyframeDataType.Linear => new LinearIntKeyframeData(),
            IntKeyframeDataType.NoEasing => new NoEasingIntKeyframeData(),
            _ => throw new Exception(""),
        };
    }
}