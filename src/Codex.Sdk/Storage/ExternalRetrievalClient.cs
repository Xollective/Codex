using Codex.ObjectModel;
using Codex.Sdk.Utilities;
using Codex.Storage;
using Codex.Storage.BlockLevel;

namespace Codex.Lucene.Search;

public record struct GetExternalArguments<T>(GetExternalArguments Arguments, Include<T> Includes = default, IExternalRetrievalContext Context = null)
    where T : class, ISearchEntity
{
    public EntityMappingKey Key => Arguments.Key;

    public AddressKind Kind => Key.AddressKind;

    public static implicit operator GetExternalArguments(GetExternalArguments<T> arguments) => arguments.Arguments;
}

public partial record struct GetExternalArguments(EntityMappingKey Key, ArrayBuilder<SnapshotRange> SnapshotRanges = null)
{
    public static implicit operator GetExternalArguments(EntityMappingKey key) => new(key);
}

public interface IExternalRetrievalContext
{ 
}

public interface IDescriber<T>
    where T : ISearchEntity
{
    object GetDescriptor(T entity);
}

public interface IExternalRetrievalClient<T> : IDescriber<T>
    where T : class, ISearchEntity
{
    ValueTask<T> Get(GetExternalArguments<T> arguments);
}

public class BinarySearchEntity : SearchEntity, ISearchEntity<BinarySearchEntity>
{
    public byte[] Bytes { get; set; }
}


public interface IExternalRetrievalClient :
    IExternalRetrievalClient<IBoundSourceSearchModel>,
    IExternalRetrievalClient<IReferenceSearchModel>,
    IExternalRetrievalClient<ITextChunkSearchModel>,
    IExternalRetrievalClient<IDefinitionSearchModel>,
    IExternalRetrievalClient<IProjectSearchModel>,
    IExternalRetrievalClient<IProjectReferenceSearchModel>,
    IExternalRetrievalClient<IPropertySearchModel>,
    IExternalRetrievalClient<ITextSourceSearchModel>,
    IDescriber<ICommitSearchModel>,
    IDescriber<IRepositorySearchModel>
{
    Task InitializeAsync();

    protected ValueTask<T> GetRawEntityCoreAsync<T>(GetExternalArguments<T> arguments)
        where T : class, ISearchEntity;

    ValueTask<T> GetEntityAsync<T>(GetExternalArguments<T> arguments)
        where T : class, ISearchEntity
    {
        if (this is IExternalRetrievalClient<T> client)
        {
            return client.Get(arguments);
        }

        return GetRawEntityCoreAsync(arguments);
    }

    GetSearchEntityAsync GetSearchEntityAsync => new(this);

    GetSearchEntityDescriptor GetSearchEntityDescriptor => new(this);
}

public struct GetSearchEntityDescriptor(IExternalRetrievalClient client) : IDerivedLambda<ISearchEntity, GetExternalArguments, ValueTask<object>>
{
    async ValueTask<object> IDerivedLambda<ISearchEntity, GetExternalArguments, ValueTask<object>>.Invoke<T>(GetExternalArguments state)
    {
        var entity = await client.GetEntityAsync<T>(new GetExternalArguments<T>(state));
        if (client is IDescriber<T> d && entity != null)
        {
            return d.GetDescriptor(entity);
        }

        return entity;
    }
}

public struct GetSearchEntityAsync(IExternalRetrievalClient client) : IDerivedLambda<ISearchEntity, GetExternalArguments, ValueTask<ISearchEntity>>
{
    async ValueTask<ISearchEntity> IDerivedLambda<ISearchEntity, GetExternalArguments, ValueTask<ISearchEntity>>.Invoke<T>(GetExternalArguments state)
    {
        var entity = await client.GetEntityAsync<T>(new GetExternalArguments<T>(state));
        return entity;
    }
}
