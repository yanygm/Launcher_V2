using System.IO;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public abstract class RotateKeyframeData : IRotateKeyframeData, IKeyframeData<Quaternion>
{
    public bool IsReadOnly => false;

    public abstract RotateKeyframeDataType ListType { get; }

    public abstract Quaternion GetValue(float time);

    public abstract void DecodeObject(BinaryReader reader, int count);
}