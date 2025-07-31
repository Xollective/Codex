namespace Codex.Storage;

public record CompositeObjectStorage(IObjectStorage Local, IObjectStorage Remote) : IObjectStorage
{
    public void Dispose()
    {
        using (Local)
        using (Remote)
        {

        }
    }

    public void Finalize(string message)
    {
        Local.Finalize(message);

        Remote.Finalize(message);
    }

    public void Initialize()
    {
        Local.Initialize();

        Remote.Initialize();
    }

    public Stream Load(string relativePath)
    {
        return Local.Load(relativePath);
    }

    public string Write(string relativePath, MemoryStream stream)
    {
        return Local.Write(relativePath, stream);
    }

    public async ValueTask<string> WriteAsync(string relativePath, MemoryStream stream)
    {
        // For now all the APIs except Git return the relative path (modulo forward or backslash)
        Placeholder.Todo("Which result should be returned.");

        var result = await Local.WriteAsync(relativePath, stream);

        await Remote.WriteAsync(relativePath, stream);

        return result;
    }
}
