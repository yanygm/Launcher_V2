using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public interface IRotateKeyframeData : IKeyframeData<Quaternion>
{
    RotateKeyframeDataType ListType { get; }
}