
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Codex.Utilities;

public class KeyedArrayBuilder<TKey, TValue> : IStandardReadOnlyDictionary<TKey, TValue>
{
    public Dictionary<TKey, int> Map { get; }

    public int Count => Map.Count;

    private ArrayBuilder<TValue> _array = new();

    public KeyedArrayBuilder(IEqualityComparer<TKey> comparer = null)
    {
        Map = new(comparer ?? EqualityComparer<TKey>.Default);
    }

    public ref TValue this[TKey key]
    {
        get
        {
            return ref GetOrAdd(key, out _);
        }
    }

    public ref TValue GetOrAdd(TKey key, out int index)
    {
        if (!Map.TryGetValue(key, out index))
        {
            Map[key] = index = _array.Count;
            _array.Add(default);
        }

        return ref _array[index];
    }

    public void Clear()
    {
        Map.Clear();
        _array.Clear();
    }

    public ArrayBuilder<TValue> ValuesUnsafe => _array;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var entry in Map)
        {
            yield return new(entry.Key, _array[entry.Value]);
        }
    }

    private ArrayBuilder<KeyValuePair<TKey, TValue>> DebuggerView 
        => new ArrayBuilder<KeyValuePair<TKey, TValue>>(this.ToArray());

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (Map.TryGetValue(key, out var index))
        {
            value = _array[index];
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
