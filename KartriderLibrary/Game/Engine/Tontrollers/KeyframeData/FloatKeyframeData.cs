using KartLibrary.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    public class FloatKeyframeDataFactory
    {
        public IFloatKeyframeData CreateFloatKeyframeData(FloatKeyframeDataType KeyframeDataType)
        {
            switch (KeyframeDataType)
            {
                case FloatKeyframeDataType.Cubic:
                    return new CubicFloatKeyframeData();
                case FloatKeyframeDataType.Linear:
                    return new LinearFloatKeyframeData();
                case FloatKeyframeDataType.CubicAlt:
                    return new CubicAltFloatKeyframeData();
                case FloatKeyframeDataType.NoEasing:
                    return new NoEasingFloatKeyframeData();
                default:
                    throw new Exception($"Couldn't find any FloatKeyframeData type for dataType:{KeyframeDataType}");
            }
        }
    }
    
    public interface IFloatKeyframeData : IKeyframeData<float>
    {
        FloatKeyframeDataType KeyframeDataType { get; }
    }

    public abstract class FloatKeyframeData<TKeyframe> : IFloatKeyframeData, IList<TKeyframe> where TKeyframe : IKeyframe<float>
    {
        protected FloatKeyframeData()
        {

        }

        private List<TKeyframe> _container = new List<TKeyframe>();

        public TKeyframe this[int index] { get => _container[index]; set => _container[index] = value; }

        public bool IsReadOnly => false;

        public int Count => _container.Count;

        public abstract FloatKeyframeDataType KeyframeDataType { get; }

        public float GetValue(float time)
        {
            int start = 0;
            int end = Count - 1;
            while (Math.Abs(start - end) > 1)
            {
                int mid = (start + end) >> 1;
                if (time < this[mid].Time)
                {
                    end = mid;
                }
                else if (mid < this[mid].Time)
                {
                    start = mid;
                }
                else
                {
                    while (start + 1 < end && this[start + 1].Time == this[start].Time)
                    {
                        start++;
                    }
                    break;
                }
            }
            if (this[end].Time < time)
                return this[end].Value;
            else if (this[start].Time > time)
                return this[start].Value;
            else
            {
                IKeyframe<float> curKeyFrame = this[start];
                IKeyframe<float>? nextKeyFrame = start + 1 >= Count ? null : this[start + 1];
                float duration = (nextKeyFrame?.Time ?? curKeyFrame.Time) - curKeyFrame.Time;
                float t = (time - curKeyFrame.Time) / (duration == 0 ? 1 : duration);
                return curKeyFrame.CalculateKeyFrame(t, nextKeyFrame);
            }
        }

        public void Add(TKeyframe item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException();
            _container.Add(default);
            int i;
            for (i = Count - 1; i > 0; i--)
            {
                TKeyframe curObj = _container[i - 1];
                if (curObj.Time > item.Time)
                    _container[i] = curObj;
                else
                    break;
            }
            _container[i] = item;
        }

        public void Clear()
        {
            _container.Clear();
        }

        public bool Contains(TKeyframe item)
        {
            return _container.Contains(item);
        }

        public void CopyTo(TKeyframe[] array, int arrayIndex)
        {
            _container.CopyTo(array, arrayIndex);
        }

        public int IndexOf(TKeyframe item)
        {
            return _container.IndexOf(item);
        }

        public void Insert(int index, TKeyframe item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKeyframe item)
        {
            return _container.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _container.RemoveAt(index);
        }

        public IEnumerator<TKeyframe> GetEnumerator()
        {
            return _container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void DecodeObject(BinaryReader reader, int count);

        public class Enumerator : IEnumerator<IKeyframe<float>>
        {
            private IEnumerator<TKeyframe> baseEnumerator;

            public TKeyframe Current => baseEnumerator.Current;

            object IEnumerator.Current => baseEnumerator.Current;

            IKeyframe<float> IEnumerator<IKeyframe<float>>.Current => baseEnumerator.Current;

            public Enumerator(IEnumerator<TKeyframe> baseEnumerator)
            {
                this.baseEnumerator = baseEnumerator;
            }

            public void Dispose()
            {
                baseEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                return baseEnumerator.MoveNext();
            }

            public void Reset()
            {
                baseEnumerator.Reset();
            }
        }
    }

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

    public class CubicFloatKeyframeData : FloatKeyframeData<CubicFloatKeyframe>
    {
        public override FloatKeyframeDataType KeyframeDataType => FloatKeyframeDataType.Cubic;

        public override void DecodeObject(BinaryReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int time = reader.ReadInt32();
                float value = reader.ReadSingle();
                float leftSlop = reader.ReadSingle();
                float rightSlop = reader.ReadSingle();
                Add(new CubicFloatKeyframe
                {
                    Time = time,
                    Value = value,
                    LeftSlop = leftSlop,
                    RightSlop = rightSlop
                });
            }
        }
    }

    public class CubicAltFloatKeyframeData : FloatKeyframeData<CubicAltFloatKeyframe>
    {
        public override FloatKeyframeDataType KeyframeDataType => FloatKeyframeDataType.CubicAlt;

        public override void DecodeObject(BinaryReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int time = reader.ReadInt32();
                float value = reader.ReadSingle();
                float x1 = reader.ReadSingle();
                float x2 = reader.ReadSingle();
                float x3 = reader.ReadSingle();
                Add(new CubicAltFloatKeyframe
                {
                    Time = time,
                    Value = value,
                    X1 = x1,
                    X2 = x2,
                    X3 = x3,
                });
            }
            if(count > 1)
            {
                float delta01 = this[1].Value - this[0].Value;
                float delta12 = 0;
                this[0].LeftSlop = ((1 + this[0].X2) * (1 - this[0].X3) + (1 - this[0].X2) * (1 + this[0].X3)) 
                                    * (1 - this[0].X1) * delta01 * 0.5f;
                this[0].RightSlop = ((1 - this[0].X2) * (1 - this[0].X3) + (1 + this[0].X2) * (1 + this[0].X3))
                                    * (1 - this[0].X1) * delta01 * 0.5f;
                for(int i = 1; i < count - 2; i++)
                {
                    delta01 = this[i].Value - this[i - 1].Value;
                    delta12 = this[i + 1].Value - this[i].Value;
                    double duration01 = this[i].Time - this[i - 1].Time;
                    double duration12 = this[i + 1].Time - this[i].Time;
                    double duration02 = duration01 + duration12;
                    this[i].LeftSlop = (float)(((1 + this[i].X2) * (1 - this[i].X3) * delta12 + 
                                                (1 - this[i].X2) * (1 + this[0].X3) * delta01)
                                              * (1 - this[i].X1) * (duration01 / duration02));
                    this[i].RightSlop = (float)(((1 - this[i].X2) * (1 - this[i].X3) * delta12 +
                                                 (1 + this[i].X2) * (1 + this[0].X3) * delta01)
                                               * (1 - this[i].X1) * (duration12 / duration02));
                }
                if(count > 2)
                {
                    this[^1].LeftSlop = ((1 + this[^1].X2) * (1 - this[^1].X3) + (1 - this[^1].X2) * (1 + this[^1].X3))
                                        * (1 - this[^1].X1) * delta01 * 0.5f;
                    this[^1].RightSlop = ((1 - this[^1].X2) * (1 - this[^1].X3) + (1 + this[^1].X2) * (1 + this[^1].X3))
                                        * (1 - this[^1].X1) * delta01 * 0.5f;
                }
            }
        }
    }

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
                    Value = value,
                });
            }
        }
    }

    public class LinearFloatKeyframe : IKeyframe<float>
    {
        public int Time { get; set; }
        public float Value { get; set; }
        public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
        {
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not LinearFloatKeyframe)
                throw new ArgumentException();
            float result = Value * (1 - t) + nextKeyframe.Value * t;
            return result;
        }
    }

    public class CubicFloatKeyframe : IKeyframe<float>
    {
        public int Time { get; set; }
        public float Value { get; set; }
        public float LeftSlop { get; set; }
        public float RightSlop { get; set; }
        public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
        {
            // Suppose y = ax^3 + bx^2 + cx is a cubic polynomial equation where 0 <= x <= 1.
            // Define this cubic polynomial equation by
            // giving a tangent value "currentKeyframe.RightSlop" at x = 0 and a tangent value "nextKeyframe.LeftSlop" at x = 1.
            // 令y = ax^3 + bx^2 + cx為一個一元三次方程式，其中0 <= x <= 1。
            // 藉由給定x = 0與x = 1時的切線斜率currentKeyframe.RightSlop與nextKeyframe.LeftSlop定義該一元三次方程式。
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not CubicFloatKeyframe)
                throw new ArgumentException();
            CubicFloatKeyframe next = (CubicFloatKeyframe)nextKeyframe;
            float delta = next.Value - Value;
            float a = RightSlop + next.LeftSlop - 2 * delta;
            float b = 3 * delta - next.LeftSlop - 2 * RightSlop;
            float c = RightSlop;
            float result = ((a * t + b) * t + c) * t + Value;
            return result;
        }
    }

    public class CubicAltFloatKeyframe : IKeyframe<float>
    {
        public int Time { get; set; }
        public float Value { get; set; }
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float X3 { get; set; }

        public float LeftSlop { get; set; }
        public float RightSlop { get; set; }

        public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
        {
            // Suppose y = ax^3 + bx^2 + cx be a cubic polynomial equation where 0 <= x <= 1.
            // Define this cubic polynomial equation by
            // giving a tangent value "currentKeyframe.RightSlop" at x = 0 and a tangent value "nextKeyframe.LeftSlop" at x = 1.
            // 令y = ax^3 + bx^2 + cx為一個一元三次方程式，其中0 <= x <= 1。
            // 藉由給定x = 0與x = 1時的切線斜率currentKeyframe.RightSlop與nextKeyframe.LeftSlop定義該一元三次方程式。
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not CubicFloatKeyframe)
                throw new ArgumentException();
            CubicFloatKeyframe next = (CubicFloatKeyframe)nextKeyframe;
            float delta = next.Value - Value;
            float a = RightSlop + next.LeftSlop - 2 * delta;
            float b = 3 * delta - next.LeftSlop - 2 * RightSlop;
            float c = RightSlop;
            float result = ((a * t + b) * t + c) * t + Value;
            return result;
        }
    }

    public class NoEasingFloatKeyframe : IKeyframe<float>
    {
        public int Time { get; set; }
        public float Value { get; set; }
        public float CalculateKeyFrame(float t, IKeyframe<float>? nextKeyframe)
        {
            return Value;
        }
    }

    public enum FloatKeyframeDataType
    {
        Cubic = 0,
        Linear = 1,
        CubicAlt = 2,
        NoEasing = 3,
    }
}
