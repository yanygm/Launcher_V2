using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public interface IKeyframeData<TValue>
{
    TValue GetValue(float time);

    void DecodeObject(BinaryReader reader, int count);
}