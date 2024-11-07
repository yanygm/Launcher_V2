using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public interface IVector3KeyframeData : IKeyframeData<Vector3>
{
    Vector3KeyframeDataType ListType { get; }
}