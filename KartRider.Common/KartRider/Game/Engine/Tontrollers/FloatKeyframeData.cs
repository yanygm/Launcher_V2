using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace KartLibrary.Game.Engine.Tontrollers;

public abstract class FloatKeyframeData<TKeyframe> : IFloatKeyframeData, IKeyframeData<float>, IList<TKeyframe>, ICollection<TKeyframe>, IEnumerable<TKeyframe>, IEnumerable where TKeyframe : IKeyframe<float>
{
    public class Enumerator : IEnumerator<IKeyframe<float>>, IEnumerator, IDisposable
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

    private List<TKeyframe> _container = new List<TKeyframe>();

    public TKeyframe this[int index]
    {
        get
        {
            return _container[index];
        }
        set
        {
            _container[index] = value;
        }
    }

    public bool IsReadOnly => false;

    public int Count => _container.Count;

    public abstract FloatKeyframeDataType KeyframeDataType { get; }

    public float GetValue(float time)
    {
        int i = 0;
        int num = Count - 1;
        while (Math.Abs(i - num) > 1)
        {
            int num2 = i + num >> 1;
            if (time < (float)this[num2].Time)
            {
                num = num2;
                continue;
            }

            if (num2 < this[num2].Time)
            {
                i = num2;
                continue;
            }

            for (; i + 1 < num && this[i + 1].Time == this[i].Time; i++)
            {
            }

            break;
        }

        if ((float)this[num].Time < time)
        {
            return this[num].Value;
        }

        if ((float)this[i].Time > time)
        {
            return this[i].Value;
        }

        IKeyframe<float> keyframe = this[i];
        object obj;
        if (i + 1 < Count)
        {
            IKeyframe<float> keyframe2 = this[i + 1];
            obj = keyframe2;
        }
        else
        {
            obj = null;
        }

        IKeyframe<float> keyframe3 = (IKeyframe<float>)obj;
        float num3 = (keyframe3?.Time ?? keyframe.Time) - keyframe.Time;
        float t = (time - (float)keyframe.Time) / ((num3 == 0f) ? 1f : num3);
        return keyframe.CalculateKeyFrame(t, keyframe3);
    }

    public void Add(TKeyframe item)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException();
        }

        _container.Add(default(TKeyframe));
        int num;
        for (num = Count - 1; num > 0; num--)
        {
            TKeyframe value = _container[num - 1];
            if (value.Time <= item.Time)
            {
                break;
            }

            _container[num] = value;
        }

        _container[num] = item;
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
}