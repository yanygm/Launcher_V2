using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace KartLibrary.Game.Engine.Tontrollers;

public abstract class RotateKeyframeData<TKeyframe> : RotateKeyframeData, IList<TKeyframe>, ICollection<TKeyframe>, IEnumerable<TKeyframe>, IEnumerable where TKeyframe : IKeyframe<Quaternion>
{
    public class Enumerator : IEnumerator<IKeyframe<Quaternion>>, IEnumerator, IDisposable
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

    private List<TKeyframe> container = new List<TKeyframe>();

    public TKeyframe this[int index]
    {
        get
        {
            return container[index];
        }
        set
        {
            container[index] = value;
        }
    }

    public new bool IsReadOnly => false;

    public int Count => container.Count;

    public abstract override RotateKeyframeDataType ListType { get; }

    public override Quaternion GetValue(float time)
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

        IKeyframe<Quaternion> keyframe = this[i];
        object obj;
        if (i + 1 < Count)
        {
            IKeyframe<Quaternion> keyframe2 = this[i + 1];
            obj = keyframe2;
        }
        else
        {
            obj = null;
        }

        IKeyframe<Quaternion> keyframe3 = (IKeyframe<Quaternion>)obj;
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

        container.Add(default(TKeyframe));
        int num;
        for (num = Count - 1; num > 0; num--)
        {
            TKeyframe value = container[num - 1];
            if (value.Time <= item.Time)
            {
                break;
            }

            container[num] = value;
        }

        container[num] = item;
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

    public abstract override void DecodeObject(BinaryReader reader, int count);
}