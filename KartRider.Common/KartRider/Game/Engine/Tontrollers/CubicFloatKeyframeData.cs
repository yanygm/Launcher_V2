using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicFloatKeyframeData : FloatKeyframeData<CubicFloatKeyframe>
{
    public override FloatKeyframeDataType KeyframeDataType => FloatKeyframeDataType.Cubic;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float value = reader.ReadSingle();
            float leftSlop = reader.ReadSingle();
            float rightSlop = reader.ReadSingle();
            Add(new CubicFloatKeyframe
            {
                Time = time,
                Value = value,
                LeftSlop = leftSlop,
                RightSlop = rightSlop
            });
        }
    }
}