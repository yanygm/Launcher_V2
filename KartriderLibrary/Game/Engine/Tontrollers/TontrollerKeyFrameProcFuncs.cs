using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Game.Engine.Tontrollers;

namespace KartLibrary.Game.Engine
{
    public static class TontrollerKeyFrameProcFuncs
    {
        // Type0 EasingFunc:0(Cubic) FloatKeyframe
        public static object[] Func00(BinaryReader reader, int count)
        {
            object[] output = new object[1];
            IFloatKeyframeData floatKeyframeData = new CubicFloatKeyframeData();
            floatKeyframeData.DecodeObject(reader, count);
            output[0] = floatKeyframeData;
            return output;
        }
        // Type0 EasingFunc:1(Linear) FloatKeyframe
        public static object[] Func01(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                float uu2 = reader.ReadSingle();
                //int uu3 = reader.ReadInt32();
                //int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new LinearFloatKeyframe
                {
                    Time = uu1,
                    Value = uu2,
                };
            }
            return output;
        }

        // Type0 EasingFunc:2(CubicAlt) FloatKeyframe
        public static object[] Func02(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32(); // Time
                int uu2 = reader.ReadInt32(); // Value
                int uu3 = reader.ReadInt32(); // val2
                int uu4 = reader.ReadInt32(); // val3
                int uu5 = reader.ReadInt32(); // val4
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4, uu5 };
            }
            return output;
        }
        // Type0 EasingFunc:3(NoEasing) FloatKeyframe
        public static object[] Func03(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                //int uu3 = reader.ReadInt32();
                //int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2 };
            }
            return output;
        }

        //Type1, EasingFunc:0 Vector3KeyFrame
        public static object[] Func10(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                float uu2 = reader.ReadSingle();
                float uu3 = reader.ReadSingle();
                float uu4 = reader.ReadSingle();
                float uu5 = reader.ReadSingle();
                float uu6 = reader.ReadSingle();
                float uu7 = reader.ReadSingle();
                float uu8 = reader.ReadSingle();
                float uu9 = reader.ReadSingle();
                float uu10 = reader.ReadSingle();
                output[i] = new CubicVector3Keyframe
                {
                    Time = uu1,
                    Value = new System.Numerics.Vector3(uu2, uu3, uu4),
                    LeftSlop = new System.Numerics.Vector3(uu5, uu6, uu7),
                    RightSlop = new System.Numerics.Vector3(uu8, uu9, uu10),
                };
            }
            return output;
        }
        public static object[] Func11(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                float uu2 = reader.ReadSingle();
                float uu3 = reader.ReadSingle();
                float uu4 = reader.ReadSingle();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new LinearVector3Keyframe
                {
                    Time = uu1,
                    Value = new System.Numerics.Vector3(uu2, uu3, uu4)
                };
            }
            return output;
        }
        public static object[] Func12(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                int uu5 = reader.ReadInt32();
                int uu6 = reader.ReadInt32();
                int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4, uu5, uu6, uu7 };
            }
            return output;
        }
        public static object[] Func13(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4 };
            }
            return output;
        }
        public static object[] Func15(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                switch (uu1)
                {
                    case 0: output[i] = Func00(reader, uu2); break;
                    case 1: output[i] = Func01(reader, uu2); break;
                    case 2: output[i] = Func02(reader, uu2); break;
                    case 3: output[i] = Func03(reader, uu2); break;
                }
                //int uu3 = reader.ReadInt32();
                //int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
            }
            return output;
        }
        public static object[] Func20(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] =  new object[] { uu1, uu2, uu3, uu4, uu5 };
            }
            return output;
        }
        public static object[] Func21(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4, uu5 };
            }
            return output;
        }
        public static object[] Func22(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                int uu5 = reader.ReadInt32();
                int uu6 = reader.ReadInt32();
                int uu7 = reader.ReadInt32();
                int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4, uu5, uu6, uu7, uu8 };
            }
            return output;
        }
        public static object[] Func23(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4, uu5 };
            }
            return output;
        }
        public static object[] Func24(BinaryReader reader, int count)
        {
            int u1 = reader.ReadInt32();
            float u2 = reader.ReadSingle();
            float u3 = reader.ReadSingle();
            float u4 = reader.ReadSingle();
            float u5 = reader.ReadSingle();
            object[] output = new object[4];
            output[0] = new object[] {u1, u2, u3, u4, u5};

            for(int i = 1; i < 4; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                switch (uu1)
                {
                    case 0: output[i] = Func00(reader, uu2); break;
                    case 1: output[i] = Func01(reader, uu2); break;
                    case 2: output[i] = Func02(reader, uu2); break;
                    case 3: output[i] = Func03(reader, uu2); break;
                }
            }
            //int uu3 = reader.ReadInt32();
            //int uu4 = reader.ReadInt32();
            //int uu5 = reader.ReadInt32();
            //int uu6 = reader.ReadInt32();
            //int uu7 = reader.ReadInt32();
            //int uu8 = reader.ReadInt32();
            //int uu9 = reader.ReadInt32();
            //int uu10 = reader.ReadInt32();
            return output;
        }
        public static object[] Func30(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] =  new object[] { uu1, uu2, uu3, uu4};
            }
            return output;
        }
        public static object[] Func31(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                //int uu3 = reader.ReadInt32();
                //int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2 };
            }
            return output;
        }
        public static object[] Func32(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                int uu3 = reader.ReadInt32();
                int uu4 = reader.ReadInt32();
                int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2, uu3, uu4, uu5 };
            }
            return output;
        }
        public static object[] Func33(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                int uu2 = reader.ReadInt32();
                //int uu3 = reader.ReadInt32();
                //int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2 };
            }
            return output;
        }
        public static object[] Func43(BinaryReader reader, int count)
        {
            object[] output = new object[count];
            for (int i = 0; i < count; i++)
            {
                int uu1 = reader.ReadInt32();
                byte uu2 = reader.ReadByte();
                //int uu3 = reader.ReadInt32();
                //int uu4 = reader.ReadInt32();
                //int uu5 = reader.ReadInt32();
                //int uu6 = reader.ReadInt32();
                //int uu7 = reader.ReadInt32();
                //int uu8 = reader.ReadInt32();
                //int uu9 = reader.ReadInt32();
                //int uu10 = reader.ReadInt32();
                output[i] = new object[] { uu1, uu2 };
            }
            return output;
        }
    }
}
