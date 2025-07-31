using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codex.Utilities.Serialization;

public interface IBinaryFormattable<TSelf>
{
    static abstract TSelf FromBytes(ReadOnlySequence<byte> bytes);

    ReadOnlySpan<byte> GetBytes();
}