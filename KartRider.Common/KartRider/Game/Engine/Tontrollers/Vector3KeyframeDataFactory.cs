using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class Vector3KeyframeDataFactory
{
    public IVector3KeyframeData CreateVector3KeyframeData(Vector3KeyframeDataType dataType)
    {
        return dataType switch
        {
            Vector3KeyframeDataType.Linear => new LinearVector3KeyframeData(),
            Vector3KeyframeDataType.Cubic => new CubicVector3KeyframeData(),
            _ => throw new Exception(),
        };
    }
}