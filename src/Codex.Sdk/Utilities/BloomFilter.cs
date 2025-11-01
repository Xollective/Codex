// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.ContractsLight;
using System.Numerics;

namespace BuildXL.Utilities.Collections
{
    using static BloomFilter;

    public interface IBloomSlots<TBits>
        where TBits : IBloomSlots<TBits>
    {

        TBits Initialize(int slotCount);

        bool this[int index] { get; set; }

        int GetAndSet(int index, bool value);

        void Clear();
    }

    public readonly record struct CountingBits(byte log2BitWidth) : IBloomSlots<CountingBits>
    {
        private readonly ulong[] bits;

        public int SlotCount
        {
            init
            {
                var bitCount = value * log2BitWidth;
                bits = new ulong[NumberUtils.DivCeiling(bitCount, 64)];
            }
        }

        private readonly ulong Mask = (ulong)(1 << log2BitWidth) - 1;
        private readonly int IndexShift = 6 - log2BitWidth;
        private readonly int IndexShiftMask = (1 << (6 - log2BitWidth)) - 1;

        public bool this[int index]
        {
            get
            {
                var result = bits[index >> IndexShift];
                result = result >> (index & IndexShiftMask);
                return (Mask & result) != 0;
            }

            set
            {
                long increment = (value ? 1L : -1L) << (index & IndexShiftMask);
                ref var l = ref bits[index >> IndexShift];
                Interlocked.Add(ref l, (ulong)increment);
            }
        }

        public CountingBits Initialize(int slotCount)
        {
            return this with
            {
                SlotCount = slotCount
            };
        }

        public int GetAndSet(int index, bool value)
        {
            var shift = index & IndexShiftMask;
            long increment = (value ? 1L : -1L) << shift;
            ref var l = ref bits[index >> IndexShift];
            var result = Interlocked.Add(ref l, (ulong)increment) - (ulong)increment;
            result = result >> shift;
            return (int)(Mask & result);
        }

        public void Clear()
        {
            Array.Clear(bits);
        }
    }

    public class CountingBloomFilter(Parameters parameters, byte log2BitWidth)
        : BloomFilter<CountingBits>(parameters, new CountingBits(log2BitWidth))
    {
    }

    public static class BloomFilter
    {
        /// <summary>
        /// Bloom filter parameters for size and number of hash functions.
        /// </summary>
        public sealed class Parameters
        {
            /// <summary>
            /// Number of hash functions applied to each element. (commonly 'k')
            /// </summary>
            public readonly int NumberOfHashFunctions;

            /// <summary>
            /// Number of bits in the filter. (commonly 'm')
            /// </summary>
            public readonly int NumberOfBits;

            private const double Log2Squared = 0.48045301391820144; // ln(2)^2

            private const double Log2 = 0.6931471805599453;

            /// <summary>
            /// Represents the given (not necessarily optimal) parameters.
            /// </summary>
            public Parameters(int numberOfBits, int numberOfHashFunctions)
            {
                Contract.Requires(numberOfBits > 0);
                Contract.Requires(numberOfHashFunctions > 0);

                NumberOfHashFunctions = numberOfHashFunctions;
                NumberOfBits = numberOfBits;
            }

            /// <summary>
            /// Finds optimal parameters to achieve a given false positive rate - in (0.0, 1.0) - for an expected number of elements.
            /// </summary>
            public static Parameters CreateOptimalWithFalsePositiveProbability(int numberOfElements, double targetFalsePositiveProbability)
            {
                Contract.Requires(numberOfElements > 0);
                Contract.Requires(targetFalsePositiveProbability < 1.0 && targetFalsePositiveProbability > 0.0);

                // m = -n ln(p) / ln(2)^2 = n ln(p^-1) / ln(2)^2
                int numberOfBits = checked((int)Math.Ceiling(numberOfElements * Math.Log(1 / targetFalsePositiveProbability) / Log2Squared));
                Contract.Assert(numberOfBits > 0);

                // k = m / n * ln
                int numberOfHashFunctions = checked((int)Math.Round(numberOfBits / (double)numberOfElements * Log2));

                if (numberOfHashFunctions == 0)
                {
                    numberOfHashFunctions = 1;
                }

                Contract.Assert(numberOfHashFunctions > 0);

                return new Parameters(numberOfBits, numberOfHashFunctions);
            }
        }
    }

    /// <summary>
    /// Compact probabilistic set. A query answers 'definitely not in set' or 'maybe in set'.
    /// </summary>
    public class BloomFilter<TBits>
        where TBits : IBloomSlots<TBits>
    {
        private readonly TBits m_bits;
        private readonly Parameters m_parameters;

        /// <summary>
        /// Creates an empty filter with the given parameters.
        /// The <see cref="Parameters.NumberOfBits"/> specifies the final size of the filter.
        /// </summary>
        public BloomFilter(Parameters parameters, TBits bits)
        {
            Contract.RequiresNotNull(parameters);

            bits.Initialize(parameters.NumberOfBits);
            m_bits = bits;
            m_parameters = parameters;
        }

        /// <summary>
        /// Indicates if an item has possibly been added (false positives may occur).
        /// </summary>
        public bool PossiblyContains(MurmurHash hash)
        {
            for (int i = 0; i < m_parameters.NumberOfHashFunctions; i++)
            {
                int index = (int)(unchecked(hash.High + (hash.Low * (ulong)i)) % (ulong)m_parameters.NumberOfBits);
                if (!m_bits[index])
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            m_bits.Clear();
        }

        public bool Set(MurmurHash hash, bool value = true)
        {
            bool result = false;
            int comparisionValue = value ? 0 : 1;
            for (int i = 0; i < m_parameters.NumberOfHashFunctions; i++)
            {
                int index = (int)(unchecked(hash.High + (hash.Low * (ulong)i)) % (ulong)m_parameters.NumberOfBits);
                // If any of the counters is zero, the blob was added
                result |= m_bits.GetAndSet(index, value) == comparisionValue;
            }

            return result;
        }

        public bool Unset(MurmurHash hash)
        {
            return Set(hash, value: false);
        }

        /// <summary>
        /// Adds an item. Subsequently, <see cref="PossiblyContains"/> for this item
        /// is guaranteed to return true.
        /// </summary>
        public void Add(MurmurHash hash)
        {
            Set(hash.High, hash.Low, value: true);
        }

        /// <summary>
        /// Removes an item.
        /// WARNING: This is unsafe for bit sets, only counting bitsets should use this operation
        /// </summary>
        public void UnsafeRemove(MurmurHash hash)
        {
            Set(hash.High, hash.Low, value: false);
        }

        private void Set(ulong high, ulong low, bool value)
        {
            for (int i = 0; i < m_parameters.NumberOfHashFunctions; i++)
            {
                int index = (int)(unchecked(high + (low * (ulong)i)) % (ulong)m_parameters.NumberOfBits);
                m_bits[index] = value;
            }
        }
    }
}