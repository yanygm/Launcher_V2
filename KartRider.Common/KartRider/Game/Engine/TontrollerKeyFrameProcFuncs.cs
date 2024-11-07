using System.IO;
using System.Numerics;
using KartLibrary.Game.Engine.Tontrollers;

namespace KartLibrary.Game.Engine;

public static class TontrollerKeyFrameProcFuncs
{
    public static object[] Func00(BinaryReader reader, int count)
    {
        object[] array = new object[1];
        IFloatKeyframeData floatKeyframeData = new CubicFloatKeyframeData();
        floatKeyframeData.DecodeObject(reader, count);
        array[0] = floatKeyframeData;
        return array;
    }

    public static object[] Func01(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float value = reader.ReadSingle();
            array[i] = new LinearFloatKeyframe
            {
                Time = time,
                Value = value
            };
        }

        return array;
    }

    public static object[] Func02(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            array[i] = new object[5] { num, num2, num3, num4, num5 };
        }

        return array;
    }

    public static object[] Func03(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            array[i] = new object[2] { num, num2 };
        }

        return array;
    }

    public static object[] Func10(BinaryReader reader, int count)
    {
        object[] array = new object[count];
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
            array[i] = new CubicVector3Keyframe
            {
                Time = time,
                Value = new Vector3(x, y, z),
                LeftSlop = new Vector3(x2, y2, z2),
                RightSlop = new Vector3(x3, y3, z3)
            };
        }

        return array;
    }

    public static object[] Func11(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int time = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            array[i] = new LinearVector3Keyframe
            {
                Time = time,
                Value = new Vector3(x, y, z)
            };
        }

        return array;
    }

    public static object[] Func12(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            int num6 = reader.ReadInt32();
            int num7 = reader.ReadInt32();
            array[i] = new object[7] { num, num2, num3, num4, num5, num6, num7 };
        }

        return array;
    }

    public static object[] Func13(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            array[i] = new object[4] { num, num2, num3, num4 };
        }

        return array;
    }

    public static object[] Func15(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int count2 = reader.ReadInt32();
            switch (num)
            {
                case 0:
                    array[i] = Func00(reader, count2);
                    break;
                case 1:
                    array[i] = Func01(reader, count2);
                    break;
                case 2:
                    array[i] = Func02(reader, count2);
                    break;
                case 3:
                    array[i] = Func03(reader, count2);
                    break;
            }
        }

        return array;
    }

    public static object[] Func20(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            array[i] = new object[5] { num, num2, num3, num4, num5 };
        }

        return array;
    }

    public static object[] Func21(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            array[i] = new object[5] { num, num2, num3, num4, num5 };
        }

        return array;
    }

    public static object[] Func22(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            int num6 = reader.ReadInt32();
            int num7 = reader.ReadInt32();
            int num8 = reader.ReadInt32();
            array[i] = new object[8] { num, num2, num3, num4, num5, num6, num7, num8 };
        }

        return array;
    }

    public static object[] Func23(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            array[i] = new object[5] { num, num2, num3, num4, num5 };
        }

        return array;
    }

    public static object[] Func24(BinaryReader reader, int count)
    {
        int num = reader.ReadInt32();
        float num2 = reader.ReadSingle();
        float num3 = reader.ReadSingle();
        float num4 = reader.ReadSingle();
        float num5 = reader.ReadSingle();
        object[] array = new object[4]
        {
            new object[5] { num, num2, num3, num4, num5 },
            null,
            null,
            null
        };
        for (int i = 1; i < 4; i++)
        {
            int num6 = reader.ReadInt32();
            int count2 = reader.ReadInt32();
            switch (num6)
            {
                case 0:
                    array[i] = Func00(reader, count2);
                    break;
                case 1:
                    array[i] = Func01(reader, count2);
                    break;
                case 2:
                    array[i] = Func02(reader, count2);
                    break;
                case 3:
                    array[i] = Func03(reader, count2);
                    break;
            }
        }

        return array;
    }

    public static object[] Func30(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            array[i] = new object[4] { num, num2, num3, num4 };
        }

        return array;
    }

    public static object[] Func31(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            array[i] = new object[2] { num, num2 };
        }

        return array;
    }

    public static object[] Func32(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            int num3 = reader.ReadInt32();
            int num4 = reader.ReadInt32();
            int num5 = reader.ReadInt32();
            array[i] = new object[5] { num, num2, num3, num4, num5 };
        }

        return array;
    }

    public static object[] Func33(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            int num2 = reader.ReadInt32();
            array[i] = new object[2] { num, num2 };
        }

        return array;
    }

    public static object[] Func43(BinaryReader reader, int count)
    {
        object[] array = new object[count];
        for (int i = 0; i < count; i++)
        {
            int num = reader.ReadInt32();
            byte b = reader.ReadByte();
            array[i] = new object[2] { num, b };
        }

        return array;
    }
}