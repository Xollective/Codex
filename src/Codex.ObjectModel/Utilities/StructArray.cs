// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codex.Utilities.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Codex.Utilities;

using static SpanSerializationExtensions;

/// <summary>
/// Abstract representation of lightweight array.
/// </summary>
[GeneratorExclude]
public interface IReadOnlyArraySlim<out T>
{
    /// <summary>
    /// The length of the array.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    T this[int index] { get; }

    /// <summary>
    /// For performance reasons of copy operation, slim arrays should expose underlying array.
    /// This is used only for <see cref="StructArraySlimWrapper{T}"/> in <see cref="ReadOnlyArrayExtensions.CopyTo{TArray,T}"/> method
    /// and should not be used by any other clients.
    /// </summary>
    /// <remarks>
    /// There are other potential solutions for the perf problem, but none of them are perfect.
    /// For instance, adding CopyTo method to this interface will require generic argument T to be invariant (currently it is contravariant).
    /// This is possible, but it will require other changes in the entire codebase.
    /// So current solution with exposing underlying array is not perfect, but not worst.
    /// </remarks>
    [AllowNull]
    [SuppressMessage("Microsoft.Performance", "CA1819")]
    T[] UnderlyingArrayUnsafe => null;

    protected T GetItem(int index)
    {
        return this[index];
    }
}

public interface IMemory<T>
{
    ref T this[int index] { get; }

    Span<T> Span { get; }

    Memory<T> Memory { get; }

    int Length { get; }
}

[GeneratorExclude]
public interface IArray<T> : IReadOnlyArraySlimList<T>
{
    new ref T this[int index] { get; }

    T IReadOnlyArraySlim<T>.this[int index] => this[index];
}

[GeneratorExclude]
public interface IStructArray<T, TSelf> : IReadOnlyArraySlimList<T>
    where TSelf : struct, IStructArray<T, TSelf>
{
    TSelf Self { get; }

    abstract static ref T ItemRef(in TSelf self, int index);

    T IReadOnlyArraySlim<T>.this[int index] => TSelf.ItemRef(Self, index);

    public virtual static Span<T> GetSpan(in TSelf t = default)
    {
        return Unsafe.AsRef(t).GetItemSpan<TSelf, T>();
    }
}

[GeneratorExclude]
public interface IArray<T, TSelf> : IArray<T>, IReadOnlyArraySlimList<T, TSelf>
{
}

[GeneratorExclude]
public interface IReadOnlyArraySlimList<out T> : IReadOnlyArraySlimCollection<T>, IReadOnlyList<T>
{
    T IReadOnlyList<T>.this[int index] => GetItem(index);
}

[GeneratorExclude]
public interface IReadOnlyArraySlimList<out T, TSelf> : IReadOnlyArraySlimList<T>
{
}

