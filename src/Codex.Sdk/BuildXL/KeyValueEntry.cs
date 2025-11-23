// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable disable // Disabling nullability for generic type

namespace BuildXL.Utilities.Collections
{
    public static class KeyValueEntry
    {
        public static KeyValueEntry<TKey, TValue> ToEntry<TKey, TValue>(this KeyValuePair<TKey, TValue> e) => e;
    }

    public record struct KeyValueEntry<TKey, TValue>(TKey Key, TValue Value)
    {
        public readonly TKey Key = Key;
        public TValue Value = Value;

        public static implicit operator KeyValuePair<TKey, TValue>(KeyValueEntry<TKey, TValue> e) => new(e.Key, e.Value);
        public static implicit operator KeyValueEntry<TKey, TValue>(KeyValuePair<TKey, TValue> e) => new(e.Key, e.Value);

        public KeyValuePair<TKey, TValue> ToPair() => this;
    }
}
