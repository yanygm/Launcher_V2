using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public class CubicAltFloatKeyframeData : FloatKeyframeData<CubicAltFloatKeyframe>
{
    public override FloatKeyframeDataType KeyframeDataType => FloatKeyframeDataType.CubicAlt;

    public override void DecodeObject(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float value = reader.ReadSingle();
            float x = reader.ReadSingle();
            float x2 = reader.ReadSingle();
            float x3 = reader.ReadSingle();
            Add(new CubicAltFloatKeyframe
            {
                Time = time,
                Value = value,
                X1 = x,
                X2 = x2,
                X3 = x3
            });
        }

        if (count > 1)
        {
            float num = base[1].Value - base[0].Value;
            float num2 = 0f;
            base[0].LeftSlop = ((1f + base[0].X2) * (1f - base[0].X3) + (1f - base[0].X2) * (1f + base[0].X3)) * (1f - base[0].X1) * num * 0.5f;
            base[0].RightSlop = ((1f - base[0].X2) * (1f - base[0].X3) + (1f + base[0].X2) * (1f + base[0].X3)) * (1f - base[0].X1) * num * 0.5f;
            for (int j = 1; j < count - 2; j++)
            {
                num = base[j].Value - base[j - 1].Value;
                num2 = base[j + 1].Value - base[j].Value;
                double num3 = base[j].Time - base[j - 1].Time;
                double num4 = base[j + 1].Time - base[j].Time;
                double num5 = num3 + num4;
                base[j].LeftSlop = (float)((double)(((1f + base[j].X2) * (1f - base[j].X3) * num2 + (1f - base[j].X2) * (1f + base[0].X3) * num) * (1f - base[j].X1)) * (num3 / num5));
                base[j].RightSlop = (float)((double)(((1f - base[j].X2) * (1f - base[j].X3) * num2 + (1f + base[j].X2) * (1f + base[0].X3) * num) * (1f - base[j].X1)) * (num4 / num5));
            }

            if (count > 2)
            {
                this[Count - 1].LeftSlop = ((1f + this[Count - 1].X2) * (1f - this[Count - 1].X3) + (1f - this[Count - 1].X2) * (1f + this[Count - 1].X3)) * (1f - this[Count - 1].X1) * num * 0.5f;
                this[Count - 1].RightSlop = ((1f - this[Count - 1].X2) * (1f - this[Count - 1].X3) + (1f + this[Count - 1].X2) * (1f + this[Count - 1].X3)) * (1f - this[Count - 1].X1) * num * 0.5f;
            }
        }
    }
}