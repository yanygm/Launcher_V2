using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class LinearFloatKeyframeData : FloatKeyframeData<LinearFloatKeyframe>
{
    public override FloatKeyframeDataType KeyframeDataType => FloatKeyframeDataType.Linear;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float value = reader.ReadSingle();
            Add(new LinearFloatKeyframe
            {
                Time = time,
                Value = value
            });
        }
    }
}