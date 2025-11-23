namespace Codex.Utilities
{
    public ref struct SpanRingBuffer<T>(Span<T> items)
    {
        private Span<T> _items = items;
        private int _head = 0;

        public void Clear()
        {
            this = new(_items);
        }

        public void Push(T item)
        {
            _head = (_head + 1) % _items.Length;
            _items[_head] = item;
            Count = Math.Min(Count + 1, _items.Length);
        }

        public int Count { get; private set; }

        public ref T this[int index] => ref _items[(_head + _items.Length + index) % _items.Length];
    }
}
