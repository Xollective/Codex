using Codex.Utilities.Tasks;

namespace BuildXL.Utilities.Collections;

public static class SdkCollectionUtilities
{
    public static KeyedEnumerable<TKey, TValue> AsKeyed<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> entries, IEqualityComparer<TKey> comparer = null)
    {
        return new KeyedEnumerable<TKey, TValue>(entries, comparer);
    }
}