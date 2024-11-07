namespace KartLibrary.Game.Engine.Tontrollers;

public class NoEasingIntKeyframe : IKeyframe<int>
{
    public int Time { get; set; }

    public int Value { get; set; }

    public int CalculateKeyFrame(float t, IKeyframe<int>? nextKeyframe)
    {
        return Value;
    }
}