using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Codex.Logging;
using Codex.Lucene.Search;
using Codex.Utilities;
using Lucene.Net.Store;

namespace Codex.Lucene.Framework;

public class TieredDirectory : BaseDirectory
{
    private IndexDirectory _overlayDirectory;
    private IndexDirectory _backingDirectory;
    private Lazy<string[]> _backingFiles;

    private ConcurrentDictionary<string, bool> _deletedBackingFiles = new(StringComparer.OrdinalIgnoreCase);

    public string RelativeRoot;

    public TieredDirectory(string relativeRoot, IndexDirectory overlayDirectory, IndexDirectory backingDirectory)
    {
        RelativeRoot = relativeRoot;
        _overlayDirectory = overlayDirectory;
        _backingDirectory = backingDirectory;
        _backingFiles = new Lazy<string[]>(() => _backingDirectory.ListAll());
    }

    public override LockFactory LockFactory => _overlayDirectory.LockFactory;

    public override IndexOutput CreateOutput(string name, IOContext context)
    {
        _deletedBackingFiles[name] = default;
        return _overlayDirectory.CreateOutput(name, context);
    }

    public override Lock MakeLock(string name)
    {
        return _overlayDirectory.MakeLock(name);
    }

    public override void ClearLock(string name)
    {
        _overlayDirectory.ClearLock(name);
    }

    public override void SetLockFactory(LockFactory lockFactory)
    {
        _overlayDirectory.SetLockFactory(lockFactory);
    }

    public override ChecksumIndexInput OpenChecksumInput(string name, IOContext context)
    {
        return base.OpenChecksumInput(name, context);
    }

    public override void DeleteFile(string name)
    {
        var existence = GetExistence(name);
        if (existence != Existence.None)
        {
            _deletedBackingFiles[name] = default;
        }

        if (existence == Existence.Overlay)
        {
            _overlayDirectory.DeleteFile(name);
        }
    }

    public override bool FileExists(string name)
    {
        var existence = GetExistence(name);
        return existence != Existence.None;
    }

    public override long FileLength(string name)
    {
        var existence = GetExistence(name);
        if (existence == Existence.Backing)
        {
            return _backingDirectory.FileLength(name);
        }
        else
        {
            return _overlayDirectory.FileLength(name);
        }
    }

    public override string[] ListAll()
    {
        var files = _backingFiles.Value.Except(_deletedBackingFiles.Keys, StringComparer.OrdinalIgnoreCase);

        return files.Concat(_overlayDirectory.ListAll()).Distinct().ToArray();
    }

    public IEnumerable<PagingFileInfo> GetFileInfos()
    {
        var files = ListAll();
        return files.Select(f => new PagingFileInfo(PathUtilities.UriCombine(RelativeRoot, f), FileLength(f)));
    }

    public override IndexInput OpenInput(string name, IOContext context)
    {
        var existence = GetExistence(name);
        if (existence == Existence.Backing)
        {
            return _backingDirectory.OpenInput(name, context);
        }
        else
        {
            return _overlayDirectory.OpenInput(name, context);
        }
    }

    public IEnumerable<string> GetDeletions()
    {
        var overlayFiles = _overlayDirectory.ListAll().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var backingFiles = _backingFiles.Value.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _deletedBackingFiles.Keys
            .Where(fileName => !overlayFiles.Contains(fileName) && backingFiles.Contains(fileName))
            .ToList();
    }

    public override void Sync(ICollection<string> names)
    {
        _overlayDirectory.Sync(names);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _overlayDirectory.Dispose();
            _backingDirectory.Dispose();
        }
    }

    private enum Existence
    { 
        None,
        Backing,
        Overlay
    }

    private Existence GetExistence(string name)
    {
        if (_overlayDirectory.FileExists(name))
        {
            return Existence.Overlay;
        }
        else if (!_deletedBackingFiles.ContainsKey(name) && _backingDirectory.FileExists(name))
        {
            return Existence.Backing;
        }

        return Existence.None;
    }
}
