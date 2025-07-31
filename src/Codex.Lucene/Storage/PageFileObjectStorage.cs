using Codex.Lucene.Search;

namespace Codex.Storage;

public record PageFileObjectStorage(IPageFileAccessor Accessor, PagingDirectoryInfo Info) : IAsyncObjectStorage
{
    public async ValueTask<Stream> LoadAsync(string relativePath)
    {
        return await Accessor.OpenStreamAsync(relativePath);
    }

    public ValueTask<string> WriteAsync(string relativePath, MemoryStream stream)
    {
        throw new NotImplementedException();
    }
}
