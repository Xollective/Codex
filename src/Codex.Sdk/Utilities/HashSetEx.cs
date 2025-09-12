// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using CommunityToolkit.HighPerformance;

namespace System.Collections.Generic
{
    /// <summary>
    /// Hash set implementation copied from implementation in dotnet/runtime. It has been extended to allow access by index.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class HashSetEx<T> : ICollection<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        // This uses the same array-based implementation as Dictionary<TKey, TValue>.

        // Constants for serialization
        private const string CapacityName = "Capacity"; // Do not rename (binary serialization)
        private const string ElementsName = "Elements"; // Do not rename (binary serialization)
        private const string ComparerName = "Comparer"; // Do not rename (binary serialization)
        private const string VersionName = "Version"; // Do not rename (binary serialization)

        /// <summary>Cutoff point for stackallocs. This corresponds to the number of ints.</summary>
        private const int StackAllocThreshold = 100;

        /// <summary>
        /// When constructing a hashset from an existing collection, it may contain duplicates,
        /// so this is used as the max acceptable excess ratio of capacity to count. Note that
        /// this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
        /// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
        /// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
        /// </summary>
        private const int ShrinkThreshold = 3;
        private const int StartOfFreeList = -3;

        private int[]? _buckets;
        private int _bucketOffset = 1;
        private Entry[]? _entries;
#if TARGET_64BIT
        private ulong _fastModMultiplier;
#endif
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;
        private IEqualityComparer<T>? _comparer;

        #region Constructors

        public HashSetEx() : this((IEqualityComparer<T>?)null) { }

        public HashSetEx(IEqualityComparer<T>? comparer)
        {
            // For reference types, we always want to store a comparer instance, either
            // the one provided, or if one wasn't provided, the default (accessing
            // EqualityComparer<TKey>.Default with shared generics on every dictionary
            // access can add measurable overhead).  For value types, if no comparer is
            // provided, or if the default is provided, we'd prefer to use
            // EqualityComparer<TKey>.Default.Equals on every use, enabling the JIT to
            // devirtualize and possibly inline the operation.
            if (!typeof(T).IsValueType)
            {
                _comparer = comparer ?? EqualityComparer<T>.Default;
            }
            else if (comparer is not null && // first check for null to avoid forcing default comparer instantiation unnecessarily
                     comparer != EqualityComparer<T>.Default)
            {
                _comparer = comparer;
            }
        }

        public HashSetEx(int capacity) : this(capacity, null) { }

