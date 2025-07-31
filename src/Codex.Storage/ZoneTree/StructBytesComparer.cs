using System.Runtime.CompilerServices;
using Tenray.ZoneTree.Comparers;

namespace Codex.Storage;

public class StructBytesComparer<T> : IRefComparer<T>
    where T : unmanaged
{
    public unsafe int Compare(in T x, in T y)
    {
        Span<byte> sw1 = stackalloc byte[sizeof(T)];
        Unsafe.As<byte, T>(ref sw1[0]) = x;
        Span<byte> sw2 = stackalloc byte[sizeof(T)];
        Unsafe.As<byte, T>(ref sw2[0]) = y;

        var result = sw1.SequenceCompareTo(sw2);
        return result;
    }
}
