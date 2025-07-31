
using System.Collections.Generic;

namespace Codex.Utilities;

public class ArrayBuilderSet<T> : IStandardEnumerable<T>
{
    public HashSet<int> Map { get; }
    public ArrayBuilder<T> Array { get; }

    private T _current;

    public ArrayBuilderSet(IEqualityComparer<T> comparer = null, ArrayBuilder<T> array = null)
    {
        Array = array ?? new();
        Map = new(Compare.SelectEquality<int, T>(i => i < 0 ? _current : Array[i], comparer ?? EqualityComparer<T>.Default));
    }

    public bool Add(T item)
    {
        Add(item, out var added);
        return added;
    }

    public ref T Add(T item, out bool added)
    {
        _current = item;
        added = false;
        if (!Map.TryGetValue(-1, out var index))
        {
            index = Array.Count;
            Array.Add(item);
            Map.Add(index);
            added = true;
        }

        _current = default;
        return ref Array[index];
    }

    public void Clear()
    {
        Map.Clear();
        Array.Clear();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Array.GetEnumerator<ArrayBuilder<T>, T>();
    }
}
