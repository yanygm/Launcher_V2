namespace KartLibrary.Game.Engine.Tontrollers;

public interface IFloatKeyframeData : IKeyframeData<float>
{
    FloatKeyframeDataType KeyframeDataType { get; }
}