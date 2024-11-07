using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicIntKeyframeData : IntKeyframeData<CubicIntKeyframe>
{
    public override IntKeyframeDataType DataType => IntKeyframeDataType.Cubic;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            int value = reader.ReadInt32();
            float leftSlop = reader.ReadSingle();
            float rightSlop = reader.ReadSingle();
            Add(new CubicIntKeyframe
            {
                Time = time,
                Value = value,
                LeftSlop = leftSlop,
                RightSlop = rightSlop
            });
        }
    }
}