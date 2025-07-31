using System.Collections;
using System.Collections.Immutable;

namespace Tenray.ZoneTree.Collections;

internal static class CollectionExtensions
{
    public static ImmutableArray<T> ToReverseArray<T>(this IReadOnlyList<T> items)
    {
        return ImmutableArray<T>.Empty.AddRange(new ReverseList<T>(items));
    }

    public static IEnumerable<T> EnumerateRangeFromBounds<T>(this IReadOnlyList<T> items, int start, int endExclusive)
    {
        for (int i = start; i < endExclusive; i++)
        {
            yield return items[i];
        }
    }

    public static IReadOnlyList<T> ToReverseList<T>(this IReadOnlyList<T> items) => new ReverseList<T>(items);

    private record ReverseList<T>(IReadOnlyList<T> Items) : IReadOnlyList<T>
    {
        public T this[int index] => Items[Items.Count - index];

        public int Count => Items.Count;

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                yield return Items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}