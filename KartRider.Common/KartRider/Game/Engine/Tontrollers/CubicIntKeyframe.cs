using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicIntKeyframe : IKeyframe<int>
{
    public int Time { get; set; }

    public int Value { get; set; }

    public float LeftSlop { get; set; }

    public float RightSlop { get; set; }

    public int CalculateKeyFrame(float t, IKeyframe<int>? nextKeyframe)
    {
        if (nextKeyframe == null)
        {
            return Value;
        }

        if (!(nextKeyframe is CubicIntKeyframe))
        {
            throw new ArgumentException();
        }

        CubicIntKeyframe cubicIntKeyframe = (CubicIntKeyframe)nextKeyframe;
        float num = cubicIntKeyframe.Value - Value;
        float num2 = RightSlop + cubicIntKeyframe.LeftSlop - 2f * num;
        float num3 = 3f * num - cubicIntKeyframe.LeftSlop - 2f * RightSlop;
        float rightSlop = RightSlop;
        return (int)(((num2 * t + num3) * t + rightSlop) * t + (float)Value);
    }
}