namespace KartLibrary.Game.Engine.Tontrollers;

public interface IIntKeyframeData : IKeyframeData<int>
{
    IntKeyframeDataType DataType { get; }
}