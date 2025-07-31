using Codex.Sdk.Search;

namespace Codex.ObjectModel.Implementation
{
    public abstract partial class ClientBase : IClient
    {
        public abstract IIndex<T> CreateIndex<T>(SearchType<T> searchType)
            where T : class, ISearchEntity<T>;

        protected virtual Lazy<IIndex<T>> GetIndexFactory<T>(SearchType<T> searchType)
            where T : class, ISearchEntity<T>
        {
            return new Lazy<IIndex<T>>(() => CreateIndex<T>(searchType));
        }
    }
}