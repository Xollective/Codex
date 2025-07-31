using Codex.Utilities;

namespace Codex.Storage;

public interface IObjectStorage : IAsyncObjectStorage, IDisposable
{
    void Initialize();
    
    async ValueTask<Stream> IAsyncObjectStorage.LoadAsync(string relativePath)
    {
        return Load(relativePath);
    }

    Stream Load(string relativePath);

    void Finalize(string message);

    string Write(string relativePath, MemoryStream stream);

    async ValueTask<string> IAsyncObjectStorage.WriteAsync(string relativePath, MemoryStream stream)
    {
        return Write(relativePath, stream);
    }
}

public interface IAsyncObjectStorage
{
    ValueTask<Stream> LoadAsync(string relativePath);

    ValueTask<string> WriteAsync(string relativePath, MemoryStream stream);
}


public class NullObjectStorage : IObjectStorage
{
    public static readonly NullObjectStorage Instance = new();

    public void Dispose()
    {
    }

    public void Finalize(string message)
    {
    }

    public void Initialize()
    {
    }

    public Stream Load(string relativePath)
    {
        return Stream.Null;
    }

    public string Write(string relativePath, MemoryStream stream)
    {
        return relativePath;
    }
}

public static class ObjectStorageExtensions
{
    public static StoredFile<T> GetFile<T>(this IAsyncObjectStorage storage, string relativePath)
        where T : class, new()
    {
        return new(storage, relativePath);
    }

    public static async ValueTask<T> CreateOrLoadValueAsync<T>(this IAsyncObjectStorage storage, string relativePath, AsyncOut<bool> exists = null)
        where T : class, new()
    {
        return await storage.LoadValueAsync<T>(relativePath, exists) ?? new T();
    }

    public static async ValueTask<T> LoadValueAsync<T>(this IAsyncObjectStorage storage, string relativePath, AsyncOut<bool> exists = null)
        where T : class
    {
        using var stream = await storage.LoadAsync(relativePath);

        if (stream == null || stream.Length == 0) return null;
        exists?.Set(true);

        return JsonSerializationUtilities.DeserializeEntity<T>(stream);

    }
}
