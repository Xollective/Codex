namespace Tenray.ZoneTree.AbstractFileStream;

public sealed class LocalFileStream : StreamFileStream
{
    public LocalFileStream(string path,
        FileMode mode,
        FileAccess access,
        FileShare share,
        int bufferSize,
        FileOptions options)
        : base(path, new FileStream(path, mode, access, share, bufferSize, options))
    {
    }
}
