using System.Collections;

namespace Codex.Utilities;

public static class LazyList
{
    public static LazyList<T> Create<T>(Func<IReadOnlyList<T>> listFactory)
    {
        return new(new(listFactory));
    }
}

public record LazyList<T>(Lazy<IReadOnlyList<T>> ListFactory) : IReadOnlyList<T>
{
    public IReadOnlyList<T> Inner => ListFactory.Value; 

    public T this[int index] => Inner[index];

    public int Count => Inner.Count;

    public IEnumerator<T> GetEnumerator()
    {
        return Inner.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Inner).GetEnumerator();
    }
}
