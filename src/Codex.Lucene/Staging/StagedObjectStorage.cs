using System.Buffers;
using System.Diagnostics;
using Codex.Logging;
using Codex.Lucene.Framework;
using Codex.Lucene.Search;
using Codex.Storage;
using Codex.Utilities;
using Lucene.Net.Index;

namespace Codex.Lucene;

public record StagedObjectStorage(DiskObjectStorage OverlayStorage, IObjectStorage BackingStorage) : IObjectStorage
{
    public void Dispose()
    {
        OverlayStorage.Dispose();
        BackingStorage.Dispose();
    }

    public void Finalize(string message)
    {
        OverlayStorage.Finalize(message);
    }

    public void Initialize()
    {
        BackingStorage.Initialize();
        OverlayStorage.Initialize();
    }

    public Stream Load(string relativePath)
    {
        return OverlayStorage.Load(relativePath) ?? BackingStorage.Load(relativePath);
    }

    public string Write(string relativePath, MemoryStream stream)
    {
        return OverlayStorage.Write(relativePath, stream);
    }
}
