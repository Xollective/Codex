using System.Runtime.CompilerServices;
using Codex.Utilities.Serialization;
using DotNext;

namespace Codex.Utilities;

public class VerificationReadStream(Stream expected, Stream actual) : ReadStream
{
    public string DebugName { get; set; }
    public override bool CanSeek => Verify(expected.CanSeek, actual.CanSeek);

    public override long Length => Verify(expected.Length, actual.Length);

    private T Verify<T>(T expected, T actual, IEqualityComparer<T> comparer = null, [CallerMemberName]string caller = null, [CallerLineNumber]int line = 0)
    {
        comparer ??= EqualityComparer<T>.Default;
        Contract.Check(comparer.Equals(expected, actual))?.Assert($"[{caller}:{line}]{expected} != {actual}");
        return expected;
    }

    public override long Position
    {
        get => Verify(expected.Position, actual.Position);
        set
        {
            expected.Position = value;
            actual.Position = value;
        }
    }

    public override int ReadCore(Span<byte> buffer)
    {
        var actualReadCount = actual.Read(buffer);
        var actualHash = buffer.Truncate(actualReadCount).BitwiseHashCode();
        var expectedReadCount = expected.Read(buffer);
        var expectedHash = buffer.Truncate(expectedReadCount).BitwiseHashCode();

        Verify(actualHash, expectedHash);
        return Verify(expectedReadCount, actualReadCount);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Verify(expected.Seek(offset, origin), actual.Seek(offset, origin));
    }
}
