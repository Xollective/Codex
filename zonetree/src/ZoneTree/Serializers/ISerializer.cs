using System.Runtime.CompilerServices;
using Tenray.ZoneTree.Comparers;

namespace Tenray.ZoneTree.Serializers;

/// <summary>
/// Generic Serializer interface for any type.
/// </summary>
/// <typeparam name="TEntry"></typeparam>
public interface ISerializer<TEntry>
{
    bool IsFixedSize => !RuntimeHelpers.IsReferenceOrContainsReferences<TEntry>();

    int FixedSize => IsFixedSize ? Unsafe.SizeOf<TEntry>() : -1;

    /// <summary>
    /// Deserialize the bytes into entry type.
    /// </summary>
    /// <param name="bytes">The bytes to be deserialized.</param>
    /// <returns>The deserialized entry.</returns>
    TEntry Deserialize(byte[] bytes);

    /// <summary>
    /// Serialize the entry into byte array.
    /// </summary>
    /// <param name="entry">The entry</param>
    /// <returns>The serialized bytes.</returns>
    byte[] Serialize(in TEntry entry);

    /// <summary>
    /// Deserialize the bytes into entry type.
    /// </summary>
    /// <param name="bytes">The bytes to be deserialized.</param>
    /// <returns>The deserialized entry.</returns>
    TEntry SpanDeserialize(ReadOnlyMemory<byte> bytes) => Deserialize(bytes.ToArrayUnsafe());

    /// <summary>
    /// Serialize the entry into bytes.
    /// </summary>
    /// <param name="entry">The entry</param>
    /// <returns>The serialized bytes.</returns>
    ReadOnlySpan<byte> SpanSerialize(in TEntry entry) => Serialize(entry);
}

/// <summary>
/// A <see cref="ISerializer{TEntry}"/> using <see cref="ReadOnlySpan{byte}"/> and
/// <see cref="ReadOnlyMemory{byte}"/> by default for (de)serialization.
/// Overrides all base members to use these implementations.
/// </summary>
/// <typeparam name="TEntry">the entry type</typeparam>
public interface ISpanSerializer<TEntry> : ISerializer<TEntry>, IRefComparer<TEntry>
{
    /// <summary>
    /// Serialize the entry into bytes.
    /// </summary>
    /// <param name="entry">The entry</param>
    /// <returns>The serialized bytes.</returns>
    new ReadOnlySpan<byte> Serialize(in TEntry entry);

    /// <summary>
    /// Deserialize the bytes into entry type.
    /// </summary>
    /// <param name="bytes">The bytes to be deserialized.</param>
    /// <returns>The deserialized entry.</returns>
    TEntry Deserialize(ReadOnlySpan<byte> bytes);

    int IRefComparer<TEntry>.Compare(in TEntry x, in TEntry y)
    {
        var s1 = Serialize(x);
        var s2 = Serialize(y);

        var result = s1.SequenceCompareTo(s2);
        return result;
    }

    TEntry ISerializer<TEntry>.Deserialize(byte[] bytes) => Deserialize(bytes);

    byte[] ISerializer<TEntry>.Serialize(in TEntry entry) => Serialize(entry).ToArray();

    TEntry ISerializer<TEntry>.SpanDeserialize(ReadOnlyMemory<byte> bytes) => Deserialize(bytes.Span);

    ReadOnlySpan<byte> ISerializer<TEntry>.SpanSerialize(in TEntry entry) => Serialize(entry);

}