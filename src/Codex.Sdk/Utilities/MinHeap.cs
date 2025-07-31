using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;

namespace Codex.Utilities
{
    /// <summary>
    /// MinHeap provides a stack-like interface, except that objects
    /// "pushed" in arbitrary order are "popped" in order of priority, i.e.,
    /// from least to greatest as defined by the specified comparer.
    /// </summary>
    /// <remarks>
    /// Push and Pop are each O(log N). Pushing N objects and them popping
    /// them all is equivalent to performing a heap sort and is O(N log N).
    /// </remarks>
    public class MinHeap<T> : HeapBase<T>
    {
        public MinHeap(int capacity = DefaultCapacity, IComparer<T> comparer = null)
            : base(capacity, comparer)
        {
        }

        protected override bool ShouldBeLessThanParent(Node node) => false;

        // sift a gap at the given index down to the bottom of the heap,
        // return the resulting index
        protected override NodeSlot SiftDown(NodeSlot slot)
        {
            // Loop invariants:
            //
            //  1.  parent is the index of a gap in the logical tree
            //  2.  leftChild is
            //      (a) the index of parent's left child if it has one, or
            //      (b) a value >= _count if parent is a leaf node
            //

            while (slot.TryGetLeftChild(out var leftChild))
            {
                bool isRightBetter = leftChild.TryGetRightSibling(out var rightChild) &&
                    rightChild.IsBetterThan(leftChild, min: true);

                var bestChild = isRightBetter ? rightChild : leftChild;

                bestChild.SwapValue(slot);

                slot = bestChild;
            }

            return slot;
        }

        protected override NodeSlot SiftUp(NodeSlot slot, int boundary = 0)
        {
            while (slot.Node > boundary)
            {
                var parent = slot.Parent;
                if (slot.IsBetterThan(parent, min: true))
                {
                    slot.SwapValue(parent);
                    slot = parent;
                }
                else
                {
                    break;
                }
            }

            return slot;
        }
    }
}