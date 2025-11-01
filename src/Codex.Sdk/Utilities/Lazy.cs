using System.Collections;

namespace Codex.Utilities;

public static class Lazy
{
    public static LazyEx<T> Create<T>(Func<T> factory)
    {
        return new LazyEx<T>(factory);
    }

    public static DisposeAction<LazyEx<T>> CreateDisposable<T>(Func<T> factory)
        where T : IDisposable
    {
        return DisposeAction.Create(new LazyEx<T>(factory), lazy => { if (lazy.IsValueCreated) lazy.Value.Dispose(); });
    }

    public static LazyList<T> CreateList<T>(Func<IReadOnlyList<T>> factory)
    {
        return new LazyList<T>(Create(factory));
    }
}

public interface ILazy<out T>
{
    T Value { get; }

    bool IsValueCreated { get; }
}

public class LazyEx<T> : Lazy<T>, ILazy<T>
{
    public LazyEx(Func<T> factory)
        : base(factory)
    {
    }

    private Eval<T> LazyEval => Eval.Create(() => Value);
}
