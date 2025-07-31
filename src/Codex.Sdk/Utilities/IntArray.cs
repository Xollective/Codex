using System.Collections;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Codex.ObjectModel;
using Codex.Utilities.Serialization;

namespace Codex.Utilities;

public abstract class IntArray : IJsonConvertible<IntArray, IntArray.JsonIntArray>, IReadOnlyList<int>, IIntArray
{
    public static readonly IntArray Empty = From(Array.Empty<int>());

    public abstract int Count { get; }

    protected abstract int ElementSize { get; }

    public abstract int this[int index] { get; set; }

    protected abstract Span<byte> GetBytes();

    [DataContract]
    private record JsonIntArray(
        [property: DataMember] int Count,
        [property: DataMember] int ElementSize,
        [property: DataMember] byte[] Data);

    static IntArray IJsonConvertible<IntArray, JsonIntArray>.ConvertFromJson(JsonIntArray jsonFormat)
    {
        var max = (1 << (jsonFormat.ElementSize * 8)) - 1;
        var array = New(max, count: jsonFormat.Count);

        jsonFormat.Data.CopyTo(array.GetBytes());

        return array;
    }

    JsonIntArray IJsonConvertible<IntArray, JsonIntArray>.ConvertToJson()
    {
        return new JsonIntArray(Count, ElementSize, GetBytes().ToArray());
    }

    protected void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        var bytes = GetBytes();
        writer.WriteBase64String($"{ElementSize}x{Count}", bytes);

        writer.WriteEndObject();
    }

    public static IntArray From(IEnumerable<int> values)
    {
        Span<uint> span = stackalloc[] { uint.MaxValue };

        int max = 0;
        int count = 0;
        foreach (var value in values)
        {
            count++;
            max = Math.Max(value, max);
            Contract.Assert(value >= 0);
        }

        IntArray array = New(max, count);

        int index = 0;
        foreach (var value in values)
        {
            array[index++] = value;
        }

        return array;
    }

    public static int GetElementSize(int max)
    {
        return 4 - (BitOperations.LeadingZeroCount((uint)max) / 8);
    }

    public static IntArray New(int max, int count)
    {
        var elementSize = GetElementSize(max);

        IntArray array = elementSize switch
        {
            4 => new IntArray<int>(count),
            3 => new IntArray<UInt24>(count),
            2 => new IntArray<ushort>(count),
            1 => new IntArray<byte>(count),
            _ => new ZeroIntArray(count)
        };

        return array;
    }

    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class ZeroIntArray : IntArray
    {
        public ZeroIntArray(int count)
        {
            Count = count;
        }

        public override int this[int index]
        {
            get
            {
                CheckRange(index);
                return 0;
            }
            set
            {
                CheckRange(index);
                Contract.Check(value == 0)?.Assert($"{value} must be zero");
            }
        }

        private void CheckRange(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new IndexOutOfRangeException($"{index} is not in range [0, {Count})");
            }
        }

        public override int Count { get; }

        protected override int ElementSize => 0;

        protected override Span<byte> GetBytes()
        {
            return Array.Empty<byte>();
        }
    }
}

public class IntArray<TInt> : IntArray
    where TInt : unmanaged
{
    private TInt[] _array;

    public override int Count => _array.Length;

    protected override int ElementSize => MemoryMarshal.AsBytes(stackalloc TInt[1]).Length;

    public IntArray(int count)
    {
        _array = new TInt[count];
    }

    public override int this[int index]
    {
        get
        {
            var value = _array[index];
            ReadOnlySpan<int> span = MemoryMarshal.Cast<TInt, int>(stackalloc[] { value, default, default, default });
            return span[0];
        }
        set
        {
            ReadOnlySpan<TInt> span = MemoryMarshal.Cast<int, TInt>(stackalloc[] { value });
            _array[index] = span[0];
        }
    }

    protected override Span<byte> GetBytes()
    {
        return MemoryMarshal.AsBytes(_array.AsSpan());
    }
}