        public HashSetEx(int capacity, IEqualityComparer<T>? comparer) : this(comparer)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity > 0)
            {
                Initialize(capacity);
            }
        }

        #endregion

        #region Extended Behavior methods

        public T this[int index] => _entries[index].Value;

        public ref T ItemRefUnsafe(int index) => ref _entries[index].Value;

        public bool TryFind(T item, out int index)
        {
            index = FindItemIndex(item);
            return index >= 0;
        }

        /// <summary>
        /// Reset by just clearing fields instead of clearing arrays
        /// </summary>
        public void Reset()
        {
            int count = _count;
            if (count > 0)
            {
                // TODO: _bucketOffset += count and corresponding logic to do instant reset
                Array.Clear(_buckets);
                _count = 0;
                _freeList = -1;
                _freeCount = 0;
            }
        }

        #endregion

        #region ICollection<T> methods

        void ICollection<T>.Add(T item) => AddIfNotPresent(item, out _);

        /// <summary>Removes all elements from the <see cref="HashSetEx{T}"/> object.</summary>
        public void Clear()
        {
            int count = _count;
            if (count > 0)
            {
                Debug.Assert(_buckets != null, "_buckets should be non-null");
                Debug.Assert(_entries != null, "_entries should be non-null");

                Array.Clear(_buckets);
                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                Array.Clear(_entries, 0, count);
            }
        }

        /// <summary>Determines whether the <see cref="HashSetEx{T}"/> contains the specified element.</summary>
        /// <param name="item">The element to locate in the <see cref="HashSetEx{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSetEx{T}"/> object contains the specified element; otherwise, false.</returns>
        public bool Contains(T item) => FindItemIndex(item) >= 0;

        /// <summary>Gets the index of the item in <see cref="_entries"/>, or -1 if it's not in the set.</summary>
        private int FindItemIndex(T item)
        {
            if (_buckets != null)
            {
                Entry[]? entries = _entries;
                Debug.Assert(entries != null, "Expected _entries to be initialized");

                uint collisionCount = 0;
                IEqualityComparer<T>? comparer = _comparer;

                if (typeof(T).IsValueType && // comparer can only be null for value types; enable JIT to eliminate entire if block for ref types
                    comparer == null)
                {
                    // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                    int hashCode = item!.GetHashCode();
                    int i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                    while (i >= 0)
                    {
                        ref Entry entry = ref entries[i];
                        if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, item))
                        {
                            return i;
                        }
                        i = entry.Next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop, which means a concurrent update has happened.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
                else
                {
                    Debug.Assert(comparer is not null);
                    int hashCode = item != null ? comparer.GetHashCode(item) : 0;
                    int i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                    while (i >= 0)
                    {
                        ref Entry entry = ref entries[i];
                        if (entry.HashCode == hashCode && comparer.Equals(entry.Value, item))
                        {
                            return i;
                        }
                        i = entry.Next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop, which means a concurrent update has happened.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>Gets a reference to the specified hashcode's bucket, containing an index into <see cref="_entries"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucketRef(int hashCode)
        {
            int[] buckets = _buckets!;
#if TARGET_64BIT
            return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
#else
            return ref buckets[(uint)hashCode % (uint)buckets.Length];
#endif
        }

        public bool Remove(T item)
        {
            if (_buckets != null)
            {
                Entry[]? entries = _entries;
                Debug.Assert(entries != null, "entries should be non-null");

                uint collisionCount = 0;
                int last = -1;

                IEqualityComparer<T>? comparer = _comparer;
                Debug.Assert(typeof(T).IsValueType || comparer is not null);
                int hashCode =
                    typeof(T).IsValueType && comparer == null ? item!.GetHashCode() :
                    item is not null ? comparer!.GetHashCode(item) :
                    0;

                ref int bucket = ref GetBucketRef(hashCode);
                int i = bucket - 1; // Value in buckets is 1-based

                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];

                    if (entry.HashCode == hashCode && (comparer?.Equals(entry.Value, item) ?? EqualityComparer<T>.Default.Equals(entry.Value, item)))
                    {
                        if (last < 0)
                        {
                            bucket = entry.Next + 1; // Value in buckets is 1-based
                        }
                        else
                        {
                            entries[last].Next = entry.Next;
                        }

                        Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.Next = StartOfFreeList - _freeList;

                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        {
                            entry.Value = default!;
                        }

                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }

            return false;
        }

        /// <summary>Gets the number of elements that are contained in the set.</summary>
        public int Count => _count - _freeCount;

        /// <summary>
        /// Gets the total numbers of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity => _entries?.Length ?? 0;

        bool ICollection<T>.IsReadOnly => false;

        #endregion

        #region AlternateLookup

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on the current <see cref="HashSetEx{T}"/>
        /// using a <typeparamref name="TAlternate"/> instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TAlternate">The alternate type of instance for performing lookups.</typeparam>
        /// <returns>The created lookup instance.</returns>
        /// <exception cref="InvalidOperationException">The set's comparer is not compatible with <typeparamref name="TAlternate"/>.</exception>
        /// <remarks>
        /// The set must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternate, T}"/> with
        /// <typeparamref name="TAlternate"/> and <typeparamref name="T"/>. If it doesn't, an exception will be thrown.
        /// </remarks>
        public AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>(IAlternateEqualityComparer<TAlternate, T> comparer)
        {
            return new AlternateLookup<TAlternate>(this, comparer);
        }

        /// <summary>
        /// Provides a type that may be used to perform operations on a <see cref="HashSetEx{T}"/>
        /// using a <typeparamref name="TAlternate"/> instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TAlternate">The alternate type of instance for performing lookups.</typeparam>
        public struct AlternateLookup<TAlternate>
        {
            /// <summary>Initialize the instance. The set must have already been verified to have a compatible comparer.</summary>
            internal AlternateLookup(HashSetEx<T> set, IAlternateEqualityComparer<TAlternate, T> comparer)
            {
                Debug.Assert(set is not null);
                Set = set;
                _comparer = comparer;
            }

            /// <summary>Gets the <see cref="HashSetEx{T}"/> against which this instance performs operations.</summary>
            public HashSetEx<T> Set { get; }

            private readonly IAlternateEqualityComparer<TAlternate, T> _comparer;

            /// <summary>Gets the set's alternate comparer. The set must have already been verified as compatible.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal IAlternateEqualityComparer<TAlternate, T> GetAlternateComparer(HashSetEx<T> set) => _comparer;

            /// <summary>Adds the specified element to a set.</summary>
            /// <param name="item">The element to add to the set.</param>
            /// <returns>true if the element is added to the set; false if the element is already present.</returns>
            public bool Add(TAlternate item)
            {
                GetOrAdd(item, new(out var added));
                return added;
            }

            public ref T GetOrAdd(TAlternate item, Out<bool> added = default, Out<int> itemIndex = default)
            {
                HashSetEx<T> set = Set;
                IAlternateEqualityComparer<TAlternate, T> comparer = GetAlternateComparer(set);

                if (set._buckets == null)
                {
                    set.Initialize(0);
                }
                Debug.Assert(set._buckets != null);

                Entry[]? entries = set._entries;
                Debug.Assert(entries != null, "expected entries to be non-null");

                int hashCode;

                uint collisionCount = 0;
                ref int bucket = ref Unsafe.NullRef<int>();

                Debug.Assert(comparer is not null);
                hashCode = comparer.GetHashCode(item);
                bucket = ref set.GetBucketRef(hashCode);
                int i = bucket - 1; // Value in _buckets is 1-based
                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.HashCode == hashCode && comparer.Equals(item, entry.Value))
                    {
                        added.Set(false);
                        itemIndex.Set(i);
                        return ref entry.Value;
                    }
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop, which means a concurrent update has happened.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }

                // Invoke comparer.Map before allocating space in the collection in order to avoid corrupting
                // the collection if the operation fails.
                T mappedItem = comparer.Create(item);

                int index;
                if (set._freeCount > 0)
                {
                    index = set._freeList;
                    set._freeCount--;
                    Debug.Assert((StartOfFreeList - entries[set._freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
                    set._freeList = StartOfFreeList - entries[set._freeList].Next;
                }
                else
                {
                    int count = set._count;
                    if (count == entries.Length)
                    {
                        set.Resize();
                        bucket = ref set.GetBucketRef(hashCode);
                    }
                    index = count;
                    set._count = count + 1;
                    entries = set._entries;
                }

                {
                    ref Entry entry = ref entries![index];
                    entry.HashCode = hashCode;
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    entry.Value = mappedItem;
                    bucket = index + 1;
                    set._version++;

                    added.Set(true);
                    itemIndex.Set(index);
                    return ref entry.Value;
                }
            }

            /// <summary>Removes the specified element from a set.</summary>
            /// <param name="item">The element to remove.</param>
            /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
            public bool Remove(TAlternate item)
            {
                HashSetEx<T> set = Set;
                IAlternateEqualityComparer<TAlternate, T> comparer = GetAlternateComparer(set);

                if (set._buckets != null)
                {
                    Entry[]? entries = set._entries;
                    Debug.Assert(entries != null, "entries should be non-null");

                    uint collisionCount = 0;
                    int last = -1;

                    int hashCode = item is not null ? comparer.GetHashCode(item) : 0;

                    ref int bucket = ref set.GetBucketRef(hashCode);
                    int i = bucket - 1; // Value in buckets is 1-based

                    while (i >= 0)
                    {
                        ref Entry entry = ref entries[i];

                        if (entry.HashCode == hashCode && comparer.Equals(item, entry.Value))
                        {
                            if (last < 0)
                            {
                                bucket = entry.Next + 1; // Value in buckets is 1-based
                            }
                            else
                            {
                                entries[last].Next = entry.Next;
                            }

                            Debug.Assert((StartOfFreeList - set._freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                            entry.Next = StartOfFreeList - set._freeList;

                            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            {
                                entry.Value = default!;
                            }

                            set._freeList = i;
                            set._freeCount++;
                            return true;
                        }

                        last = i;
                        i = entry.Next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop; which means a concurrent update has happened.
                            // Break out of the loop and throw, rather than looping forever.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }

                return false;
            }

            /// <summary>Determines whether a set contains the specified element.</summary>
            /// <param name="item">The element to locate in the set.</param>
            /// <returns>true if the set contains the specified element; otherwise, false.</returns>
            public bool Contains(TAlternate item, Out<int> index = default) => !Unsafe.IsNullRef(in FindValue(item, index));

            /// <summary>Searches the set for a given value and returns the equal value it finds, if any.</summary>
            /// <param name="equalValue">The value to search for.</param>
            /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            public bool TryGetValue(TAlternate equalValue, [MaybeNullWhen(false)] out T actualValue)
            {
                ref readonly T value = ref FindValue(equalValue);
                if (!Unsafe.IsNullRef(in value))
                {
                    actualValue = value;
                    return true;
                }

                actualValue = default!;
                return false;
            }

            public ref readonly T this[TAlternate item]
            {
                get
                {
                    ref readonly var entry = ref FindValue(item);
                    if (Unsafe.IsNullRef(in entry))
                    {
                        throw new KeyNotFoundException();
                    }

                    return ref entry;
                }
            }

            /// <summary>Finds the item in the set and returns a reference to the found item, or a null reference if not found.</summary>
            internal ref readonly T FindValue(TAlternate item, Out<int> index = default)
            {
                HashSetEx<T> set = Set;
                IAlternateEqualityComparer<TAlternate, T> comparer = GetAlternateComparer(set);

                ref Entry entry = ref Unsafe.NullRef<Entry>();
                ref int i = ref index.Ensure().Value;
                if (set._buckets != null)
                {
                    Debug.Assert(set._entries != null, "expected entries to be != null");

                    int hashCode = comparer.GetHashCode(item);
                    i = set.GetBucketRef(hashCode);
                    Entry[]? entries = set._entries;
                    uint collisionCount = 0;
                    i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
                    do
                    {
                        // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                        // Test in if to drop range check for following array access
                        if ((uint)i >= (uint)entries.Length)
                        {
                            goto ReturnNotFound;
                        }

                        entry = ref entries[i];
                        if (entry.HashCode == hashCode && comparer.Equals(item, entry.Value))
                        {
                            goto ReturnFound;
                        }

                        i = entry.Next;

                        collisionCount++;
                    } while (collisionCount <= (uint)entries.Length);

                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    goto ConcurrentOperation;
                }

                goto ReturnNotFound;

            ConcurrentOperation:
                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
            ReturnFound:
                ref readonly T value = ref entry.Value;
            Return:
                return ref value;
            ReturnNotFound:
                value = ref Unsafe.NullRef<T>();
                goto Return;
            }
        }
        #endregion

        #region IEnumerable methods

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            Count == 0 ? Enumerable.Empty<T>().GetEnumerator() :
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        #endregion

        #region HashSetEx methods

        /// <summary>Adds the specified element to the <see cref="HashSetEx{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSetEx{T}"/> object; false if the element is already present.</returns>
        public AddResult Add(T item) => new(AddIfNotPresent(item, out var location), location);

        /// <summary>Searches the set for a given value and returns the equal value it finds, if any.</summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            if (_buckets != null)
            {
                int index = FindItemIndex(equalValue);
                if (index >= 0)
                {
                    actualValue = _entries![index].Value;
                    return true;
                }
            }

            actualValue = default;
            return false;
        }

        public void CopyTo(T[] array) => CopyTo(array, 0, Count);

        /// <summary>Copies the elements of a <see cref="HashSetEx{T}"/> object to an array, starting at the specified array index.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex, Count);

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(array));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            // Will the array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            Entry[]? entries = _entries;
            for (int i = 0; i < _count && count != 0; i++)
            {
                ref Entry entry = ref entries![i];
                if (entry.Next >= -1)
                {
                    array[arrayIndex++] = entry.Value;
                    count--;
                }
            }
        }

        /// <summary>Removes all elements that match the conditions defined by the specified predicate from a <see cref="HashSetEx{T}"/> collection.</summary>
        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(match));
            }

            Entry[]? entries = _entries;
            int numRemoved = 0;
            for (int i = 0; i < _count; i++)
            {
                ref Entry entry = ref entries![i];
                if (entry.Next >= -1)
                {
                    // Cache value in case delegate removes it
                    T value = entry.Value;
                    if (match(value))
                    {
                        // Check again that remove actually removed it.
                        if (Remove(value))
                        {
                            numRemoved++;
                        }
                    }
                }
            }

            return numRemoved;
        }

        /// <summary>Gets the <see cref="IEqualityComparer"/> object that is used to determine equality for the values in the set.</summary>
        public IEqualityComparer<T> Comparer
        {
            get
            {
                return _comparer ?? EqualityComparer<T>.Default;
            }
        }

        /// <summary>
        /// Similar to <see cref="Comparer"/> but surfaces the actual comparer being used to hash entries.
        /// </summary>
        internal IEqualityComparer<T> EffectiveComparer => _comparer ?? EqualityComparer<T>.Default;

        /// <summary>Ensures that this hash set can hold the specified number of elements without growing.</summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity));
            }

            int currentCapacity = _entries == null ? 0 : _entries.Length;
            if (currentCapacity >= capacity)
            {
                return currentCapacity;
            }

            if (_buckets == null)
            {
                return Initialize(capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize, forceNewHashCodes: false);
            return newSize;
        }

        private void Resize() => Resize(HashHelpers.ExpandPrime(_count), forceNewHashCodes: false);

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            // Value types never rehash
            Debug.Assert(!forceNewHashCodes || !typeof(T).IsValueType);
            Debug.Assert(_entries != null, "_entries should be non-null");
            Debug.Assert(newSize >= _entries.Length);

            var entries = new Entry[newSize];

            int count = _count;
            Array.Copy(_entries, entries, count);

            if (!typeof(T).IsValueType && forceNewHashCodes)
            {
                IEqualityComparer<T> comparer = _comparer = Comparer;

                for (int i = 0; i < count; i++)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.Next >= -1)
                    {
                        entry.HashCode = entry.Value != null ? comparer.GetHashCode(entry.Value) : 0;
                    }
                }
            }

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _buckets = new int[newSize];
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
#endif
            for (int i = 0; i < count; i++)
            {
                ref Entry entry = ref entries[i];
                if (entry.Next >= -1)
                {
                    ref int bucket = ref GetBucketRef(entry.HashCode);
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = i + 1;
                }
            }

            _entries = entries;
        }

        /// <summary>
        /// Sets the capacity of a <see cref="HashSetEx{T}"/> object to the actual number of elements it contains,
        /// rounded up to a nearby, implementation-specific value.
        /// </summary>
        public void TrimExcess() => TrimExcess(Count);

        /// <summary>
        /// Sets the capacity of a <see cref="HashSetEx{T}"/> object to the specified number of entries,
        /// rounded up to a nearby, implementation-specific value.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">Passed capacity is lower than entries count.</exception>
        public void TrimExcess(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, Count);

            int newSize = HashHelpers.GetPrime(capacity);
            Entry[]? oldEntries = _entries;
            int currentCapacity = oldEntries == null ? 0 : oldEntries.Length;
            if (newSize >= currentCapacity)
            {
                return;
            }

            int oldCount = _count;
            _version++;
            Initialize(newSize);
            Entry[]? entries = _entries;
            int count = 0;
            for (int i = 0; i < oldCount; i++)
            {
                int hashCode = oldEntries![i].HashCode; // At this point, we know we have entries.
                if (oldEntries[i].Next >= -1)
                {
                    ref Entry entry = ref entries![count];
                    entry = oldEntries[i];
                    ref int bucket = ref GetBucketRef(hashCode);
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = count + 1;
                    count++;
                }
            }

            _count = count;
            _freeCount = 0;
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
        /// greater than or equal to capacity.
        /// </summary>
        private int Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            var buckets = new int[size];
            var entries = new Entry[size];

            // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
            _freeList = -1;
            _buckets = buckets;
            _entries = entries;
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
#endif

            return size;
        }

        /// <summary>Adds the specified element to the set if it's not already contained.</summary>
        /// <param name="value">The element to add to the set.</param>
        /// <param name="location">The index into <see cref="_entries"/> of the element.</param>
        /// <returns>true if the element is added to the <see cref="HashSetEx{T}"/> object; false if the element is already present.</returns>
        private bool AddIfNotPresent(T value, out int location)
        {
            if (_buckets == null)
            {
                Initialize(0);
            }
            Debug.Assert(_buckets != null);

            Entry[]? entries = _entries;
            Debug.Assert(entries != null, "expected entries to be non-null");

            IEqualityComparer<T>? comparer = _comparer;
            int hashCode;

            uint collisionCount = 0;
            ref int bucket = ref Unsafe.NullRef<int>();

            if (typeof(T).IsValueType && // comparer can only be null for value types; enable JIT to eliminate entire if block for ref types
                comparer == null)
            {
                hashCode = value!.GetHashCode();
                bucket = ref GetBucketRef(hashCode);
                int i = bucket - 1; // Value in _buckets is 1-based

                // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, value))
                    {
                        location = i;
                        return false;
                    }
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop, which means a concurrent update has happened.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }
            else
            {
                Debug.Assert(comparer is not null);
                hashCode = value != null ? comparer.GetHashCode(value) : 0;
                bucket = ref GetBucketRef(hashCode);
                int i = bucket - 1; // Value in _buckets is 1-based
                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.HashCode == hashCode && comparer.Equals(entry.Value, value))
                    {
                        location = i;
                        return false;
                    }
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop, which means a concurrent update has happened.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }

            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                _freeCount--;
                Debug.Assert((StartOfFreeList - entries[_freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = StartOfFreeList - entries[_freeList].Next;
            }
            else
            {
                int count = _count;
                if (count == entries.Length)
                {
                    Resize();
                    bucket = ref GetBucketRef(hashCode);
                }
                index = count;
                _count = count + 1;
                entries = _entries;
            }

            {
                ref Entry entry = ref entries![index];
                entry.HashCode = hashCode;
                entry.Next = bucket - 1; // Value in _buckets is 1-based
                entry.Value = value;
                bucket = index + 1;
                _version++;
                location = index;
            }

            return true;
        }

        /// <summary>
        /// Checks if equality comparers are equal. This is used for algorithms that can
        /// speed up if it knows the other item has unique elements. I.e. if they're using
        /// different equality comparers, then uniqueness assumption between sets break.
        /// </summary>
        internal static bool EqualityComparersAreEqual(HashSetEx<T> set1, HashSetEx<T> set2) => set1.Comparer.Equals(set2.Comparer);

        /// <summary>
        /// Checks if effective equality comparers are equal. This is used for algorithms that
        /// require that both collections use identical hashing implementations for their entries.
        /// </summary>
        internal static bool EffectiveEqualityComparersAreEqual(HashSetEx<T> set1, HashSetEx<T> set2) => set1.EffectiveComparer.Equals(set2.EffectiveComparer);

        #endregion

        private struct Entry
        {
            public int HashCode;
            /// <summary>
            /// 0-based index of next entry in chain: -1 means end of chain
            /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            /// </summary>
            public int Next;
            public T Value;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly HashSetEx<T> _hashSet;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(HashSetEx<T> hashSet)
            {
                _hashSet = hashSet;
                _version = hashSet._version;
                _index = 0;
                _current = default!;
            }

            public bool MoveNext()
            {
                if (_version != _hashSet._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_hashSet._count)
                {
                    ref Entry entry = ref _hashSet._entries![_index++];
                    if (entry.Next >= -1)
                    {
                        _current = entry.Value;
                        return true;
                    }
                }

                _index = _hashSet._count + 1;
                _current = default!;
                return false;
            }

            public T Current => _current;

            public void Dispose() { }

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _hashSet._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return _current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _hashSet._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _current = default!;
            }
        }

        internal static partial class HashHelpers
        {
            public const uint HashCollisionThreshold = 100;

            // This is the maximum prime smaller than Array.MaxLength.
            public const int MaxPrimeArrayLength = 0x7FFFFFC3;

            public const int HashPrime = 101;

            // Table of prime numbers to use as hash table sizes.
            // A typical resize algorithm would pick the smallest prime number in this array
            // that is larger than twice the previous capacity.
            // Suppose our Hashtable currently has capacity x and enough elements are added
            // such that a resize needs to occur. Resizing first computes 2x then finds the
            // first prime in the table greater than 2x, i.e. if primes are ordered
            // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
            // Doubling is important for preserving the asymptotic complexity of the
            // hashtable operations such as add.  Having a prime guarantees that double
            // hashing does not lead to infinite loops.  IE, your hash function will be
            // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
            // We prefer the low computation costs of higher prime numbers over the increased
            // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
            internal static ReadOnlySpan<int> Primes =>
            [
                3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
            ];

            public static bool IsPrime(int candidate)
            {
                if ((candidate & 1) != 0)
                {
                    int limit = (int)Math.Sqrt(candidate);
                    for (int divisor = 3; divisor <= limit; divisor += 2)
                    {
                        if ((candidate % divisor) == 0)
                            return false;
                    }
                    return true;
                }
                return candidate == 2;
            }

            public static int GetPrime(int min)
            {
                if (min < 0)
                    throw new ArgumentException(nameof(min));

                foreach (int prime in Primes)
                {
                    if (prime >= min)
                        return prime;
                }

                // Outside of our predefined table. Compute the hard way.
                for (int i = (min | 1); i < int.MaxValue; i += 2)
                {
                    if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                        return i;
                }
                return min;
            }

            // Returns size of hashtable to grow to.
            public static int ExpandPrime(int oldSize)
            {
                int newSize = 2 * oldSize;

                // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
                {
                    Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
                    return MaxPrimeArrayLength;
                }

                return GetPrime(newSize);
            }

            /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
            /// <remarks>This should only be used on 64-bit.</remarks>
            public static ulong GetFastModMultiplier(uint divisor) =>
                ulong.MaxValue / divisor + 1;

            /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
            /// <remarks>This should only be used on 64-bit.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint FastMod(uint value, uint divisor, ulong multiplier)
            {
                // We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
                // which allows to avoid the long multiplication if the divisor is less than 2**31.
                Debug.Assert(divisor <= int.MaxValue);

                // This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
                // is faster than BigMul currently because we only need the high bits.
                uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

                Debug.Assert(highbits == value % divisor);
                return highbits;
            }
        }

        public enum ExceptionResource
        {
            ArgumentOutOfRange_NeedNonNegNum,
            Arg_ArrayPlusOffTooSmall,
            Argument_InvalidArrayType,
            Serialization_MissingKeys,
            Serialization_NullKey,
            NotSupported_KeyCollectionSet,
            NotSupported_ValueCollectionSet,
            InvalidOperation_EnumFailedVersion,
            InvalidOperation_EnumOpCantHappen,
            InvalidOperation_EmptyCollection,
            InvalidOperation_IncompatibleComparer
        }

        public class ThrowHelper
        {
            public static void ThrowArgumentNullException(string argument)
            {
                throw new ArgumentNullException(argument);
            }

            public static void ThrowArgumentOutOfRangeException(string argument)
            {
                throw new ArgumentOutOfRangeException(argument);
            }

            public static void ThrowArgumentException(ExceptionResource resource)
            {
                throw new ArgumentException(resource.ToString());
            }

            public static void ThrowSerializationException(ExceptionResource resource)
            {
                throw new SerializationException(resource.ToString());
            }

            public static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
            {
                throw new InvalidOperationException("Concurrent operations are not supported.");
            }

            public static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
            {
                throw new InvalidOperationException("Enumeration failed due to version mismatch.");
            }

            public static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
            {
                throw new InvalidOperationException("Enumeration operation cannot happen.");
            }

            public static void ThrowInvalidOperationException(ExceptionResource resource)
            {
                throw new InvalidOperationException(resource.ToString());
            }
        }
    }

    public record struct AddResult(bool Added, int Index)
    {
        public static implicit operator bool(AddResult r) => r.Added;
        public static implicit operator int(AddResult r) => r.Index;

        public bool IsAdded(out int index) => Out.TryVar(Added, out index, Index);
    }

