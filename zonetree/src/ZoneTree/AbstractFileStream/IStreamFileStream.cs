namespace Tenray.ZoneTree.AbstractFileStream;

public interface IStreamFileStream : IFileStream
{
    Stream Stream { get; }

    void IDisposable.Dispose() => Stream.Dispose();

    long IFileStream.Position { get => Stream.Position; set => Stream.Position = value; }

    long IFileStream.Length => Stream.Length;

    bool IFileStream.CanWrite => Stream.CanWrite;

    bool IFileStream.CanRead => Stream.CanRead;

    void IFileStream.Close() => Stream.Close();

    void IFileStream.CopyTo(Stream destination) => Stream.CopyTo(destination);

    ValueTask IFileStream.DisposeAsync() => Stream.DisposeAsync();

    int IFileStream.Read(Span<byte> buffer) => Stream.Read(buffer);

    long IFileStream.Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);

    void IFileStream.SetLength(long value) => Stream.SetLength(value);

    void IFileStream.Write(ReadOnlySpan<byte> buffer) => Stream.Write(buffer);

    Stream IFileStream.ToStream() => Stream;
}
