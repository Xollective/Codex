using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Codex.Utilities
{
    /// <summary>
    /// PriorityQueue provides a stack-like interface, except that objects
    /// "pushed" in arbitrary order are "popped" in order of priority, i.e.,
    /// from least to greatest as defined by the specified comparer.
    /// </summary>
    /// <remarks>
    /// Push and Pop are each O(log N). Pushing N objects and them popping
    /// them all is equivalent to performing a heap sort and is O(N log N).
    /// </remarks>
    public class TopMinHeap<T> : MinHeap<T>
    {
        public int Capacity { get; }

        private bool _inverted = true;

        public Action<T> OnEvicted { get; set; }

        public TopMinHeap(int capacity, IComparer<T> comparer = null)
            : base(capacity + 1, comparer)
        {
            Capacity = capacity;
        }

        protected override OrderResult Compare(T left, T right)
        {
            return _inverted
                ? base.Compare(right, left)
                : base.Compare(left, right);
        }

        protected override NodeSlot MinSlot()
        {
            SetInverted(false);
            return base.MinSlot();
        }

        protected override NodeSlot MaxSlot()
        {
            if (_inverted)
            {
                return Slot(0);
            }

            return base.MaxSlot();
        }

        public bool TryPush(in T value)
        {
            SetInverted(true);

            if (Count >= Capacity)
            {
                var maxSlot = RootSlot();
                if (base.Compare(maxSlot.Value, value).IsRightLesser)
                {
                    OnEvicted?.Invoke(maxSlot.Value);
                    ChangeRoot(maxSlot, value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                base.Push(value);
                return true;
            }
        }

        public override void Push(in T value)
        {
            TryPush(value);
        }

        public void SetInverted(bool inverted)
        {
            if (_inverted != inverted)
            {
                _inverted = inverted;
                ChangeCompare();
            }
        }
    }
}