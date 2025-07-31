namespace Tenray.ZoneTree.Segments.Disk;

public interface IRandomAccessDevice : IDisposable
{
    long SegmentId { get; }

    bool Writable { get; }

    long Length { get; }

    /// <summary>
    /// Returns read buffer count.
    /// </summary>
    int ReadBufferCount { get; }

    void ClearContent();

    /// <summary>
    /// Appends bytes and returns the position of the appended bytes.
    /// </summary>
    /// <param name="bytes">Bytes</param>
    /// <returns>Position of the bytes.</returns>
    long AppendBytesReturnPosition(ReadOnlySpan<byte> bytes);

    ReadOnlyMemory<byte> GetBytes(long offset, int length);

    int GetBytes(long offset, Span<byte> buffer);

    void Close();

    void Delete();

    /// <summary>
    /// Seals the device.
    /// Sealed devices can not accept new writes
    /// and becomes immutable.
    /// </summary>
    void SealDevice();

    /// <summary>
    /// Release internal read buffers 
    /// that are not used after given ticks.
    /// </summary>
    /// <returns>Total released read buffer count.</returns>
    int ReleaseReadBuffers(long ticks);
}
