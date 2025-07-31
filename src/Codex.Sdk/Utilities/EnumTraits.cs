// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.ContractsLight;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Codex.Utilities
{
    /// <summary>
    /// Traits for an enum type (validation, int casting, etc.)
    /// These traits may only be instantiated for enum types which are bijective; no two enum constants may have the same
    /// value.
    /// </summary>
    public static class EnumTraits<TEnum>
        where TEnum : unmanaged, System.Enum
    {
        private static readonly Dictionary<long, TEnum> s_integerToValue = new();
        private static readonly Dictionary<TEnum, long> s_valueToInteger = new();
        private static readonly long s_allFlags;

        public static string Name { get; } = typeof(TEnum).Name;

        static EnumTraits()
        {
            Contract.Assume(typeof(TEnum).GetTypeInfo().IsEnum, "EnumTraits may only be instantiated for an enum type");

            long? max = null;
            long? min = null;
            foreach (var val in Enum.GetValues<TEnum>())
            {
                long intVal = ToInteger(val);

                if (!max.HasValue || max.Value < intVal)
                {
                    max = intVal;
                }

                if (!min.HasValue || min.Value > intVal)
                {
                    min = intVal;
                }

                Contract.Assume(!s_integerToValue.ContainsKey(intVal), "Two enum values have the same integer representation.");
                s_integerToValue.Add(intVal, val);
                s_valueToInteger.Add(val, intVal);
                s_allFlags |= intVal;
            }

            MinValue = min ?? 0;
            MaxValue = max ?? 0;
        }

        /// <summary>
        /// Gets whether flags exist for all bits set in the integral value of the enum
        /// </summary>
        public static bool AreFlagsDefined(long value)
        {
            return (s_allFlags & value) == value;
        }

        /// <summary>
        /// Gets the count of values for the enum
        /// </summary>
        public static int ValueCount => s_valueToInteger.Count;

        /// <summary>
        /// Minimum integer value corresponding to an enum constant.
        /// </summary>
        public static long MinValue { get; }

        /// <summary>
        /// Maximum integer value corresponding to an enum constant.
        /// </summary>
        public static long MaxValue { get; }

        /// <summary>
        /// Returns an enumerable for all values of the enum.
        /// </summary>
        public static IEnumerable<TEnum> EnumerateValues()
        {
            return s_valueToInteger.Keys;
        }

        /// <summary>
        /// Tries to return the enum constant for a given integer value.
        /// </summary>
        /// <remarks>
        /// This conversion will fail if <paramref name="intValue" /> doesn't correspond to a declared constant,
        /// such as if it represents a combination of flags.
        /// </remarks>
        public static bool TryConvert(long intValue, out TEnum value)
        {
            return s_integerToValue.TryGetValue(intValue, out value);
        }

        /// <summary>
        /// Returns the integer value for a given enum constant.
        /// </summary>
        /// <remarks>
        /// It is an error to attempt this conversion if <paramref name="value" /> is not a declared constant,
        /// such as if it represents a combination of flags.
        /// </remarks>
        public static long ToInteger(TEnum value)
        {
            return IntHelpers.ToInteger<TEnum>(value);
        }

        public static TEnum ToEnum(long value)
        {
            return IntHelpers.ToEnum<TEnum>(value);
        }
    }

    public class EnumMap<TEnum, TValue>
        where TEnum : unmanaged, System.Enum
    {
        public TValue[] Values { get; } = new TValue[GetValuesLength()];

        private static long GetValuesLength()
        {
            var name = EnumTraits<TEnum>.Name;
            var minValue = EnumTraits<TEnum>.MinValue;
            Contract.Check(minValue >= 0)?.Assert($"{minValue} ({name}.{EnumTraits<TEnum>.ToEnum(minValue)}) < 0");
            var maxValue = EnumTraits<TEnum>.MaxValue;
            Contract.Check(maxValue < ushort.MaxValue)?.Assert($"{maxValue} ({name}.{EnumTraits<TEnum>.ToEnum(maxValue)}) >= {ushort.MaxValue}");
            return maxValue;
        }

        public TValue this[TEnum key]
        {
            get => Values[EnumTraits<TEnum>.ToInteger(key)];
            set => Values[EnumTraits<TEnum>.ToInteger(key)] = value;
        }
    }
}
