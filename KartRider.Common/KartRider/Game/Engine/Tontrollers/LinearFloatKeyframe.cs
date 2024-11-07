using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class LinearFloatKeyframe : IKeyframe<float>
{
    public int Time { get; set; }

    public float Value { get; set; }

    public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
    {
        if (nextKeyframe == null)
        {
            return Value;
        }

        if (!(nextKeyframe is LinearFloatKeyframe))
        {
            throw new ArgumentException();
        }

        return Value * (1f - t) + nextKeyframe.Value * t;
    }
}