using System;
using System.Threading;

namespace KartRider.Common.Utilities
{
	public sealed class LockFreeQueue<T>
	where T : class
	{
		private LockFreeQueue<T>.SingleLinkNode mHead;

		private LockFreeQueue<T>.SingleLinkNode mTail;

		public T Next
		{
			get
			{
				return (this.mHead.Next == null ? default(T) : this.mHead.Next.Item);
			}
		}

		public LockFreeQueue()
		{
			this.mHead = new LockFreeQueue<T>.SingleLinkNode();
			this.mTail = this.mHead;
		}

		private static bool CompareAndExchange(ref LockFreeQueue<T>.SingleLinkNode pLocation, LockFreeQueue<T>.SingleLinkNode pComparand, LockFreeQueue<T>.SingleLinkNode pNewValue)
		{
			return pComparand == Interlocked.CompareExchange<LockFreeQueue<T>.SingleLinkNode>(ref pLocation, pNewValue, pComparand);
		}

		public bool Dequeue(out T pItem)
		{
			bool flag;
			pItem = default(T);
			LockFreeQueue<T>.SingleLinkNode singleLinkNode = null;
			bool flag1 = false;
			while (true)
			{
				if (!flag1)
				{
					singleLinkNode = this.mHead;
					LockFreeQueue<T>.SingleLinkNode singleLinkNode1 = this.mTail;
					LockFreeQueue<T>.SingleLinkNode next = singleLinkNode.Next;
					if (singleLinkNode == this.mHead)
					{
						if (singleLinkNode != singleLinkNode1)
						{
							pItem = next.Item;
							flag1 = LockFreeQueue<T>.CompareAndExchange(ref this.mHead, singleLinkNode, next);
						}
						else if (next != null)
						{
							LockFreeQueue<T>.CompareAndExchange(ref this.mTail, singleLinkNode1, next);
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
			this.Dequeue(out t);
			return t;
		}

		public void Enqueue(T pItem)
		{
			LockFreeQueue<T>.SingleLinkNode singleLinkNode = null;
			LockFreeQueue<T>.SingleLinkNode singleLinkNode1 = new LockFreeQueue<T>.SingleLinkNode()
			{
				Item = pItem
			};
			bool flag = false;
			while (!flag)
			{
				singleLinkNode = this.mTail;
				LockFreeQueue<T>.SingleLinkNode next = singleLinkNode.Next;
				if (this.mTail == singleLinkNode)
				{
					if (next != null)
					{
						LockFreeQueue<T>.CompareAndExchange(ref this.mTail, singleLinkNode, next);
					}
					else
					{
						flag = LockFreeQueue<T>.CompareAndExchange(ref this.mTail.Next, null, singleLinkNode1);
					}
				}
			}
			LockFreeQueue<T>.CompareAndExchange(ref this.mTail, singleLinkNode, singleLinkNode1);
		}

		private class SingleLinkNode
		{
			public LockFreeQueue<T>.SingleLinkNode Next;

			public T Item;

			public SingleLinkNode()
			{
			}
		}
	}
}