using System.IO;
using Codex.Storage.BlockLevel;

namespace Codex.Sdk.Storage;

public class IndexDirectoryLayout(string directory)
{
    public const string DatabaseRelativeDirectory = "db";

    public string Directory => directory;

    public string StagingIndexDirectory => Path.Combine(directory, "stageindex");

    public string OverlayDirectory => Path.Combine(directory, "overlay");

    public string DatabaseDirectory => Path.Combine(directory, DatabaseRelativeDirectory);

    public static implicit operator IndexDirectoryLayout(string directory) => string.IsNullOrEmpty(directory) ? null : new(directory);

    public static implicit operator string(IndexDirectoryLayout layout) => layout?.Directory;
}