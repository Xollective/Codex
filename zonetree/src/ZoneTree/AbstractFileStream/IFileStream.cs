namespace Tenray.ZoneTree.AbstractFileStream;

public interface IFileStream : IDisposable
{
    string FilePath { get; }

    long Position { get; set; }

    long Length { get; }

    bool CanWrite { get; }

    bool CanRead { get; }

    void Close();

    void CopyTo(Stream destination);

    ValueTask DisposeAsync();

    void Flush(bool flushToDisk);

    int Read(Span<byte> buffer);

    long Seek(long offset, SeekOrigin origin);

    void SetLength(long value);

    void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    void Write(ReadOnlySpan<byte> buffer);

    Stream ToStream();
}
