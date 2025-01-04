using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Tontrollers
{
    public class Vector3KeyframeDataFactory
    {
        public IVector3KeyframeData CreateVector3KeyframeData(Vector3KeyframeDataType dataType)
        {
            switch(dataType)
            {
                case Vector3KeyframeDataType.Linear:
                    return new LinearVector3KeyframeData();
                case Vector3KeyframeDataType.Cubic:
                    return new CubicVector3KeyframeData();
                default:
                    throw new Exception();
            }
        }
    }
    public interface IVector3KeyframeData : IKeyframeData<Vector3>
    {
        Vector3KeyframeDataType ListType { get; }
    }

    public abstract class Vector3KeyframeData<TKeyframe> : IVector3KeyframeData, IList<TKeyframe> where TKeyframe : IKeyframe<Vector3>
    {
        protected Vector3KeyframeData()
        {

        }

        private List<TKeyframe> container = new List<TKeyframe>();

        public TKeyframe this[int index] { get => container[index]; set => container[index] = value; }

        public bool IsReadOnly => false;

        public int Count => container.Count;

        public abstract Vector3KeyframeDataType ListType { get; }

        public Vector3 GetValue(float time)
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
                else if (this[mid].Time < time)
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
                IKeyframe<Vector3> curKeyFrame = this[start];
                IKeyframe<Vector3>? nextKeyFrame = start + 1 >= Count ? null : this[start + 1];
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

        public class Enumerator : IEnumerator<IKeyframe<Vector3>>
        {
            private IEnumerator<TKeyframe> baseEnumerator;

            public TKeyframe Current => baseEnumerator.Current;

            object IEnumerator.Current => baseEnumerator.Current;

            IKeyframe<Vector3> IEnumerator<IKeyframe<Vector3>>.Current => baseEnumerator.Current;

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

    public class LinearVector3KeyframeData : Vector3KeyframeData<LinearVector3Keyframe>
    {
        public override Vector3KeyframeDataType ListType => Vector3KeyframeDataType.Linear;

        public override void DecodeObject(BinaryReader reader, int count)
        {
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
                Add(new LinearVector3Keyframe
                {
                    Time = uu1,
                    Value = new System.Numerics.Vector3(uu2, uu3, uu4)
                });
            }
        }
    }

    public class CubicVector3KeyframeData : Vector3KeyframeData<CubicVector3Keyframe>
    {
        public override Vector3KeyframeDataType ListType => Vector3KeyframeDataType.Cubic;

        public override void DecodeObject(BinaryReader reader, int count)
        {
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
                Add(new CubicVector3Keyframe
                {
                    Time = uu1,
                    Value = new System.Numerics.Vector3(uu2, uu3, uu4),
                    LeftSlop = new System.Numerics.Vector3(uu5, uu6, uu7),
                    RightSlop = new System.Numerics.Vector3(uu8, uu9, uu10),
                });
            }
        }
    }

    public class LinearVector3Keyframe : IKeyframe<Vector3>
    {
        public int Time { get; set; }
        public Vector3 Value { get; set; }

        public Vector3 CalculateKeyFrame(float t, IKeyframe<Vector3>? nextKeyframe)
        {
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not LinearVector3Keyframe)
                throw new ArgumentException();
            Vector3 result = Value * (1 - t) + nextKeyframe.Value * t;
            return result;
        }
    }

    public class CubicVector3Keyframe : IKeyframe<Vector3>
    {
        public int Time { get; set; }
        public Vector3 Value { get; set; }
        public Vector3 LeftSlop { get; set; }
        public Vector3 RightSlop { get; set; }

        public Vector3 CalculateKeyFrame(float t, IKeyframe<Vector3>? nextKeyframe)
        {
            if (nextKeyframe is null)
                return Value;
            if (nextKeyframe is not CubicVector3Keyframe)
                throw new ArgumentException();
            CubicVector3Keyframe next = (CubicVector3Keyframe)nextKeyframe;
            Vector3 delta = nextKeyframe.Value - Value;
            Vector3 a = RightSlop + next.LeftSlop - 2 * delta;
            Vector3 b = 3 * delta - next.LeftSlop - 2 * RightSlop;
            Vector3 c = RightSlop;
            Vector3 result = ((a * t + b) * t + c) * t + Value;
            return result;
        }
    }

    public enum Vector3KeyframeDataType
    {
        Cubic,
        Linear,
    }
}
