using System.IO;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicVector3KeyframeData : Vector3KeyframeData<CubicVector3Keyframe>
{
    public override Vector3KeyframeDataType ListType => Vector3KeyframeDataType.Cubic;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float x2 = reader.ReadSingle();
            float y2 = reader.ReadSingle();
            float z2 = reader.ReadSingle();
            float x3 = reader.ReadSingle();
            float y3 = reader.ReadSingle();
            float z3 = reader.ReadSingle();
            Add(new CubicVector3Keyframe
            {
                Time = time,
                Value = new Vector3(x, y, z),
                LeftSlop = new Vector3(x2, y2, z2),
                RightSlop = new Vector3(x3, y3, z3)
            });
        }
    }
}