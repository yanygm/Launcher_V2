using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class FloatKeyframeDataFactory
{
    public IFloatKeyframeData CreateFloatKeyframeData(FloatKeyframeDataType KeyframeDataType)
    {
        return KeyframeDataType switch
        {
            FloatKeyframeDataType.Cubic => new CubicFloatKeyframeData(),
            FloatKeyframeDataType.Linear => new LinearFloatKeyframeData(),
            FloatKeyframeDataType.CubicAlt => new CubicAltFloatKeyframeData(),
            FloatKeyframeDataType.NoEasing => new NoEasingFloatKeyframeData(),
            _ => throw new Exception($"Couldn't find any FloatKeyframeData type for dataType:{KeyframeDataType}"),
        };
    }
}