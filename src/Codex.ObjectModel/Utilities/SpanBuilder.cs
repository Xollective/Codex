// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This class is taken from System.Collections.Generic.ArrayBuilder in System.Private.CoreLib

using System;
using System.Diagnostics;
using System.Diagnostics.ContractsLight;
using System.Xml;
using Codex.Utilities.Serialization;

namespace Codex.Utilities
{
    /// <summary>
    /// Helper type for avoiding allocations while building arrays.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public ref struct SpanBuilder<T>
    {
        private readonly Span<T> _array; // Starts out null, initialized on first Add.
        private int _count; // Number of items into _array we're using.

        public SpanBuilder(Span<T> initialArray)
        {
            _array = initialArray;
        }

        /// <summary>
        /// Gets the number of items this instance can store without re-allocating,
        /// or 0 if the backing array is <c>null</c>.
        /// </summary>
        public int Capacity => _array.Length;

        /// <summary>
        /// Gets the number of items in the array currently in use.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets the number of items in the array currently in use.
        /// </summary>
        public int Length => _count;

        /// <summary>
        /// Gets or sets the item at a certain index in the array.
        /// </summary>
        /// <param name="index">The index into the array.</param>
        public ref T this[int index]
        {
            get
            {
                Extent.CheckBounds(index, _count);
                return ref _array![index];
            }
        }

        /// <summary>
        /// Adds an item to the backing array, resizing it if necessary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            AddAndGet(item);
        }

        public ref T AddAndGet(T item)
        {
            if (_count == Capacity)
            {
                EnsureCapacity(_count + 1);
            }

            return ref UncheckedAdd(item);
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            EnsureCapacity(_count + items.Length);
            var priorCount = _count;
            _count += items.Length;
            items.CopyTo(Span.Slice(priorCount));
        }

        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            CheckRange(index);

            _array[index] = item;
            _count++;
        }

        private void CheckRange(int index)
        {
            Contract.Check((uint)index < (uint)_count)?.Assert($"{index} out of range. List length = {_count}");
        }

        public bool TryAdd(T value)
        {
            if (_count < Capacity)
            {
                UncheckedAdd(value);
                return true;
            }

            return false;
        }

        public Span<T> Span => _array.Slice(0, _count);

        /// <summary>
        /// Makes the instance empty WITHOUT clearing the contents
        /// </summary>
        public void Reset()
        {
            _count = 0;
        }

        /// <summary>
        /// Makes the instance empty AND clears the contents
        /// </summary>
        public void Clear()
        {
            Span.Clear();
            Reset();
        }

        public ref T First => ref Span[0];

        public ref T Last => ref Span[_count - 1];

        /// <summary>
        /// Creates an array from the contents of this builder.
        /// </summary>
        public T[] ToArray()
        {
            if (_count == 0)
            {
                return Array.Empty<T>();
            }

            return Span.ToArray();
        }

        /// <summary>
        /// Adds an item to the backing array, without checking if there is room.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use this method if you know there is enough space in the <see cref="ArrayBuilder{T}"/>
        /// for another item, and you are writing performance-sensitive code.
        /// </remarks>
        public ref T UncheckedAdd(T item)
        {
            Debug.Assert(_count < Capacity);

            ref var slot = ref _array![_count++];
            slot = item;
            return ref slot;
        }

        public void SetLength(int newLength)
        {
            EnsureCapacity(newLength);
            _count = newLength;
        }

        public void EnsureCapacity(int minimum)
        {
            CheckRange(minimum);
        }

        public static implicit operator SpanBuilder<T>(Span<T> span)
        {
            return new SpanBuilder<T>(span);
        }
    }
}