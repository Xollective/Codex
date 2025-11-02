using System.Diagnostics.CodeAnalysis;

namespace BuildXL.Utilities.Collections;

public class KeyedEnumerable<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> entries, IEqualityComparer<TKey> comparer = null) : IStandardReadOnlyDictionary<TKey, TValue>
{
    public int Count => entries.Count();

    private IEqualityComparer<TKey> Comparer => field ??= comparer ?? EqualityComparer<TKey>.Default;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return entries.GetEnumerator();
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Out.TryVar(entries.TryFind((Comparer, key), (k, d) => d.Comparer.Equals(k.Key, d.key), out var entry), out value, entry.Value);
    }
}
