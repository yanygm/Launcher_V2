using System.IO;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public class LinearVector3KeyframeData : Vector3KeyframeData<LinearVector3Keyframe>
{
    public override Vector3KeyframeDataType ListType => Vector3KeyframeDataType.Linear;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            Add(new LinearVector3Keyframe
            {
                Time = time,
                Value = new Vector3(x, y, z)
            });
        }
    }
}