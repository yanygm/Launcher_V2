using System;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicAltFloatKeyframe : IKeyframe<float>
{
    public int Time { get; set; }

    public float Value { get; set; }

    public float X1 { get; set; }

    public float X2 { get; set; }

    public float X3 { get; set; }

    public float LeftSlop { get; set; }

    public float RightSlop { get; set; }

    public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
    {
        if (nextKeyframe == null)
        {
            return Value;
        }

        if (!(nextKeyframe is CubicFloatKeyframe))
        {
            throw new ArgumentException();
        }

        CubicFloatKeyframe cubicFloatKeyframe = (CubicFloatKeyframe)nextKeyframe;
        float num = cubicFloatKeyframe.Value - Value;
        float num2 = RightSlop + cubicFloatKeyframe.LeftSlop - 2f * num;
        float num3 = 3f * num - cubicFloatKeyframe.LeftSlop - 2f * RightSlop;
        float rightSlop = RightSlop;
        return ((num2 * t + num3) * t + rightSlop) * t + Value;
    }
}