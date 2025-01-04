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
    // Keyframe Type 2
    public interface IRotateKeyframeData : IKeyframeData<Quaternion>
    {
        RotateKeyframeDataType ListType { get; }
    }
    public abstract class RotateKeyframeData: IRotateKeyframeData
    {
        protected RotateKeyframeData()
        {

        }
        
        public bool IsReadOnly => false;

        public abstract RotateKeyframeDataType ListType { get; }

        public abstract Quaternion GetValue(float time);

        public abstract void DecodeObject(BinaryReader reader, int count);

    }

    public abstract class RotateKeyframeData<TKeyframe> : RotateKeyframeData, IList<TKeyframe> where TKeyframe: IKeyframe<Quaternion>
    {
        protected RotateKeyframeData()
        {

        }

        private List<TKeyframe> container = new List<TKeyframe>();

        public TKeyframe this[int index] { get => container[index]; set => container[index] = value; }

        public bool IsReadOnly => false;

        public int Count => container.Count;

        public override abstract RotateKeyframeDataType ListType { get; }

        public override Quaternion GetValue(float time)
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
                IKeyframe<Quaternion> curKeyFrame = this[start];
                IKeyframe<Quaternion>? nextKeyFrame = start + 1 >= Count ? null : this[start + 1];
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

        public override abstract void DecodeObject(BinaryReader reader, int count);

        public class Enumerator : IEnumerator<IKeyframe<Quaternion>>
        {
            private IEnumerator<TKeyframe> baseEnumerator;

            public TKeyframe Current => baseEnumerator.Current;

            object IEnumerator.Current => baseEnumerator.Current;

            IKeyframe<Quaternion> IEnumerator<IKeyframe<Quaternion>>.Current => baseEnumerator.Current;

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

    // DataType 4
    public class ThreeAxisRotateKeyframeData : RotateKeyframeData
    {
        private IFloatKeyframeData _xAxisKeyframeData;
        private IFloatKeyframeData _yAxisKeyframeData;
        private IFloatKeyframeData _zAxisKeyframeData;
        
        public IFloatKeyframeData XAxisKeyframeData => _xAxisKeyframeData;
        public IFloatKeyframeData YAxisKeyframeData => _yAxisKeyframeData;
        public IFloatKeyframeData ZAxisKeyframeData => _zAxisKeyframeData;

        private float _u2;
        private float _u3;
        private float _u4;
        private float _u5;

        public ThreeAxisRotateKeyframeData()
        {

        }

        public ThreeAxisRotateKeyframeData(IFloatKeyframeData xAxisKeyframeData, IFloatKeyframeData yAxisKeyframeData, IFloatKeyframeData zAxisKeyframeData)
        {
            _xAxisKeyframeData = xAxisKeyframeData;
            _yAxisKeyframeData = yAxisKeyframeData;
            _zAxisKeyframeData = zAxisKeyframeData;
        }

        public override RotateKeyframeDataType ListType => RotateKeyframeDataType.ThreeAxis;

        public override void DecodeObject(BinaryReader reader, int count)
        {
            // Ignore count parameter.
            int time = reader.ReadInt32();
            _u2 = (float)reader.ReadSingle();
            _u3 = (float)reader.ReadSingle();
            _u4 = (float)reader.ReadSingle();
            _u5 = (float)reader.ReadSingle();
            
            int floatKeyFrameType = reader.ReadInt32();
            int floatKeyFrameCount = reader.ReadInt32();
            // x-axis
            switch(floatKeyFrameType)
            {
                case 0: // Cubic
                    _xAxisKeyframeData = new CubicFloatKeyframeData();
                    _xAxisKeyframeData.DecodeObject(reader, floatKeyFrameCount);
                    break;
                case 1: // Linear
                    _xAxisKeyframeData = new LinearFloatKeyframeData();
                    _xAxisKeyframeData.DecodeObject(reader, floatKeyFrameCount);
                    break;
                default:
                    throw new NotSupportedException("Sorry, Author is too stupid to finish this section.");
            }

            // y-axis
            floatKeyFrameType = reader.ReadInt32();
            floatKeyFrameCount = reader.ReadInt32();
            switch (floatKeyFrameType)
            {
                case 0: // Cubic
                    _yAxisKeyframeData = new CubicFloatKeyframeData();
                    _yAxisKeyframeData.DecodeObject(reader, floatKeyFrameCount);
                    break;
                case 1: // Linear
                    _yAxisKeyframeData = new LinearFloatKeyframeData();
                    _yAxisKeyframeData.DecodeObject(reader, floatKeyFrameCount);
                    break;
                default:
                    throw new NotSupportedException("Sorry, Author is too stupid to finish this section.");
            }

            // z-axis
            floatKeyFrameType = reader.ReadInt32();
            floatKeyFrameCount = reader.ReadInt32();
            switch (floatKeyFrameType)
            {
                case 0: // Cubic
                    _zAxisKeyframeData = new CubicFloatKeyframeData();
                    _zAxisKeyframeData.DecodeObject(reader, floatKeyFrameCount);
                    break;
                case 1: // Linear
                    _zAxisKeyframeData = new LinearFloatKeyframeData();
                    _zAxisKeyframeData.DecodeObject(reader, floatKeyFrameCount);
                    break;
                default:
                    throw new NotSupportedException("Sorry, Author is too stupid to finish this section.");
            }
        }

        public override Quaternion GetValue(float time)
        {
            float xAngle = _xAxisKeyframeData.GetValue(time);
            float yAngle = _yAxisKeyframeData.GetValue(time);
            float zAngle = _zAxisKeyframeData.GetValue(time);
            Quaternion xAxisQuaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitX, xAngle);
            Quaternion yAxisQuaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yAngle);
            Quaternion zAxisQuaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, zAngle);
            return zAxisQuaternion * yAxisQuaternion * xAxisQuaternion;
        }
    }

    public enum RotateKeyframeDataType
    {
        ThreeAxis = 4
    }
}
