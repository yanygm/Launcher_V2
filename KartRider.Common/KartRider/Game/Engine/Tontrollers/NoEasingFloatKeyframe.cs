namespace KartLibrary.Game.Engine.Tontrollers;

public class NoEasingFloatKeyframe : IKeyframe<float>
{
    public int Time { get; set; }

    public float Value { get; set; }

    public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
    {
        return Value;
    }
}