[GeneratorExclude]
public interface IReadOnlyArraySlimCollection<out T> : IReadOnlyArraySlim<T>, IReadOnlyCollection<T>
{
    int IReadOnlyCollection<T>.Count => Length;

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        for (int i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <nodoc/>
public static class ReadOnlyArrayExtensions
{
    /// <nodoc/>
    public static void CopyTo<TArray, T>(this TArray array, int sourceIndex, T[] destination, int destinationIndex, int length)
        where TArray : IReadOnlyArraySlim<T>
    {
        // Using more efficient implementation if array is a wrapper around real array.
        var underlyingArray = array.UnderlyingArrayUnsafe;
        if (underlyingArray != null)
        {
            Array.Copy(underlyingArray, sourceIndex, destination, destinationIndex, length);
        }

        // Otherwise (for small arrays) using regular for-based copy.
        for (int i = 0; i < length; i++)
        {
            destination[i + destinationIndex] = array[i + sourceIndex];
        }
    }

    /// <summary>
    /// Method that throw an <see cref="IndexOutOfRangeException"/>.
    /// </summary>
    /// <remarks>
    /// This method is not generic which means that the underlying array would be boxed.
    /// This will lead to additional memory allocation in a failure case, but will
    /// lead to more readable code, because C# langauge doesn't have partial generic arguments
    /// inferece.
    /// </remarks>
    [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
    public static Exception ThrowOutOfRange<T>(this IReadOnlyArraySlim<T> array, int index)
    {
        string message = string.Format(
            CultureInfo.InvariantCulture,
            "Index '{0}' was outside the bounds of the array with length '{1}'", index, array.Length);

        throw new IndexOutOfRangeException(message);
    }

    /// <nodoc/>
    [System.Diagnostics.Contracts.Pure]
    public static IReadOnlyList<T> ToReadOnlyList<TArray, T>(this TArray array)
        where TArray : IReadOnlyArraySlim<T>
    {
        return new ReadOnlyArrayList<T, TArray>(array);
    }

    /// <nodoc/>
    [System.Diagnostics.Contracts.Pure]
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IReadOnlyArraySlim<T> array)
    {
        return new ReadOnlyArrayList<T, IReadOnlyArraySlim<T>>(array);
    }

    private sealed class ReadOnlyArrayList<T, TArray> : IReadOnlyList<T>
        where TArray : IReadOnlyArraySlim<T>
    {
        // Intentionally leaving this field as non-readonly to avoid defensive copy on each access.
        private readonly TArray m_array;

        public ReadOnlyArrayList(TArray array)
        {
            m_array = array;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < m_array.Length; i++)
            {
                yield return m_array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => m_array.Length;

        public T this[int index]
        {
            get { return m_array[index]; }
        }
    }
}

/// <summary>
/// Factory class that responsible for creating different struct arrays by different input.
/// </summary>
public static class StructArray
{
    /// <nodoc />
    public static StructArray0<T> Create<T>()
    {
        return default(StructArray0<T>);
    }

    /// <nodoc />
    public static StructArray1<T> Create<T>(T item)
    {
        return new StructArray1<T>(item);
    }

    /// <nodoc />
    public static StructArray2<T> Create<T>(T item1, T item2)
    {
        return new StructArray2<T>(item1, item2);
    }

    /// <nodoc />
    public static StructArray3<T> Create<T>(T item1, T item2, T item3)
    {
        return new StructArray3<T>(item1, item2, item3);
    }

    /// <nodoc />
    public static StructArray4<T> Create<T>(T item1, T item2, T item3, T item4)
    {
        return new StructArray4<T>(item1, item2, item3, item4);
    }

    /// <nodoc />
    public static StructArray5<T> Create<T>(T item1, T item2, T item3, T item4, T item5)
    {
        return new StructArray5<T>(item1, item2, item3, item4, item5);
    }

    /// <nodoc />
    public static StructArraySlimWrapper<T> Create<T>(T[] items)
    {
        return new StructArraySlimWrapper<T>(items);
    }

    /// <nodoc />
    public static MemoryArraySlimWrapper<T> Create<T>(Memory<T> items)
    {
        return new MemoryArraySlimWrapper<T>(items);
    }

    public static Span<T> GetValuesSpan<T, TValues>(this ref ValueArray<T, TValues> list)
        where TValues : struct
        where T : struct
    {
        return MemoryMarshal.Cast<TValues, T>(MemoryMarshal.CreateSpan(ref list.Values, 1)).Slice(0, list.Length);
    }

    public static ReadOnlySpan<T> AsReadOnlySpanUnsafe<T, TValues>(this in ValueArray<T, TValues> list)
        where TValues : struct
        where T : struct
    {
        return MemoryMarshal.Cast<TValues, T>(SpanSerializationExtensions.AsReadOnlySpanUnsafe(list.Values)).Slice(0, list.Length);
    }

    public static Span<T> GetItemSpan<TArray, T>(this ref TArray list)
        where TArray : struct, IStructArray<T, TArray>
    {
        return MemoryMarshal.CreateSpan(ref TArray.ItemRef(list, 0), list.Length);
    }
}

public record struct T5<T>((T, T, T, T, T) Values);

public record struct T4<T> : IStructArray<T, T4<T>>
{
    public T Item0;
    public T Item1;
    public T Item2;
    public T Item3;

    public int Length => 4;

    public T4<T> Self => this;

    public static ref T ItemRef(in T4<T> self, int index)
    {
        ref var me = ref Unsafe.AsRef(self);
        switch (index)
        {
            case 0:
                return ref me.Item0;
            case 1:
                return ref me.Item1;
            case 2:
                return ref me.Item2;
            case 3:
                return ref me.Item3;
            default:
                throw self.ThrowOutOfRange(index);
        }
    }
}

public record struct T2<T> : IStructArray<T, T2<T>>
{
    public T Item0;
    public T Item1;

    public int Length => 2;

    public T2<T> Self => this;

    public static ref T ItemRef(in T2<T> self, int index)
    {
        ref var me = ref Unsafe.AsRef(self);
        switch (index)
        {
            case 0:
                return ref me.Item0;
            case 1:
                return ref me.Item1;
            default:
                throw self.ThrowOutOfRange(index);
        }
    }
}



[StructLayout(LayoutKind.Explicit, Size = 512)]
public struct T512 { }

[StructLayout(LayoutKind.Explicit, Size = 256)]
public struct T256 { }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct T64<T> : IStructArray<T, T64<T>>
{
    public T4<T4<T4<T>>> Array;

    public T64<T> Self => this;

    public int Length => 64;

    public static ref T ItemRef(in T64<T> self, int index)
    {
        ref var me = ref Unsafe.AsRef(self);
        if (index == 0)
        {
            return ref me.Array.Item0.Item0.Item0;
        }

        throw self.ThrowOutOfRange(index);
    }
}


public readonly record struct ValueArrayLength(int Length)
{
    public static readonly ValueArrayLength MaxCapacity = new(-1);
    public static readonly ValueArrayLength Empty = new(0);
}

public record struct ValueArray<T, TValues> :
    IReadOnlyArraySlimList<T, ValueArray<T, TValues>>,
    IBinarySpanItem<ValueArray<T, TValues>>
    where TValues : struct
    where T : struct
{
    public static readonly int MaxLength = Unsafe.SizeOf<TValues>() / Unsafe.SizeOf<T>();

    public int MaxCount => MaxLength;

    public TValues Values;
    public int Length { get; set; }

    public T[] UnderlyingArrayUnsafe => null;

    public ValueArray<T, TValues> Self => this;

    private T[] AsArray => this.ToArray();

    public ValueArray(int length)
        : this(default, length)
    {
    }

    public ValueArray(TValues values, int length = -1) : this()
    {
        Values = values;
        Length = length >= 0 ? length : MaxLength;
    }

    public T this[int index]
    {
        get { return this.GetValuesSpan()[index]; }
        set { this.GetValuesSpan()[index] = value; }
    }

    public bool TryAdd(T value)
    {
        var index = Length;
        if (index < MaxLength)
        {
            Length++;
            this[index] = value;
            return true;
        }
        else
        {
            return false;
        }
    }

    public override string ToString()
    {
        return this.GetValuesSpan().ToArray().ToString();
    }

    int IBinaryItem.Length => GetSpan(this).Length;

    public static ReadOnlySpan<byte> GetSpan(in ValueArray<T, TValues> array)
    {
        return MemoryMarshal.AsBytes(array.AsReadOnlySpanUnsafe());
    }

    public static ValueArray<T, TValues> FromSpan(ReadOnlySpan<byte> bytes)
    {
        var source = MemoryMarshal.Cast<byte, T>(bytes);
        var result = new ValueArray<T, TValues>(source.Length);
        source.CopyTo(result.GetValuesSpan());
        return result;
    }

    public static implicit operator ValueArray<T, TValues>(ValueArrayLength length)
    {
        return new(length.Length);
    }
}

/// <summary>
/// Lightweight empty struct array.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StructArray0<T> : IReadOnlyArraySlimList<T, StructArray0<T>>
{
    public T this[int index]
    {
        get { throw this.ThrowOutOfRange(index); }
    }

    public int Length => 0;

    public T[] UnderlyingArrayUnsafe => null;

    public StructArray0<T> Self => this;
}

/// <summary>
/// Lightweight struct array that holds one element.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StructArray1<T> : IReadOnlyArraySlimList<T, StructArray1<T>>
{
    private readonly T m_item;

    public StructArray1(T item)
    {
        m_item = item;
    }

    public T this[int index]
    {
        get
        {
            if (index != 0)
            {
                throw this.ThrowOutOfRange(index);
            }

            return m_item;
        }
    }

    public int Length => 1;

    public T[] UnderlyingArrayUnsafe => null;

    public StructArray1<T> Self => this;
}

/// <summary>
/// Lightweight struct array that holds 2 elements.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StructArray2<T> : IReadOnlyArraySlimList<T, StructArray2<T>>
{
    private readonly T m_item0;
    private readonly T m_item1;

    public StructArray2(T item0, T item1)
    {
        m_item0 = item0;
        m_item1 = item1;
    }

    public T this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return m_item0;
                case 1:
                    return m_item1;
                default:
                    throw this.ThrowOutOfRange(index);
            }
        }
    }

