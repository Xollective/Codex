using System.IO;
using Codex.Utilities;
using Tenray.ZoneTree.AbstractFileStream;

namespace Codex.Storage.ZoneTree;

public record RemapFileStreamProvider(IFileStreamProvider Inner, string SourceRoot, string TargetRoot) : IFileStreamProvider
{
    public string SourceRootNoSlash { get; } = SourceRoot.EnsureTrailingSlash(normalize: true).TrimTrailingSlash();
    public string SourceRoot { get; } = SourceRoot.EnsureTrailingSlash(normalize: true);
    public string TargetRoot { get; } = TargetRoot.EnsureTrailingSlash(normalize: true);

    private IFileStreamProvider GetProvider(ref string path)
    {
        RemapPath(ref path);
        return Inner;
    }

    public void RemapPath(ref string path, string overrideTargetRoot = null)
    {
        if (path?.EqualsIgnoreCase(SourceRootNoSlash) == true)
        {
            path = TargetRoot;
        }
        else
        {
            path = path?.ReplaceIgnoreCase(SourceRoot, overrideTargetRoot ?? TargetRoot);
        }
    }

    public void CreateDirectory(string path)
    {
        GetProvider(ref path).CreateDirectory(path);
    }

    public IFileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize = 4096, FileOptions options = FileOptions.None)
    {
        return GetProvider(ref path).CreateFileStream(path, mode, access, share, bufferSize, options);
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        GetProvider(ref path).DeleteDirectory(path, recursive);
    }

    public void DeleteFile(string path)
    {
        GetProvider(ref path).DeleteFile(path);
    }

    public bool DirectoryExists(string path)
    {
        return GetProvider(ref path).DirectoryExists(path);
    }

    public bool FileExists(string path)
    {
        return GetProvider(ref path).FileExists(path);
    }

    public DurableFileWriter GetDurableFileWriter()
    {
        return new DurableFileWriter(this);
    }

    public byte[] ReadAllBytes(string path)
    {
        return GetProvider(ref path).ReadAllBytes(path);
    }

    public string ReadAllText(string path)
    {
        return GetProvider(ref path).ReadAllText(path);
    }

    public void ReplaceDontRemapSource(string sourceFileName, string destinationFileName, string destinationBackupFileName)
    {
        RemapPath(ref destinationFileName);
        RemapPath(ref destinationBackupFileName);

        Inner.Replace(sourceFileName, destinationFileName, destinationBackupFileName);
    }

    public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName)
    {
        RemapPath(ref sourceFileName);
        ReplaceDontRemapSource(sourceFileName, destinationFileName, destinationBackupFileName);
    }
}
