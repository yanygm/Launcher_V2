using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class LinearIntKeyframeData : IntKeyframeData<LinearIntKeyframe>
{
    public override IntKeyframeDataType DataType => IntKeyframeDataType.Linear;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            int value = reader.ReadInt32();
            Add(new LinearIntKeyframe
            {
                Time = time,
                Value = value
            });
        }
    }
}