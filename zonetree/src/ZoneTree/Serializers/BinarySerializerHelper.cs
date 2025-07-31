using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tenray.ZoneTree.Serializers;

public static class BinarySerializerHelper
{
    public static unsafe byte[] ToByteArray<T>(in T value) where T : unmanaged
    {
        byte[] result = new byte[sizeof(T)];
        Unsafe.As<byte, T>(ref result[0]) = value;
        return result;
    }

    public static T FromByteArray<T>(ReadOnlyMemory<byte> data) where T : unmanaged
        => FromByteSpan<T>(data.Span);

    public static T FromByteArray<T>(ReadOnlyMemory<byte> data, int off) where T : unmanaged
        => FromByteSpan<T>(data.Span, off);

    public static T FromByteSpan<T>(ReadOnlySpan<byte> data) where T : unmanaged
    => Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(data));

    public static T FromByteSpan<T>(ReadOnlySpan<byte> data, int off) where T : unmanaged
        => Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(data.Slice(off)));

    public static void Write(this BinaryWriter writer, ReadOnlyMemory<byte> data)
    {
        writer.Write(data.Span);
    }

    public static T[] ToArrayUnsafe<T>(this ReadOnlyMemory<T> memory)
    {
        if (MemoryMarshal.TryGetArray(memory, out var segment) && segment.Offset == 0 && segment.Count == memory.Length)
        {
            return segment.Array;
        }
        else
        {
            return segment.ToArray();
        }
    }
}
