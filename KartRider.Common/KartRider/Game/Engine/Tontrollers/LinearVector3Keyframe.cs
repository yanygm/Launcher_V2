using System;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public class LinearVector3Keyframe : IKeyframe<Vector3>
{
    public int Time { get; set; }

    public Vector3 Value { get; set; }

    public Vector3 CalculateKeyFrame(float t, IKeyframe<Vector3>? nextKeyframe)
    {
        if (nextKeyframe == null)
        {
            return Value;
        }

        if (!(nextKeyframe is LinearVector3Keyframe))
        {
            throw new ArgumentException();
        }

        return Value * (1f - t) + nextKeyframe.Value * t;
    }
}