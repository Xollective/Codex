// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file is mostly a copy of ArraySortHelper<T> used by the BCL to sort arrays/spans.
// This is needed to allow some augmentations and to add support for sorting spans on full framework.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Codex.Sdk.Utilities;

#nullable disable
#nullable enable annotations

namespace BuildXL.Utilities.Collections
{
    /// <summary>
    /// Extension methods for sorting spans
    /// </summary>
    public static class ArraySortHelper
    {
        /// <nodoc/>
        public static ArraySpan<T, TArray> ToSpan<TArray, T>(this TArray array, ITypeBox<T> type = default)
            where TArray : IArray<T>
        {
            return new(array, 0, array.Length);
        }

        /// <nodoc/>
        public static int BinarySearch<TArray, T>(this TArray array, T value, IComparer<T>? comparer = null)
            where TArray : IReadOnlyArraySlim<T>
        {
            comparer ??= Comparer<T>.Default;
            return InternalBinarySearch(array, 0, array.Length, value, comparer);
        }

        /// <nodoc/>
        public static int BinarySearch<TArray, T>(this TArray array, int index, int length, T value, IComparer<T>? comparer = null)
            where TArray : IReadOnlyArraySlim<T>
        {
            comparer ??= Comparer<T>.Default;
            return InternalBinarySearch(array, index, length, value, comparer);
        }

        /// <nodoc/>
        public static void Sort<TArray, T>(this TArray keys, IComparer<T>? comparer = null)
            where TArray : IArray<T>
        {
            comparer ??= Comparer<T>.Default;
            ArraySortHelper<TArray, T>.IntrospectiveSort(new(keys, 0, keys.Length), comparer);
        }

        internal static int InternalBinarySearch<TArray, T>(TArray array, int index, int length, T value, IComparer<T> comparer)
            where TArray : IReadOnlyArraySlim<T>
        {
            Debug.Assert(array != null, "Check the arguments in the caller!");
            Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(array[i], value);

                if (order == 0) return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
    }

    public interface ISpan<T, TSelf> : IArray<T>
        where TSelf : ISpan<T, TSelf>
    {
        TSelf Slice(int start, int length);
    }

    public record struct ArraySpan<T, TArray>(TArray array, int Start, int Length) : ISpan<T, ArraySpan<T, TArray>>
        where TArray : IArray<T>
    {
        public ref  T this[int index]
        {
            get => ref array[index - Start];
        }

        public ArraySpan<T, TArray> Slice(int start, int length)
        {
            return new(array, start + Start, length);
        }
    }

    /// <summary>
    /// Helper class for sorting spans
    /// </summary>
    public static class ArraySortHelper<TArray, T>
        where TArray : IArray<T>
    {
        internal static void Swap(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        // This is the threshold where Introspective sort switches to Insertion sort.
        // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
        // Large value types may benefit from a smaller number.
        internal const int IntrosortSizeThreshold = 16;

        private static void SwapIfGreater(ArraySpan<T, TArray> keys, IComparer<T> comparer, int i, int j)
        {
            Debug.Assert(i != j);

            if (comparer.Compare(keys[i], keys[j]) > 0)
            {
                T key = keys[i];
                keys[i] = keys[j];
                keys[j] = key;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ArraySpan<T, TArray> a, int i, int j)
        {
            Debug.Assert(i != j);

            T t = a[i];
            a[i] = a[j];
            a[j] = t;
        }

        internal static void IntrospectiveSort(ArraySpan<T, TArray> keys, IComparer<T> comparer)
        {
            Debug.Assert(comparer != null);

            if (keys.Length > 1)
            {
                IntroSort(keys, 2 * (Log2((uint)keys.Length) + 1), comparer);
            }
        }

        private static int Log2(uint value)
        {
#if NETCOREAPP
            return BitOperations.Log2(value);
#else
            return Bits.FindLowestBitSet(Bits.HighestBitSet(value));
#endif
        }

        private static void IntroSort(ArraySpan<T, TArray> keys, int depthLimit, IComparer<T> comparer)
        {
            Debug.Assert(keys.Length > 0);
            Debug.Assert(depthLimit >= 0);
            Debug.Assert(comparer != null);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= IntrosortSizeThreshold)
                {

                    if (partitionSize == 2)
                    {
                        SwapIfGreater(keys, comparer, 0, 1);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreater(keys, comparer, 0, 1);
                        SwapIfGreater(keys, comparer, 0, 2);
                        SwapIfGreater(keys, comparer, 1, 2);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize), comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize), comparer);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize), comparer);

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                int start = p + 1;
                IntroSort(keys.Slice(start, partitionSize - start), depthLimit, comparer);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(ArraySpan<T, TArray> keys, IComparer<T> comparer)
        {
            Debug.Assert(keys.Length >= IntrosortSizeThreshold);
            Debug.Assert(comparer != null);

            int hi = keys.Length - 1;

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = hi >> 1;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreater(keys, comparer, 0, middle);  // swap the low with the mid point
            SwapIfGreater(keys, comparer, 0, hi);   // swap the low with the high
            SwapIfGreater(keys, comparer, middle, hi); // swap the middle with the high

            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0) ;
                while (comparer.Compare(pivot, keys[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(keys, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(keys, left, hi - 1);
            }
            return left;
        }

        private static void HeapSort(ArraySpan<T, TArray> keys, IComparer<T> comparer)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(keys.Length > 0);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, i, n, comparer);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(keys, 0, i - 1);
                DownHeap(keys, 1, i - 1, comparer);
            }
        }

        private static void DownHeap(ArraySpan<T, TArray> keys, int i, int n, IComparer<T> comparer)
        {
            Debug.Assert(comparer != null);

            T d = keys[i - 1];
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && comparer.Compare(keys[child - 1], keys[child]) < 0)
                {
                    child++;
                }

                if (!(comparer.Compare(d, keys[child - 1]) < 0))
                    break;

                keys[i - 1] = keys[child - 1];
                i = child;
            }

            keys[i - 1] = d;
        }

        private static void InsertionSort(ArraySpan<T, TArray> keys, IComparer<T> comparer)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                T t = keys[i + 1];

                int j = i;
                while (j >= 0 && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    j--;
                }

                keys[j + 1] = t;
            }
        }
    }
}