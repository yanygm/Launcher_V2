using System;
using System.Collections.Generic;

namespace KartRider.Common.Utilities
{
    /// <summary>
    /// 线程安全的 byte 集合，语义上等价于原来房间里的 List&lt;byte&gt; CloseSlotIds
    /// （Add/Remove/Contains/Count/foreach/Clear），但枚举走快照，
    /// 因此不会出现"另一个线程正在 Add，这边 foreach 抛 Collection was modified"。
    /// </summary>
    public sealed class SlotIdSet
    {
        private readonly object _gate = new object();
        private readonly List<byte> _items = new List<byte>(8);

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _items.Count;
                }
            }
        }

        public void Add(byte value)
        {
            lock (_gate)
            {
                if (!_items.Contains(value))
                {
                    _items.Add(value);
                }
            }
        }

        public bool Remove(byte value)
        {
            lock (_gate)
            {
                return _items.Remove(value);
            }
        }

        public bool Contains(byte value)
        {
            lock (_gate)
            {
                return _items.Contains(value);
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _items.Clear();
            }
        }

        /// <summary>返回当前内容的快照，可安全枚举。</summary>
        public byte[] Snapshot()
        {
            lock (_gate)
            {
                return _items.ToArray();
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)Snapshot()).GetEnumerator();
        }
    }
}
