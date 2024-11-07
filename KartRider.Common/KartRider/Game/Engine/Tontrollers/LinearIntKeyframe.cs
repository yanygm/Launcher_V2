using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class LinearIntKeyframe : IKeyframe<int>
{
    public int Time { get; set; }

    public int Value { get; set; }

    public int CalculateKeyFrame(float t, IKeyframe<int>? nextKeyframe)
    {
        if (nextKeyframe == null)
        {
            return Value;
        }

        if (!(nextKeyframe is LinearIntKeyframe))
        {
            throw new ArgumentException();
        }

        return (int)((float)Value * (1f - t) + (float)nextKeyframe.Value * t);
    }
}