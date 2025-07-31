using Tenray.ZoneTree;

namespace Codex.Storage;

public static class ZoneTreeExtensions
{
    public static IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable<TKey, TValue>(this IZoneTreeIterator<TKey, TValue> iterator)
    {
        while (iterator.Next())
        {
            yield return iterator.Current;
        }
    }
}