#if !NET9_0_OR_GREATER
    /// <summary>
    /// Implemented by an <see cref="IEqualityComparer{T}"/> to support comparing
    /// a <typeparamref name="TAlternate"/> instance with a <typeparamref name="T"/> instance.
    /// </summary>
    /// <typeparam name="TAlternate">The alternate type to compare.</typeparam>
    /// <typeparam name="T">The type to compare.</typeparam>
    public interface IAlternateEqualityComparer<in TAlternate, T>
    {
        /// <summary>Determines whether the specified <paramref name="alternate"/> equals the specified <paramref name="other"/>.</summary>
        /// <param name="alternate">The instance of type <typeparamref name="TAlternate"/> to compare.</param>
        /// <param name="other">The instance of type <typeparamref name="T"/> to compare.</param>
        /// <returns><see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.</returns>
        bool Equals(TAlternate alternate, T other);

        /// <summary>Returns a hash code for the specified alternate instance.</summary>
        /// <param name="alternate">The instance of type <typeparamref name="TAlternate"/> for which to get a hash code.</param>
        /// <returns>A hash code for the specified instance.</returns>
        /// <remarks>
        /// This interface is intended to be implemented on a type that also implements <see cref="IEqualityComparer{T}"/>.
        /// The result of this method should return the same hash code as would invoking the <see cref="IEqualityComparer{T}.GetHashCode"/>
        /// method on any <typeparamref name="T"/> for which <see cref="Equals(TAlternate, T)"/>
        /// returns <see langword="true"/>.
        /// </remarks>
        int GetHashCode(TAlternate alternate);

        /// <summary>
        /// Creates a <typeparamref name="T"/> that is considered by <see cref="Equals(TAlternate, T)"/> to be equal
        /// to the specified <paramref name="alternate"/>.
        /// </summary>
        /// <param name="alternate">The instance of type <typeparamref name="TAlternate"/> for which an equal <typeparamref name="T"/> is required.</param>
        /// <returns>A <typeparamref name="T"/> considered equal to the specified <paramref name="alternate"/>.</returns>
        T Create(TAlternate alternate);
    }
#endif
}