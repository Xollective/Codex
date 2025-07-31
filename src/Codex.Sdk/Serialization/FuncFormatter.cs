using System.Text.Json;
using System.Text.Json.Serialization;
using Codex.Utilities.Serialization;
using MessagePack;
using MessagePack.Formatters;

namespace Codex.Utilities;

/// <summary>
/// Creates json converters from delegates
/// </summary>
public static class FuncFormatter
{
    /// <summary>
    /// Creates a message pack converter from the given delegates
    /// </summary>
    public static IMessagePackFormatter<T> Create<T, TData>(
        MessagePackRead<T, TData> read,
        MessagePackWrite<T, TData> write,
        TData data,
        IJsonFormatter<T> jsonFormatter = null)
    {
        return new Formatter<T, TData>(read, write, data, jsonFormatter);
    }

    /// <summary>
    /// Creates a json formatter from the given delegates
    /// </summary>
    public static JsonFuncFormatter<T, TData> CreateJson<T, TData>(
        JsonRead<T, TData> Read, 
        JsonWrite<T, TData> Write, 
        TData data, 
        Func<TData, bool> IsApplicable = null)
    {
        return new JsonFuncFormatter<T, TData>(Read, Write, data, IsApplicable);
    }

    /// <nodoc />
    public delegate T MessagePackRead<T, TData>(ref MessagePackReader reader, MessagePackSerializerOptions options, TData data);
    public delegate void MessagePackWrite<T, TData>(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, TData data);

    public delegate T JsonRead<T, TData>(ref Utf8JsonReader reader, JsonSerializerOptions options, TData data);
    public delegate void JsonWrite<T, TData>(Utf8JsonWriter writer, T value, bool asPropertyName, JsonSerializerOptions options, TData data);

    public class JsonFuncFormatter<T, TData> : JsonConverter<T>, IJsonFormatter<T>
    {
        public JsonFuncFormatter(JsonRead<T, TData> read, JsonWrite<T, TData> write, TData data, Func<TData, bool> isApplicable = null)
        {
            JsonRead = read;
            JsonWrite = write;
            Data = data;
            IsApplicable = isApplicable;
        }

        public JsonRead<T, TData> JsonRead { get; }
        public JsonWrite<T, TData> JsonWrite { get; }
        public TData Data { get; }
        public Func<TData, bool> IsApplicable { get; }

        public JsonConverter<T> AsConverter()
        {
            return this;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonRead(ref reader, options, Data);
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = JsonRead(ref reader, options, Data);
            var tokenType = reader.TokenType;
            return result;
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonWrite(writer, value, asPropertyName: true, options, Data);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonWrite(writer, value, asPropertyName: false, options, Data);
        }

        bool IJsonFormatter<T>.IsApplicable() => IsApplicable?.Invoke(Data) ?? true;

        public T Read(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return JsonRead(ref reader, options, Data);
        }
    }

    private record Formatter<T, TData>(MessagePackRead<T, TData> Read, MessagePackWrite<T, TData> Write, TData data, IJsonFormatter<T> JsonFormatter = null) 
        : IMessagePackFormatter<T>, IJsonFormatterProvider<T>
    {
        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);
            try
            {
                return Read(ref reader, options, data);
            }
            finally
            {
                reader.Depth--;
            }
        }

        public IJsonFormatter<T> GetJsonFormatter()
        {
            return JsonFormatter;
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            Write(ref writer, value, options, data);
        }
    }
}
