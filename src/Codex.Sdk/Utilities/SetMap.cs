using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Codex.Utilities;

public class SetMap<TKey, TValue>(IEqualityComparer<TKey>? comparer = null) : IStandardReadOnlyDictionary<TKey, TValue>
{
    public record struct Entry(TKey Key, TValue Value)
    {
        public TKey Key = Key;
        public TValue Value = Value;
    }

    private EntryComparer _entryComparer = EntryComparer.Get(comparer);

    public int Count => Set.Count;

    private HashSetEx<Entry> set;
    public HashSetEx<Entry> Set => set ??= new HashSetEx<Entry>(_entryComparer);

    private HashSetEx<Entry>.AlternateLookup<TKey> lookup;
    public HashSetEx<Entry>.AlternateLookup<TKey> Lookup => lookup.Set != null ? lookup : lookup = Set.GetAlternateLookup(_entryComparer);

    public ref TValue this[TKey key]
    {
        get
        {
            return ref Unsafe.AsRef(in Lookup[key]).Value;
        }
    }

    public void TryAdd(TKey key, TValue value)
    {
        Set.Add(new(key, value));
    }

    public ref TValue GetOrAdd(TKey key, Out<int> index = default)
    {
        return ref Lookup.GetOrAdd(key, itemIndex: index).Value;
    }

    public void Reset()
    {
        Set.Reset();
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Out.TryVar(Lookup.TryGetValue(key, out var entry), out value, entry.Value);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var item in Set)
        {
            yield return new (item.Key, item.Value);
        }
    }

    private class EntryComparer(IEqualityComparer<TKey>? comparer) : IEqualityComparer<Entry>, IAlternateEqualityComparer<TKey, Entry>
    {
        private IEqualityComparer<TKey> comparer = comparer ?? EqualityComparer<TKey>.Default;
        private static EntryComparer _default;

        public static EntryComparer Get(IEqualityComparer<TKey>? comparer)
        {
            if (comparer == null || comparer == EqualityComparer<TKey>.Default)
            {
                return _default ??= new EntryComparer(EqualityComparer<TKey>.Default);
            }
            else
            {
                return new(comparer);
            }
        }

        public Entry Create(TKey alternate)
        {
            return new Entry(alternate, default);
        }

        public bool Equals(TKey alternate, Entry other)
        {
            return comparer.Equals(alternate, other.Key);
        }

        public bool Equals(Entry x, Entry y)
        {
            return comparer.Equals(x.Key, y.Key);
        }

        public int GetHashCode(TKey alternate)
        {
            return comparer.GetHashCode(alternate);
        }

        public int GetHashCode(Entry obj)
        {
            return comparer.GetHashCode(obj.Key);
        }
    }
}
