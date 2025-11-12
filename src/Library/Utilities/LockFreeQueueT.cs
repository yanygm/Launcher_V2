namespace Launcher.Library.Utilities
{
    public sealed class LockFreeQueue<T>
    where T : class
    {
        private SingleLinkNode mHead;

        private SingleLinkNode mTail;

        public T Next
        {
            get
            {
                return mHead.Next == null ? default : mHead.Next.Item;
            }
        }

        public LockFreeQueue()
        {
            mHead = new LockFreeQueue<T>.SingleLinkNode();
            mTail = mHead;
        }

        private static bool CompareAndExchange(ref SingleLinkNode pLocation, SingleLinkNode pComparand, SingleLinkNode pNewValue)
        {
            return pComparand == Interlocked.CompareExchange(ref pLocation, pNewValue, pComparand);
        }

        public bool Dequeue(out T pItem)
        {
            bool flag;
            pItem = default;
            SingleLinkNode singleLinkNode = null;
            bool flag1 = false;
            while (true)
            {
                if (!flag1)
                {
                    singleLinkNode = mHead;
                    SingleLinkNode singleLinkNode1 = mTail;
                    SingleLinkNode next = singleLinkNode.Next;
                    if (singleLinkNode == mHead)
                    {
                        if (singleLinkNode != singleLinkNode1)
                        {
                            pItem = next.Item;
                            flag1 = LockFreeQueue<T>.CompareAndExchange(ref mHead, singleLinkNode, next);
                        }
                        else if (next != null)
                        {
                            LockFreeQueue<T>.CompareAndExchange(ref mTail, singleLinkNode1, next);
                        }
                        else
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                else
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        public T Dequeue()
        {
            T t;
            Dequeue(out t);
            return t;
        }

        public void Enqueue(T pItem)
        {
            SingleLinkNode singleLinkNode = null;
            SingleLinkNode singleLinkNode1 = new LockFreeQueue<T>.SingleLinkNode()
            {
                Item = pItem
            };
            bool flag = false;
            while (!flag)
            {
                singleLinkNode = mTail;
                SingleLinkNode next = singleLinkNode.Next;
                if (mTail == singleLinkNode)
                {
                    if (next != null)
                    {
                        LockFreeQueue<T>.CompareAndExchange(ref mTail, singleLinkNode, next);
                    }
                    else
                    {
                        flag = LockFreeQueue<T>.CompareAndExchange(ref mTail.Next, null, singleLinkNode1);
                    }
                }
            }
            LockFreeQueue<T>.CompareAndExchange(ref mTail, singleLinkNode, singleLinkNode1);
        }

        private class SingleLinkNode
        {
            public SingleLinkNode Next;

            public T Item;

            public SingleLinkNode()
            {
            }
        }
    }
}