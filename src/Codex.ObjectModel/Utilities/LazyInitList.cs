namespace Codex.ObjectModel;

public ref struct LazyInitList<T>
{
    private ref List<T> _listRef;

    public LazyInitList(ref List<T> listRef)
    {
        _listRef = ref listRef;
    }

    public List<T> List
    {
        get
        {
            if (_listRef == null)
            {
                Interlocked.CompareExchange(ref _listRef, new List<T>(), null);
            }

            return _listRef;
        }
        set
        {
            _listRef = value;
        }
    }

    public List<T> RawList => _listRef;

    public IReadOnlyList<T> Items => _listRef ?? (IReadOnlyList<T>)Array.Empty<T>();
}