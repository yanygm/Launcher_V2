using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class NoEasingFloatKeyframeData : FloatKeyframeData<NoEasingFloatKeyframe>
{
    public override FloatKeyframeDataType KeyframeDataType => FloatKeyframeDataType.NoEasing;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float value = reader.ReadSingle();
            Add(new NoEasingFloatKeyframe
            {
                Time = time,
                Value = value
            });
        }
    }
}