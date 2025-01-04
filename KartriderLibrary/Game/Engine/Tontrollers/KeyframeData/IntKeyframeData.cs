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
    public class IntKeyframeDataFactory
    {
        public IIntKeyframeData CreateIntKeyframeData(IntKeyframeDataType dataType)
        {
            switch(dataType)
            {
                case IntKeyframeDataType.Cubic:
                    return new CubicIntKeyframeData();
                case IntKeyframeDataType.Linear:
                    return new LinearIntKeyframeData();
                case IntKeyframeDataType.NoEasing:
                    return new NoEasingIntKeyframeData();
                default:
                    throw new Exception("");
            }
        }
    }

    public interface IIntKeyframeData : IKeyframeData<int>
    {
        IntKeyframeDataType DataType { get; }
    }

    public abstract class IntKeyframeData<TKeyframe> : IIntKeyframeData, IList<TKeyframe> where TKeyframe : IKeyframe<int>
    {
        protected IntKeyframeData()
        {

        }

        private List<TKeyframe> container = new List<TKeyframe>();

        public TKeyframe this[int index] { get => container[index]; set => container[index] = value; }

        public bool IsReadOnly => false;

        public int Count => container.Count;

        public abstract IntKeyframeDataType DataType { get; }

        public int GetValue(float time)
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
                IKeyframe<int> curKeyFrame = this[start];
                IKeyframe<int>? nextKeyFrame = start + 1 >= Count ? null : this[start + 1];
                float duration = (nextKeyFrame?.Time ?? curKeyFrame.Time) - curKeyFrame.Time;
                float t = (time - curKeyFrame.Time) / (duration == 0 ? 1 : duration);
                return curKeyFrame.CalculateKeyFrame(t, nextKeyFrame);
            }
        }

        public void Add(TKeyframe item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException();
            container.Add(default);
            int i;
            for (i = Count - 1; i > 0; i--)
            {
                TKeyframe curObj = container[i - 1];
                if (curObj.Time > item.Time)
                    container[i] = curObj;
                else
                    break;
            }
            container[i] = item;
        }

        public void Clear()
        {
            container.Clear();
        }

        public bool Contains(TKeyframe item)
        {
            return container.Contains(item);
        }

        public void CopyTo(TKeyframe[] array, int arrayIndex)
        {
            container.CopyTo(array, arrayIndex);
        }

        public int IndexOf(TKeyframe item)
        {
            return container.IndexOf(item);
        }

        public void Insert(int index, TKeyframe item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKeyframe item)
        {
            return container.Remove(item);
        }

        public void RemoveAt(int index)
        {
            container.RemoveAt(index);
        }

        public IEnumerator<TKeyframe> GetEnumerator()
        {
            return container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void DecodeObject(BinaryReader reader, int count);

        public class Enumerator : IEnumerator<IKeyframe<int>>
        {
            private IEnumerator<TKeyframe> baseEnumerator;

            public TKeyframe Current => baseEnumerator.Current;

            object IEnumerator.Current => baseEnumerator.Current;

            IKeyframe<int> IEnumerator<IKeyframe<int>>.Current => baseEnumerator.Current;

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

    public class CubicIntKeyframeData : IntKeyframeData<CubicIntKeyframe>
    {
        public override IntKeyframeDataType DataType => IntKeyframeDataType.Cubic;

        public override void DecodeObject(BinaryReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int time = reader.ReadInt32();
                int value = reader.ReadInt32();
                float leftSlop = reader.ReadSingle();
                float rightSlop = reader.ReadSingle();
                Add(new CubicIntKeyframe
                {
                    Time = time,
                    Value = value,
                    LeftSlop = leftSlop,
                    RightSlop = rightSlop
                });
            }
        }
    }

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
                    Value = value,
                });
            }
        }
    }

    public class LinearIntKeyframe : IKeyframe<int>
    {
        public int Time { get; set; }
        public int Value { get; set; }
        public int CalculateKeyFrame(float t, IKeyframe<int>? nextKeyframe)
        {
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not LinearIntKeyframe)
                throw new ArgumentException();
            int result = (int)(Value * (1 - t) + nextKeyframe.Value * t);
            return result;
        }
    }

    public class CubicIntKeyframe : IKeyframe<int>
    {
        public int Time { get; set; }
        public int Value { get; set; }
        public float LeftSlop { get; set; }
        public float RightSlop { get; set; }
        public int CalculateKeyFrame(float t, IKeyframe<int>? nextKeyframe)
        {
            // y = ax^3 + bx^2 + cx，一個一元三次方程式曲線，其中0 <= x <= 1。
            // 藉由給定x = 0與x = 1時的切線斜率currentKeyframe.RightSlop與nextKeyframe.LeftSlop定義該一元三次方程式。
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not CubicIntKeyframe)
                throw new ArgumentException();
            CubicIntKeyframe next = (CubicIntKeyframe)nextKeyframe;
            float delta = next.Value - Value;
            float a = RightSlop + next.LeftSlop - 2 * delta;
            float b = 3 * delta - next.LeftSlop - 2 * RightSlop;
            float c = RightSlop;
            int result = (int)(((a * t + b) * t + c) * t + Value);
            return result;
        }
    }

    public class NoEasingIntKeyframe : IKeyframe<int>
    {
        public int Time { get; set; }
        public int Value { get; set; }
        public int CalculateKeyFrame(float t, IKeyframe<int>? nextKeyframe)
        {
            return Value;
        }
    }

    public enum IntKeyframeDataType
    {
        Cubic = 0,
        Linear = 1,
        NoEasing = 3
    }
}