    public int Length => 2;

    public T[] UnderlyingArrayUnsafe => null;

    public StructArray2<T> Self => this;
}

/// <summary>
/// Lightweight struct array that holds 3 elements.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StructArray3<T> : IReadOnlyArraySlimList<T, StructArray3<T>>
{
    private readonly T m_item0;
    private readonly T m_item1;
    private readonly T m_item2;

    public StructArray3(T item0, T item1, T item2)
    {
        m_item0 = item0;
        m_item1 = item1;
        m_item2 = item2;
    }

    public T this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return m_item0;
                case 1:
                    return m_item1;
                case 2:
                    return m_item2;
                default:
                    throw this.ThrowOutOfRange(index);
            }
        }
    }

    public int Length => 3;

    public T[] UnderlyingArrayUnsafe => null;

    public StructArray3<T> Self => this;
}

/// <summary>
/// Lightweight struct array that holds 4 elements.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StructArray4<T> : IReadOnlyArraySlim<T>
{
    private readonly T m_item0;
    private readonly T m_item1;
    private readonly T m_item2;
    private readonly T m_item3;

    public StructArray4(T item0, T item1, T item2, T item3)
    {
        m_item0 = item0;
        m_item1 = item1;
        m_item2 = item2;
        m_item3 = item3;
    }

    public T this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return m_item0;
                case 1:
                    return m_item1;
                case 2:
                    return m_item2;
                case 3:
                    return m_item3;
                default:
                    throw this.ThrowOutOfRange(index);
            }
        }
    }

    public int Length => 4;

    public T[] UnderlyingArrayUnsafe => null;
}

