using Codex.ObjectModel;

namespace Codex.Utilities
{
    public class LazySearchTypesMap<T>
        where T : class
    {
        private T[] _valuesById = new T[SearchTypes.RegisteredSearchTypes.Count];
        private Func<SearchType, T> _valueFactory;

        public T this[SearchType searchType]
        {
            get
            {
                ref var result = ref _valuesById[searchType.Id];
                if (result == null)
                {
                    var newResult = _valueFactory(searchType);
                    Interlocked.CompareExchange(ref result, newResult, null);
                }

                return result;
            }
        }

        public LazySearchTypesMap(Func<SearchType, T> valueFactory, bool initializeAll = false)
        {
            _valueFactory = valueFactory;
            if (initializeAll)
            {
                ForEach(_ => { });
            }
        }

        public IEnumerable<KeyValuePair<SearchType, T>> Enumerate(bool allowInit = true)
        {
            foreach (var searchType in SearchTypes.RegisteredSearchTypes)
            {
                T value = allowInit ? this[searchType] : _valuesById[searchType.Id];
                if (value != null)
                {
                    yield return new(searchType, value);
                }
            }
        }

        public IEnumerable<KeyValuePair<SearchType, Lazy<T>>> EnumerateLazy(bool allowInit = false)
        {
            foreach (var searchType in SearchTypes.RegisteredSearchTypes)
            {
                yield return new(searchType, new Lazy<T>(() => this[searchType]));
            }
        }

        public void ForEach(Action<T> action)
        {
            ForEach((_, v) => action(v));
        }

        public void ForEach(Action<SearchType, T> action)
        {
            foreach (var searchType in SearchTypes.RegisteredSearchTypes)
            {
                action(searchType, this[searchType]);
            }
        }
    }
}