namespace Codex.Utilities;

public abstract class ReadStream : Stream
{
    public override bool CanRead => true;

    public override bool CanWrite => false;

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadCore(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        return ReadCore(buffer);
    }

    public override int ReadByte()
    {
        Span<byte> byteSpan = stackalloc byte[1];
        int read = Read(byteSpan);
        return read > 0 ? byteSpan[0] : -1;
    }

    public abstract int ReadCore(Span<byte> buffer);
}
