using Codex.ObjectModel;
using Codex.Sdk.Utilities;

namespace Codex.Storage;

public interface ICodexStoreWriter
{
    AsyncLocalScope<EntityBase> EnterRootEntityScope(EntityBase rootEntity);

    ValueTask AddAsync<T>(SearchType<T> searchType, T entity, IndexAddOptions options = default)
        where T : class, ISearchEntity;

    Task InitializeAsync();

    Task FinalizeAsync();
}

public interface ICodexStoreWriterProvider : ICodexStore
{
    Task<ICodexStoreWriter> CreateStoreWriterAsync(IRepositoryStoreInfo storeInfo);
}

public interface IPrefilterCodexStoreWriter : ICodexStoreWriter
{
    IStableIdStorage IdTracker { get; }

    Task StoreFilterAsync(IAsyncObjectStorage storage);

    Task LoadFilterAsync(IAsyncObjectStorage storage);
}

public class NullCodexStoreWriter : ICodexStoreWriter, ICodexStoreWriterProvider
{
    public async Task<ICodexRepositoryStore> CreateRepositoryStore(RepositoryStoreInfo info)
    {
        return new NullCodexRepositoryStore();
    }

    public async Task<ICodexStoreWriter> CreateStoreWriterAsync(IRepositoryStoreInfo storeInfo)
    {
        return this;
    }

    public AsyncLocalScope<EntityBase> EnterRootEntityScope(EntityBase rootEntity)
    {
        return default;
    }

    public Task FinalizeAsync()
    {
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    ValueTask ICodexStoreWriter.AddAsync<T>(SearchType<T> searchType, T entity, IndexAddOptions options)
    {
        return ValueTask.CompletedTask;
    }
}