/// <summary>
/// Lightweight struct array that holds 5 elements.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StructArray5<T> : IReadOnlyArraySlim<T>
{
    private readonly T m_item0;
    private readonly T m_item1;
    private readonly T m_item2;
    private readonly T m_item3;
    private readonly T m_item4;

    public StructArray5(T item0, T item1, T item2, T item3, T item4)
    {
        m_item0 = item0;
        m_item1 = item1;
        m_item2 = item2;
        m_item3 = item3;
        m_item4 = item4;
    }

    public T this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return m_item0;
                case 1:
                    return m_item1;
                case 2:
                    return m_item2;
                case 3:
                    return m_item3;
                case 4:
                    return m_item4;
                default:
                    throw this.ThrowOutOfRange(index);
            }
        }
    }

    public int Length => 5;

    public T[] UnderlyingArrayUnsafe => null;
}

/// <summary>
/// Lightweight struct array that holds one element.
/// </summary>
public readonly struct StructArraySlimWrapper<T> : IReadOnlyArraySlim<T>, IArray<T>, IStructArray<T, StructArraySlimWrapper<T>>, IMemory<T>
{
    private readonly T[] m_items;

    /// <nodoc/>
    public StructArraySlimWrapper(T[] items)
    {
        m_items = items;
    }

    /// <inheritdoc/>
    public ref T this[int index]
    {
        get { return ref m_items[index]; }
    }

    T IReadOnlyArraySlim<T>.this[int index] => this[index];

    /// <inheritdoc/>
    public int Length => m_items.Length;

    /// <inheritdoc/>
    public T[] UnderlyingArrayUnsafe => m_items;

    public StructArraySlimWrapper<T> Self => this;

    public Span<T> Span => m_items;

    public Memory<T> Memory => m_items;

    public static ref T ItemRef(in StructArraySlimWrapper<T> self, int index)
    {
        return ref self[index];
    }
}

public readonly struct MemoryArraySlimWrapper<T>(Memory<T> items) : IReadOnlyArraySlim<T>, IArray<T>, IStructArray<T, MemoryArraySlimWrapper<T>>, IMemory<T>
{
    private readonly Span<T> m_items => items.Span;

    /// <inheritdoc/>
    public ref T this[int index]
    {
        get { return ref m_items[index]; }
    }

    T IReadOnlyArraySlim<T>.this[int index] => this[index];

    /// <inheritdoc/>
    public int Length => m_items.Length;

    public MemoryArraySlimWrapper<T> Self => this;

    public Span<T> Span => m_items;

    public Memory<T> Memory => items;

    public static ref T ItemRef(in MemoryArraySlimWrapper<T> self, int index)
    {
        return ref self[index];
    }
}
