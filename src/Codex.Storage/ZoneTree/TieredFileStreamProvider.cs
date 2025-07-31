using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.ContractsLight;
using Codex.Utilities;
using Tenray.ZoneTree.AbstractFileStream;

namespace Codex.Storage.ZoneTree;

public record TieredFileStreamProvider(
    RemapFileStreamProvider OverlayDirectory,
    RemapFileStreamProvider BackingDirectory) : IFileStreamProvider
{
    private ConcurrentDictionary<string, bool> _deletedBackingFiles { get; } = new (StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> DeletedBackingFiles => _deletedBackingFiles.Keys;

    public IFileStreamProvider GetProvider(string path, bool? writing = false)
    {
        if (writing == true) return OverlayDirectory;

        var existence = GetExistence(path);
        return GetProvider(existence);
    }

    private IFileStreamProvider GetProvider(Existence existence)
    {
        return existence switch
        {
            Existence.Overlay => OverlayDirectory,
            Existence.Backing => BackingDirectory,
            _ => OverlayDirectory
        };
    }

    public string GetFullOverlayPath(string path) => throw new NotImplementedException();
    public string GetFullBackingPath(string path) => throw new NotImplementedException();

    public void CreateDirectory(string path)
    {
        var provider = GetProvider(path, writing: true);
        if (!provider.DirectoryExists(path))
        {
            provider.CreateDirectory(path);
        }
    }

    public IFileStream CreateFileStream(
        string path,
        FileMode mode,
        FileAccess access,
        FileShare share,
        int bufferSize = 4096,
        FileOptions options = FileOptions.None)
    {
        bool writing = mode != FileMode.Open;
        writing |= (access & FileAccess.Write) != 0;
        writing |= (options & FileOptions.DeleteOnClose) != 0;

        IFileStreamProvider provider;
        var existence = GetExistence(path);
        if (writing)
        {
            if (existence == Existence.Backing)
            {
                Contract.Check(mode == FileMode.Create)?.Assert($"Cannot open a backing file {path} with write behavior");
            }

            // Ensure the directory is created
            var directoryPath = Path.GetDirectoryName(path) ?? "";
            OverlayDirectory.CreateDirectory(directoryPath.EnsureTrailingSlash());

            provider = OverlayDirectory;
        }
        else
        {
            provider = GetProvider(existence);
        }

        return provider.CreateFileStream(path, mode, access, share, bufferSize, options);
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        throw new NotSupportedException();
    }

    public void DeleteFile(string path)
    {
        var existence = GetExistence(path);
        if (existence != Existence.None)
        {
            _deletedBackingFiles[path] = default;
        }

        if (existence == Existence.Overlay)
        {
            OverlayDirectory.DeleteFile(path);
        }
    }

    public bool DirectoryExists(string path)
    {
        var existence = GetDirectoryExistence(path);
        if (existence == Existence.Backing)
        {
            OverlayDirectory.CreateDirectory(path);
        }

        return existence != Existence.None;
    }

    public bool FileExists(string path)
    {
        var existence = GetExistence(path);
        return existence != Existence.None;
    }

    public DurableFileWriter GetDurableFileWriter()
    {
        return new DurableFileWriter(this);
    }

    public byte[] ReadAllBytes(string path)
    {
        return GetProvider(path).ReadAllBytes(path);
    }

    public string ReadAllText(string path)
    {
        return GetProvider(path).ReadAllText(path);
    }

    public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName)
    {
        _deletedBackingFiles[sourceFileName] = default;
        _deletedBackingFiles[destinationFileName] = default;
        if (destinationBackupFileName != null)
        {
            _deletedBackingFiles[destinationBackupFileName] = default;
        }

        var sourceExistence = GetExistence(sourceFileName);
        if (sourceExistence == Existence.Backing)
        {
            BackingDirectory.RemapPath(ref sourceFileName);
        }
        else
        {
            OverlayDirectory.RemapPath(ref sourceFileName);
        }

        OverlayDirectory.RemapPath(ref destinationFileName);
        File.Move(sourceFileName, destinationFileName, true);

        //OverlayDirectory.ReplaceDontRemapSource(sourceFileName, destinationFileName, destinationBackupFileName);
    }

    public IEnumerable<string> GetDeletions()
    {
        return _deletedBackingFiles.Keys.ToList();
    }

    private enum Existence
    {
        None,
        Backing,
        Overlay
    }

    private Existence GetDirectoryExistence(string path)
    {
        if (OverlayDirectory.DirectoryExists(path))
        {
            return Existence.Overlay;
        }
        else if (!_deletedBackingFiles.ContainsKey(path) && BackingDirectory.DirectoryExists(path))
        {
            return Existence.Backing;
        }

        return Existence.None;
    }

    private Existence GetExistence(string path)
    {
        if (OverlayDirectory.FileExists(path))
        {
            return Existence.Overlay;
        }
        else if (!_deletedBackingFiles.ContainsKey(path) && BackingDirectory.FileExists(path))
        {
            return Existence.Backing;
        }

        return Existence.None;
    }
}
