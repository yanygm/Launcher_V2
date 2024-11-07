using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class NoEasingIntKeyframeData : IntKeyframeData<NoEasingIntKeyframe>
{
    public override IntKeyframeDataType DataType => IntKeyframeDataType.NoEasing;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            int value = reader.ReadInt32();
            Add(new NoEasingIntKeyframe
            {
                Time = time,
                Value = value
            });
        }
    }
}