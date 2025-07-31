using MessagePack.Formatters;
using Codex.ObjectModel;
using MessagePack;
using Codex.Sdk.Utilities;
using Codex.Sdk.Search;
using System.Buffers;
using System.Text.Json;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Codex.Storage;

namespace Codex.Utilities.Serialization;
public static partial class MessagePacker
{
    private static class PredefinedFormatter<T>
    {
        public static IMessagePackFormatter<T> Formatter;

        public static IJsonFormatter<T> JsonFormatter;
    }

    static MessagePacker()
    {
        Add(FuncFormatter.Create<CharString, None>(ReadCharString, WriteCharString, None.Value,
            FuncFormatter.CreateJson<CharString, None>(ReadCharString, WriteCharString, None.Value)));
        Add(CreateConversionFormatter<ClassifiedExtent, int>(s => s.AsIntegral(), ClassifiedExtent.FromIntegral));
        Add(CreateConversionFormatter<SymbolId, string>(s => s.Value, SymbolId.UnsafeCreateWithValue));
        Add(CreateConversionFormatter<SymbolIdArgument, string>(s => s.Value.Value, s => SymbolId.UnsafeCreateWithValue(s)));
        Add<PropertyMap>(new GenericDictionaryFormatter<StringEnum<PropertyKey>, string, PropertyMap>());
        Add(CreateConversionJsonFormatter<Extent, string>(r => r.Serialize(), s => Extent.Parse(s)));
        Add(FuncFormatter.CreateJson<ReadOnlyMemory<byte>, None>(Read, Write, None.Value));
        Add(CreateConversionJsonFormatter<MurmurHash, string>(s => s.ToBase64String(), s => MurmurHash.Parse(s)));
    }

    private static void WriteCharString(Utf8JsonWriter writer, CharString value, bool asPropertyName, JsonSerializerOptions options, None data)
    {
        if (value.Length == 0) writer.WriteStringValue("");
        else writer.WriteStringValue(value.Chars.Span);
    }

    private static CharString ReadCharString(ref Utf8JsonReader reader, JsonSerializerOptions options, None data)
    {
        return reader.GetString() is string s ? new(s.AsMemory()) : new();
    }

    private static void WriteCharString(ref MessagePackWriter writer, CharString value, MessagePackSerializerOptions options, None data)
    {
        if (value.Length == 0) writer.Write((string?)null);
        else writer.Write(value.Chars.Span);
    }

    private static CharString ReadCharString(ref MessagePackReader reader, MessagePackSerializerOptions options, None data)
    {
        return reader.ReadString() is string s ? new(s.AsMemory()) : new();
    }

    private static void Write(ref MessagePackWriter writer, ReadOnlySpan<byte> span)
    {
        writer.Write(span);
    }

    private static void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> value, bool asPropertyName, JsonSerializerOptions options, None data)
    {
        writer.WriteBase64StringValue(value.Span);
    }

    private static ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, JsonSerializerOptions options, None data)
    {
        return reader.GetBytesFromBase64();
    }

    public static void Add<T>(IMessagePackFormatter<T> formatter)
    {
        PredefinedFormatter<T>.Formatter = formatter;
    }

    public static void Add<T>(IJsonFormatter<T> formatter)
    {
        PredefinedFormatter<T>.JsonFormatter = formatter;
    }
}