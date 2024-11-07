using System;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicVector3Keyframe : IKeyframe<Vector3>
{
    public int Time { get; set; }

    public Vector3 Value { get; set; }

    public Vector3 LeftSlop { get; set; }

    public Vector3 RightSlop { get; set; }

    public Vector3 CalculateKeyFrame(float t, IKeyframe<Vector3>? nextKeyframe)
    {
        if (nextKeyframe == null)
        {
            return Value;
        }

        if (!(nextKeyframe is CubicVector3Keyframe))
        {
            throw new ArgumentException();
        }

        CubicVector3Keyframe cubicVector3Keyframe = (CubicVector3Keyframe)nextKeyframe;
        Vector3 vector = nextKeyframe.Value - Value;
        Vector3 vector2 = RightSlop + cubicVector3Keyframe.LeftSlop - 2f * vector;
        Vector3 vector3 = 3f * vector - cubicVector3Keyframe.LeftSlop - 2f * RightSlop;
        Vector3 rightSlop = RightSlop;
        return ((vector2 * t + vector3) * t + rightSlop) * t + Value;
    }
}