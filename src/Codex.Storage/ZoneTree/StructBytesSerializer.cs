using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codex.Utilities.Serialization;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Serializers;

namespace Codex.Storage;

public class StructBytesSerializer<T> : IRefComparer<T>, ISpanSerializer<T>
    where T : unmanaged
{
    public unsafe int Compare(in T x, in T y)
    {
        var s1 = Serialize(x);
        var s2 = Serialize(y);

        var result = s1.SequenceCompareTo(s2);
        return result;
    }

    public T Deserialize(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<T>(bytes);
    }

    public ReadOnlySpan<byte> Serialize(in T entry)
    {
        return SpanSerializationExtensions.AsBytesUnsafe(entry);
    }
}
