using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;

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
    public abstract class HeapBase<T>
    {
        //
        // The _heap array represents a binary tree with the "shape" property.
        // If we number the nodes of a binary tree from left-to-right and top-
        // to-bottom as shown,
        //
        //             0
        //           /   \
        //          /     \
        //         1       2
        //       /  \     / \
        //      3    4   5   6
        //     /\    /
        //    7  8  9
        //
        // The shape property means that there are no gaps in the sequence of
        // numbered nodes, i.e., for all N > 0, if node N exists then node N-1
        // also exists. For example, the next node added to the above tree would
        // be node 10, the right child of node 4.
        //
        // Because of this constraint, we can easily represent the "tree" as an
        // array, where node number == array index, and parent/child relationships
        // can be calculated instead of maintained explicitly. For example, for
        // any node N > 0, the parent of N is at array index (N - 1) / 2.
        //
        // In addition to the above, the first _count members of the _heap array
        // compose a "heap", meaning each child node is greater than or equal to
        // its parent node; thus, the root node is always the minimum (i.e., the
        // best match for the specified style, weight, and stretch) of the nodes
        // in the heap.
        //
        // Initially _count < 0, which means we have not yet constructed the heap.
        // On the first call to MoveNext, we construct the heap by "pushing" all
        // the nodes into it. Each successive call "pops" a node off the heap
        // until the heap is empty (_count == 0), at which time we've reached the
        // end of the sequence.
        //

        #region constructors

        public HeapBase(int capacity = DefaultCapacity, IComparer<T> comparer = null)
        {
            _heap = new T[capacity > 0 ? capacity : DefaultCapacity];
            _count = 0;
            Comparer = NotNull(comparer ?? Comparer<T>.Default);
        }

        protected static IComparer<T> NotNull(IComparer<T> comparer)
        {
            return comparer ?? Comparer<T>.Default;
        }

        #endregion

        #region public members

        /// <summary>
        /// Gets the number of items in the priority queue.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        public T Min => MinSlot().Value;

        public IEnumerable<T> GetMinConsumingEnumerable()
        {
            while (TryGetMin(out var value))
            {
                yield return value;
            }
        }

        public bool TryGetMin(out T value, bool pop = true)
        {
            return TryGet(min: true, out value, pop: pop);
        }

        protected bool TryGet(bool min, out T value, bool pop = true)
        {
            if (Count > 0)
            {
                var slot = min ? MinSlot() : MaxSlot();
                value = slot.Value;
                if (pop) PopNode(slot);
                return true;
            }

            value = default;
            return false;
        }

        protected virtual NodeSlot RootSlot()
        {
            EnsureHeap();
            return Slot(0);
        }

        protected virtual NodeSlot MinSlot()
        {
            return RootSlot();
        }

        protected virtual NodeSlot MaxSlot()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds an object to the priority queue.
        /// </summary>
        public virtual void Push(in T value)
        {
            // Increase the size of the array if necessary.
            if (_count == _heap.Length)
            {
                Array.Resize<T>(ref _heap, _count * 2);
            }

            // A common usage is to Push N items, then Pop them.  Optimize for that
            // case by treating Push as a simple append until the first Top or Pop,
            // which establishes the heap property.  After that, Push needs
            // to maintain the heap property.
            if (_isHeap)
            {
                var slot = Slot(_count++);
                slot.Value = value;
                SiftUp(slot, 0);
            }
            else
            {
                _heap[_count++] = value;
            }

        }

        protected void EnsureHeap(bool requireNonEmpty = true)
        {
            Debug.Assert(!requireNonEmpty || _count != 0);
            if (!_isHeap)
            {
                Heapify();
            }
        }

        protected NodeSlot Slot(Node node)
        {
            return node.GetSlot(this);
        }

        private void PopNode(NodeSlot node)
        {
            EnsureHeap();

            if (_count > 0)
            {
                --_count;

                // discarding the root creates a gap at position 0.  We fill the
                // gap with the item x from the last position, after first sifting
                // the gap to a position where inserting x will maintain the
                // heap property.  This is done in two phases - SiftDown and SiftUp.
                //
                // The one-phase method found in many textbooks does 2 comparisons
                // per level, while this method does only 1.  The one-phase method
                // examines fewer levels than the two-phase method, but it does
                // more comparisons unless x ends up in the top 2/3 of the tree.
                // That accounts for only n^(2/3) items, and x is even more likely
                // to end up near the bottom since it came from the bottom in the
                // first place.  Overall, the two-phase method is noticeably better.
                var tail = Slot(_count);
                var tailValue = tail.Value;
                tail.Value = default;

                ChangeRoot(node, tailValue);
            }
        }

        protected void ChangeRoot(NodeSlot node, T value)
        {
            var root = Slot(0);
            if (node.Node != root.Node) node.SwapValue(root);
            var slot = SiftDown(root);
            slot.Value = value;
            SiftUp(slot, 0);
        }

        #endregion

        #region private members

        // sift a gap at the given index down to the bottom of the heap,
        // return the resulting index
        protected abstract NodeSlot SiftDown(NodeSlot slot);

        // sift a gap at index up until it reaches the correct position for x,
        // or reaches the given boundary.  Place x in the resulting position.
        protected abstract NodeSlot SiftUp(NodeSlot slot, int boundary);

        // Establish the heap property:  _heap[k] >= _heap[HeapParent(k)], for 0<k<_count
        // Do this "bottom up", by iterating backwards.  At each iteration, the
        // property inductively holds for k >= HeapLeftChild(i)+2;  the body of
        // the loop extends the property to the children of position i (namely
        // k=HLC(i) and k=HLC(i)+1) by lifting item x out from position i, sifting
        // the resulting gap down to the bottom, then sifting it back up (within
        // the subtree under i) until finding x's rightful position.
        //
        // Iteration i does work proportional to the height (distance to leaf)
        // of the node at position i.  Half the nodes are leaves with height 0;
        // there's nothing to do for these nodes, so we skip them by initializing
        // i to the last non-leaf position.  A quarter of the nodes have height 1,
        // an eigth have height 2, etc. so the total work is ~ 1*n/4 + 2*n/8 +
        // 3*n/16 + ... = O(n).  This is much cheaper than maintaining the
        // heap incrementally during the "Push" phase, which would cost O(n*log n).
        private void Heapify()
        {
            if (!_isHeap)
            {
                for (int i = _count / 2 - 1; i >= 0; --i)
                {
                    // we use a two-phase method for the same reason Pop does
                    var start = Slot(i);
                    var slot = SiftDown(start);
                    SiftUp(slot, i);
                }

                _isHeap = true;
            }
        }

        protected abstract bool ShouldBeLessThanParent(Node node);

        private DebugNode[] D => new RangeList(new(0, _count)).SelectArray(i => new DebugNode(this, i));

        private record struct DebugNode(HeapBase<T> Owner, int Index)
        {
            public override string ToString()
            {
                var slot = Owner.Slot(Index);

                if (Index == 0)
                {
                    return $"Root ({slot.Value})";
                }

                if (slot.IsMinLevel)
                {
                    string m = "min";
                    bool isCorrect = slot.Compare(slot.Parent.Value).IsLeftLesser;
                    return $"{isCorrect} {m}({slot.Value}, {slot.Parent.Value})";
                }
                else
                {
                    string m = "max";
                    bool isCorrect = slot.Compare(slot.Parent.Value).IsLeftGreater;
                    return $"{isCorrect} {m}({slot.Value}, {slot.Parent.Value})";
                }
            }
        }

        public void Verify()
        {
            if (!_isHeap) return;

            for (int i = 1; i < _count; i++)
            {
                var slot = Slot(i);
                if (ShouldBeLessThanParent(slot.Node))
                {
                    Contract.Assert(slot.Compare(slot.Parent.Value).IsLeftLesser);
                }
                else
                {
                    Contract.Assert(slot.Compare(slot.Parent.Value).IsLeftGreater);
                }
            }
        }

        public void Clear()
        {
            Reset();
            _heap.AsSpan().Clear();
        }

        public void Reset()
        {
            _isHeap = false;
            _count = 0;
        }

        public void ChangeCompare()
        {
            _isHeap = false;
        }

        protected readonly ref struct NodeSlot
        {
            public bool IsValid => Owner != null;

            public readonly HeapBase<T> Owner;
            public T[] Heap => Owner._heap;
            public readonly ref T Value;
            public readonly Node Node;

            public NodeSlot(HeapBase<T> owner, Node node)
            {
                Owner = owner;
                Value = ref Heap[node.Index];
                Node = node;
            }

            public override string ToString()
            {
                return $"Index={Node} Value={Value} IsMinLevel={IsMinLevel}";
            }

            public bool IsMinLevel => Owner.ShouldBeLessThanParent(Node);

            public NodeSlot Parent => new(Owner, Node.Parent);

            public NodeSlot LeftChild => new(Owner, Node.LeftChild);

            public bool TryGetLeftChild(out NodeSlot leftChild)
            {
                var leftChildNode = Node.LeftChild;
                return TryGetRef(leftChildNode, Owner._count, out leftChild);
            }

            public OrderResult Compare(in T otherValue)
            {
                var order = Owner.Compare(Value, otherValue);
                return order;
            }

            public bool IsBetterThan(NodeSlot other, bool min)
            {
                var order = Owner.Compare(Value, other.Value);
                return min ? order.IsLeftLesser : order.IsLeftGreater;
            }

            public bool TryGetGrandParent(out NodeSlot result, int min = -1)
            {
                var grandParent = Node.GrandParent;
                int max = Node;
                if (grandParent <= min) max = 0; 

                return TryGetRef(grandParent, max, out result);
            }

            public bool TryGetRightSibling(out NodeSlot result)
            {
                var rightSibling = Node.RightSibling;
                return TryGetRef(rightSibling, Owner._count, out result);
            }

            private bool TryGetRef(Node node, int count, out NodeSlot result)
            {
                if (node.IsValid(count))
                {
                    result = node.GetSlot(Owner);
                    return true;
                }

                result = default;
                return false;
            }

            public void SwapValue(NodeSlot other)
            {
                var tmp = other.Value;
                other.Value = Value;
                Value = tmp;
            }
        }

        protected readonly record struct Node(int Index)
        {
            public bool IsRoot => Index == 0;
            public bool IsMinLevel => (BitOperations.Log2((uint)Index + 1) & 1) == 0;
            public Node Parent => (Index - 1) / 2;
            public Node GrandParent => unchecked((int)(((uint)(Index - 3)) / 4));
            public Node LeftmostGrandChild => (Index * 4) + 3;
            public Node LeftChild => (Index * 2) + 1;
            public Node RightSibling => Index + 1;
            public Node RightmostCousin => Index + 3;
            public Node ValidRightmostCousin(int count) => Math.Min(count - 1, RightmostCousin);

            public NodeSlot GetSlot(HeapBase<T> owner) => new NodeSlot(owner, this);

            public bool IsValid(int count) => unchecked((uint)Index < (uint)count);

            public static implicit operator Node(int value) => new(value);
            public static implicit operator int(Node node) => node.Index;

            public override string ToString()
            {
                return Index.ToString();
            }
        }

        protected virtual OrderResult Compare(T left, T right) => Comparer.Compare(left, right);

        private T[] _heap;
        private int _count;
        private readonly IComparer<T> Comparer;
        private bool _isHeap;
        protected const int DefaultCapacity = 6;

        #endregion
    }
}