using System.Diagnostics.ContractsLight;
using Codex.Utilities;

namespace Codex.Storage;

public record FileSystemReadOnlyObjectStorage(FileSystem FileSystem, string Directory) : IObjectStorage
{
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
        var path = Path.Combine(Directory, relativePath);
        if (!FileSystem.FileExists(path)) return null;

        return FileSystem.OpenFile(path);
    }

    public string Write(string relativePath, MemoryStream stream)
    {
        throw Contract.AssertFailure("Write is not supported.");
    }
}
